using EddyLib.OpenFOAM;
using System;
using System.IO;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "OpenFOAM")]
    public class Test_OpenFOAMLogParser
    {
        [Fact]
        public void ParseSimulationLog_ReadsCorrectRun_FromMultiRunLog()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D-Tests", "OpenFOAMLogParser", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var logPath = Path.Combine(tempDir, "simpleFoam.log");

            try
            {
                var lines = new List<string>();

                // Run 1: Completed
                lines.Add("Create time");
                lines.Add("Time = 1");
                lines.Add("ExecutionTime = 10 s");
                lines.Add("Time = 10");
                lines.Add("ExecutionTime = 100 s");
                lines.Add("End");

                // Run 2: In progress
                lines.Add("Create time");
                lines.Add("Time = 1");
                lines.Add("ExecutionTime = 1 s"); // Much faster
                lines.Add("Time = 5");
                lines.Add("ExecutionTime = 5 s");

                File.WriteAllLines(logPath, lines);

                var options = new OpenFOAMLogParseOptions { TotalIterations = 100 };
                var status = OpenFOAMLogParser.ParseSimulationLog(logPath, options);

                Assert.True(status.HasLog);
                Assert.False(status.HasError);
                Assert.False(status.IsFinished, "Should not be finished based on second run");
                Assert.Equal(5, status.CurrentIteration);
                Assert.Equal(5, status.ExecutionTimeSeconds);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void ParseMeshingLog_ReadsCorrectRun_FromMultiRunLog()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D-Tests", "OpenFOAMLogParser", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var logPath = Path.Combine(tempDir, "snappyHexMesh.log");

            try
            {
                var lines = new List<string>();

                // Run 1: Morphing
                lines.Add("Create time");
                lines.Add("Morph iteration 1");
                lines.Add("Finished meshing");

                // Run 2: Morphing in progress
                lines.Add("Create time");
                lines.Add("Morph iteration 2");

                File.WriteAllLines(logPath, lines);

                var status = OpenFOAMLogParser.ParseMeshingLog(logPath);

                Assert.True(status.HasLog);
                Assert.False(status.IsFinished, "Should not be finished based on second run");
                Assert.Equal(2, status.MorphIteration);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void ParseMeshingWorkflow_CombinesBlockSurfaceAndSnappyLogs()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D-Tests", "OpenFOAMLogParser", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                File.WriteAllLines(Path.Combine(tempDir, "blockMesh.log"), new[]
                {
                    "Create time",
                    "Writing polyMesh",
                    "End"
                });

                File.WriteAllLines(Path.Combine(tempDir, "surfaceFeatures.log"), new[]
                {
                    "Create time",
                    "End"
                });

                File.WriteAllLines(Path.Combine(tempDir, "snappyHexMesh.log"), new[]
                {
                    "Create time",
                    "Refinement phase",
                    "Morphing phase",
                    "Snapping to features in 15 iterations ...",
                    "Morph iteration 7",
                    "--> FOAM Warning : Displacement points through surrounding patch faces",
                    "Moved mesh in = 0.73 s"
                });

                var status = OpenFOAMLogParser.ParseMeshingWorkflow(tempDir);

                Assert.True(status.HasLog);
                Assert.False(status.HasError);
                Assert.False(status.IsFinished);
                Assert.Equal("snappyHexMesh", status.StepName);
                Assert.Equal("Morphing", status.Phase);
                Assert.Equal(7, status.MorphIteration);
                Assert.Equal(15, status.MorphIterationsTotal);
                Assert.Equal(1, status.WarningCount);
                Assert.True(status.Progress > 0.6);
                Assert.True(status.Progress < 1.0);
                Assert.NotEmpty(status.LogPath);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void ParseMeshingWorkflow_FinishesWhenSnappyFinished()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D-Tests", "OpenFOAMLogParser", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                File.WriteAllLines(Path.Combine(tempDir, "snappyHexMesh.log"), new[]
                {
                    "Create time",
                    "Morphing phase",
                    "Snapping to features in 15 iterations ...",
                    "Morph iteration 14",
                    "Checking final mesh ...",
                    "Finished meshing without any errors",
                    "End"
                });

                var status = OpenFOAMLogParser.ParseMeshingWorkflow(tempDir);

                Assert.True(status.HasLog);
                Assert.True(status.IsFinished);
                Assert.Equal(1.0, status.Progress);
                Assert.Equal("done", OpenFOAMStatusFormatter.FormatRemainingTime(status));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void ParseMeshingWorkflow_IgnoresStaleSnappyLogWhenBlockMeshRestarted()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D-Tests", "OpenFOAMLogParser", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var snappyPath = Path.Combine(tempDir, "snappyHexMesh.log");
                File.WriteAllLines(snappyPath, new[]
                {
                    "Create time",
                    "Finished meshing without any errors",
                    "End"
                });
                File.SetLastWriteTimeUtc(snappyPath, DateTime.UtcNow.AddMinutes(-5));

                var blockPath = Path.Combine(tempDir, "blockMesh.log");
                File.WriteAllLines(blockPath, new[]
                {
                    "Create time",
                    "Writing polyMesh"
                });
                File.SetLastWriteTimeUtc(blockPath, DateTime.UtcNow);

                var status = OpenFOAMLogParser.ParseMeshingWorkflow(tempDir);

                Assert.True(status.HasLog);
                Assert.False(status.IsFinished);
                Assert.Equal("blockMesh", status.StepName);
                Assert.True(status.Progress > 0.0);
                Assert.True(status.Progress < 0.2);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
