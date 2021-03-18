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
    public class RSurface_Settings
    {
         


        public RSurface_Settings() { }


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

        public string Name { get; set; }
        [DataMember]

        public string RadianceMaterial { get; set; }

        public override string ToString() { return this.toJSON(); }



       public Material GetMaterial() {

            Material mat = new Material();
            mat.Conductivity = Conductivity;
            mat.Density = Density;
            mat.Roughness = Roughness;
            mat.SolarAbsorptance = SolarAbsorptance;
            mat.SpecificHeat = SpecificHeat;
            mat.ThermalAbsorptance = ThermalAbsorptance;
            mat.Thickness = Thickness;
            mat.VisibleAbsorptance = VisibleAbsorptance;

            return mat;
    }
        public Construction GetConstruction()
        {
            Construction con = new Construction();
            con.Layer2 = "DefaultXPS";
            con.OutsideLayer = Name;
            return con;
        }
        public static RSurface_Settings GenerateGround()
        {

            var o = new RSurface_Settings();
            o.RadianceMaterial = RadianceMaterials.DefaultGround;
            o.Name = "Asphalt";
            o.Conductivity = 0.75;
            o.SpecificHeat = 920;
            o.ThermalAbsorptance = .9;
            o.Density = 2350;
            o.SolarAbsorptance = 0.32;
            o.VisibleAbsorptance = 0.32;
            o.Thickness = 0.1;

            return o;
        }

        public static RSurface_Settings GenerateFacade()
        {

            var o = new RSurface_Settings();
            o.RadianceMaterial = RadianceMaterials.DefaultFacade;
            o.Name = "RedBrick";
            o.Conductivity = 0.89;
            o.SpecificHeat = 920;
            o.ThermalAbsorptance = .9;
            o.Density = 1920;
            o.SolarAbsorptance = 0.6;
            o.VisibleAbsorptance = 0.6;
            o.Thickness = 0.2;

            return o;
        }

        public static RSurface_Settings fromJSON(string json)
        {
            return DeserializeJSON<RSurface_Settings>(json);
        }

        public string toJSON()
        {
            return SerializeJSON<RSurface_Settings>(this);
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
                    TypeNameHandling = TypeNameHandling.Auto,
                    NullValueHandling = NullValueHandling.Ignore
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
                TypeNameHandling = TypeNameHandling.Auto,
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(component, set);
        }
    }
}
