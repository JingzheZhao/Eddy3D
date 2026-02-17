using EddyLib.FluidX3D;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Integration")]
    [Trait("Category", "FluidX3D")]
    public class FluidX3DSimulationIntegrationTests
    {
        [Fact]
        public void FluidX3DSimulation_EndToEnd_PreparesCaseFromBuildingStlInputs()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string sourceRoot = Path.Combine(tempRoot, "source");
                string workingRoot = Path.Combine(tempRoot, "working");
                string stagingStlDir = Path.Combine(tempRoot, "staging-stl");
                Directory.CreateDirectory(stagingStlDir);
                CreateStubFluidX3DSource(sourceRoot);

                string stagedA = Path.Combine(stagingStlDir, "building_000.stl");
                string stagedB = Path.Combine(stagingStlDir, "building_001.stl");
                WriteBinaryBoxStl(stagedA, 10f, 30f, 0f, 30f, 50f, 35f);
                WriteBinaryBoxStl(stagedB, 45f, 62f, 0f, 60f, 78f, 22f);

                FluidX3DAblSettings settings = new FluidX3DAblSettings
                {
                    MemoryMb = 800,
                    Uref = 5.0,
                    Zref = 10.0,
                    Z0 = 0.1,
                    SimSeconds = 20.0,
                    ExportIntervalSeconds = 10.0,
                    DomainLx = 180.0,
                    DomainLy = 220.0,
                    DomainLz = 100.0
                };
                settings.BuildingStlFiles.Add("building_000.stl");
                settings.BuildingStlFiles.Add("building_001.stl");

                FluidX3DAblPrepareResult result = FluidX3DAblWorkflow.PrepareCase(sourceRoot, workingRoot, settings);

                string caseStlDir = Path.Combine(result.CaseRoot, "stl");
                Directory.CreateDirectory(caseStlDir);
                string caseStlA = Path.Combine(caseStlDir, "building_000.stl");
                string caseStlB = Path.Combine(caseStlDir, "building_001.stl");
                File.Copy(stagedA, caseStlA, true);
                File.Copy(stagedB, caseStlB, true);

                Assert.True(File.Exists(result.SetupPath));
                Assert.True(File.Exists(result.DefinesPath));
                Assert.True(File.Exists(result.CommandScriptPath));
                Assert.True(File.Exists(result.BatchScriptPath));
                Assert.True(File.Exists(result.ReadmePath));
                Assert.Equal(Path.Combine(result.CaseRoot, "bin", "export"), result.ExportDirectory);

                string setupText = File.ReadAllText(result.SetupPath);
                Assert.Contains("../stl/building_000.stl", setupText);
                Assert.Contains("../stl/building_001.stl", setupText);
                Assert.Contains("const float si_Lx = 180.0f;", setupText);
                Assert.Contains("const float si_Ly = 220.0f;", setupText);
                Assert.Contains("const float si_Lz = 100.0f;", setupText);
                Assert.Contains("voxelize_mesh_on_device(building, TYPE_S | TYPE_X)", setupText);

                string readmeText = File.ReadAllText(result.ReadmePath);
                Assert.Contains("- STL building count: 2", readmeText);

                AssertValidBinaryStl(caseStlA);
                AssertValidBinaryStl(caseStlB);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        private static void WriteBinaryBoxStl(
            string stlPath,
            float minX,
            float minY,
            float minZ,
            float maxX,
            float maxY,
            float maxZ)
        {
            string directory = Path.GetDirectoryName(stlPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (BinaryWriter writer = new BinaryWriter(File.Open(stlPath, FileMode.Create, FileAccess.Write, FileShare.None)))
            {
                byte[] header = new byte[80];
                byte[] title = Encoding.ASCII.GetBytes("Eddy3D FluidX3D integration test STL");
                Buffer.BlockCopy(title, 0, header, 0, Math.Min(title.Length, header.Length));
                writer.Write(header);
                writer.Write(12u);

                var v = new (float X, float Y, float Z)[]
                {
                    (minX, minY, minZ), // 0
                    (maxX, minY, minZ), // 1
                    (maxX, maxY, minZ), // 2
                    (minX, maxY, minZ), // 3
                    (minX, minY, maxZ), // 4
                    (maxX, minY, maxZ), // 5
                    (maxX, maxY, maxZ), // 6
                    (minX, maxY, maxZ)  // 7
                };

                WriteTriangle(writer, 0f, 0f, -1f, v[0], v[2], v[1]);
                WriteTriangle(writer, 0f, 0f, -1f, v[0], v[3], v[2]);
                WriteTriangle(writer, 0f, 0f, 1f, v[4], v[5], v[6]);
                WriteTriangle(writer, 0f, 0f, 1f, v[4], v[6], v[7]);
                WriteTriangle(writer, 0f, -1f, 0f, v[0], v[1], v[5]);
                WriteTriangle(writer, 0f, -1f, 0f, v[0], v[5], v[4]);
                WriteTriangle(writer, 0f, 1f, 0f, v[3], v[7], v[6]);
                WriteTriangle(writer, 0f, 1f, 0f, v[3], v[6], v[2]);
                WriteTriangle(writer, -1f, 0f, 0f, v[0], v[4], v[7]);
                WriteTriangle(writer, -1f, 0f, 0f, v[0], v[7], v[3]);
                WriteTriangle(writer, 1f, 0f, 0f, v[1], v[2], v[6]);
                WriteTriangle(writer, 1f, 0f, 0f, v[1], v[6], v[5]);
            }
        }

        private static void WriteTriangle(
            BinaryWriter writer,
            float nx,
            float ny,
            float nz,
            (float X, float Y, float Z) a,
            (float X, float Y, float Z) b,
            (float X, float Y, float Z) c)
        {
            writer.Write(nx);
            writer.Write(ny);
            writer.Write(nz);

            writer.Write(a.X);
            writer.Write(a.Y);
            writer.Write(a.Z);

            writer.Write(b.X);
            writer.Write(b.Y);
            writer.Write(b.Z);

            writer.Write(c.X);
            writer.Write(c.Y);
            writer.Write(c.Z);

            writer.Write((ushort)0);
        }

        private static void AssertValidBinaryStl(string stlPath)
        {
            Assert.True(File.Exists(stlPath), "Expected STL file missing: " + stlPath);

            using (FileStream fs = File.OpenRead(stlPath))
            {
                Assert.True(fs.Length >= 84, "Binary STL too small: " + stlPath);

                byte[] headerAndCount = new byte[84];
                int read = fs.Read(headerAndCount, 0, headerAndCount.Length);
                Assert.Equal(84, read);

                uint triangleCount = BitConverter.ToUInt32(headerAndCount, 80);
                Assert.True(triangleCount > 0, "Binary STL has no triangles: " + stlPath);

                long expectedLength = 84L + (50L * triangleCount);
                Assert.Equal(expectedLength, fs.Length);
            }
        }

        private static void CreateStubFluidX3DSource(string sourceRoot)
        {
            Directory.CreateDirectory(sourceRoot);
            Directory.CreateDirectory(Path.Combine(sourceRoot, "src"));

            File.WriteAllText(
                Path.Combine(sourceRoot, "src", "setup.cpp"),
                "// original setup");

            File.WriteAllText(
                Path.Combine(sourceRoot, "src", "defines.hpp"),
@"#pragma once
//#define FP16S
#define BENCHMARK
//#define FORCE_FIELD
//#define EQUILIBRIUM_BOUNDARIES
//#define SUBGRID
");

            File.WriteAllText(Path.Combine(sourceRoot, "make.sh"), "#!/usr/bin/env bash\n");
            File.WriteAllText(Path.Combine(sourceRoot, "FluidX3D.sln"), "stub");
        }

        private static string CreateTempDir()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "Eddy3D-Tests",
                "FluidX3D-Integration",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
