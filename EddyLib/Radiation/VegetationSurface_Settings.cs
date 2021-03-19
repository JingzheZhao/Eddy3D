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


        [DataMember]
        public double SaturationVolumetricMoistureContentOfTheSoilLayer { get; set; } = 0.4;

        [DataMember]
        public double ResidualVolumetricMoistureContentOfTheSoilLayer { get; set; } = 0.01;

        [DataMember]
        public double InitialVolumetricMoistureContentOfTheSoilLayer { get; set; } = 0.2;




        [DataMember]

        public string RadianceMaterial { get; set; } = RadianceMaterials.DefaultGrass;





        public MaterialRoofVegetation GetMaterial()
        {

            MaterialRoofVegetation mat = new MaterialRoofVegetation();
 


            mat.HeightOfPlants = HeightOfPlants;
            mat.LeafAreaIndex = LeafAreaIndex;
            mat.LeafReflectivity = LeafReflectivity;
            mat.LeafEmissivity = LeafEmissivity;
            mat.MinimumStomatalResistance = MinimumStomatalResistance;
            mat.SoilLayerName  = "GreenRoofSoil";
            mat.Roughness  = RoughnessOfCollectorEnum.Rough;
            mat.ConductivityOfDrySoil = ConductivityOfDrySoil;
            mat.DensityOfDrySoil = DensityOfDrySoil;
            mat.SpecificHeatOfDrySoil = SpecificHeatOfDrySoil;
            mat.ThermalAbsorptance = ThermalAbsorptance;
            mat.SolarAbsorptance = SolarAbsorptance;
            mat.VisibleAbsorptance = VisibleAbsorptance;
            mat.SaturationVolumetricMoistureContentOfTheSoilLayer = SaturationVolumetricMoistureContentOfTheSoilLayer;
            mat.ResidualVolumetricMoistureContentOfTheSoilLayer = ResidualVolumetricMoistureContentOfTheSoilLayer;
            mat.InitialVolumetricMoistureContentOfTheSoilLayer = InitialVolumetricMoistureContentOfTheSoilLayer;




            return mat;
        }
        public Construction GetConstruction()
        {
            Construction con = new Construction();
            con.Layer3 = "DefaultXPS";
            con.Layer2 = "DefaultConcrete";
            con.OutsideLayer = Name;
            return con;
        }














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
