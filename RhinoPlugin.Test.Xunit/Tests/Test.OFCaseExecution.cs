using EddyLib;
using EddyLib.BCs;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;
using Rhino;
using Rhino.DocObjects;
using RhinoPlugin.Test.Xunit.Tests;

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
            var stlAbs = Path.Combine(GetSolutionRoot(), solutionRelativePath);
            if (!File.Exists(stlAbs))
                throw new FileNotFoundException($"STL not found: {stlAbs}");

            using (var doc = RhinoDoc.CreateHeadless(null))
            {
                var opts = new FileStlReadOptions();
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
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\.."));
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
        [Fact]
        public void BuildingGeo_HasExpectedTopology()
        {
            // Re-use the helper
            // Replace this line:
            // Mesh mesh = GeometryHelpers.LoadMergedMesh(@"RhinoPlugin.Tests.Xunit\Resources\BuildingGeo.stl");

            // With this:
            var ns = typeof(OFExecutionTests).Namespace;
            var resourcePath = $@"{ns}\Resources\BuildingGeo.stl";
            Mesh mesh = GeometryHelpers.LoadMergedMesh(resourcePath);

            // Quick sanity checks
            Assert.True(mesh.IsValid);

            // *** Replace with real counts once known ***
            Assert.Equal(233, mesh.Vertices.Count);
            Assert.Equal(172, mesh.Faces.Count);
        }

        [Fact]
        public void CylDomainCase_GeneratesAndExecutesSuccessfully()
        {
            // Arrange
            var caseDir = Path.Combine(Path.GetTempPath(), $"testcase-cyl-{Guid.NewGuid():N}\\");

            // Clean up the directory and all contents
            //if (Directory.Exists(caseDir))
            //{
            //    Directory.Delete(caseDir, true);
            //}
            Directory.CreateDirectory(caseDir);

            var ns = typeof(OFExecutionTests).Namespace;
            var resourcePath = $@"{ns}\Resources\BuildingGeo.stl";
            Mesh BuildingMesh = GeometryHelpers.LoadMergedMesh(resourcePath);

            Rectangle3d rect = new Rectangle3d(Plane.WorldXY, 1000.0, 1000.0);
            // Calculate the center point of the rectangle
            Point3d center = rect.Center;

            // Create a translation vector from the center to the origin
            Vector3d moveToOrigin = Point3d.Origin - center;

            // Move the rectangle
            rect.Transform(Transform.Translation(moveToOrigin));

            // Now convert to mesh
            Mesh flatPlate = GeometryHelpers.RectangleToMesh(rect);

            var meshSettings = new OFMeshSettings
            {
                accBuildings = 4,
                accFeatures = 3,
                accGround = 4,
                accBoxRefinement = 3
            };
            meshSettings.SetDirectories(caseDir);
            var runSettings = new OFRunSettings
            {
                iter = 1000,
                CPUs = 8,
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Optimized,
                turbModel = TurbModel.RNGkEpsilon
            };

            var bc = new ABL();
            var bc1 = new ABL(45);
            var bc2 = new ABL(90);
            var bc3 = new ABL(135);
            var bc4 = new ABL(180);
            var bc5 = new ABL(225);
            var bc6 = new ABL(270);
            var bc7 = new ABL(315);

            var bcList = new List<BC> { bc, bc1, bc2, bc3, bc4, bc5, bc6, bc7 };
            var bcColl = new BCCollection(bcList);
            var domCyl = new OFCylDomain(BuildingMesh, flatPlate, bcColl, 15, 40, 519, 80);

            Directory.CreateDirectory(caseDir);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunCyl(domCyl, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domCyl, meshSettings, runSettings, out var logfileOutput);
            RunFoamSimulation.Run(domCyl, meshSettings, runSettings, caseDir);
            var result = RunBatchFileInteractive(caseDir, "run.bat");

            foreach (var boundarycond in bcList)
            {
                // each wind dir gets its own sub-folder, e.g. 0, 45, 90 …
                var logPath = Path.Combine(caseDir, boundarycond.windDir.ToString(), "log");

                Assert.True(File.Exists(logPath), $"Log file not found: {logPath}");

                var logContent = File.ReadAllText(logPath);
                Assert.Contains("Time = 1000", logContent);
            }
        }

        [Fact]
        public void BoxDomainCase_GeneratesAndExecutesSuccessfully()

        {
            // Arrange
            var caseDir = Path.Combine(Path.GetTempPath(), $"testcase-box-{Guid.NewGuid():N}\\");

            // Clean up the directory and all contents at the beginning for debugging
            //if (Directory.Exists(caseDir))
            //{
            //    Directory.Delete(caseDir, true);
            //}
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
            var logFile = Path.Combine(caseDir, windDir.ToString(), "log");
            Assert.True(File.Exists(logFile), $"Log file not found: {logFile}");
            var logContent = File.ReadAllText(logFile);
            Assert.Contains("SIMPLE solution converged", logContent);
        }

        // This version opens a terminal and shows the progress
        private (bool Success, string Log) RunBatchFileInteractive(string workingDir, string batchFileName)
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
    }
}