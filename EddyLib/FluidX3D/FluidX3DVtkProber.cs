using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib.FluidX3D
{
    public enum FluidX3DVtkProbeQuantity
    {
        VelocityU = 0,
        DensityRho = 1
    }

    public enum FluidX3DVtkProbeTimeMode
    {
        Latest = 0,
        ClosestPhysicalTime = 1,
        AverageOverRange = 2
    }

    public struct FluidX3DPoint3
    {
        public double X;
        public double Y;
        public double Z;

        public FluidX3DPoint3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static FluidX3DPoint3 operator +(FluidX3DPoint3 a, FluidX3DPoint3 b)
        {
            return new FluidX3DPoint3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static FluidX3DPoint3 operator /(FluidX3DPoint3 a, double divisor)
        {
            return new FluidX3DPoint3(a.X / divisor, a.Y / divisor, a.Z / divisor);
        }
    }

    public sealed class FluidX3DVtkProbeRequest
    {
        public string ExportDirectory { get; set; }
        public List<FluidX3DPoint3> RhinoPoints { get; } = new List<FluidX3DPoint3>();
        public FluidX3DVtkProbeQuantity Quantity { get; set; } = FluidX3DVtkProbeQuantity.VelocityU;
        public FluidX3DVtkProbeTimeMode TimeMode { get; set; } = FluidX3DVtkProbeTimeMode.Latest;
        public double TargetTimeSeconds { get; set; } = 0.0;
        public double StartTimeSeconds { get; set; } = 0.0;
        public double EndTimeSeconds { get; set; } = 0.0;
    }

    public sealed class FluidX3DVtkProbeResult
    {
        public FluidX3DVtkProbeQuantity Quantity { get; set; }
        public List<double> SampledTimesSeconds { get; } = new List<double>();
        public List<long> SampledSteps { get; } = new List<long>();
        public List<string> SampledFiles { get; } = new List<string>();
        public List<FluidX3DPoint3[]> VectorValuesByTime { get; } = new List<FluidX3DPoint3[]>();
        public List<double[]> ScalarValuesByTime { get; } = new List<double[]>();
        public FluidX3DPoint3[] AverageVectors { get; set; }
        public double[] AverageScalars { get; set; }
        public int OutsideDomainPointCount { get; set; }
        public string Status { get; set; }
    }

    public static class FluidX3DVtkProber
    {
        private sealed class FieldFile
        {
            public string Path;
            public long Step;
            public double PhysicalTimeSeconds;
        }

        private sealed class ProbeTransform
        {
            public double XMin;
            public double YMin;
            public double GroundZ;
            public double ExportIntervalSeconds;
        }

        private sealed class VtkHeader
        {
            public int Nx;
            public int Ny;
            public int Nz;
            public double OriginX;
            public double OriginY;
            public double OriginZ;
            public double SpacingX;
            public double SpacingY;
            public double SpacingZ;
            public int Components;
            public long DataOffset;
        }

        private static readonly Regex StepRegex = new Regex(@"^(?:u|rho)-(?<step>\d+)\.vtk$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ExportIntervalRegex = new Regex(@"Export\s+interval\s*:\s*(?<seconds>[0-9]+(?:\.[0-9]+)?)\s*s", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static FluidX3DVtkProbeResult Probe(FluidX3DVtkProbeRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ExportDirectory))
            {
                throw new ArgumentException("Export directory is required.", nameof(request.ExportDirectory));
            }

            string exportDirectory = Path.GetFullPath(request.ExportDirectory.Trim());
            if (!Directory.Exists(exportDirectory))
            {
                throw new DirectoryNotFoundException("Export directory not found: " + exportDirectory);
            }

            if (request.RhinoPoints == null || request.RhinoPoints.Count == 0)
            {
                throw new ArgumentException("At least one probe point is required.", nameof(request.RhinoPoints));
            }

            List<FieldFile> availableFiles = DiscoverFieldFiles(exportDirectory, request.Quantity);
            if (availableFiles.Count == 0)
            {
                throw new FileNotFoundException("No matching VTK files found in export directory for selected field.");
            }

            ProbeTransform transform = ReadProbeTransform(exportDirectory);
            double exportIntervalSeconds = transform.ExportIntervalSeconds > 0.0
                ? transform.ExportIntervalSeconds
                : TryReadExportIntervalSeconds(exportDirectory);
            long firstPositiveStep = availableFiles.Where(f => f.Step > 0L).Select(f => f.Step).DefaultIfEmpty(0L).Min();
            for (int i = 0; i < availableFiles.Count; i++)
            {
                availableFiles[i].PhysicalTimeSeconds = ComputePhysicalTimeSeconds(availableFiles[i].Step, firstPositiveStep, exportIntervalSeconds);
            }

            List<FieldFile> selectedFiles = SelectFiles(availableFiles, request);
            if (selectedFiles.Count == 0)
            {
                throw new InvalidOperationException("No VTK files selected for probing after applying time filter.");
            }

            VtkHeader firstHeader = ReadHeader(selectedFiles[0].Path);
            if (firstHeader.Components != ExpectedComponentCount(request.Quantity))
            {
                throw new InvalidDataException(
                    "Unexpected component count in VTK file " + selectedFiles[0].Path + ". Expected "
                    + ExpectedComponentCount(request.Quantity).ToString(CultureInfo.InvariantCulture)
                    + ", got " + firstHeader.Components.ToString(CultureInfo.InvariantCulture) + ".");
            }

            int pointCount = request.RhinoPoints.Count;
            int[] pointIndices = new int[pointCount];
            int outsideCount = 0;

            for (int i = 0; i < pointCount; i++)
            {
                FluidX3DPoint3 p = request.RhinoPoints[i];
                double localX = p.X - transform.XMin;
                double localY = p.Y - transform.YMin;
                double localZ = p.Z - transform.GroundZ;

                int ix = (int)Math.Round(localX / firstHeader.SpacingX, MidpointRounding.AwayFromZero);
                int iy = (int)Math.Round(localY / firstHeader.SpacingY, MidpointRounding.AwayFromZero);
                int iz = (int)Math.Round(localZ / firstHeader.SpacingZ, MidpointRounding.AwayFromZero);

                bool outside = ix < 0 || ix >= firstHeader.Nx
                    || iy < 0 || iy >= firstHeader.Ny
                    || iz < 0 || iz >= firstHeader.Nz;

                if (outside)
                {
                    outsideCount++;
                }

                ix = Clamp(ix, 0, firstHeader.Nx - 1);
                iy = Clamp(iy, 0, firstHeader.Ny - 1);
                iz = Clamp(iz, 0, firstHeader.Nz - 1);

                pointIndices[i] = ix + firstHeader.Nx * (iy + firstHeader.Ny * iz);
            }

            FluidX3DVtkProbeResult result = new FluidX3DVtkProbeResult
            {
                Quantity = request.Quantity,
                OutsideDomainPointCount = outsideCount
            };

            for (int f = 0; f < selectedFiles.Count; f++)
            {
                FieldFile selected = selectedFiles[f];
                VtkHeader header = ReadHeader(selected.Path);

                if (header.Nx != firstHeader.Nx || header.Ny != firstHeader.Ny || header.Nz != firstHeader.Nz)
                {
                    throw new InvalidDataException("VTK dimensions differ across sampled files.");
                }

                if (header.Components != firstHeader.Components)
                {
                    throw new InvalidDataException("VTK component count differs across sampled files.");
                }

                result.SampledFiles.Add(selected.Path);
                result.SampledSteps.Add(selected.Step);
                result.SampledTimesSeconds.Add(selected.PhysicalTimeSeconds);

                if (request.Quantity == FluidX3DVtkProbeQuantity.VelocityU)
                {
                    result.VectorValuesByTime.Add(ReadVectorSamples(selected.Path, header, pointIndices));
                }
                else
                {
                    result.ScalarValuesByTime.Add(ReadScalarSamples(selected.Path, header, pointIndices));
                }
            }

            if (request.Quantity == FluidX3DVtkProbeQuantity.VelocityU)
            {
                result.AverageVectors = AverageVectors(result.VectorValuesByTime, pointCount);
                result.Status = "Sampled " + result.VectorValuesByTime.Count.ToString(CultureInfo.InvariantCulture)
                    + " velocity timestep(s).";
            }
            else
            {
                result.AverageScalars = AverageScalars(result.ScalarValuesByTime, pointCount);
                result.Status = "Sampled " + result.ScalarValuesByTime.Count.ToString(CultureInfo.InvariantCulture)
                    + " density timestep(s).";
            }

            if (outsideCount > 0)
            {
                result.Status += " " + outsideCount.ToString(CultureInfo.InvariantCulture)
                    + " probe point(s) were outside domain and clamped to the nearest cell.";
            }

            return result;
        }

        private static List<FieldFile> DiscoverFieldFiles(string exportDirectory, FluidX3DVtkProbeQuantity quantity)
        {
            string prefix = quantity == FluidX3DVtkProbeQuantity.VelocityU ? "u-" : "rho-";
            List<FieldFile> files = new List<FieldFile>();

            string[] paths = Directory.GetFiles(exportDirectory, prefix + "*.vtk", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < paths.Length; i++)
            {
                string fileName = Path.GetFileName(paths[i]);
                Match m = StepRegex.Match(fileName);
                if (!m.Success)
                {
                    continue;
                }

                long step;
                if (!long.TryParse(m.Groups["step"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out step))
                {
                    continue;
                }

                files.Add(new FieldFile
                {
                    Path = paths[i],
                    Step = step
                });
            }

            files.Sort((a, b) => a.Step.CompareTo(b.Step));
            return files;
        }

        private static List<FieldFile> SelectFiles(List<FieldFile> availableFiles, FluidX3DVtkProbeRequest request)
        {
            if (request.TimeMode == FluidX3DVtkProbeTimeMode.Latest)
            {
                return new List<FieldFile> { availableFiles[availableFiles.Count - 1] };
            }

            if (request.TimeMode == FluidX3DVtkProbeTimeMode.ClosestPhysicalTime)
            {
                double target = request.TargetTimeSeconds;
                FieldFile best = availableFiles
                    .OrderBy(f => Math.Abs(f.PhysicalTimeSeconds - target))
                    .ThenBy(f => f.Step)
                    .First();
                return new List<FieldFile> { best };
            }

            double start = Math.Min(request.StartTimeSeconds, request.EndTimeSeconds);
            double end = Math.Max(request.StartTimeSeconds, request.EndTimeSeconds);
            List<FieldFile> inRange = availableFiles
                .Where(f => f.PhysicalTimeSeconds >= start && f.PhysicalTimeSeconds <= end)
                .ToList();

            if (inRange.Count > 0)
            {
                return inRange;
            }

            double center = 0.5 * (start + end);
            FieldFile fallback = availableFiles
                .OrderBy(f => Math.Abs(f.PhysicalTimeSeconds - center))
                .ThenBy(f => f.Step)
                .First();

            return new List<FieldFile> { fallback };
        }

        private static double TryReadExportIntervalSeconds(string exportDirectory)
        {
            DirectoryInfo exportDirInfo = new DirectoryInfo(exportDirectory);
            DirectoryInfo binDir = exportDirInfo.Parent;
            DirectoryInfo caseDir = binDir != null ? binDir.Parent : null;
            DirectoryInfo workingDir = caseDir != null ? caseDir.Parent : null;

            if (workingDir == null)
            {
                return 0.0;
            }

            string readmePath = Path.Combine(workingDir.FullName, "FluidX3D_Eddy_Readme.txt");
            if (!File.Exists(readmePath))
            {
                return 0.0;
            }

            // Bolt: Replaced File.ReadAllLines with File.ReadLines for lazy evaluation,
            // significantly reducing memory allocation and allowing an early return when a match is found.
            foreach (string line in File.ReadLines(readmePath))
            {
                Match m = ExportIntervalRegex.Match(line);
                if (!m.Success)
                {
                    continue;
                }

                double seconds;
                if (double.TryParse(m.Groups["seconds"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds)
                    && seconds > 0.0)
                {
                    return seconds;
                }
            }

            return 0.0;
        }

        private static double ComputePhysicalTimeSeconds(long step, long firstPositiveStep, double exportIntervalSeconds)
        {
            if (step <= 0L)
            {
                return 0.0;
            }

            if (firstPositiveStep > 0L && exportIntervalSeconds > 0.0)
            {
                return step * (exportIntervalSeconds / firstPositiveStep);
            }

            return step;
        }

        private static ProbeTransform ReadProbeTransform(string exportDirectory)
        {
            string path = Path.Combine(exportDirectory, "eddy_probe_transform.txt");
            if (!File.Exists(path))
            {
                return new ProbeTransform();
            }

            ProbeTransform transform = new ProbeTransform();

            // Bolt: Replaced File.ReadAllLines with File.ReadLines for lazy evaluation,
            // preventing the entire text file from being allocated in memory as an array.
            foreach (string rawLine in File.ReadLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int idx = line.IndexOf('=');
                if (idx <= 0 || idx >= line.Length - 1)
                {
                    continue;
                }

                string key = line.Substring(0, idx).Trim();
                string value = line.Substring(idx + 1).Trim();

                double parsed;
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
                {
                    continue;
                }

                if (key.Equals("x_min", StringComparison.OrdinalIgnoreCase))
                {
                    transform.XMin = parsed;
                }
                else if (key.Equals("y_min", StringComparison.OrdinalIgnoreCase))
                {
                    transform.YMin = parsed;
                }
                else if (key.Equals("ground_z", StringComparison.OrdinalIgnoreCase))
                {
                    transform.GroundZ = parsed;
                }
                else if (key.Equals("export_interval_seconds", StringComparison.OrdinalIgnoreCase))
                {
                    transform.ExportIntervalSeconds = parsed;
                }
            }

            return transform;
        }

        private static VtkHeader ReadHeader(string vtkPath)
        {
            using (FileStream fs = File.OpenRead(vtkPath))
            {
                VtkHeader header = new VtkHeader();
                bool scalarsSeen = false;
                bool vectorsSeen = false;

                while (true)
                {
                    string line = ReadAsciiLine(fs);
                    if (line == null)
                    {
                        throw new InvalidDataException("Unexpected end of VTK header in " + vtkPath);
                    }

                    string trimmed = line.Trim();
                    if (trimmed.Length == 0)
                    {
                        continue;
                    }

                    if (trimmed.StartsWith("DIMENSIONS", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] p = SplitTokens(trimmed);
                        header.Nx = int.Parse(p[1], CultureInfo.InvariantCulture);
                        header.Ny = int.Parse(p[2], CultureInfo.InvariantCulture);
                        header.Nz = int.Parse(p[3], CultureInfo.InvariantCulture);
                    }
                    else if (trimmed.StartsWith("ORIGIN", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] p = SplitTokens(trimmed);
                        header.OriginX = double.Parse(p[1], CultureInfo.InvariantCulture);
                        header.OriginY = double.Parse(p[2], CultureInfo.InvariantCulture);
                        header.OriginZ = double.Parse(p[3], CultureInfo.InvariantCulture);
                    }
                    else if (trimmed.StartsWith("SPACING", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] p = SplitTokens(trimmed);
                        header.SpacingX = double.Parse(p[1], CultureInfo.InvariantCulture);
                        header.SpacingY = double.Parse(p[2], CultureInfo.InvariantCulture);
                        header.SpacingZ = double.Parse(p[3], CultureInfo.InvariantCulture);
                    }
                    else if (trimmed.StartsWith("SCALARS", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] p = SplitTokens(trimmed);
                        header.Components = p.Length >= 4
                            ? int.Parse(p[3], CultureInfo.InvariantCulture)
                            : 1;
                        scalarsSeen = true;
                    }
                    else if (trimmed.StartsWith("VECTORS", StringComparison.OrdinalIgnoreCase))
                    {
                        header.Components = 3;
                        vectorsSeen = true;
                        header.DataOffset = fs.Position;
                        break;
                    }
                    else if (trimmed.StartsWith("LOOKUP_TABLE", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!scalarsSeen)
                        {
                            throw new InvalidDataException("LOOKUP_TABLE found before SCALARS in " + vtkPath);
                        }

                        header.DataOffset = fs.Position;
                        break;
                    }
                }

                if (header.Nx <= 0 || header.Ny <= 0 || header.Nz <= 0)
                {
                    throw new InvalidDataException("Invalid VTK dimensions in " + vtkPath);
                }

                if (header.SpacingX <= 0.0 || header.SpacingY <= 0.0 || header.SpacingZ <= 0.0)
                {
                    throw new InvalidDataException("Invalid VTK spacing in " + vtkPath);
                }

                if (header.Components <= 0)
                {
                    header.Components = vectorsSeen ? 3 : 1;
                }

                return header;
            }
        }

        private static FluidX3DPoint3[] ReadVectorSamples(string vtkPath, VtkHeader header, int[] pointIndices)
        {
            FluidX3DPoint3[] values = new FluidX3DPoint3[pointIndices.Length];
            byte[] buffer = new byte[header.Components * 4];

            using (FileStream fs = File.OpenRead(vtkPath))
            {
                for (int i = 0; i < pointIndices.Length; i++)
                {
                    long pointOffset = header.DataOffset + (long)pointIndices[i] * header.Components * 4L;
                    fs.Position = pointOffset;
                    fs.ReadExactly(buffer, 0, buffer.Length);

                    double x = ReadFloatBigEndian(buffer, 0);
                    double y = header.Components >= 2 ? ReadFloatBigEndian(buffer, 4) : 0.0;
                    double z = header.Components >= 3 ? ReadFloatBigEndian(buffer, 8) : 0.0;
                    values[i] = new FluidX3DPoint3(x, y, z);
                }
            }

            return values;
        }

        private static double[] ReadScalarSamples(string vtkPath, VtkHeader header, int[] pointIndices)
        {
            double[] values = new double[pointIndices.Length];
            byte[] buffer = new byte[header.Components * 4];

            using (FileStream fs = File.OpenRead(vtkPath))
            {
                for (int i = 0; i < pointIndices.Length; i++)
                {
                    long pointOffset = header.DataOffset + (long)pointIndices[i] * header.Components * 4L;
                    fs.Position = pointOffset;
                    fs.ReadExactly(buffer, 0, buffer.Length);

                    values[i] = ReadFloatBigEndian(buffer, 0);
                }
            }

            return values;
        }

        private static FluidX3DPoint3[] AverageVectors(List<FluidX3DPoint3[]> vectorsByTime, int pointCount)
        {
            FluidX3DPoint3[] average = new FluidX3DPoint3[pointCount];
            if (vectorsByTime.Count == 0)
            {
                return average;
            }

            for (int t = 0; t < vectorsByTime.Count; t++)
            {
                FluidX3DPoint3[] values = vectorsByTime[t];
                for (int i = 0; i < pointCount; i++)
                {
                    average[i] = average[i] + values[i];
                }
            }

            for (int i = 0; i < pointCount; i++)
            {
                average[i] = average[i] / vectorsByTime.Count;
            }

            return average;
        }

        private static double[] AverageScalars(List<double[]> scalarsByTime, int pointCount)
        {
            double[] average = new double[pointCount];
            if (scalarsByTime.Count == 0)
            {
                return average;
            }

            for (int t = 0; t < scalarsByTime.Count; t++)
            {
                double[] values = scalarsByTime[t];
                for (int i = 0; i < pointCount; i++)
                {
                    average[i] += values[i];
                }
            }

            for (int i = 0; i < pointCount; i++)
            {
                average[i] /= scalarsByTime.Count;
            }

            return average;
        }

        private static int ExpectedComponentCount(FluidX3DVtkProbeQuantity quantity)
        {
            return quantity == FluidX3DVtkProbeQuantity.VelocityU ? 3 : 1;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static string[] SplitTokens(string line)
        {
            return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static string ReadAsciiLine(FileStream fs)
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                int raw = fs.ReadByte();
                if (raw < 0)
                {
                    return sb.Length == 0 ? null : sb.ToString();
                }

                char ch = (char)raw;
                if (ch == '\n')
                {
                    return sb.ToString();
                }

                if (ch != '\r')
                {
                    sb.Append(ch);
                }
            }
        }

        private static float ReadFloatBigEndian(byte[] buffer, int offset)
        {
            int raw = BinaryPrimitives.ReadInt32BigEndian(new ReadOnlySpan<byte>(buffer, offset, 4));
            return BitConverter.Int32BitsToSingle(raw);
        }
    }
}
