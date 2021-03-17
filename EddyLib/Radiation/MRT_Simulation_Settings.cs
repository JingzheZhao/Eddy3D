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
    public class MRT_Simulation_Settings
    {
        public MRT_Simulation_Settings() { }


        // Switch between direct only raycast and Radiance DDS

        [DataMember]
        public bool ComputeReflectionsAndDiffuseRadiation { get; set; } = true;

        // Surface Temperatures with EnergyPlus

        [DataMember]
        public double CummulativeViewFactorCutoff { get; set; } = 0.1;

        [DataMember]
        public bool ComputeSurfaceTemperatureEnergyPlus { get; set; } = true;

        [DataMember]
        public bool ComputeLongWaveExchangeEnergyPlus { get; set; } = false;

        public override string ToString() { return this.toJSON(); }

















        public static MRT_Simulation_Settings fromJSON(string json)
        {
            return DeserializeJSON<MRT_Simulation_Settings>(json);
        }


        public string toJSON()
        {
            return SerializeJSON<MRT_Simulation_Settings>(this);
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
