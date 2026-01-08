using EddyLib.Helpers;
using System.Runtime.Serialization;

namespace EddyLib.Radiation
{
    /// <summary>
    /// Settings for vegetation/green roof surfaces in radiation simulations.
    /// </summary>
    [DataContract]
    public class VegetationSurface_Settings
    {
        public VegetationSurface_Settings() { }

        #region Vegetation Properties

        [DataMember]
        public string Name { get; set; } = "Vegetation";

        [DataMember]
        public double HeightOfPlants { get; set; } = 0.5;

        [DataMember]
        public double LeafAreaIndex { get; set; } = 5;

        [DataMember]
        public double LeafReflectivity { get; set; } = 0.2;

        [DataMember]
        public double LeafEmissivity { get; set; } = 0.95;

        [DataMember]
        public double MinimumStomatalResistance { get; set; } = 180;

        #endregion

        #region Soil Properties

        [DataMember]
        public string SoilLayerName { get; set; } = "GreenRoofSoil";

        [DataMember]
        public RoughnessOfCollectorEnum Roughness { get; set; } = RoughnessOfCollectorEnum.Rough;

        [DataMember]
        public double ConductivityOfDrySoil { get; set; } = 0.4;

        [DataMember]
        public double DensityOfDrySoil { get; set; } = 641;

        [DataMember]
        public double SpecificHeatOfDrySoil { get; set; } = 1100;

        [DataMember]
        public double ThermalAbsorptance { get; set; } = 0.95;

        [DataMember]
        public double SolarAbsorptance { get; set; } = 0.8;

        [DataMember]
        public double VisibleAbsorptance { get; set; } = 0.7;

        #endregion

        #region Moisture Properties

        [DataMember]
        public double SaturationVolumetricMoistureContentOfTheSoilLayer { get; set; } = 0.4;

        [DataMember]
        public double ResidualVolumetricMoistureContentOfTheSoilLayer { get; set; } = 0.01;

        [DataMember]
        public double InitialVolumetricMoistureContentOfTheSoilLayer { get; set; } = 0.2;

        #endregion

        #region Radiance

        [DataMember]
        public string RadianceMaterial { get; set; } = RadianceMaterials.DefaultGrass;

        #endregion

        #region EnergyPlus Objects

        /// <summary>
        /// Creates an EnergyPlus MaterialRoofVegetation object from these settings.
        /// </summary>
        public MaterialRoofVegetation GetMaterial()
        {
            return new MaterialRoofVegetation
            {
                HeightOfPlants = HeightOfPlants,
                LeafAreaIndex = LeafAreaIndex,
                LeafReflectivity = LeafReflectivity,
                LeafEmissivity = LeafEmissivity,
                MinimumStomatalResistance = MinimumStomatalResistance,
                SoilLayerName = SoilLayerName,
                Roughness = Roughness,
                ConductivityOfDrySoil = ConductivityOfDrySoil,
                DensityOfDrySoil = DensityOfDrySoil,
                SpecificHeatOfDrySoil = SpecificHeatOfDrySoil,
                ThermalAbsorptance = ThermalAbsorptance,
                SolarAbsorptance = SolarAbsorptance,
                VisibleAbsorptance = VisibleAbsorptance,
                SaturationVolumetricMoistureContentOfTheSoilLayer = SaturationVolumetricMoistureContentOfTheSoilLayer,
                ResidualVolumetricMoistureContentOfTheSoilLayer = ResidualVolumetricMoistureContentOfTheSoilLayer,
                InitialVolumetricMoistureContentOfTheSoilLayer = InitialVolumetricMoistureContentOfTheSoilLayer
            };
        }

        /// <summary>
        /// Creates an EnergyPlus Construction object from these settings.
        /// </summary>
        public Construction GetConstruction()
        {
            return new Construction
            {
                Layer3 = "DefaultXPS",
                Layer2 = "DefaultConcrete",
                OutsideLayer = Name
            };
        }

        #endregion

        #region Serialization

        public override string ToString() => toJSON();

        public static VegetationSurface_Settings fromJSON(string json) => JsonHelper.Deserialize<VegetationSurface_Settings>(json);

        public string toJSON() => JsonHelper.Serialize(this);

        #endregion
    }
}