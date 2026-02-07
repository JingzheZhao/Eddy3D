using EddyLib;
using EddyLib.BCs;
using EddyLib.Indoor;
using EddyLib.Indoor.FunctionObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    [Trait("Category", "Execution")]
    public class OFExecutionTests
    {
        private readonly ITestOutputHelper _output;

        public OFExecutionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Gets the recommended CPU count for tests: 75% of available cores, minimum 1, default 8 if detection fails.
        /// </summary>
        private static int GetTestCpuCount()
        {
            try
            {
                int availableCores = System.Environment.ProcessorCount;
                if (availableCores > 0)
                {
                    int cpuCount = (int)Math.Ceiling(availableCores * 0.75);
                    return Math.Max(1, cpuCount); // Ensure at least 1 CPU
                }
            }
            catch
            {
                // Fall through to default
            }

            return 8; // Default fallback
        }

        [NotWindowsServerFact]
        public void BoxDomainCase_GeneratesAndExecutesSuccessfully()
        {
            // Arrange
            var caseDir = TestFixtures.CreateTestDirectory("testcase-box");

            var meshSettings = TestFixtures.CreateDefaultMeshSettings(caseDir);
            var runSettings = TestFixtures.CreateDefaultRunSettings();
            runSettings.CPUs = GetTestCpuCount();
            _output.WriteLine($"Using {runSettings.CPUs} CPUs (75% of {Environment.ProcessorCount} available cores)");

            var windDir = 0;
            var boundaryCondition = new ABL(windDir);

            var bcColl = new BCCollection(boundaryCondition);
            var domBox = new OFBoxDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunBox(domBox, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domBox, meshSettings, runSettings, out _);
            RunFoamSimulation.Run(domBox, meshSettings, runSettings, caseDir);

            _ = RunBatchFileInteractive(caseDir, Path.Combine("Scripts", "run.bat"));

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

        [NotWindowsServerTheory]
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
            AssertLogContainsTimeIfPresent(caseDir, windDir, expectedTime: 500);
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
            _ = RunBatchFileInteractive(caseDir, Path.Combine("Scripts", "run.bat"));
        }

        [NotWindowsServerFact]
        public void IndoorSimpleCase_GeneratesAndExecutesSuccessfully()
        {
            // Arrange
            var caseDir = TestFixtures.CreateTestDirectory("testcase-indoor-simple");

            // Use procedural geometry to keep the indoor execution test independent
            // from STL import plugins in headless Rhino test hosts.
            var (envelopeMesh, inletMesh, outletMesh) = CreateIndoorSimpleRoomGeometry();

            // Boundary Conditions
            // Envelope: 20C, Refinement 2
            var walls = new List<IndoorBC.Wall>
            {
                new IndoorBC.Wall(envelopeMesh, 2, 20.0) { Name = "Envelope" }
            };

            // Inlet: 20C, 1 m/s (Assuming X direction for now), Refinement 2
            var inlets = new List<IndoorBC.Inlet>
            {
                new IndoorBC.Inlet(inletMesh, 20.0, 2, new Vector3d(0, 1, 0)) { Name = "Inlet" }
            };

            // Outlet: Refinement 2
            var outlets = new List<IndoorBC.Outlet>
            {
                new IndoorBC.Outlet(outletMesh, 2) { Name = "Outlet" }
            };

            // Setup Simulation Parameters
            double cellSize = 0.2; // Meters - Updated as per user request
            int endTime = 500;
            int cpus = GetTestCpuCount();

            // Point inside domain - using centroid of envelope as a guess, typically indoor geometry is centered or simple enough
            var bbox = envelopeMesh.GetBoundingBox(true);
            var pointInside = bbox.Center;

            _output.WriteLine($"Using {cpus} CPUs (75% of {Environment.ProcessorCount} available cores)");
            _output.WriteLine($"Meters BBox: {bbox.Min} to {bbox.Max}");
            _output.WriteLine($"PointInside: {pointInside}");
            // Function Objects (None for this simple test)
            var fos = new List<FunctionObject>();

            // Act
            // IndoorDomain constructor generates all files
            var domain = new IndoorDomain(
                endTime,
                caseDir,
                cellSize,
                pointInside,
                walls,
                inlets,
                outlets,
                fos,
                cpus
            );

            // Assert: Check if critical files were created
            AssertIndoorCaseFilesGenerated(caseDir);

            // Execute the batch file
            _output.WriteLine("Running simulation batch file...");
            var (success, log) = RunBatchFileInteractive(caseDir, "run_all.bat");

            _output.WriteLine($"Batch execution completed. Success: {success}");
            _output.WriteLine($"Log info: {log}");

            // Verify simulation completed by checking log file
            AssertIndoorSimulationCompleted(caseDir, endTime);

            // Create case.foam
            File.Create(Path.Combine(caseDir, "case.foam")).Dispose();

            _output.WriteLine($"Indoor Simulation Case completed successfully at: {caseDir}");
        }

        private static (Mesh envelope, Mesh inlet, Mesh outlet) CreateIndoorSimpleRoomGeometry()
        {
            // Room: 10m x 10m x 3m, with inlet at X=0 and outlet at X=10.
            var p0 = new Point3d(0, 0, 0);
            var p1 = new Point3d(10, 0, 0);
            var p2 = new Point3d(10, 10, 0);
            var p3 = new Point3d(0, 10, 0);
            var p4 = new Point3d(0, 0, 3);
            var p5 = new Point3d(10, 0, 3);
            var p6 = new Point3d(10, 10, 3);
            var p7 = new Point3d(0, 10, 3);

            var mp = new MeshingParameters();

            var inletSrf = NurbsSurface.CreateFromCorners(p0, p3, p7, p4);
            var inletMesh = Mesh.CreateFromBrep(inletSrf.ToBrep(), mp)[0];

            var outletSrf = NurbsSurface.CreateFromCorners(p1, p2, p6, p5);
            var outletMesh = Mesh.CreateFromBrep(outletSrf.ToBrep(), mp)[0];

            var envelopeMesh = new Mesh();
            envelopeMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(p0, p1, p2, p3).ToBrep(), mp)[0]); // floor
            envelopeMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(p4, p5, p6, p7).ToBrep(), mp)[0]); // ceiling
            envelopeMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(p0, p1, p5, p4).ToBrep(), mp)[0]); // wall y=0
            envelopeMesh.Append(Mesh.CreateFromBrep(NurbsSurface.CreateFromCorners(p3, p2, p6, p7).ToBrep(), mp)[0]); // wall y=10

            return (envelopeMesh, inletMesh, outletMesh);
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
            runSettings.CPUs = GetTestCpuCount();
            runSettings.schemes = fvSchemes.Default;
            return runSettings;
        }

        private static void AssertCaseFilesGenerated(string caseDir)
        {
            var blockMeshDict = Path.Combine(caseDir, "mesh", "system", "blockMeshDict");
            var snappyHexMeshDict = Path.Combine(caseDir, "mesh", "system", "snappyHexMeshDict");
            var controlDict = Path.Combine(caseDir, "mesh", "system", "controlDict");
            var runBat = Path.Combine(caseDir, "Scripts", "run.bat");

            Assert.True(File.Exists(blockMeshDict), $"blockMeshDict not found: {blockMeshDict}");
            Assert.True(File.Exists(snappyHexMeshDict), $"snappyHexMeshDict not found: {snappyHexMeshDict}");
            Assert.True(File.Exists(controlDict), $"controlDict not found: {controlDict}");
            Assert.True(File.Exists(runBat), $"run.bat not found: {runBat}");
        }

        private static void AssertLogContainsTimeIfPresent(string caseDir, int windDir, int expectedTime)
        {
            var logPath = Path.Combine(caseDir, windDir.ToString(), "simpleFoam.log");
            Assert.True(File.Exists(logPath), $"Log file not found: {logPath}. Simulation may have been cancelled or failed to start.");

            var logContent = File.ReadAllText(logPath);
            Assert.Contains($"Time = {expectedTime}", logContent);
        }

        private static void AssertIndoorCaseFilesGenerated(string caseDir)
        {
            var systemDir = Path.Combine(caseDir, "system");

            Assert.True(File.Exists(Path.Combine(systemDir, "blockMeshDict")), "blockMeshDict not found");
            Assert.True(File.Exists(Path.Combine(systemDir, "snappyHexMeshDict")), "snappyHexMeshDict not found");
            Assert.True(File.Exists(Path.Combine(systemDir, "controlDict")), "controlDict not found");
            Assert.True(File.Exists(Path.Combine(caseDir, "run_all.bat")), "run_all.bat not found");
        }

        private static void AssertIndoorSimulationCompleted(string caseDir, int expectedEndTime)
        {
            // Check for the buoyantSimpleFoam log file
            var logPath = Path.Combine(caseDir, "buoyantSimpleFoam.log");
            Assert.True(File.Exists(logPath),
                $"Log file not found: {logPath}. Simulation may have failed to start or was cancelled.");

            // Read log content
            var logContent = File.ReadAllText(logPath);

            // Check if simulation reached the expected end time
            // OpenFOAM outputs "Time = XXX" at each iteration
            var timeReachedPattern = $"Time = {expectedEndTime}";
            Assert.True(logContent.Contains(timeReachedPattern),
                $"Simulation did not reach expected end time ({expectedEndTime}). Check log file: {logPath}");

            // Additionally check for "End" which OpenFOAM outputs when finishing successfully
            Assert.True(logContent.Contains("End"),
                $"Simulation log does not contain 'End' statement. Simulation may have failed. Check log file: {logPath}");
        }
    }
}
