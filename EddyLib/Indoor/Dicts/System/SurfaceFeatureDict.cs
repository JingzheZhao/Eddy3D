using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EddyLib.Indoor.Dicts
{
    internal class SurfaceFeatureDict : GenericDict
    {
        public List<String> InternalDict = new List<string>();

        public Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> GeometryDict { get; set; }

        public List<Dictionary<string, Dictionary<string, string>>> GeometrySubDict { get; set; }

        public SurfaceFeatureDict(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
        {
            this.DictionaryName = "surfaceFeaturesDict";
            this.FC = FieldClass.dictionary;
            this.Location = DictLocation.system;
            this.Header = GetHeader(this);

            var AllBCs = new List<IndoorBC>();

            foreach (var sf in inlet)
            {
                AllBCs.Add(sf);
            }
            foreach (var sf in outlet)
            {
                AllBCs.Add(sf);
            }
            foreach (var sf in wall)
            {
                AllBCs.Add(sf);
            }

            this.InternalDict.Add(SurfaceDict(AllBCs));

            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetDict()));

            string[] parts = {
               this.Header, "\n", //(This is a subdict and doesn't need a header
         String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static string SurfaceDict(List<IndoorBC> AllBCs)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("surfaces");
            sb.AppendLine("(");

            foreach (var item in AllBCs)
            {
                sb.AppendLine(@"""" + item.Id + @".stl""");
            }

            sb.AppendLine(");");

            return sb.ToString();
        }

        private static Dictionary<string, dynamic> GetDict()
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> subsetFeatures = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> trimFeatures = new Dictionary<string, dynamic>();

            Dict.Add("includedAngle", "180.00");
            Dict.Add("writeObj", "yes");

            subsetFeatures.Add("nonManifoldEdges", "yes");
            subsetFeatures.Add("openEdges", "yes");

            Dict.Add("subsetFeatures", subsetFeatures);

            trimFeatures.Add("minElem", "0");
            trimFeatures.Add("minLen", "0");

            Dict.Add("trimFeatures", trimFeatures);

            return Dict;
        }
    }
}