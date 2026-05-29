using EddyLib.Radiation;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib
{
    //===========================================RADIANCE FILE PROC============================================

    public class RadianceFiles
    {
        private static readonly CultureInfo radianceCulture = new CultureInfo("en-US");

        public static string Epw2Wea(string weatherFilePath, string targetPath)
        {
            // ✅ GOOD: Validate user-controlled paths before use in process execution
            Utilities.ValidatePathForShell(weatherFilePath);
            Utilities.ValidatePathForShell(targetPath);

            string epwdatname = Path.GetFileNameWithoutExtension(weatherFilePath);

            try
            {
                if (!File.Exists(Path.Combine(targetPath, epwdatname + @".wea")))
                {
                    if (Directory.Exists(targetPath) == false)
                    {
                        Directory.CreateDirectory(targetPath);
                    }

                    //if (Directory.GetFiles(targetPath, "*.wea").Length > 0)
                    //{
                    //    Array.ForEach(Directory.GetFiles(targetPath, "*.wea"), delegate (string path) { File.Delete(path); });
                    //}

                    ProcessStartInfo processInfo = new ProcessStartInfo
                    {
                        FileName = DefaultDirectoriesAndPaths.ResolveExePath(DefaultDirectoriesAndPaths.RadianceBinDir, "epw2wea"),
                        WorkingDirectory = DefaultDirectoriesAndPaths.RadianceBinDir,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    processInfo.ArgumentList.Add(Path.GetFullPath(weatherFilePath));
                    processInfo.ArgumentList.Add(Path.GetFullPath(Path.Combine(targetPath, epwdatname + @".wea")));

                    Process p = new Process
                    {
                        StartInfo = processInfo
                    };

                    // p.OutputDataReceived += DebugLog.CaptureOutput; p.ErrorDataReceived += DebugLog.CaptureError;

                    p.Start();

                    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                           Console.WriteLine("output>>" + e.Data);
                    p.BeginOutputReadLine();

                    p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                        Console.WriteLine("error>>" + e.Data);
                    p.BeginErrorReadLine();

                    p.WaitForExit();

                    Console.WriteLine("ExitCode: {0}", p.ExitCode);
                    p.Close();

                    Debug.WriteLine("WEA FILE EXSISTS? " + File.Exists(Path.GetFullPath(Path.Combine(targetPath, epwdatname + @".wea"))).ToString());

                    return epwdatname;
                }
                else
                {
                    return epwdatname;
                }
            }
            catch
            {
                Debug.WriteLine("SetWeather failed");
                return "";
            }
        }

        private static string FormatPointAndNormal(Point3d p, Vector3d n) =>
           String.Format(radianceCulture, "{0:0.###} {1:0.###} {2:0.###} {3:0.###} {4:0.###} {5:0.###}", p.X, p.Y, p.Z, n.X, n.Y, n.Z);

        private static string FormatPoint(Point3d p) =>
        String.Format(radianceCulture, "{0:0.###} {1:0.###} {2:0.###}", p.X, p.Y, p.Z);

        #region MeshProc Helpers

        /// <summary>
        /// Gets model tolerance for face filtering.
        /// </summary>
        private static double GetModelTolerance() => RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 0.01;

        /// <summary>
        /// Gets model angle tolerance for quad conversion.
        /// </summary>
        private static double GetModelAngleTolerance() => RhinoDoc.ActiveDoc?.ModelAngleToleranceRadians ?? 0.01;

        /// <summary>
        /// Prepares mesh for Radiance export.
        /// </summary>
        private static void PrepareMesh(Mesh mesh)
        {
            mesh.Faces.ConvertNonPlanarQuadsToTriangles(GetModelTolerance(), GetModelAngleTolerance(), 0);
            mesh.Faces.CullDegenerateFaces();
        }

        /// <summary>
        /// Checks if a mesh face should be skipped due to small area.
        /// </summary>
        private static bool ShouldSkipFace(Mesh mesh, int faceIndex) =>
            Utilities.MeshFaceArea(faceIndex, mesh) < GetModelTolerance();

        /// <summary>
        /// Writes a Radiance file header.
        /// </summary>
        private static void WriteRadianceHeader(StreamWriter sw, string materialLib = null)
        {
            sw.WriteLine($"# Grasshopper Eddy3D {EddyLib.EddyVersion.ProductVersion}");
            sw.WriteLine("");
            if (materialLib != null) sw.WriteLine(materialLib);
            sw.WriteLine("");
        }

        #endregion MeshProc Helpers

        public static void MeshProc(Mesh _m, string _fname, string _mat)
        {
            using var sw = new System.IO.StreamWriter(_fname, false, new System.Text.UTF8Encoding(false), 65536);
            sw.WriteLine("# Grasshopper Eddy3D " + EddyLib.EddyVersion.ProductVersion);
            sw.WriteLine("");

            _m.Faces.ConvertNonPlanarQuadsToTriangles(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAngleToleranceRadians, 0);
            _m.Faces.CullDegenerateFaces();

            for (int i = 0; i < _m.Faces.Count; ++i)
            {
                var area = Utilities.MeshFaceArea(i, _m);
                if (area < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    continue;
                }

                if (_m.Faces[i].IsTriangle)
                {
                    sw.WriteLine(_mat + " polygon " + _mat + "." + (i + 1).ToString());
                    sw.WriteLine("0");
                    sw.WriteLine("0");
                    sw.WriteLine("9");

                    int v0 = _m.Faces[i].A;
                    int v1 = _m.Faces[i].B;
                    int v2 = _m.Faces[i].C;

                    Utilities.WritePV(sw, _m.Vertices[v0]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v1]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v2]); sw.WriteLine();
                    sw.WriteLine();
                }
                else
                {
                    sw.WriteLine(_mat + " polygon " + _mat + "." + (i + 1).ToString());
                    sw.WriteLine("0");
                    sw.WriteLine("0");
                    sw.WriteLine("12");

                    int v0 = _m.Faces[i].A;
                    int v1 = _m.Faces[i].B;
                    int v2 = _m.Faces[i].C;
                    int v3 = _m.Faces[i].D;

                    Utilities.WritePV(sw, _m.Vertices[v0]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v1]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v2]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v3]); sw.WriteLine();
                    sw.WriteLine();
                }
            }
        }

        public static void MeshProc(Mesh _m, string _fname, string _mat, string _matLib)
        {
            using var sw = new System.IO.StreamWriter(_fname, false, new System.Text.UTF8Encoding(false), 65536);
            sw.WriteLine("# Grasshopper Eddy3D " + EddyLib.EddyVersion.ProductVersion);
            sw.WriteLine("");
            sw.WriteLine(_matLib);
            sw.WriteLine("");

            // Need this for unit testing
            if (RhinoDoc.ActiveDoc == null)
            {
                _m.Faces.ConvertNonPlanarQuadsToTriangles(0.01, 0.01, 0);
            }
            else
            {
                _m.Faces.ConvertNonPlanarQuadsToTriangles(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAngleToleranceRadians, 0);
            }

            _m.Faces.CullDegenerateFaces();

            for (int i = 0; i < _m.Faces.Count; ++i)
            {
                var area = Utilities.MeshFaceArea(i, _m);

                // Need this for unit testing
                if (RhinoDoc.ActiveDoc == null)
                {
                    if (area < 0.01)
                    {
                        continue;
                    }
                }
                else
                {
                    if (area < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                    {
                        continue;
                    }
                }

                if (_m.Faces[i].IsTriangle)
                {
                    // Change this material for 2Phase method

                    sw.WriteLine(_mat + " polygon " + _mat + "." + (i + 1).ToString());
                    sw.WriteLine("0");
                    sw.WriteLine("0");
                    sw.WriteLine("9");

                    int v0 = _m.Faces[i].A;
                    int v1 = _m.Faces[i].B;
                    int v2 = _m.Faces[i].C;

                    Utilities.WritePV(sw, _m.Vertices[v0]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v1]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v2]); sw.WriteLine();
                    sw.WriteLine();
                }
                else
                {
                    sw.WriteLine(_mat + " polygon " + _mat + "." + (i + 1).ToString());
                    sw.WriteLine("0");
                    sw.WriteLine("0");
                    sw.WriteLine("12");

                    int v0 = _m.Faces[i].A;
                    int v1 = _m.Faces[i].B;
                    int v2 = _m.Faces[i].C;
                    int v3 = _m.Faces[i].D;

                    Utilities.WritePV(sw, _m.Vertices[v0]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v1]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v2]); sw.WriteLine();
                    Utilities.WritePV(sw, _m.Vertices[v3]); sw.WriteLine();
                    sw.WriteLine();
                }
            }
        }

        public static void MeshProc(List<RSurface> rsurfs, string _fname)
        {
            StringBuilder _matLib = new StringBuilder();
            Dictionary<string, string> matLib = new Dictionary<string, string>();

            foreach (var s in rsurfs)
            {
                if (!matLib.ContainsKey(s.MaterialID))
                {
                    if (s.Settings != null) matLib.Add(s.MaterialID, s.Settings.RadianceMaterial);
                    if (s.VegSettings != null) matLib.Add(s.MaterialID, s.VegSettings.RadianceMaterial);
                    if (s.TreeSettings != null) matLib.Add(s.MaterialID, s.TreeSettings.RadianceMaterial);

                    if (s.Settings != null) _matLib.AppendLine(s.Settings.RadianceMaterial);
                    if (s.VegSettings != null) _matLib.AppendLine(s.VegSettings.RadianceMaterial);
                    if (s.TreeSettings != null) _matLib.AppendLine(s.TreeSettings.RadianceMaterial);

                    _matLib.AppendLine();
                }
            }

            using var sw = new System.IO.StreamWriter(_fname, false, new System.Text.UTF8Encoding(false), 65536);
            sw.WriteLine("# Grasshopper Eddy3D " + EddyLib.EddyVersion.ProductVersion);
            sw.WriteLine("");
            sw.WriteLine(_matLib.ToString());
            sw.WriteLine("");

            int polyCnt = 0;

            foreach (var s in rsurfs)
            {
                Mesh _m = s.LowPoly.DuplicateMesh();

                // Need this for unit testing
                if (RhinoDoc.ActiveDoc == null)
                {
                    _m.Faces.ConvertNonPlanarQuadsToTriangles(0.01, 0.01, 0);
                }
                else
                {
                    _m.Faces.ConvertNonPlanarQuadsToTriangles(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAngleToleranceRadians, 0);
                }

                _m.Faces.CullDegenerateFaces();

                for (int i = 0; i < _m.Faces.Count; ++i)
                {
                    var area = Utilities.MeshFaceArea(i, _m);

                    // Need this for unit testing
                    if (RhinoDoc.ActiveDoc == null)
                    {
                        if (area < 0.01)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (area < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                        {
                            continue;
                        }
                    }

                    if (_m.Faces[i].IsTriangle)
                    {
                        // Change this material for 2Phase method

                        sw.WriteLine(s.MaterialID + " polygon " + s.MaterialID + "." + (polyCnt + 1).ToString());
                        sw.WriteLine("0");
                        sw.WriteLine("0");
                        sw.WriteLine("9");

                        int v0 = _m.Faces[i].A;
                        int v1 = _m.Faces[i].B;
                        int v2 = _m.Faces[i].C;

                        Utilities.WritePV(sw, _m.Vertices[v0]); sw.WriteLine();
                        Utilities.WritePV(sw, _m.Vertices[v1]); sw.WriteLine();
                        Utilities.WritePV(sw, _m.Vertices[v2]); sw.WriteLine();
                        sw.WriteLine();
                    }
                    else
                    {
                        sw.WriteLine(s.MaterialID + " polygon " + s.MaterialID + "." + (polyCnt + 1).ToString());
                        sw.WriteLine("0");
                        sw.WriteLine("0");
                        sw.WriteLine("12");

                        int v0 = _m.Faces[i].A;
                        int v1 = _m.Faces[i].B;
                        int v2 = _m.Faces[i].C;
                        int v3 = _m.Faces[i].D;

                        Utilities.WritePV(sw, _m.Vertices[v0]); sw.WriteLine();
                        Utilities.WritePV(sw, _m.Vertices[v1]); sw.WriteLine();
                        Utilities.WritePV(sw, _m.Vertices[v2]); sw.WriteLine();
                        Utilities.WritePV(sw, _m.Vertices[v3]); sw.WriteLine();
                        sw.WriteLine();
                    }
                    polyCnt++;
                }
            }
        }

        public static void writePTS(string pts_path, List<Point3d> pts, List<Vector3d> pts_norm)
        {
            // Bolt: Replaced StringBuilder with StreamWriter to stream directly to disk,
            // preventing LOH allocations and OOM exceptions on large datasets.
            using var sw = new StreamWriter(pts_path);
            for (int k = 0; k < pts.Count; k++)
            {
                Utilities.WritePV(sw, pts[k], pts_norm[k]);
                sw.WriteLine();
            }
        }

        public static void writePTS(string pts_path, List<Point3d> pts)
        {
            // Bolt: Replaced StringBuilder with StreamWriter to stream directly to disk,
            // preventing LOH allocations and OOM exceptions on large datasets.
            using var sw = new StreamWriter(pts_path);
            for (int k = 0; k < pts.Count; k++)
            {
                Utilities.WritePV(sw, pts[k], Vector3d.ZAxis);
                sw.WriteLine();
            }
        }

        public static double[][] readPTS(string pts_path)
        {
            // Bolt: Replaced File.ReadAllLines with File.ReadLines for lazy evaluation,
            // reducing LOH allocations when reading large PTS files.
            var pointsList = new System.Collections.Generic.List<double[]>();
            foreach (var line in File.ReadLines(pts_path))
            {
                string[] ptsString = line.Split(' ').Take(3).ToArray();
                double[] pts = Array.ConvertAll<string, double>(ptsString, Double.Parse);
                pointsList.Add(pts);
            }

            return pointsList.ToArray();
        }

        public static double[,] readDatFile(string path)
        {
            // Bolt: Replaced File.ReadAllLines with File.ReadLines for lazy evaluation,
            // reducing LOH allocations when reading large dat files.
            var lines = File.ReadLines(path);
            var tempRGB = new System.Collections.Generic.List<double[]>();

            foreach (var line in lines)
            {
                string[] ln = line.Split('\t');
                tempRGB.Add(new double[] { double.Parse(ln[0]), double.Parse(ln[1]), double.Parse(ln[2]) });
            }

            double[,] RGB = new double[tempRGB.Count, 3];
            for (int i = 0; i < tempRGB.Count; i++)
            {
                RGB[i, 0] = tempRGB[i][0];
                RGB[i, 1] = tempRGB[i][1];
                RGB[i, 2] = tempRGB[i][2];
            }
            return RGB;
        }

        public static double[,] readCSVFile(string path)
        {
            // Bolt: Replaced File.ReadAllLines with File.ReadLines for lazy evaluation,
            // reducing LOH allocations when reading large CSV files.
            var lines = File.ReadLines(path);
            var rowData = new System.Collections.Generic.List<double[]>();

            foreach (var line in lines)
            {
                string[] ln = line.Trim(',').Split(',');
                var parsedRow = new double[ln.Length];
                for (int j = 0; j < ln.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(ln[j])) continue;
                    parsedRow[j] = double.Parse(ln[j]);
                }
                rowData.Add(parsedRow);
            }

            if (rowData.Count == 0) return new double[0, 0];

            double[,] data = new double[rowData.Count, rowData[0].Length];
            for (int i = 0; i < rowData.Count; i++)
            {
                for (int j = 0; j < rowData[0].Length; j++)
                {
                    if (j < rowData[i].Length)
                        data[i, j] = rowData[i][j];
                }
            }
            return data;
        }

        //====================================== ILLU FILE

        /// <summary>
        /// Loads an Daysim Illuminance file and converts it into a 2D double array.
        /// </summary>
        /// <returns>A list of list of doubles where [x][] is time and [][x] are sensor points.</returns>
        /// <param name="fileName">File name.</param>
        /// <param name="start">Start.</param>
        /// <param name="stop">Stop.</param>
        private static double[] ParseILLLine(ReadOnlySpan<char> span)
        {
            const int dataStart = 4;

            // First pass: count total valid tokens
            int tokenCount = 0;
            int spaceIndex;
            var tempSpan = span;

            while ((spaceIndex = tempSpan.IndexOf(' ')) != -1)
            {
                if (spaceIndex > 0) tokenCount++;
                tempSpan = tempSpan.Slice(spaceIndex + 1);
            }
            if (tempSpan.Length > 0) tokenCount++;

            int resultSize = Math.Max(0, tokenCount - dataStart);
            if (resultSize == 0) return Array.Empty<double>();

            var results = new double[resultSize];
            int currentTokenIndex = 0;
            int resultIndex = 0;

            while ((spaceIndex = span.IndexOf(' ')) != -1)
            {
                if (spaceIndex > 0)
                {
                    if (currentTokenIndex >= dataStart)
                    {
                        results[resultIndex++] = double.Parse(span.Slice(0, spaceIndex), CultureInfo.InvariantCulture);
                    }
                    currentTokenIndex++;
                }
                span = span.Slice(spaceIndex + 1);
            }

            if (span.Length > 0 && currentTokenIndex >= dataStart)
            {
                results[resultIndex] = double.Parse(span, CultureInfo.InvariantCulture);
            }

            return results;
        }

        public static double[][] loadILL(string fileName, int start, int stop)
        {
            // [x][] time [][x] points

            double[][] values = new double[stop - start][];
            int index = 0;

            foreach (string line in System.IO.File.ReadLines(fileName).Skip(start).Take(stop - start))
            {
                values[index++] = ParseILLLine(line.AsSpan());
            }

            return values;
        }

        public static double[][] loadILL(string illFileName) // total illuminance data
        {
            // [x][] time
            // [][x] points

            var values = new List<double[]>();
            foreach (string line in System.IO.File.ReadLines(illFileName))
            {
                values.Add(ParseILLLine(line.AsSpan()));
            }
            return values.ToArray();
        }

        public static void saveILLBin(string illFileName) // total illuminance data
        {
            // [x][] time
            // [][x] points

            var valuesList = new List<double[]>();
            foreach (string line in System.IO.File.ReadLines(illFileName))
            {
                valuesList.Add(ParseILLLine(line.AsSpan()));
            }
            double[][] values = valuesList.ToArray();

            writeBin(illFileName + ".bin", values);
        }

        public static float[,] loadBin(string filename)
        {
            // [i, time j] points

            float[,] data;

            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                int iDim = b.ReadInt32();
                int jDim = b.ReadInt32();
                data = new float[iDim, jDim];

                int totalFloats = iDim * jDim;
                byte[] buffer = b.ReadBytes(totalFloats * sizeof(float));
                float[] flat = new float[totalFloats];
                Buffer.BlockCopy(buffer, 0, flat, 0, buffer.Length);
                for (int idx = 0; idx < totalFloats; idx++)
                    data[idx / jDim, idx % jDim] = flat[idx];
            }

            return data;
        }

        public static double[,] loadBinD(string filename)
        {
            // [i, time j] points

            double[,] data;

            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                int iDim = b.ReadInt32();
                int jDim = b.ReadInt32();
                data = new double[iDim, jDim];

                int totalFloats = iDim * jDim;
                byte[] buffer = b.ReadBytes(totalFloats * sizeof(float));
                float[] flat = new float[totalFloats];
                Buffer.BlockCopy(buffer, 0, flat, 0, buffer.Length);
                for (int idx = 0; idx < totalFloats; idx++)
                    data[idx / jDim, idx % jDim] = flat[idx];
            }

            return data;
        }

        public static double[] loadBin1D(string filename)
        {
            double[] data;

            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                int iDim = b.ReadInt32();
                data = new double[iDim];

                byte[] buffer = b.ReadBytes(iDim * sizeof(float));
                float[] flat = new float[iDim];
                Buffer.BlockCopy(buffer, 0, flat, 0, buffer.Length);
                for (int i = 0; i < iDim; i++)
                    data[i] = flat[i];
            }

            return data;
        }

        public static float[][] loadBinJagged(string filename)
        {
            // [i][ time j] points

            float[][] data;

            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                int iDim = b.ReadInt32();
                int jDim = b.ReadInt32();
                data = new float[iDim][];

                int totalFloats = iDim * jDim;
                byte[] buffer = b.ReadBytes(totalFloats * sizeof(float));
                float[] flat = new float[totalFloats];
                Buffer.BlockCopy(buffer, 0, flat, 0, buffer.Length);

                for (int i = 0; i < iDim; i++)
                {
                    data[i] = new float[jDim];
                    Array.Copy(flat, i * jDim, data[i], 0, jDim);
                }
            }

            return data;
        }

        public static void writeBin(string fileName, double[][] values)
        {
            // [i, time j] points

            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                if (values == null || values.Length == 0 || values[0].Length == 0)
                {
                    throw new ArgumentException("Cannot write empty or null data to Radiance binary file. This often happens if the Radiance simulation failed to produce results.");
                }

                bw.Write((int)values.Length);
                bw.Write((int)values[0].Length);

                for (int i = 0; i < values.Length; i++)
                {
                    for (int j = 0; j < values[i].Length; j++)
                    {
                        float fval = (float)values[i][j];

                        bw.Write(fval);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        public static void writeBin(string fileName, double[,] values)
        {
            // [i, time j] points

            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                bw.Write((Int32)values.GetLength(0));
                bw.Write((Int32)values.GetLength(1));

                for (int i = 0; i < values.GetLength(0); i++)
                {
                    for (int j = 0; j < values.GetLength(1); j++)
                    {
                        float fval = (float)values[i, j];
                        bw.Write(fval);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        public static void writeBin1D(string fileName, double[] values)
        {
            // [i, time j] points

            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                bw.Write((Int32)values.GetLength(0));

                for (int i = 0; i < values.GetLength(0); i++)
                {
                    float fval = (float)values[i];
                    bw.Write(fval);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        public static void writeBinScalars(string fileName, double[] values)
        {
            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                // Write array dimensions
                bw.Write((Int32)values.GetLength(0));

                for (int i = 0; i < values.GetLength(0); i++)
                {
                    float val = (float)values[i];

                    bw.Write(val);
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        public static double[] loadBinScalars(string filename) //, out int iDim, out int jDim)
        {
            // [i, time j] points

            double[] data;

            //   int numberOfWindDirs;

            //reading from the file
            // 1.
            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                // 2. Position and length variables.
                int pos = 0;

                // 2A. Use BaseStream.
                int length = (int)b.BaseStream.Length;

                int iDim = b.ReadInt32();

                data = new double[iDim];
                pos += sizeof(int);

                int i = 0;

                while (pos < length)
                {
                    float x = b.ReadSingle();
                    data[i] = (x);

                    pos += sizeof(float);

                    i++;
                }
            }

            return data;
        }

        public static void writeBinVectors(string fileName, Vector3d[] values)
        {
            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                // Write array dimensions
                bw.Write((Int32)values.GetLength(0));

                for (int i = 0; i < values.GetLength(0); i++)
                {
                    //  for (int j = 0; j < values.GetLength(1); j++)
                    //  {
                    float fvalX = (float)values[i].X;
                    float fvalY = (float)values[i].Y;
                    float fvalZ = (float)values[i].Z;

                    bw.Write(fvalX);
                    bw.Write(fvalY);
                    bw.Write(fvalZ);
                    // }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        public static Vector3d[] loadBinVectors(string filename) //, out int iDim
        {
            // [i] points

            Vector3d[] data;

            //reading from the file
            // 1.
            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                // 2. Position and length variables.
                int pos = 0;

                // 2A. Use BaseStream.
                int length = (int)b.BaseStream.Length;

                int iDim = b.ReadInt32();

                data = new Vector3d[iDim];
                pos += sizeof(int);

                int i = 0;

                while (pos < length)
                {
                    float x = b.ReadSingle();
                    data[i].X = (x);

                    float y = b.ReadSingle();
                    data[i].Y = (y);

                    float z = b.ReadSingle();
                    data[i].Z = (z);

                    pos += sizeof(float);
                    pos += sizeof(float);
                    pos += sizeof(float);

                    i++;
                }
            }

            return data;
        }

        public static Vector3d[,] loadBinDVectors(string filename, out int[] windDirs)//, out int iDim, out int jDim)
        {
            // [i, time j] points

            Vector3d[,] data;

            int numberOfWindDirs;

            //reading from the file
            // 1.
            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                // 2. Position and length variables.
                int pos = 0;

                // 2A. Use BaseStream.
                int length = (int)b.BaseStream.Length;

                int iDim = b.ReadInt32();
                int jDim = b.ReadInt32();
                data = new Vector3d[iDim, jDim];
                pos += sizeof(int);
                pos += sizeof(int);

                // Read wind dirs
                numberOfWindDirs = b.ReadInt32();
                pos += sizeof(int);

                windDirs = new int[numberOfWindDirs];

                for (int wd = 0; wd < numberOfWindDirs; wd++)
                {
                    windDirs[wd] = b.ReadInt32();
                    pos += sizeof(int);
                }

                int i = 0;
                int j = 0;
                while (pos < length)
                {
                    float x = b.ReadSingle();
                    data[i, j].X = (x);

                    float y = b.ReadSingle();
                    data[i, j].Y = (y);

                    float z = b.ReadSingle();
                    data[i, j].Z = (z);

                    pos += sizeof(float);
                    pos += sizeof(float);
                    pos += sizeof(float);

                    j++;
                    if (j == jDim) { j = 0; i++; }
                }
            }

            return data;
        }

        public static void writeBinVectors(string fileName, Vector3d[,] values, int[] windDirs)
        {
            // [i, time j] points

            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                // Write array dimensions
                bw.Write((Int32)values.GetLength(0));
                bw.Write((Int32)values.GetLength(1));

                // Write wind directions
                bw.Write(windDirs.Length);
                for (int w = 0; w < windDirs.Length; w++)
                {
                    int dir = windDirs[w];
                    bw.Write(dir);
                }

                for (int i = 0; i < values.GetLength(0); i++)
                {
                    for (int j = 0; j < values.GetLength(1); j++)
                    {
                        float fvalX = (float)values[i, j].X;
                        float fvalY = (float)values[i, j].Y;
                        float fvalZ = (float)values[i, j].Z;

                        bw.Write(fvalX);
                        bw.Write(fvalY);
                        bw.Write(fvalZ);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        private static double[] ParseDCLine(ReadOnlySpan<char> span)
        {
            var values = new List<double>();

            // Trim
            span = span.Trim();

            int tabIndex;
            while ((tabIndex = span.IndexOf('\t')) != -1)
            {
                if (tabIndex > 0)
                {
                    values.Add(double.Parse(span.Slice(0, tabIndex), CultureInfo.InvariantCulture));
                }
                span = span.Slice(tabIndex + 1);
            }

            if (span.Length > 0)
            {
                values.Add(double.Parse(span, CultureInfo.InvariantCulture));
            }

            return values.ToArray();
        }

        public static double[][] loadDC(string file) // total illuminance data
        {
            // [x][] lines [][x] coeffs

            var valuesList = new List<double[]>();

            foreach (string line in System.IO.File.ReadLines(file))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.TrimStart().StartsWith("#")) continue;

                valuesList.Add(ParseDCLine(line.AsSpan()));
            }

            return valuesList.ToArray();
        }

        public static void writeDC(string file, double[][] dif, double[][] dir) // total illuminance data
        {
            // [x][] lines [][x] coeffs
            // Bolt: Replaced StringBuilder with StreamWriter to stream directly to disk,
            // preventing LOH allocations and OOM exceptions on large datasets.
            using var sw = new StreamWriter(file);

            for (int h = 0; h < dif.Length; h++)
            {
                for (int c = 0; c < dif[h].Length; c++)
                {
                    sw.Write(dif[h][c].ToString());
                    sw.Write('\t');
                }
                for (int c = 0; c < dir[h].Length; c++)
                {
                    sw.Write(dir[h][c].ToString());
                    sw.Write('\t');
                }
                sw.WriteLine("");
            }
        }

        public static void writeDC_DIF(string file, double[][] dif, double[][] dir) // total illuminance data
        {
            // [x][] lines [][x] coeffs
            // Bolt: Replaced StringBuilder with StreamWriter to stream directly to disk,
            // preventing LOH allocations and OOM exceptions on large datasets.
            using var sw = new StreamWriter(file);

            for (int h = 0; h < dif.Length; h++)
            {
                for (int c = 0; c < dif[h].Length; c++)
                {
                    sw.Write(dif[h][c].ToString());
                    sw.Write('\t');
                }
                for (int c = 0; c < dir[h].Length; c++)
                {
                    sw.Write("0");
                    sw.Write('\t');
                }
                sw.WriteLine("");
            }
        }

        public static void writeDC_DIR(string file, double[][] dif, double[][] dir) // total illuminance data
        {
            // [x][] lines [][x] coeffs
            // Bolt: Replaced StringBuilder with StreamWriter to stream directly to disk,
            // preventing LOH allocations and OOM exceptions on large datasets.
            using var sw = new StreamWriter(file);

            for (int h = 0; h < dif.Length; h++)
            {
                for (int c = 0; c < dif[h].Length; c++)
                {
                    sw.Write("0");
                    sw.Write('\t');
                }
                for (int c = 0; c < dir[h].Length; c++)
                {
                    sw.Write(dir[h][c].ToString());
                    sw.Write('\t');
                }
                sw.WriteLine("");
            }
        }
    }
}
