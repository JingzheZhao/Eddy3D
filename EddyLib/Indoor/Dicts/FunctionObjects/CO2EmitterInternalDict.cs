using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;
using Rhino.Geometry;
using EddyLib.Indoor.FunctionObjects;

namespace EddyLib.Indoor.Dicts
{
    public class CO2EmitterInternalDict : GenericDict
    {

        public List<String> InternalDict = new List<string>();

        public CO2EmitterInternalDict(CO2Emitter co2Em, Point3d PointInsideDomain)
        {

            this.DictionaryName = "co2Emitter";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalC02Dict(co2Em)));

            string[] parts = {
               String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");




            //this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)co2Em, PointInsideDomain));
            //this.FunctionObjectSubDictString = CppMapSerializerDyn.Serialize(GetInternalC02Dict(co2Em));
        }

        private static Dictionary<string, dynamic> GetInternalC02Dict(CO2Emitter input)
        {
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict4 = new Dictionary<string, dynamic>();

            Dict1.Add(input.ID, Dict2);

            Dict2.Add("active", "true");
            Dict2.Add("type", "semiImplicitSource");
            Dict2.Add("scalarSemiImplicitSourceCoeffs", Dict3);

            Dict3.Add("selectionMode", "cellZone");
            Dict3.Add("cellZone", input.ID);
            Dict3.Add("volumeMode", "specific");
            Dict3.Add("injectionRateSuSp", Dict4);

            //update for CO2 instead of Covid-19
            Dict4.Add("CO2", "(1.076e-4 0)");

            return Dict1;
        }
    }
}