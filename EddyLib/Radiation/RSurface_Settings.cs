using EddyLib.Helpers;
using System.Runtime.Serialization;

namespace EddyLib.Radiation
{
    /// <summary>
    /// Settings for building/ground surface materials in radiation simulations.
    /// </summary>
    [DataContract]
    public class RSurface_Settings
    {
        public RSurface_Settings() { }

        #region Material Properties

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public double Conductivity { get; set; }

        [DataMember]
        public double Density { get; set; }

        [DataMember]
        public RoughnessOfCollectorEnum Roughness { get; set; } = RoughnessOfCollectorEnum.Rough;

        [DataMember]
        public double SolarAbsorptance { get; set; }

        [DataMember]
        public double SpecificHeat { get; set; }

        [DataMember]
        public double ThermalAbsorptance { get; set; }

        [DataMember]
        public double Thickness { get; set; }

        [DataMember]
        public double VisibleAbsorptance { get; set; }

        [DataMember]
        public string RadianceMaterial { get; set; }

        #endregion

        #region EnergyPlus Objects

        /// <summary>
        /// Creates an EnergyPlus Material object from these settings.
        /// </summary>
        public Material GetMaterial()
        {
            return new Material
            {
                Conductivity = Conductivity,
                Density = Density,
                Roughness = Roughness,
                SolarAbsorptance = SolarAbsorptance,
                SpecificHeat = SpecificHeat,
                ThermalAbsorptance = ThermalAbsorptance,
                Thickness = Thickness,
                VisibleAbsorptance = VisibleAbsorptance
            };
        }

        /// <summary>
        /// Creates an EnergyPlus Construction object from these settings.
        /// </summary>
        public Construction GetConstruction()
        {
            return new Construction
            {
                Layer2 = "DefaultXPS",
                OutsideLayer = Name
            };
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates default ground/asphalt surface settings.
        /// </summary>
        public static RSurface_Settings GenerateGround()
        {
            return new RSurface_Settings
            {
                RadianceMaterial = RadianceMaterials.DefaultGround,
                Name = "Asphalt",
                Conductivity = 0.75,
                SpecificHeat = 920,
                ThermalAbsorptance = 0.9,
                Density = 2350,
                SolarAbsorptance = 0.32,
                VisibleAbsorptance = 0.32,
                Thickness = 0.1
            };
        }

        /// <summary>
        /// Creates default facade/brick surface settings.
        /// </summary>
        public static RSurface_Settings GenerateFacade()
        {
            return new RSurface_Settings
            {
                RadianceMaterial = RadianceMaterials.DefaultFacade,
                Name = "RedBrick",
                Conductivity = 0.89,
                SpecificHeat = 920,
                ThermalAbsorptance = 0.9,
                Density = 1920,
                SolarAbsorptance = 0.6,
                VisibleAbsorptance = 0.6,
                Thickness = 0.2
            };
        }

        #endregion

        #region Serialization

        public override string ToString() => toJSON();

        public static RSurface_Settings fromJSON(string json) => JsonHelper.Deserialize<RSurface_Settings>(json);

        public string toJSON() => JsonHelper.Serialize(this);

        #endregion
    }
}