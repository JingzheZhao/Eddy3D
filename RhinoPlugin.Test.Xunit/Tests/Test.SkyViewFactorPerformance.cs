using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using EddyLib.Radiation;

namespace RhinoPlugin.Test.Xunit.Tests
{
    public class Test_SkyViewFactorPerformance
    {
        private readonly ITestOutputHelper _output;

        public Test_SkyViewFactorPerformance(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void LoadResultFile_ShouldCountHitsCorrectly()
        {
            // Arrange
            string tempFile = Path.GetTempFileName();
            int raysPerSensor = 5;
            int numSensors = 100000; // Increased to 100k for performance testing
            int expectedHitsPerSensor = 2;

            // Create file
            using (StreamWriter sw = new StreamWriter(tempFile))
            {
                for (int i = 0; i < numSensors; i++)
                {
                    sw.WriteLine("* hit");
                    sw.WriteLine("  * hit with space");
                    sw.WriteLine("1.234 2.345 3.456");
                    sw.WriteLine("0.000 0.000 0.000");
                    sw.WriteLine(" miss");
                }
            }

            try
            {
                // Act
                Stopwatch sw = Stopwatch.StartNew();
                var results = SkyViewFactor.LoadResultFile(raysPerSensor, tempFile, true);
                sw.Stop();

                _output.WriteLine($"Processed {numSensors} sensors in {sw.ElapsedMilliseconds} ms");

                // Assert
                Assert.Equal(numSensors, results.Length);
                // Verify first and last to ensure integrity
                Assert.Equal(expectedHitsPerSensor, results[0]);
                Assert.Equal(expectedHitsPerSensor, results[numSensors - 1]);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}
