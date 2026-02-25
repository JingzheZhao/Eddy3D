using EddyLib.FluidX3D;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "FluidX3D")]
    public class Test_FluidX3DAblWorkflow
    {
        [Fact]
        public void PrepareCase_WritesSetupDefinesAndScripts()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string sourceRoot = Path.Combine(tempRoot, "source");
                string workingRoot = Path.Combine(tempRoot, "working");
                CreateStubFluidX3DSource(sourceRoot);

                FluidX3DAblSettings settings = new FluidX3DAblSettings
                {
                    MemoryMb = 1200,
                    Uref = 5.0,
                    Zref = 10.0,
                    Z0 = 0.1,
                    SimSeconds = 30.0,
                    ExportIntervalSeconds = 10.0
                };

                FluidX3DAblPrepareResult result = FluidX3DAblWorkflow.PrepareCase(sourceRoot, workingRoot, settings);

                Assert.True(File.Exists(result.SetupPath));
                Assert.True(File.Exists(result.DefinesPath));
                Assert.True(File.Exists(result.CommandScriptPath));
                Assert.True(File.Exists(result.BatchScriptPath));
                Assert.True(File.Exists(result.ReadmePath));

                string setupText = File.ReadAllText(result.SetupPath);
                Assert.Contains("const float si_u_ref   = 5.0f;", setupText);
                Assert.Contains("const float si_z_ref   = 10.0f;", setupText);
                Assert.Contains("const float si_z0      = 0.1f;", setupText);
                Assert.Contains("const uint memory = 1200u;", setupText);
                Assert.Contains("Fallback demo buildings", setupText);
                Assert.Contains("lbm.rho[n] = 1.0f;", setupText);
                Assert.Contains("lbm.flags.write_device_to_vtk();", setupText);

                string definesText = File.ReadAllText(result.DefinesPath);
                Assert.Contains("#define FP16S", definesText);
                Assert.Contains("//#define BENCHMARK", definesText);
                Assert.Contains("#define FORCE_FIELD", definesText);
                Assert.Contains("#define EQUILIBRIUM_BOUNDARIES", definesText);
                Assert.Contains("#define SUBGRID", definesText);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public void PrepareCase_WritesCrossPlatformLaunchScripts()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string sourceRoot = Path.Combine(tempRoot, "source");
                string workingRoot = Path.Combine(tempRoot, "working");
                CreateStubFluidX3DSource(sourceRoot);

                FluidX3DAblPrepareResult result = FluidX3DAblWorkflow.PrepareCase(
                    sourceRoot,
                    workingRoot,
                    new FluidX3DAblSettings());

                string commandScript = File.ReadAllText(result.CommandScriptPath);
                Assert.Contains("#!/bin/bash", commandScript);
                Assert.Contains("SOURCE_DIR=\"", commandScript);
                Assert.Contains("CASE_EXPORT_DIR=\"", commandScript);
                Assert.Contains("cd \"$SOURCE_DIR\" || exit 1", commandScript);
                Assert.Contains("ln -s \"$CASE_EXPORT_DIR\" \"$SOURCE_EXPORT_DIR\"", commandScript);
                Assert.Contains("./make.sh", commandScript);
                Assert.Contains("VTK outputs redirected to $CASE_EXPORT_DIR.", commandScript);

                string batchScript = File.ReadAllText(result.BatchScriptPath);
                Assert.Contains(@"set ""SOURCE_DIR=", batchScript);
                Assert.Contains(@"set ""CASE_EXPORT_DIR=", batchScript);
                Assert.Contains(@"cd /d ""%SOURCE_DIR%"" || exit /b 1", batchScript);
                Assert.Contains(@"mklink /J ""%SOURCE_EXPORT_DIR%"" ""%CASE_EXPORT_DIR%""", batchScript);
                Assert.Contains("MSBuild", batchScript);
                Assert.Contains("robocopy", batchScript);
                Assert.Contains("Running FluidX3D from %FLUIDX3D_EXE%", batchScript);
                Assert.Contains(@"bin\FluidX3D.exe", batchScript);
                Assert.Contains(@"bin\Release\FluidX3D.exe", batchScript);
                Assert.Contains(@"bin\x64\Release\FluidX3D.exe", batchScript);

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.Equal(result.BatchScriptPath, result.LaunchScriptPath);
                }
                else
                {
                    Assert.Equal(result.CommandScriptPath, result.LaunchScriptPath);
                }
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public void PrepareCase_ThrowsForInvalidRoughnessReferencePair()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string sourceRoot = Path.Combine(tempRoot, "source");
                string workingRoot = Path.Combine(tempRoot, "working");
                CreateStubFluidX3DSource(sourceRoot);

                FluidX3DAblSettings settings = new FluidX3DAblSettings
                {
                    MemoryMb = 1000,
                    Uref = 5.0,
                    Zref = 1.0,
                    Z0 = 1.0,
                    SimSeconds = 30.0,
                    ExportIntervalSeconds = 10.0
                };

                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    FluidX3DAblWorkflow.PrepareCase(sourceRoot, workingRoot, settings));
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public void PrepareCase_WritesStlVoxelizationWhenStlFilesProvided()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string sourceRoot = Path.Combine(tempRoot, "source");
                string workingRoot = Path.Combine(tempRoot, "working");
                CreateStubFluidX3DSource(sourceRoot);

                FluidX3DAblSettings settings = new FluidX3DAblSettings
                {
                    MemoryMb = 1000,
                    Uref = 5.0,
                    Zref = 10.0,
                    Z0 = 0.1,
                    SimSeconds = 30.0,
                    ExportIntervalSeconds = 10.0,
                    DomainLx = 150.0,
                    DomainLy = 180.0,
                    DomainLz = 90.0
                };
                settings.BuildingStlFiles.Add("building_000.stl");
                settings.BuildingStlFiles.Add("building_001.stl");

                FluidX3DAblPrepareResult result = FluidX3DAblWorkflow.PrepareCase(sourceRoot, workingRoot, settings);

                string setupText = File.ReadAllText(result.SetupPath);
                Assert.Contains("const float si_Lx = 150.0f;", setupText);
                Assert.Contains("const float si_Ly = 180.0f;", setupText);
                Assert.Contains("const float si_Lz = 90.0f;", setupText);
                Assert.Contains("../stl/building_000.stl", setupText);
                Assert.Contains("../stl/building_001.stl", setupText);
                Assert.Contains("voxelize_mesh_on_device(building, TYPE_S | TYPE_X)", setupText);
                Assert.Contains("const bool isSolid = (lbm.flags[n] & TYPE_S) != 0u;", setupText);
                int voxelizeIndex = setupText.IndexOf("voxelize_mesh_on_device(building, TYPE_S | TYPE_X)", StringComparison.Ordinal);
                int initializeIndex = setupText.IndexOf("const bool isSolid = (lbm.flags[n] & TYPE_S) != 0u;", StringComparison.Ordinal);
                Assert.True(
                    voxelizeIndex >= 0 && initializeIndex > voxelizeIndex,
                    "Velocity/boundary initialization should run after voxelization to avoid host field reset.");
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public void PrepareCase_ThrowsWhenWorkingDirectoryIsInsideSource()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string sourceRoot = Path.Combine(tempRoot, "source");
                CreateStubFluidX3DSource(sourceRoot);
                string nestedWorkingRoot = Path.Combine(sourceRoot, "nested-work");

                FluidX3DAblSettings settings = new FluidX3DAblSettings();

                Assert.Throws<InvalidOperationException>(() =>
                    FluidX3DAblWorkflow.PrepareCase(sourceRoot, nestedWorkingRoot, settings));
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public void PrepareCase_UsesInstalledSourceWithoutMirroringIntoWorkingDirectory()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string sourceRoot = Path.Combine(tempRoot, "source");
                string workingRoot = Path.Combine(tempRoot, "working");
                CreateStubFluidX3DSource(sourceRoot);

                FluidX3DAblPrepareResult result = FluidX3DAblWorkflow.PrepareCase(
                    sourceRoot,
                    workingRoot,
                    new FluidX3DAblSettings());

                Assert.Equal(Path.GetFullPath(sourceRoot), result.CaseRoot);
                Assert.False(Directory.Exists(Path.Combine(workingRoot, "FluidX3D")));
                Assert.Equal(Path.Combine(sourceRoot, "bin", "export"), result.ExportDirectory);
                Assert.True(File.Exists(result.CommandScriptPath));
                Assert.True(File.Exists(result.BatchScriptPath));
            }
            finally
            {
                Directory.Delete(tempRoot, true);
            }
        }

        [Fact]
        public void PrepareCase_WritesVelocityInitializationAlongConfiguredFlowDirection()
        {
            string tempRoot = CreateTempDir();
            try
            {
                string sourceRoot = Path.Combine(tempRoot, "source");
                string workingRoot = Path.Combine(tempRoot, "working");
                CreateStubFluidX3DSource(sourceRoot);

                FluidX3DAblSettings settings = new FluidX3DAblSettings
                {
                    FlowDirectionX = 0.0,
                    FlowDirectionY = -1.0
                };

                FluidX3DAblPrepareResult result = FluidX3DAblWorkflow.PrepareCase(sourceRoot, workingRoot, settings);
                string setupText = File.ReadAllText(result.SetupPath);

                Assert.Contains("const float flow_dir_x = 0.0f;", setupText);
                Assert.Contains("const float flow_dir_y = -1.0f;", setupText);
                Assert.Contains("lbm.u.x[n] = flow_dir_x * u_abl;", setupText);
                Assert.Contains("lbm.u.y[n] = flow_dir_y * u_abl;", setupText);
            }
            finally
            {
                Directory.Delete(tempRoot, true);
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
                "FluidX3D",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
