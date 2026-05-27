using EddyLib;
using EddyLib.BCs;
using EddyLib.OutdoorComfort;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class WindFactorsPerformanceTests
    {
        [Fact]
        public void WindFactorsTemporal_Performance()
        {
            // Arrange
            int numProbes = 10000;
            var points = Enumerable.Repeat(new Point3d(0, 0, 2), numProbes).ToList();
            var windDirs = new[] { 0, 45, 90, 135, 180, 225, 270, 315 };

            var bcond = new ABL(0, 10, 10, 1, 0);
            var bcColl = new BCCollection(bcond);
            // Simulate 36 directions for the BC collection
            bcColl.WindDirections.Clear();
            for (int i = 0; i < 360; i += 10) bcColl.WindDirections.Add(i);
            bcColl.BCs.Clear();
            foreach(var d in bcColl.WindDirections) bcColl.BCs.Add(new ABL(d, 10, 10, 1, 0));
            bcColl.ClstSimDirIndices = new int[8760];

            var weather = new Weather
            {
                WindSpeed = Enumerable.Repeat(5.0, 8760).ToArray(),
                WindDirection = Enumerable.Repeat(0, 8760).ToArray(),
                Location = "TestLocation"
            };

            var wfSpatialValues = new double[numProbes, bcColl.WindDirections.Count];
            var random = new Random(42);
            for (int i = 0; i < numProbes; i++)
            {
                for (int j = 0; j < bcColl.WindDirections.Count; j++)
                {
                    wfSpatialValues[i, j] = random.NextDouble();
                }
            }

            var workingDir = TestFixtures.CreateTestDirectory("wft-performance");
            var mdv = new MultiDirectionalVelocities(workingDir, bcColl.WindDirections.ToArray(), new Vector3d[numProbes, bcColl.WindDirections.Count], true, true);
            var wfs = new WindFactorsSpatial(workingDir, bcColl, mdv, points, false, true)
            {
                ValuesSpatial = wfSpatialValues
            };

            // Act
            var sw = Stopwatch.StartNew();
            var wft = new WindFactorsTemporal(workingDir, bcColl, weather, wfs, points, true, true);
            sw.Stop();

            Console.WriteLine($"WindFactorsTemporal (interpolate=true) took {sw.ElapsedMilliseconds}ms for {numProbes} probes.");

            // Basic validation
            Assert.NotNull(wft.ValuesTemporalAtProbingHeight);
            Assert.Equal(8760, wft.ValuesTemporalAtProbingHeight.GetLength(0));
            Assert.Equal(numProbes, wft.ValuesTemporalAtProbingHeight.GetLength(1));

            // Repeat with interpolate=false
            sw.Restart();
            var wftNoInterp = new WindFactorsTemporal(workingDir, bcColl, weather, wfs, points, false, true);
            sw.Stop();
            Console.WriteLine($"WindFactorsTemporal (interpolate=false) took {sw.ElapsedMilliseconds}ms for {numProbes} probes.");

            Assert.Equal(8760, wftNoInterp.ValuesTemporalAtProbingHeight.GetLength(0));
            Assert.Equal(numProbes, wftNoInterp.ValuesTemporalAtProbingHeight.GetLength(1));
        }
    }
}
