using EddyLib;
using EddyLib.BCs;
using EddyLib.Indoor.Dicts;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

using System.IO;
using System.Reflection;

using System.Linq;

using System;
using System.IO;
using System.Linq;

using Rhino;

using Rhino.FileIO;
using Rhino.Geometry;
using Xunit;

using Rhino.DocObjects;
using EddyLib.TestHelpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;

namespace EddyLib.TestHelpers
{
    public static class StlUtils
    {
        /// <summary>
        /// Loads every mesh contained in an STL, appends them into one mesh,
        /// cleans it (weld, unify normals, merge coplanar faces) and returns it.
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
                    merged.Append(part);                      // :contentReference[oaicite:4]{index=4}

                // ---- clean up ----
                merged.Vertices.CombineIdentical(true, true);
                merged.Weld(weldAngleRadians);
                merged.UnifyNormals();
                merged.Normals.ComputeNormals();
                merged.Compact();

                // Rhino 7: use MergeAllCoplanarFaces to shrink planar quads
                merged.MergeAllCoplanarFaces(coplanarTol);   // :contentReference[oaicite:5]{index=5}

                return merged;
            }
        }

        private static string GetSolutionRoot()
            => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\.."));
    }
}

namespace RhinoPlugin.Tests.Xunit
{
    [Collection("Rhino Collection")]
    public class OFExecutionTests
    {
        [Fact]
        public void BuildingGeo_HasExpectedTopology()
        {
            // Re-use the helper
            Mesh mesh = StlUtils.LoadMergedMesh(@"EddyLib\Resources\BuildingGeo.stl");

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

            string caseDir = Path.Combine(Path.GetTempPath(), "testcase-cyl\\");

            // Clean up the directory and all contents
            if (Directory.Exists(caseDir))
            {
                Directory.Delete(caseDir, true);
            }
            Directory.CreateDirectory(caseDir);

            Mesh mm = StlUtils.LoadMergedMesh(@"EddyLib\Resources\BuildingGeo.stl");

            var meshSettings = new OFMeshSettings
            {
                accBuildings = 4,
                accFeatures = 3,
                accGround = 4,
            };
            meshSettings.SetDirectories(caseDir);
            var runSettings = new OFRunSettings
            {
                iter = 3000,
                CPUs = 8,
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Optimized,
                turbModel = TurbModel.RNGkEpsilon,
            };

            var bc = new ABL(0, 5, 10, 1, 0);
            var bc1 = new ABL(45, 5, 10, 1, 0);
            var bc2 = new ABL(90, 5, 10, 1, 0);
            var bc3 = new ABL(135, 5, 10, 1, 0);
            var bc4 = new ABL(180, 5, 10, 1, 0);
            var bc5 = new ABL(225, 5, 10, 1, 0);
            var bc6 = new ABL(270, 5, 10, 1, 0);
            var bc7 = new ABL(315, 5, 10, 1, 0);

            var bcList = new List<BC>() { bc, bc1, bc2, bc3, bc4, bc5, bc6, bc7 };
            var bcColl = new BCCollection(bcList);

            var domCyl = new OFCylDomain(Setup.SetUpBuildingMesh(), mm, bcColl, 15, 40, 519, 80);

            Directory.CreateDirectory(caseDir);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunCyl(domCyl, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domCyl, meshSettings, runSettings, out string logfileOutput);
            RunFoamSimulation.Run(domCyl, meshSettings, runSettings, caseDir);
            var result = RunBatchFileInteractive(caseDir, "run.bat");

            foreach (var boundarycond in bcList)
            {
                // each wind dir gets its own sub-folder, e.g. 0, 45, 90 …
                string logPath = Path.Combine(caseDir, boundarycond.windDir.ToString(), "log");

                Assert.True(File.Exists(logPath), $"Log file not found: {logPath}");

                string logContent = File.ReadAllText(logPath);
                Assert.Contains("SIMPLE solution converged", logContent);
            }
        }

        [Fact]
        public void BoxDomainCase_GeneratesAndExecutesSuccessfully()

        {
            // Arrange
            string caseDir = Path.Combine(Path.GetTempPath(), "testcase-box\\");

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
                accGround = 3,
            };
            meshSettings.SetDirectories(caseDir);

            var runSettings = new OFRunSettings
            {
                iter = 3000,
                CPUs = 6,
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Optimized,
            };

            var windDir = 0;
            var boundaryCondition = new ABL(windDir, 5, 10, 1, 0);

            var bcColl = new BCCollection(boundaryCondition);
            var domBox = new OFBoxDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunBox(domBox, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domBox, meshSettings, runSettings, out string logfileOutput);
            RunFoamSimulation.Run(domBox, meshSettings, runSettings, caseDir);

            var result = RunBatchFileInteractive(caseDir, "run.bat");

            // Assert: check log file contains the expected string
            string logFile = Path.Combine(caseDir, windDir.ToString(), "log");
            Assert.True(File.Exists(logFile), $"Log file not found: {logFile}");
            string logContent = File.ReadAllText(logFile);
            Assert.Contains("SIMPLE solution converged", logContent);
        }

        // This version opens a terminal and shows the progress
        private (bool Success, string Log) RunBatchFileInteractive(string workingDir, string batchFileName)
        {
            string batchFilePath = Path.Combine(workingDir, batchFileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C \"" + batchFilePath + "\"",
                WorkingDirectory = workingDir,
                UseShellExecute = true,
                CreateNoWindow = false // Show the window
                                       // Do NOT redirect standard output or error!
            };

            Process process = Process.Start(startInfo);
            process.WaitForExit();

            // You cannot capture output when UseShellExecute = true and redirection is off
            // But you can still check the result file
            bool success = process.ExitCode == 0
                && File.Exists(Path.Combine(workingDir, "postProcessing", "residuals", "0", "residuals.dat"));

            process.Dispose();
            return (success, "See terminal window for output.");
        }
    }
}