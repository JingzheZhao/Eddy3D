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
    }
}
