using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace EddyLib.Radiation
{
    [DataContract]
    public class Tree_Settings
    {
        public static Tree_Settings GenerateTree()
        {
            var o = new Tree_Settings();
            o.RadianceMaterial = RadianceMaterials.DefaultTree;
            return o;
        }

        public Tree_Settings()
        {
        }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string RadianceMaterial { get; set; }

        public override string ToString()
        {
            return this.toJSON();
        }

        public static Tree_Settings fromJSON(string json)
        {
            return DeserializeJSON<Tree_Settings>(json);
        }

        public string toJSON()
        {
            return SerializeJSON<Tree_Settings>(this);
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
                    TypeNameHandling = TypeNameHandling.None,
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
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(component, set);
        }
    }
}