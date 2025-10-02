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
using RhinoPlugin.Test.Xunit.Tests;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Xunit;



namespace RhinoPlugin.Test.Xunit.Tests
{
    public static class GeometryHelpers
    {
        /// <summary>
        ///     Loads every mesh contained in an STL, appends them into one mesh,
        ///     cleans it (weld, unify normals, merge coplanar faces) and returns it.
        /// </summary>
        public static Mesh LoadMergedMesh(string solutionRelativePath,
            double weldAngleRadians = Math.PI,
            double coplanarTol = 1e-6)

        {

            var solutionRoot = GetSolutionRoot();
            var stlAbs = Path.Combine(solutionRoot, solutionRelativePath);
            if (!File.Exists(stlAbs))
                throw new FileNotFoundException($"STL not found: {stlAbs}");

            using (var doc = RhinoDoc.CreateHeadless(null))
            {
                var opts = new FileStlReadOptions()
                {
                    STLModelUnits=UnitSystem.Meters

                };
                if (!doc.Import(stlAbs, opts.ToDictionary()))
                    throw new InvalidOperationException("STL import failed.");

                // ---- duplicate every MeshObject (so they survive after Dispose) ----
                var pieces = new List<Mesh>();
                foreach (MeshObject mo in doc.Objects.GetObjectList(ObjectType.Mesh))
                    pieces.Add(((Mesh)mo.Geometry).DuplicateMesh());

                if (pieces.Count == 0)
                    throw new InvalidOperationException("No mesh objects in STL.");

                // ---- append all pieces into one mesh (Rhino-common pattern) ----
                var merged = new Mesh();
                foreach (var part in pieces)
                    merged.Append(part); // :contentReference[oaicite:4]{index=4}

                // ---- clean up ----
                merged.Vertices.CombineIdentical(true, true);
                merged.Weld(weldAngleRadians);
                merged.UnifyNormals();
                merged.Normals.ComputeNormals();
                merged.Compact();

                // Rhino 7: use MergeAllCoplanarFaces to shrink planar quads
                merged.MergeAllCoplanarFaces(coplanarTol); // :contentReference[oaicite:5]{index=5}

                return merged;
            }
        }

        private static string GetSolutionRoot()
        {
            // Navigate up from bin\Debug\net48 (or bin\Release\net48) to solution root
            var baseDir = AppContext.BaseDirectory;
            
            // Keep going up until we find the solution root (where .sln file would be)
            var current = new DirectoryInfo(baseDir);
            while (current != null && current.Name != "Eddy3D")
            {
                current = current.Parent;
            }
            
            if (current == null)
            {
                // Fallback to the old method
#if DEBUG
                return Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\.."));
#else
                return Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\.."));
#endif
            }
            
            return current.FullName;
        }

