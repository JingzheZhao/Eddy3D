using EddyLib;
using EddyLib.BCs;
using Rhino.Geometry;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class OFExecutionTests
    {
        [NotWindowsServerFact]
        public void BoxDomainCase_GeneratesAndExecutesSuccessfully()
        {
            // Arrange
            var caseDir = TestFixtures.CreateTestDirectory("testcase-box");

            var meshSettings = TestFixtures.CreateDefaultMeshSettings(caseDir);
            var runSettings = TestFixtures.CreateDefaultRunSettings();

            var windDir = 0;
            var boundaryCondition = new ABL(windDir);

            var bcColl = new BCCollection(boundaryCondition);
            var domBox = new OFBoxDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunBox(domBox, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domBox, meshSettings, runSettings, out _);
            RunFoamSimulation.Run(domBox, meshSettings, runSettings, caseDir);

            _ = RunBatchFileInteractive(caseDir, "run.bat");

            // Assert: check log file contains the expected string
            var logFile = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            Assert.True(File.Exists(logFile), $"Log file not found: {logFile}");

            AssertLogContainsTimeIfPresent(caseDir, windDir, expectedTime: runSettings.iter);
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

        [Theory]
        [InlineData(5)]
        [InlineData(45)]
        [InlineData(90)]
        [InlineData(135)]
        [InlineData(180)]
        [InlineData(225)]
        [InlineData(270)]
        [InlineData(315)]
        public void CylDomainCase_WithProceduralGeometry(int windDir)
        {
            var caseDir = TestFixtures.CreateTestDirectory($"testcase-cyl-procedural-{windDir}");

            // Generate case files with procedural geometry
            GenerateCylDomainCaseWithProceduralGeometry(caseDir, windDir);

            AssertCaseFilesGenerated(caseDir);
            AssertLogContainsTimeIfPresent(caseDir, windDir, expectedTime: 400);
        }

        private static void GenerateCylDomainCaseWithProceduralGeometry(string caseDir, int windDir)
        {
            // Create procedural building geometry instead of loading STL
            Mesh buildingMesh = ProceduralGeometry.CreateProceduralBuilding(15, 15, 30); // 15x15x30 meter building

            var meshSettings = CreateProceduralMeshSettings(caseDir);
            var runSettings = CreateProceduralRunSettings();
            var bcColl = new BCCollection(new ABL(windDir));

            // Use automatic domain sizing based on building dimensions
            var domCyl = new OFCylDomain(buildingMesh, new Mesh(), bcColl, coreBlockSize: 8, sizeInnerRect: 0, sizeOuterCirc: 0, sizeHeight: 0);

            // Generate files and run simulation
            RunBlockMesh.RunCyl(domCyl, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domCyl, meshSettings, runSettings, out _);
            RunFoamSimulation.Run(domCyl, meshSettings, runSettings, caseDir);

            // Run the simulation (simple approach)
            _ = RunBatchFileInteractive(caseDir, "run.bat");
        }

        private static OFMeshSettings CreateProceduralMeshSettings(string caseDir)
        {
            var meshSettings = TestFixtures.CreateDefaultMeshSettings(caseDir);
            meshSettings.accBuildings = 1; // Very coarse for fast testing
            meshSettings.accFeatures = 1;
            meshSettings.accGround = 1;
            meshSettings.snappySetting = SnappySnapSettings.BlocksSnapping;
            meshSettings.miscSettings = SnappyMiscSettings.Optimized;
            return meshSettings;
        }

        private static OFRunSettings CreateProceduralRunSettings()
        {
            var runSettings = TestFixtures.CreateDefaultRunSettings();
            runSettings.iter = 500; // reduced iterations
            runSettings.CPUs = 8;
            runSettings.schemes = fvSchemes.Default;
            return runSettings;
        }

        private static void AssertCaseFilesGenerated(string caseDir)
        {
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");
        }

        private static void AssertLogContainsTimeIfPresent(string caseDir, int windDir, int expectedTime)
        {
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            if (!File.Exists(logPath))
            {
                return;
            }

            var logContent = File.ReadAllText(logPath);
            Assert.Contains($"Time = {expectedTime}", logContent);
        }
    }
}
