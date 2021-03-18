using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    [DataContract]

    public class VegetationSurface_Settings
    {
        public VegetationSurface_Settings() { }

        [DataMember]
        public string Name { get; set; } = "MyVegetation";

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

        [DataMember]
        public string SoilLayerName { get; set; } = "GreenRoofSoil";

        [DataMember]
        public RoughnessOfCollectorEnum Roughness { get; set; } = RoughnessOfCollectorEnum.Rough;

        [DataMember]
        public double Conductivity { get; set; } = 0.4;

        [DataMember]
        public double Density { get; set; } = 641;


        [DataMember]
        public double SpecificHeat { get; set; } = 1100;

        [DataMember]
        public double ThermalAbsorptance { get; set; } = 0.95;

        [DataMember]
        public double SolarAbsorptance { get; set; } = 0.8;

        [DataMember]
        public double VisibleAbsorptance { get; set; } = 0.7;


        [DataMember]
        public double SaturationVolumetricMoistureContentSoilLayer { get; set; } = 0.4;

        [DataMember]
        public double ResidualVolumetricMoistureContentSoilLayer { get; set; } = 0.01;

        [DataMember]
        public double InitialVolumetricMoistureContentSoilLayer { get; set; } = 0.2;




        [DataMember]

        public string RadianceMaterial { get; set; } = RadianceMaterials.DefaultGrass;




        public override string ToString() { return this.toJSON(); }


        public static VegetationSurface_Settings fromJSON(string json)
        {
            return DeserializeJSON<VegetationSurface_Settings>(json);
        }


        public string toJSON()
        {
            return SerializeJSON<VegetationSurface_Settings>(this);
        }


        private static T DeserializeJSON<T>(string json)
        {
            json = json.Trim();
            if ((json.StartsWith("{") && json.EndsWith("}")) || //For object
                (json.StartsWith("[") && json.EndsWith("]"))) //For array
            {
                var set = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    TypeNameHandling = TypeNameHandling.Auto
                };
                return JsonConvert.DeserializeObject<T>(json, set);
            }
            else { return default(T); }
        }

        private static string SerializeJSON<T>(T component)
        {
            var set = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto
            };
            return JsonConvert.SerializeObject(component, set);
        }



    }

}