        public static Mesh RectangleToMesh(Rectangle3d rect)
        {
            Mesh mesh = new Mesh();
            mesh.Vertices.Add(rect.Corner(0));
            mesh.Vertices.Add(rect.Corner(1));
            mesh.Vertices.Add(rect.Corner(2));
            mesh.Vertices.Add(rect.Corner(3));
            mesh.Faces.AddFace(0, 1, 2, 3);
            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }
    }
}

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
            var logContent = File.ReadAllText(logFile);
            Assert.Contains("SIMPLE solution converged", logContent);
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
            Mesh BuildingMesh = CreateProceduralBuilding(15, 15, 30); // 15x15x30 meter building

            var meshSettings = new OFMeshSettings
            {
                accBuildings = 4, // Good balance for COST-compliant domain
                accFeatures = 2,
                accGround = 2, // Ground mesh accuracy
                snappySetting = SnappySnapSettings.BlocksSnapping,
                miscSettings = SnappyMiscSettings.Optimized
            };
            meshSettings.SetDirectories(caseDir);

            var runSettings = new OFRunSettings
            {
                iter = 800, // Reduced iterations for stability
                CPUs = 8, // Reduced CPU usage for stability
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Default,
                turbModel = TurbModel.kEpsilon
            };

            var bc = new ABL(windDir);
            var bcList = new List<BC> { bc };
            var bcColl = new BCCollection(bcList);
            
            // Use automatic domain sizing based on building dimensions
            // Building: 15x15x30m, so H=30m
            // Automatic calculation will determine optimal domain size
            var domCyl = new OFCylDomain(BuildingMesh, new Mesh(), bcColl, coreBlockSize: 8, sizeInnerRect: 0, sizeOuterCirc: 0, sizeHeight: 0);
            
            Directory.CreateDirectory(caseDir);

            // Generate files and run simulation
            RunBlockMesh.RunCyl(domCyl, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domCyl, meshSettings, runSettings, out var logfileOutput);
            RunFoamSimulation.Run(domCyl, meshSettings, runSettings, caseDir);
            
            // Run the simulation (simple approach)
            var result = RunBatchFileInteractive(caseDir, "run.bat");
        }

        private Mesh CreateProceduralBuilding(double width, double depth, double height)
        {
            // Create 3 buildings with different shapes in COMPACT layout
            var mesh = new Mesh();
            
            // Building 1: L-shaped building (main building at center)
            var lShaped = CreateLShapedBuilding(0, 0, 0, width * 3, depth * 3, height * 1);
            mesh.Append(lShaped);
            
            // Building 2: Simple box building - COMPACT RIGHT-FRONT (reduced gap to 5m, closer offset)
            var boxBuilding = CreateBox(width * 3 + 5, depth * 0.5, 0, width * 0.7 * 3, depth * 0.7 * 3, height * 0.8 * 1);
            mesh.Append(boxBuilding);
            
            // Building 3: U-shaped building - COMPACT LEFT-BACK (reduced gap to 5m, closer offset)
            var uShaped = CreateUShapedBuilding(-width * 3 - 5, -depth * 0.5, 0, width * 0.8 * 3, depth * 0.8 * 3, height * 0.9 * 1);
            mesh.Append(uShaped);
            
            mesh.Normals.ComputeNormals();
            mesh.Compact();
            
            return mesh;
        }
        
        private Mesh CreateBox(double x, double y, double z, double width, double depth, double height)
        {
            // Create a simple rectangular box
            var mesh = new Mesh();
            
            // Create 8 corner points
            var corners = new Point3d[]
            {
                new Point3d(x, y, z),                           // Bottom-left-back
                new Point3d(x + width, y, z),                   // Bottom-right-back
                new Point3d(x + width, y + depth, z),           // Bottom-right-front
                new Point3d(x, y + depth, z),                  // Bottom-left-front
                new Point3d(x, y, z + height),                  // Top-left-back
                new Point3d(x + width, y, z + height),          // Top-right-back
                new Point3d(x + width, y + depth, z + height), // Top-right-front
                new Point3d(x, y + depth, z + height)           // Top-left-front
            };
            
            // Add vertices
            for (int i = 0; i < 8; i++)
            {
                mesh.Vertices.Add(corners[i]);
            }
            
            // Add faces (6 faces of a box)
            mesh.Faces.AddFace(0, 1, 2, 3); // Bottom
            mesh.Faces.AddFace(4, 7, 6, 5); // Top
            mesh.Faces.AddFace(0, 4, 5, 1); // Front
            mesh.Faces.AddFace(2, 6, 7, 3); // Back
            mesh.Faces.AddFace(0, 3, 7, 4); // Left
            mesh.Faces.AddFace(1, 5, 6, 2); // Right
            
            return mesh;
        }
        
        private Mesh CreateLShapedBuilding(double x, double y, double z, double width, double depth, double height)
        {
            // Create an L-shaped building by combining two rectangular boxes
            var mesh = new Mesh();
            
            // Main part of L (vertical leg)
            var mainPart = CreateBox(x, y, z, width * 0.6, depth, height);
            mesh.Append(mainPart);
            
            // Horizontal leg of L
            var horizontalPart = CreateBox(x + width * 0.4, y + depth * 0.4, z, width * 0.6, depth * 0.6, height);
            mesh.Append(horizontalPart);
            
            return mesh;
        }
        
        private Mesh CreateUShapedBuilding(double x, double y, double z, double width, double depth, double height)
        {
            // Create a U-shaped building by combining three rectangular boxes
            var mesh = new Mesh();
            
            // Left leg of U
            var leftLeg = CreateBox(x, y, z, width * 0.3, depth, height);
            mesh.Append(leftLeg);
            
            // Right leg of U
            var rightLeg = CreateBox(x + width * 0.7, y, z, width * 0.3, depth, height);
            mesh.Append(rightLeg);
            
            // Back of U
            var backPart = CreateBox(x, y + depth * 0.7, z, width, depth * 0.3, height);
            mesh.Append(backPart);
            
            return mesh;
        }
      
        /// Helpers
        /// 

        public static class WindowsServerDetector
        {
            public static bool IsWindowsServer()
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return false;

                // Prefer 64-bit view to avoid WOW64 redirection; fallback to Default if needed.
                using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    var installType = key == null ? null : key.GetValue("InstallationType") as string;
                    if (!string.IsNullOrEmpty(installType) &&
                        installType.StartsWith("Server", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    var productName = key == null ? null : key.GetValue("ProductName") as string;
                    return !string.IsNullOrEmpty(productName) &&
                           productName.IndexOf("Server", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
        }

        [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
        public sealed class NotWindowsServerFactAttribute : FactAttribute
        {
            public NotWindowsServerFactAttribute()
            {
                if (WindowsServerDetector.IsWindowsServer())
                {
                    Skip = "Skipped on Windows Server.";
                }
            }
        }

    }
}