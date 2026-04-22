using EddyLib.FluidX3D;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "FluidX3D")]
    public class Test_FluidX3DVtkProber
    {
        [Fact]
        public void Probe_LatestVelocity_UsesRhinoCoordinatesAndReturnsExpectedVectors()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string exportDir = CreateExportDir(tempRoot);
                WriteProbeTransform(exportDir, xMin: 100.0, yMin: 200.0, groundZ: 10.0, exportIntervalSeconds: 10.0);

                const int nx = 4;
                const int ny = 3;
                const int nz = 2;

                WriteVectorField(
                    Path.Combine(exportDir, "u-000000000.vtk"),
                    nx,
                    ny,
                    nz,
                    (x, y, z) => new[] { (float)x, (float)y, (float)z });

                WriteVectorField(
                    Path.Combine(exportDir, "u-000000100.vtk"),
                    nx,
                    ny,
                    nz,
                    (x, y, z) => new[] { (float)(100 + x), (float)(100 + y), (float)(100 + z) });

                FluidX3DVtkProbeRequest request = new FluidX3DVtkProbeRequest
                {
                    ExportDirectory = exportDir,
                    Quantity = FluidX3DVtkProbeQuantity.VelocityU,
                    TimeMode = FluidX3DVtkProbeTimeMode.Latest
                };
                request.RhinoPoints.Add(new FluidX3DPoint3(101.2, 200.1, 10.4));
                request.RhinoPoints.Add(new FluidX3DPoint3(999.0, 200.0, 10.0));

                FluidX3DVtkProbeResult result = FluidX3DVtkProber.Probe(request);

                Assert.Equal(FluidX3DVtkProbeQuantity.VelocityU, result.Quantity);
                Assert.Single(result.SampledSteps);
                Assert.Equal(100L, result.SampledSteps[0]);
                Assert.Single(result.SampledTimesSeconds);
                Assert.Equal(10.0, result.SampledTimesSeconds[0], 8);

                Assert.Single(result.VectorValuesByTime);
                Assert.Equal(2, result.VectorValuesByTime[0].Length);

                FluidX3DPoint3 p0 = result.VectorValuesByTime[0][0];
                Assert.Equal(101.0, p0.X, 8);
                Assert.Equal(100.0, p0.Y, 8);
                Assert.Equal(100.0, p0.Z, 8);

                FluidX3DPoint3 p1 = result.VectorValuesByTime[0][1];
                Assert.Equal(103.0, p1.X, 8);
                Assert.Equal(100.0, p1.Y, 8);
                Assert.Equal(100.0, p1.Z, 8);

                Assert.NotNull(result.AverageVectors);
                Assert.Equal(2, result.AverageVectors.Length);
                Assert.Equal(1, result.OutsideDomainPointCount);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public void Probe_AverageRhoOverPhysicalTimeRange_ReturnsExpectedMean()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string exportDir = CreateExportDir(tempRoot);
                WriteProbeTransform(exportDir, xMin: 10.0, yMin: 20.0, groundZ: 0.0, exportIntervalSeconds: 10.0);

                const int nx = 4;
                const int ny = 3;
                const int nz = 2;

                WriteScalarField(
                    Path.Combine(exportDir, "rho-000000000.vtk"),
                    nx,
                    ny,
                    nz,
                    (x, y, z) => x + 10f * y + 100f * z);

                WriteScalarField(
                    Path.Combine(exportDir, "rho-000000100.vtk"),
                    nx,
                    ny,
                    nz,
                    (x, y, z) => 20f + x + 10f * y + 100f * z);

                WriteScalarField(
                    Path.Combine(exportDir, "rho-000000200.vtk"),
                    nx,
                    ny,
                    nz,
                    (x, y, z) => 40f + x + 10f * y + 100f * z);

                FluidX3DVtkProbeRequest request = new FluidX3DVtkProbeRequest
                {
                    ExportDirectory = exportDir,
                    Quantity = FluidX3DVtkProbeQuantity.DensityRho,
                    TimeMode = FluidX3DVtkProbeTimeMode.AverageOverRange,
                    StartTimeSeconds = 5.0,
                    EndTimeSeconds = 25.0
                };
                request.RhinoPoints.Add(new FluidX3DPoint3(12.2, 21.2, 0.1));
                request.RhinoPoints.Add(new FluidX3DPoint3(10.49, 20.49, 0.49));

                FluidX3DVtkProbeResult result = FluidX3DVtkProber.Probe(request);

                Assert.Equal(FluidX3DVtkProbeQuantity.DensityRho, result.Quantity);
                Assert.Equal(2, result.SampledSteps.Count);
                Assert.Equal(100L, result.SampledSteps[0]);
                Assert.Equal(200L, result.SampledSteps[1]);

                Assert.Equal(2, result.SampledTimesSeconds.Count);
                Assert.Equal(10.0, result.SampledTimesSeconds[0], 8);
                Assert.Equal(20.0, result.SampledTimesSeconds[1], 8);

                Assert.Equal(2, result.ScalarValuesByTime.Count);
                Assert.NotNull(result.AverageScalars);
                Assert.Equal(2, result.AverageScalars.Length);

                Assert.Equal(42.0, result.AverageScalars[0], 8);
                Assert.Equal(30.0, result.AverageScalars[1], 8);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        private static string CreateTempDir()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "Eddy3D-Tests",
                "FluidX3D-VTK-Prober",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static string CreateExportDir(string tempRoot)
        {
            string exportDir = Path.Combine(tempRoot, "working", "FluidX3D", "bin", "export");
            Directory.CreateDirectory(exportDir);
            return exportDir;
        }

        private static void WriteProbeTransform(
            string exportDir,
            double xMin,
            double yMin,
            double groundZ,
            double exportIntervalSeconds)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("x_min=" + xMin.ToString("0.###########", CultureInfo.InvariantCulture));
            sb.AppendLine("y_min=" + yMin.ToString("0.###########", CultureInfo.InvariantCulture));
            sb.AppendLine("ground_z=" + groundZ.ToString("0.###########", CultureInfo.InvariantCulture));
            sb.AppendLine("export_interval_seconds=" + exportIntervalSeconds.ToString("0.###########", CultureInfo.InvariantCulture));
            File.WriteAllText(Path.Combine(exportDir, "eddy_probe_transform.txt"), sb.ToString());
        }

        private static void WriteVectorField(
            string path,
            int nx,
            int ny,
            int nz,
            Func<int, int, int, float[]> valueFunc)
        {
            List<float> values = new List<float>(nx * ny * nz * 3);
            for (int z = 0; z < nz; z++)
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int x = 0; x < nx; x++)
                    {
                        float[] v = valueFunc(x, y, z);
                        values.Add(v[0]);
                        values.Add(v[1]);
                        values.Add(v[2]);
                    }
                }
            }

            WriteLegacyVtkScalars(path, nx, ny, nz, components: 3, values);
        }

        private static void WriteScalarField(
            string path,
            int nx,
            int ny,
            int nz,
            Func<int, int, int, float> valueFunc)
        {
            List<float> values = new List<float>(nx * ny * nz);
            for (int z = 0; z < nz; z++)
            {
                for (int y = 0; y < ny; y++)
                {
                    for (int x = 0; x < nx; x++)
                    {
                        values.Add(valueFunc(x, y, z));
                    }
                }
            }

            WriteLegacyVtkScalars(path, nx, ny, nz, components: 1, values);
        }

        private static void WriteLegacyVtkScalars(
            string path,
            int nx,
            int ny,
            int nz,
            int components,
            IList<float> values)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

            int expectedValues = nx * ny * nz * components;
            Assert.Equal(expectedValues, values.Count);

            using (FileStream fs = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter bw = new BinaryWriter(fs, Encoding.ASCII, leaveOpen: true))
            {
                WriteAsciiLine(bw, "# vtk DataFile Version 3.0");
                WriteAsciiLine(bw, "Eddy3D test VTK");
                WriteAsciiLine(bw, "BINARY");
                WriteAsciiLine(bw, "DATASET STRUCTURED_POINTS");
                WriteAsciiLine(bw, "DIMENSIONS " + nx + " " + ny + " " + nz);
                WriteAsciiLine(bw, "ORIGIN 0 0 0");
                WriteAsciiLine(bw, "SPACING 1 1 1");
                WriteAsciiLine(bw, "POINT_DATA " + (nx * ny * nz));
                WriteAsciiLine(bw, "SCALARS data float " + components);
                WriteAsciiLine(bw, "LOOKUP_TABLE default");

                byte[] buffer = new byte[4];
                for (int i = 0; i < values.Count; i++)
                {
                    int raw = BitConverter.SingleToInt32Bits(values[i]);
                    BinaryPrimitives.WriteInt32BigEndian(buffer, raw);
                    bw.Write(buffer);
                }
            }
        }

        private static void WriteAsciiLine(BinaryWriter bw, string line)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(line + "\n");
            bw.Write(bytes);
        }
    }
}
