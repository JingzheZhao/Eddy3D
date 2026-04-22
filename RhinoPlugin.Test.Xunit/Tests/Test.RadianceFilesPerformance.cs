using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using EddyLib;

namespace RhinoPlugin.Test.Xunit.Tests
{
    public class Test_RadianceFilesPerformance
    {
        private readonly ITestOutputHelper _output;

        public Test_RadianceFilesPerformance(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void LoadILL_PerformanceTest()
        {
            string tempFile = Path.GetTempFileName();
            // Create a reasonably large file: 10,000 lines, 100 values each
            int numLines = 10000;
            int numValues = 100;

            try
            {
                using (StreamWriter sw = new StreamWriter(tempFile))
                {
                    for (int i = 0; i < numLines; i++)
                    {
                        sw.Write("2023 1 1 12:00"); // 4 columns to skip
                        for (int j = 0; j < numValues; j++)
                        {
                            sw.Write(" " + (i + j * 0.1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                        sw.WriteLine();
                    }
                }

                // Force GC to get a clean slate for memory measurement
                GC.Collect();
                GC.WaitForPendingFinalizers();
                long startMemory = GC.GetTotalMemory(true);

                Stopwatch stopwatch = Stopwatch.StartNew();

                var result = RadianceFiles.loadILL(tempFile);

                stopwatch.Stop();
                long endMemory = GC.GetTotalMemory(false);

                _output.WriteLine($"Loaded {numLines} lines in {stopwatch.ElapsedMilliseconds} ms");
                _output.WriteLine($"Memory usage approx: {(endMemory - startMemory) / 1024.0 / 1024.0:F2} MB");

                Assert.Equal(numLines, result.Length);
                Assert.Equal(numValues, result[0].Length);
                Assert.Equal(0.0, result[0][0]);
                Assert.Equal(numLines - 1 + (numValues - 1) * 0.1, result[numLines - 1][numValues - 1], 5);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
