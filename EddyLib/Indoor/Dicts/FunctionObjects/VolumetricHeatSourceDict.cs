using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.Dicts
{
    public class VolumetricHeatSourceDict : GenericDict.FunctionObjectDict
    {
        public List<String> InternalDict = new List<string>();

        public VolumetricHeatSourceDict(List<VolumetricHeatSource> VHS)
        {
            this.Type = new Dictionary<string, string>() { "type", "volumetricHeatSources;" };

            this.Libs = new Dictionary<string, string>() { "libs", @"(""libutilityFunctionObjects.so"");" };

            this.Name = "volumetricHeatSources";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);

            foreach (var item in VHS)
            {
                this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetDict(item)));
            }

            string[] parts = {
                this.Header, "\n",
         String.Join("", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static Dictionary<string, dynamic> GetDict(VolumetricHeatSource input)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> scalarSemiImplicitSourceCoeffsDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> injectionRateSuSpDict = new Dictionary<string, dynamic>();

            Dict.Add(input.Name + "_" + input.Id, InternalDict);

            InternalDict.Add("type", "scalarSemiImplicitSource");
            InternalDict.Add("active", "on");
            InternalDict.Add("selectionMode", "cellZone");
            InternalDict.Add("cellZone", input.cellZone + "_" + input.Id);

            InternalDict.Add("scalarSemiImplicitSourceCoeffs", scalarSemiImplicitSourceCoeffsDict);

            scalarSemiImplicitSourceCoeffsDict.Add("selectionMode", "cellZone");
            scalarSemiImplicitSourceCoeffsDict.Add("cellZone", input.cellZone + "_" + input.Id);
            scalarSemiImplicitSourceCoeffsDict.Add("volumeMode", input.volumeType);
            scalarSemiImplicitSourceCoeffsDict.Add("injectionRateSuSp", injectionRateSuSpDict);

            injectionRateSuSpDict.Add("h", @"(" + input.Power.ToString() + " 0 )");

            return Dict;
        }
    }
}