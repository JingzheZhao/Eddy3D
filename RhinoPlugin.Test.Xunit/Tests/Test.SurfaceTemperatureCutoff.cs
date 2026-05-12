using EddyLib.Radiation;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    public class SurfaceTemperatureCutoffTests
    {
        [Fact]
        public void VfcPercentile_DoesNotFilterSparseTwoBuildingPorchCase()
        {
            var polys = new List<RPolygon>();

            AddPolygons(polys, "ground", RadiationSurfaceType.Ground, 96, 0.0);
            AddPolygons(polys, "building-a", RadiationSurfaceType.Building, 18, 0.012);
            AddPolygons(polys, "building-b", RadiationSurfaceType.Building, 18, 0.009);
            AddPolygons(polys, "porch", RadiationSurfaceType.Building, 6, 0.004);
            AddPolygons(polys, "ambient", RadiationSurfaceType.Ground, 12, 0.0, SimulationType.Ambient);
            AddPolygons(polys, "sky", RadiationSurfaceType.Sky, 12, 0.0, SimulationType.Ignore);

            double threshold = ThermalSystem.CalculateViewFactorCutoffThreshold(polys, 0.20);

            Assert.Equal(0.0, threshold);

            var simulatedNonSky = polys
                .Where(p => p.Type != RadiationSurfaceType.Sky &&
                            p.SimulationType == SimulationType.Simulated)
                .ToList();

            int keptByPercentile = simulatedNonSky.Count(p => p.SeenByProbes >= threshold);
            int keptByRawTwentyPercentCutoff = simulatedNonSky.Count(p => p.SeenByProbes >= 0.20);

            Assert.Equal(simulatedNonSky.Count, keptByPercentile);
            Assert.Equal(0, keptByRawTwentyPercentCutoff);
        }

        private static void AddPolygons(
            List<RPolygon> polys,
            string name,
            RadiationSurfaceType type,
            int count,
            double seenByProbes,
            SimulationType simulationType = SimulationType.Simulated)
        {
            for (int i = 0; i < count; i++)
            {
                polys.Add(new RPolygon
                {
                    Name = $"{name}-{i}",
                    Type = type,
                    SimulationType = simulationType,
                    SeenByProbes = seenByProbes
                });
            }
        }
    }
}
