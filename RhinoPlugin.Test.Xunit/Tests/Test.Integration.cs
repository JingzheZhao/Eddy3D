using EddyLib;
using EddyLib.Helpers;
using EddyLib.Radiation;
using EddyLib.UI;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;
using static RhinoPlugin.Test.Xunit.WeatherDownload;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class IntegrationTests
    {
        [NotWindowsServerFact]
        [Trait("Category", "Integration")]
        public void SurfaceTempTest_GrassVsConcrete()
        {
            // 1. Arrange
            string workingDir = TestFixtures.CreateTestDirectory("integration-grass-vs-concrete");
            string epw = DownloadEPW(); // Uses default EPW (JFK)
            Weather weather = new Weather(epw);

            // Create Geometry
            // Two adjacent squares, 10x10m

            // Concrete Surface (Left)
            var concreteBrep = Brep.CreateFromCornerPoints(
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(0, 10, 0),
                0.01);

            // Grass Surface (Right)
            var grassBrep = Brep.CreateFromCornerPoints(
                new Point3d(10, 0, 0),
                new Point3d(20, 0, 0),
                new Point3d(20, 10, 0),
                new Point3d(10, 10, 0),
                0.01);

            // Define Materials
            var concreteSettings = new RSurface_Settings
            {
                Name = "Concrete",
                RadianceMaterial = RadianceMaterials.DefaultGround, // Use default ground material strings
                Conductivity = 1.4,     // Concrete typical
                Density = 2300,         // Concrete typical
                SpecificHeat = 880,     // Concrete typical
                ThermalAbsorptance = 0.9,
                SolarAbsorptance = 0.65, // Light grey concrete
                VisibleAbsorptance = 0.65,
                Roughness = RoughnessOfCollectorEnum.MediumRough,
                Thickness = 0.2
            };

            var grassSettings = new VegetationSurface_Settings
            {
                Name = "Grass",
                HeightOfPlants = 0.2,
                LeafAreaIndex = 2.0,
                LeafReflectivity = 0.22,
                LeafEmissivity = 0.95,
                MinimumStomatalResistance = 180,
                SoilLayerName = "Soil",
                Roughness = RoughnessOfCollectorEnum.Rough,
                // Soil properties
                ConductivityOfDrySoil = 1.0,
                DensityOfDrySoil = 1200,
                SpecificHeatOfDrySoil = 1200,
                ThermalAbsorptance = 0.95,
                SolarAbsorptance = 0.85,
                VisibleAbsorptance = 0.85,
                SaturationVolumetricMoistureContentOfTheSoilLayer = 0.45,
                ResidualVolumetricMoistureContentOfTheSoilLayer = 0.05,
                InitialVolumetricMoistureContentOfTheSoilLayer = 0.3
            };

            // Create RSurfaces
            var rSurfaces = new List<RSurface>
            {
                new RSurface("ConcreteSurf", concreteBrep, RadiationSurfaceType.Building, SimulationType.Simulated, concreteSettings),
                new RSurface("GrassSurf", grassBrep, RadiationSurfaceType.Building, SimulationType.Simulated, grassSettings)
            };

            // Create Probes (sensors slightly above surfaces)
            // Concrete Probe (Left center)
            var concreteProbePt = new Point3d(5, 5, 0.5);
            // Grass Probe (Right center)
            var grassProbePt = new Point3d(15, 5, 0.5);

            var rProbes = new List<RProbe>
            {
                new RProbe(concreteProbePt, Vector3d.ZAxis),
                new RProbe(grassProbePt, Vector3d.ZAxis)
            };

            // Simulation Settings
            var simSettings = new MRT_Simulation_Settings
            {
                ComputeReflectionsAndDiffuseRadiation = true, // DDS
                ComputeSurfaceTemperatureEnergyPlus = true,   // Needed for Surface Temp differences
                ComputeLongWaveExchangeEnergyPlus = false
            };

            // 2. Act
            // Initialize System
            var mrtSystem = new MRT_Simulation_System(workingDir, weather, rSurfaces, rProbes, "", simSettings);

            // Run Simulation Steps (replicating MRT_Simulation_Component.RunSlowSimulation)
            var ct = CancellationToken.None;
            int stepCnt = 0;

            // VF
            Assert.True(mrtSystem.RunVF(true, ct, mrtSystem.TOTAL, ref stepCnt), "VF failed");

            // DDS
            Assert.True(mrtSystem.RadiationSystem.RunDDS(true, ct, mrtSystem.TOTAL, ref stepCnt), "DDS Run failed");
            mrtSystem.RadiationSystem.LoadDDSData(true, ct, mrtSystem.TOTAL, ref stepCnt);

            // Thermal / EP
            var epData = mrtSystem.ThermalSystem.RunEP(true, ct, mrtSystem.TOTAL, ref stepCnt);
            Assert.NotNull(epData);

            mrtSystem.ThermalSystem.ComputeMRT(true, ct, mrtSystem.TOTAL, ref stepCnt);

            // 3. Assert
            // We expect Grass surface temperature to be generally lower than Concrete (evapotranspiration)
            // Or MRT above Grass to be lower.

            // Check Surface Temperatures directly from ThermalSystem results?
            // The simulation stores results in Probes.
            // But Probes store MRT and Radiation.
            // Surface Temperature matches are not directly on Probes unless we look at the RSurfaces/Polygons?
            // However, MRT includes surface temp influence.

            // Let's check MRT at noon in Summer (e.g., July 1st -> Hr ~ 4344)
            int summerNoon = 4344; // Approx

            float mrtConcrete = mrtSystem.Probes[0].LongWave_MRT[summerNoon] + mrtSystem.Probes[0].SolarGain_dMRT[summerNoon];
            float mrtGrass = mrtSystem.Probes[1].LongWave_MRT[summerNoon] + mrtSystem.Probes[1].SolarGain_dMRT[summerNoon];

            // MRT above grass should be lower due to lower surface temperature
            Assert.True(mrtGrass < mrtConcrete, $"Expected MRT above Grass ({mrtGrass}) to be lower than Concrete ({mrtConcrete}) at hour {summerNoon}");

            // Cleanup
            // Directory.Delete(workingDir, true); // Keep for inspection if failed
        }
    }
}
