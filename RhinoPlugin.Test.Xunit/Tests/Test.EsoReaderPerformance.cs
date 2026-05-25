using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using EddyLib.Radiation;

namespace RhinoPlugin.Test.Xunit.Tests
{
    public class Test_EsoReaderPerformance
    {
        private readonly ITestOutputHelper _output;

        public Test_EsoReaderPerformance(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void LoadEsoFile_CorrectnessTest()
        {
            string tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllLines(tempFile, new[]
                {
                    "Program Version,EnergyPlus, Version 24.1.0",
                    "7,5,Environment,Site Outdoor Air Drybulb Temperature [C] !Hourly",
                    "8,1,ZONE_1,Zone Air Temperature [C] !Hourly",
                    "9,2,ZONE_1:G_1,Surface Outside Face Temperature [C] !Hourly",
                    "End of Data Dictionary",
                    "1,2023,1,1,1,60.0,0.0",
                    "7,10.5",
                    "8,21.2",
                    "9,5.5",
                    "1,2023,1,1,2,60.0,0.0",
                    "7,11.0",
                    "8,21.5",
                    "9,6.0",
                    "End of Data"
                });

                var results = EsoReader.LoadEsoFile(tempFile);

                Assert.NotNull(results);
                Assert.Equal(3, results.Count);

                var siteTemp = results.FirstOrDefault(r => r.tag == "Site Outdoor Air Drybulb Temperature");
                Assert.NotNull(siteTemp);
                Assert.Equal(2, siteTemp.values.Count);
                Assert.Equal(10.5, siteTemp.values[0]);
                Assert.Equal(11.0, siteTemp.values[1]);

                var zoneTemp = results.FirstOrDefault(r => r.zone == "ZONE_1" && r.typ == esoType.Zone);
                Assert.NotNull(zoneTemp);
                Assert.Equal(21.2, zoneTemp.values[0]);

                var surfaceTemp = results.FirstOrDefault(r => r.faceId == "G_1");
                Assert.NotNull(surfaceTemp);
                Assert.Equal(esoType.Face, surfaceTemp.typ);
                Assert.Equal("ZONE_1", surfaceTemp.zone);
                Assert.Equal(5.5, surfaceTemp.values[0]);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        [Trait("Category", "Performance")]
        public void LoadEsoFile_PerformanceTest()
        {
            string tempFile = Path.GetTempFileName();
            int numZones = 100;
            int numHours = 8760;

            try
            {
                using (var sw = new StreamWriter(tempFile))
                {
                    sw.WriteLine("Program Version,EnergyPlus, Version 24.1.0");
                    for (int i = 0; i < numZones; i++)
                    {
                        sw.WriteLine($"{10 + i},1,ZONE_{i},Zone Air Temperature [C] !Hourly");
                    }
                    sw.WriteLine("End of Data Dictionary");

                    for (int h = 0; h < numHours; h++)
                    {
                        sw.WriteLine($"1,2023,1,1,{h + 1},60.0,0.0");
                        for (int i = 0; i < numZones; i++)
                        {
                            sw.WriteLine($"{10 + i},{20.0 + i * 0.1 + h * 0.01}");
                        }
                    }
                    sw.WriteLine("End of Data");
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                long startMemory = GC.GetTotalMemory(true);

                Stopwatch stopwatch = Stopwatch.StartNew();
                var results = EsoReader.LoadEsoFile(tempFile);
                stopwatch.Stop();

                long endMemory = GC.GetTotalMemory(false);

                _output.WriteLine($"Loaded {numZones} zones * {numHours} hours (~{numZones * numHours} data points) in {stopwatch.ElapsedMilliseconds} ms");
                _output.WriteLine($"Memory usage approx: {(endMemory - startMemory) / 1024.0 / 1024.0:F2} MB");

                Assert.Equal(numZones, results.Count);
                Assert.Equal(numHours, results[0].values.Count);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}
