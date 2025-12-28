using EddyLib;
using EddyLib.BCs;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Rhino;
using Rhino.DocObjects;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class OFExecutionTests
    {
        [NotWindowsServerFact]
        public void BuildingGeoFine_HasExpectedTopology()
        {
            // Load the fine building geometry STL from Resources folder
            var resourcePath = @"RhinoPlugin.Test.Xunit\Resources\BuildingGeo_fine.stl";
            Mesh mesh = GeometryHelpers.LoadMergedMesh(resourcePath);

            // Quick sanity checks
            Assert.True(mesh.IsValid);

            // *** Replace with real counts once known ***
            Assert.Equal(12495, mesh.Vertices.Count);
            Assert.Equal(22322, mesh.Faces.Count);
        }

        [NotWindowsServerFact]
        public void BuildingGeo_HasExpectedTopology()
        {
            // Load the building geometry STL from Resources folder
            var resourcePath = @"RhinoPlugin.Test.Xunit\Resources\BuildingGeo.stl";
            Mesh mesh = GeometryHelpers.LoadMergedMesh(resourcePath);

            // Quick sanity checks
            Assert.True(mesh.IsValid);

            // *** Replace with real counts once known ***
            Assert.Equal(233, mesh.Vertices.Count);
            Assert.Equal(172, mesh.Faces.Count);
        }

        [NotWindowsServerFact]
        public void BoxDomainCase_GeneratesAndExecutesSuccessfully()

        {
            // Arrange
            //var caseDir = Path.Combine(Path.GetTempPath(), $"testcase-box-{Guid.NewGuid():N}\\");
            var caseDir = Path.Combine(Path.GetTempPath(),
$"testcase-box-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory and all contents at the beginning for debugging
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            var meshSettings = new OFMeshSettings
            {
                accBuildings = 3,
                accFeatures = 4,
                accGround = 3
            };
            meshSettings.SetDirectories(caseDir);

            var runSettings = new OFRunSettings
            {
                iter = 3000,
                CPUs = 6,
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Optimized
            };

            var windDir = 0;
            var boundaryCondition = new ABL(windDir);

            var bcColl = new BCCollection(boundaryCondition);
            var domBox = new OFBoxDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunBox(domBox, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domBox, meshSettings, runSettings, out var logfileOutput);
            RunFoamSimulation.Run(domBox, meshSettings, runSettings, caseDir);

            var result = RunBatchFileInteractive(caseDir, "run.bat");

            // Assert: check log file contains the expected string
            var logFile = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            Assert.True(File.Exists(logFile), $"Log file not found: {logFile}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 3000", logContent);
            }
        }

        // This version opens a terminal and shows the progress
        public static (bool Success, string Log) RunBatchFileInteractive(string workingDir, string batchFileName)
        {
            var batchFilePath = Path.Combine(workingDir, batchFileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C \"" + batchFilePath + "\"",
                WorkingDirectory = workingDir,
                UseShellExecute = true,
                CreateNoWindow = false // Show the window
                // Do NOT redirect standard output or error!
            };

            var process = Process.Start(startInfo);
            process.WaitForExit();

            // You cannot capture output when UseShellExecute = true and redirection is off
            // But you can still check the result file
            var success = process.ExitCode == 0
                          && File.Exists(Path.Combine(workingDir, "postProcessing", "residuals", "0", "residuals.dat"));

            process.Dispose();
            return (success, "See terminal window for output.");
        }

        [Fact]
        public void CylDomainCase_5_WithProceduralGeometry()
        {
            // Fast test with procedural geometry instead of STL files
            int windDir = 5;
            var caseDir = Path.Combine(Path.GetTempPath(),
                $"testcase-cyl-procedural-{windDir}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            // Assert: Check that key files were generated
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 400", logContent);
            }
        }

        [Fact]
        public void CylDomainCase_45_WithProceduralGeometry()
        {
            // Fast test with procedural geometry instead of STL files
            int windDir = 45;
            var caseDir = Path.Combine(Path.GetTempPath(),
                $"testcase-cyl-procedural-{windDir}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            // Assert: Check that key files were generated
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 400", logContent);
            }
        }

        [Fact]
        public void CylDomainCase_90_WithProceduralGeometry()
        {
            // Fast test with procedural geometry instead of STL files
            int windDir = 90;
            var caseDir = Path.Combine(Path.GetTempPath(),
                $"testcase-cyl-procedural-{windDir}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            // Assert: Check that key files were generated
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 400", logContent);
            }
        }

        [Fact]
        public void CylDomainCase_135_WithProceduralGeometry()
        {
            // Fast test with procedural geometry instead of STL files
            int windDir = 135;
            var caseDir = Path.Combine(Path.GetTempPath(),
                $"testcase-cyl-procedural-{windDir}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            // Assert: Check that key files were generated
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 400", logContent);
            }
        }

        [Fact]
        public void CylDomainCase_180_WithProceduralGeometry()
        {
            // Fast test with procedural geometry instead of STL files
            int windDir = 180;
            var caseDir = Path.Combine(Path.GetTempPath(),
                $"testcase-cyl-procedural-{windDir}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            // Assert: Check that key files were generated
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 400", logContent);
            }
        }

        [Fact]
        public void CylDomainCase_225_WithProceduralGeometry()
        {
            // Fast test with procedural geometry instead of STL files
            int windDir = 225;
            var caseDir = Path.Combine(Path.GetTempPath(),
                $"testcase-cyl-procedural-{windDir}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            // Assert: Check that key files were generated
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 400", logContent);
            }
        }

        [Fact]
        public void CylDomainCase_270_WithProceduralGeometry()
        {
            // Fast test with procedural geometry instead of STL files
            int windDir = 270;
            var caseDir = Path.Combine(Path.GetTempPath(),
                $"testcase-cyl-procedural-{windDir}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            // Assert: Check that key files were generated
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 400", logContent);
            }
        }

        [Fact]
        public void CylDomainCase_315_WithProceduralGeometry()
        {
            // Fast test with procedural geometry instead of STL files
            int windDir = 315;
            var caseDir = Path.Combine(Path.GetTempPath(),
                $"testcase-cyl-procedural-{windDir}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}\\");

            // Clean up the directory
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            // Assert: Check that key files were generated
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");

            // Check that simulation completed successfully
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (File.Exists(logPath))
            {
                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 400", logContent);
            }
        }

        public void GenerateCylDomainCaseWithProceduralGeometry(string caseDir, int windDir)
        {
            // Create procedural building geometry instead of loading STL
            Mesh BuildingMesh = ProceduralGeometry.CreateProceduralBuilding(15, 15, 30); // 15x15x30 meter building

            var meshSettings = new OFMeshSettings
            {
                accBuildings = 1, // Very coarse for fast testing
                accFeatures = 1,  // Very coarse
                accGround = 1,    // Very coarse
                snappySetting = SnappySnapSettings.BlocksSnapping,
                miscSettings = SnappyMiscSettings.Optimized
            };
            meshSettings.SetDirectories(caseDir);

            var runSettings = new OFRunSettings
            {
                iter = 500, // reduced iterations
                CPUs = 8, 
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Default,
                turbModel = TurbModel.kEpsilon
            };

            var bc = new ABL(windDir);
            var bcList = new List<BC> { bc };
            var bcColl = new BCCollection(bcList);

            // Use automatic domain sizing based on building dimensions
            var domCyl = new OFCylDomain(BuildingMesh, new Mesh(), bcColl, coreBlockSize: 8, sizeInnerRect: 0, sizeOuterCirc: 0, sizeHeight: 0);

            Directory.CreateDirectory(caseDir);

            // Generate files and run simulation
            RunBlockMesh.RunCyl(domCyl, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domCyl, meshSettings, runSettings, out var logfileOutput);
            RunFoamSimulation.Run(domCyl, meshSettings, runSettings, caseDir);

            // Run the simulation (simple approach)
            var result = RunBatchFileInteractive(caseDir, "run.bat");
        }

    }
}