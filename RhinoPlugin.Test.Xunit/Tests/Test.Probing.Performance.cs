using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Xunit;
using EddyLib;
using Rhino.Geometry;
using Xunit.Abstractions;

namespace RhinoPlugin.Test.Xunit
{
    public class ProbingPerformanceTests
    {
        private readonly ITestOutputHelper output;

        public ProbingPerformanceTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        [Trait("Category", "Performance")]
        public void Benchmark_ParseVectors_LargeFile()
        {
            var root = TestFixtures.CreateTestDirectory("testcase-probing-perf");
            try
            {
                const int iter = 100;
                var probeName = "probe-perf";
                var fieldName = "U";
                const int probeCount = 10;
                const int timeSteps = 500000; // Larger number of time steps

                var dir = Path.Combine(root, "postProcessing", probeName, iter.ToString());
                Directory.CreateDirectory(dir);
                // Create a dummy time step directory in the root so GetLatestIteration finds it
                Directory.CreateDirectory(Path.Combine(root, iter.ToString()));
                var filePath = Path.Combine(dir, fieldName);

                output.WriteLine($"Generating large probe file at {filePath}...");
                GenerateLargeProbeFile(filePath, probeCount, timeSteps);
                output.WriteLine("File generation complete.");

                var points = new List<Point3d>();
                for (int i = 0; i < probeCount; i++) points.Add(new Point3d(i, 0, 0));

                var ofField = new OFField(fieldName, probeName, 1);
                var res = new OFResult(null, new OFRunSettings(endTime: iter), null, root);

                // Run the test
                var sw = Stopwatch.StartNew();
                // Probing constructor calls ParseFromOpenFOAMResult -> ParseVectors
                // passing rerun: true forces it to parse from file instead of cache
                var probing = new Probing(points, root, root, ofField, res, rerun: true, currWindDir: "0");
                sw.Stop();

                output.WriteLine($"Parsing time: {sw.ElapsedMilliseconds} ms");

                Assert.NotNull(probing.ResultVec);
                Assert.Equal(probeCount, probing.ResultVec.Length);
            }
            finally
            {
                TestFixtures.CleanupTestDirectory(root);
            }
        }

        private void GenerateLargeProbeFile(string filePath, int probeCount, int timeSteps)
        {
            using (var writer = new StreamWriter(filePath))
            {
                // Write header
                for (int i = 0; i < probeCount; i++)
                {
                    writer.WriteLine($"# Probe {i} ({i} {i} {i})");
                }
                writer.Write("# Time");
                for (int i = 0; i < probeCount; i++)
                {
                    writer.Write($" {i}");
                }
                writer.WriteLine();

                // Write data
                var sb = new StringBuilder();
                for (int t = 0; t < timeSteps; t++)
                {
                    sb.Clear();
                    sb.Append(t * 0.1); // Time
                    for (int i = 0; i < probeCount; i++)
                    {
                        sb.Append($" ({t + i} {t + i + 1} {t + i + 2})");
                    }
                    writer.WriteLine(sb.ToString());
                }
            }
        }
    }
}
