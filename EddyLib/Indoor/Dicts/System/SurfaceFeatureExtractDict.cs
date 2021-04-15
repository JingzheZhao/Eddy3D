using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.Indoor.Dicts
{
    internal class SurfaceFeatureExtractDict : GenericDict
    {
        public List<String> InternalDict = new List<string>();

        public Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> GeometryDict { get; set; }

        public List<Dictionary<string, Dictionary<string, string>>> GeometrySubDict { get; set; }

        public SurfaceFeatureExtractDict(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
        {
            this.DictionaryName = "surfaceFeatureExtractDict";
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

            foreach (var item in AllBCs)
            {
                this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetDict(item)));
            }

            string[] parts = {
               this.Header, "\n", //(This is a subdict and doesn't need a header
         String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static Dictionary<string, dynamic> GetDict(IndoorBC input)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> extractFromSurfaceCoeffs = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> injectionRateSuSpDict = new Dictionary<string, dynamic>();

            Dict.Add(input.Id + ".stl", InternalDict);

            InternalDict.Add("extractionMethod", "extractFromSurface");
            InternalDict.Add("includedAngle", "180.00");
            InternalDict.Add("geometricTestOnly", "yes");
            InternalDict.Add("intersectionMethod", "none");
            InternalDict.Add("writeObj", "no");

            InternalDict.Add("extractFromSurfaceCoeffs", extractFromSurfaceCoeffs);

            extractFromSurfaceCoeffs.Add("includedAngle", "180");
            extractFromSurfaceCoeffs.Add("geometricTestOnly", "yes");

            return Dict;
        }
    }
}