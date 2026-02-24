using System;
using System.Collections.Generic;
using System.Diagnostics;
using EddyLib;
using Xunit;
using Xunit.Abstractions;

namespace RhinoPlugin.Test.Xunit.Tests
{
    public class Test_NaturalVentilationPerformance
    {
        private readonly ITestOutputHelper _output;

        public Test_NaturalVentilationPerformance(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NVAnalysis_Constructor_PerformanceBenchmark()
        {
            // Arrange
            int count = 10000; // O(N^2) means 10000^2 = 100,000,000 iterations.
            var listOfCps = new List<double>(count);
            var areaList = new List<double>(count);
            var rand = new Random(42);

            for (int i = 0; i < count; i++)
            {
                // Mix of positive and negative values to trigger both paths
                listOfCps.Add(rand.NextDouble() * 2 - 1); // Range [-1, 1]
                areaList.Add(rand.NextDouble() * 10);
            }

            double velocity = 5.0;

            // Act
            Stopwatch sw = Stopwatch.StartNew();
            var analysis = new NVAnalysis(listOfCps, areaList, velocity);
            sw.Stop();

            _output.WriteLine($"Processed {count} items in {sw.ElapsedMilliseconds} ms");

            // Assert
            Assert.True(analysis.FlowRate >= 0, "FlowRate should be non-negative");
        }
    }
}
