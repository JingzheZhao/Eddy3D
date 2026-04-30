using EddyLib;
using EddyLib.BCs;
using EddyLib.Indoor;
using EddyLib.Indoor.FunctionObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        /// Gets the recommended CPU count for execution tests: 75% of available cores,
        /// with EDDY3D_TEST_CPUS available for constrained CI agents.
        /// </summary>
        private static int GetTestCpuCount()
        {
            string overrideValue = Environment.GetEnvironmentVariable("EDDY3D_TEST_CPUS");
            if (int.TryParse(overrideValue, out int overridden) && overridden > 0)
            {
                return overridden;
            }

            try
            {
                int availableCores = Environment.ProcessorCount;
                if (availableCores > 0)
                {
                    return Math.Max(1, (int)Math.Ceiling(availableCores * 0.75));
                }
            }
            catch
            {
                // Fall through to the conservative default.
            }

            return 1;
        }

        [RequiresOpenFoamExecutionFact]
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
            var logFile = Path.Combine(caseDir, windDir.ToString(), "foamRun.log");
            Assert.True(File.Exists(logFile), $"Log file not found: {logFile}");

            AssertLogContainsTimeIfPresent(caseDir, windDir, expectedTime: runSettings.endTime);

            var residualPlot = CreateResidualPlotPng(caseDir, windDir);
            Assert.True(File.Exists(residualPlot), $"Residual plot not found: {residualPlot}");
        }

        [RequiresOpenFoamExecutionFact]
        public void BoxDomainCase_WithSimpleC_GeneratesAndExecutesSuccessfully()
        {
            // Arrange
            var caseDir = TestFixtures.CreateTestDirectory("testcase-box-simplec");

            var meshSettings = TestFixtures.CreateDefaultMeshSettings(caseDir);
            var runSettings = TestFixtures.CreateDefaultRunSettings();
            runSettings.CPUs = GetTestCpuCount();
            runSettings.simpleConsistent = true;
            _output.WriteLine($"Using {runSettings.CPUs} CPUs (75% of {Environment.ProcessorCount} available cores)");

            var windDir = 0;
            var boundaryCondition = new ABL(windDir);

            var bcColl = new BCCollection(boundaryCondition);
            var domBox = new OFBoxDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunBox(domBox, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domBox, meshSettings, runSettings, out _);
            RunFoamSimulation.Run(domBox, meshSettings, runSettings, caseDir);

            var fvSolutionPath = Path.Combine(caseDir, windDir.ToString(), "system", "fvSolution");
            Assert.True(File.Exists(fvSolutionPath), $"fvSolution not found: {fvSolutionPath}");
            Assert.Contains("consistent      yes;", File.ReadAllText(fvSolutionPath));

            _ = RunBatchFileInteractive(caseDir, Path.Combine("Scripts", "run.bat"));

            // Assert: check log file contains the expected string
            var logFile = Path.Combine(caseDir, windDir.ToString(), "foamRun.log");
            Assert.True(File.Exists(logFile), $"Log file not found: {logFile}");

            AssertLogContainsTimeIfPresent(caseDir, windDir, expectedTime: runSettings.endTime);

            var residualPlot = CreateResidualPlotPng(caseDir, windDir);
            Assert.True(File.Exists(residualPlot), $"Residual plot not found: {residualPlot}");
        }

        // Runs a batch file headless and captures output for diagnostics.
        public static (bool Success, string Log) RunBatchFileInteractive(string workingDir, string batchFileName)
        {
            var batchFilePath = Path.Combine(workingDir, batchFileName);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(batchFilePath);

            var logBuilder = new StringBuilder();
            using var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logBuilder.AppendLine(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    logBuilder.AppendLine("[stderr] " + e.Data);
                }
            };

            if (!process.Start())
            {
                return (false, "Failed to start batch process.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            var success = process.ExitCode == 0;
            if (!success)
            {
                logBuilder.AppendLine($"[exit] {process.ExitCode}");
            }

            return (success, logBuilder.ToString());
        }

        [RequiresOpenFoamExecutionTheory]
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

        [RequiresOpenFoamExecutionFact]
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
            return meshSettings;
        }

        private static OFRunSettings CreateProceduralRunSettings()
        {
            var runSettings = TestFixtures.CreateDefaultRunSettings();
            runSettings.endTime = 500; // reduced iterations
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
            var logPath = Path.Combine(caseDir, windDir.ToString(), "foamRun.log");
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
            // Check for the foamRun log file
            var logPath = Path.Combine(caseDir, "foamRun.log");
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

        private static string CreateResidualPlotPng(string caseDir, int windDir)
        {
            string windDirPath = Path.Combine(caseDir, windDir.ToString());
            string residualsPath = EddyLib.Strings.PlotResiduals.FindResidualsDat(windDirPath);
            string outputPath = Path.Combine(caseDir, windDir.ToString(), "residuals.png");

            Assert.True(File.Exists(residualsPath), $"residuals.dat not found under: {Path.Combine(windDirPath, "postProcessing", "residuals")}");

            string[] lines = File.ReadAllLines(residualsPath);
            var fieldNames = new List<string>();
            var xValues = new List<double>();
            var series = new List<List<double>>();

            foreach (string raw in lines)
            {
                string line = raw?.Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    string header = line.TrimStart('#').Trim();
                    if (header.StartsWith("Time", StringComparison.OrdinalIgnoreCase))
                    {
                        var tokens = SplitWhitespace(header);
                        fieldNames = tokens.Skip(1).ToList();
                        series = fieldNames.Select(_ => new List<double>()).ToList();
                    }

                    continue;
                }

                var cols = SplitWhitespace(line);
                if (cols.Length < 2)
                {
                    continue;
                }

                if (!double.TryParse(cols[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double time))
                {
                    continue;
                }

                xValues.Add(time);
                int available = Math.Min(series.Count, cols.Length - 1);

                for (int i = 0; i < available; i++)
                {
                    if (double.TryParse(cols[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double val) && val > 0)
                    {
                        series[i].Add(val);
                    }
                    else
                    {
                        series[i].Add(double.NaN);
                    }
                }

                for (int i = available; i < series.Count; i++)
                {
                    series[i].Add(double.NaN);
                }
            }

            Assert.True(xValues.Count > 1, $"Not enough residual points in: {residualsPath}");
            Assert.True(series.Count > 0, $"No residual fields found in: {residualsPath}");

            double xMin = xValues.First();
            double xMax = xValues.Last();
            if (Math.Abs(xMax - xMin) < 1e-12)
            {
                xMax = xMin + 1.0;
            }

            double yMinLog = double.PositiveInfinity;
            double yMaxLog = double.NegativeInfinity;

            foreach (var s in series)
            {
                foreach (double v in s)
                {
                    if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0)
                    {
                        continue;
                    }

                    double yLog = Math.Log10(v);
                    yMinLog = Math.Min(yMinLog, yLog);
                    yMaxLog = Math.Max(yMaxLog, yLog);
                }
            }

            Assert.False(double.IsInfinity(yMinLog) || double.IsInfinity(yMaxLog), $"Could not determine Y range from: {residualsPath}");
            if (Math.Abs(yMaxLog - yMinLog) < 1e-12)
            {
                yMaxLog = yMinLog + 1.0;
            }

            const int width = 1600;
            const int height = 900;
            const int left = 120;
            const int right = 260;
            const int top = 70;
            const int bottom = 110;

            var palette = new[]
            {
                Color.FromArgb(56, 88, 249),
                Color.FromArgb(245, 108, 66),
                Color.FromArgb(46, 184, 125),
                Color.FromArgb(160, 88, 255),
                Color.FromArgb(209, 163, 0),
                Color.FromArgb(240, 78, 152),
                Color.FromArgb(0, 165, 207)
            };

            using (var bmp = new Bitmap(width, height))
            using (var g = Graphics.FromImage(bmp))
            using (var gridPen = new Pen(Color.FromArgb(225, 225, 225), 1f))
            using (var axisPen = new Pen(Color.FromArgb(150, 150, 150), 1.2f))
            using (var textBrush = new SolidBrush(Color.FromArgb(70, 70, 70)))
            using (var titleFont = new Font("Arial", 16, FontStyle.Bold))
            using (var axisFont = new Font("Arial", 10))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);

                var plot = new RectangleF(left, top, width - left - right, height - top - bottom);

                for (int i = 0; i <= 4; i++)
                {
                    float y = plot.Top + (i / 4f) * plot.Height;
                    g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
                }

                for (int i = 0; i <= 4; i++)
                {
                    float x = plot.Left + (i / 4f) * plot.Width;
                    g.DrawLine(gridPen, x, plot.Top, x, plot.Bottom);
                }

                g.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
                g.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

                g.DrawString("OpenFOAM Residuals", titleFont, textBrush, new PointF(left, 20));
                g.DrawString("Iteration", axisFont, textBrush, new PointF(plot.Left + plot.Width * 0.5f - 25, height - 42));
                g.DrawString("Residual (log10)", axisFont, textBrush, new PointF(20, plot.Top + plot.Height * 0.5f - 10));

                for (int i = 0; i <= 4; i++)
                {
                    double xv = xMin + (xMax - xMin) * (i / 4.0);
                    float px = plot.Left + (float)((xv - xMin) / (xMax - xMin) * plot.Width);
                    g.DrawString(((int)Math.Round(xv)).ToString(CultureInfo.InvariantCulture), axisFont, textBrush, new PointF(px - 16, plot.Bottom + 8));
                }

                for (int i = 0; i <= 4; i++)
                {
                    double yvLog = yMaxLog - (yMaxLog - yMinLog) * (i / 4.0);
                    float py = plot.Top + (float)(i / 4.0 * plot.Height);
                    g.DrawString("1e" + ((int)Math.Round(yvLog)).ToString(CultureInfo.InvariantCulture), axisFont, textBrush, new PointF(38, py - 7));
                }

                float legendX = plot.Right + 20;
                float legendY = plot.Top;

                for (int s = 0; s < series.Count; s++)
                {
                    var points = new List<PointF>();
                    for (int i = 0; i < xValues.Count && i < series[s].Count; i++)
                    {
                        double v = series[s][i];
                        if (double.IsNaN(v) || v <= 0)
                        {
                            continue;
                        }

                        double yLog = Math.Log10(v);
                        float px = plot.Left + (float)((xValues[i] - xMin) / (xMax - xMin) * plot.Width);
                        float py = plot.Bottom - (float)((yLog - yMinLog) / (yMaxLog - yMinLog) * plot.Height);
                        points.Add(new PointF(px, py));
                    }

                    if (points.Count > 1)
                    {
                        using (var pen = new Pen(palette[s % palette.Length], s == 0 ? 2.4f : 1.8f))
                        {
                            g.DrawLines(pen, points.ToArray());
                        }
                    }

                    string name = s < fieldNames.Count ? fieldNames[s] : $"f{s + 1}";
                    using (var pen = new Pen(palette[s % palette.Length], 2f))
                    {
                        g.DrawLine(pen, legendX, legendY + 7, legendX + 18, legendY + 7);
                    }
                    g.DrawString(name, axisFont, textBrush, new PointF(legendX + 24, legendY));
                    legendY += 20;
                }

                bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            return outputPath;
        }

        private static string[] SplitWhitespace(string input)
        {
            return input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
