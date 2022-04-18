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

        public CO2EmitterInternalDict(List<CO2Emitter> cO2Emitters, Point3d PointInsideDomain)
        {
            this.DictionaryName = "co2Emitters";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            foreach (CO2Emitter i in cO2Emitters) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalC02Dict(i))); }

            //this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalC02Dict(co2Em)));

            string[] parts = {
               String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");

            //this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)co2Em, PointInsideDomain));
            //this.FunctionObjectSubDictString = CppMapSerializerDyn.Serialize(GetInternalC02Dict(co2Em));
        }

        private static Dictionary<string, dynamic> GetInternalC02Dict(CO2Emitter input)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> semiImplicitSourceCoeffsDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> sourcesDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> hDict = new Dictionary<string, dynamic>();

            Dict.Add(input.ID, InternalDict);

            //Dict.Add(input.Name + "_" + input.Name, InternalDict);

            InternalDict.Add("type", "semiImplicitSource");
            InternalDict.Add("active", "on");
            InternalDict.Add("selectionMode", "cellZone");
            InternalDict.Add("cellZone", input.ID);

            //InternalDict.Add("cellZone", input.cellZone + "_" + input.Name);

            InternalDict.Add("semiImplicitSourceCoeffs", semiImplicitSourceCoeffsDict);

            semiImplicitSourceCoeffsDict.Add("volumeMode", "absolute");
            semiImplicitSourceCoeffsDict.Add("selectionMode", "cellZone");
            semiImplicitSourceCoeffsDict.Add("cellZone", input.ID);
            //semiImplicitSourceCoeffsDict.Add("cellZone", input.cellZone + "_" + input.Name);

            //semiImplicitSourceCoeffsDict.Add("volumeMode", input.volumeType);
            semiImplicitSourceCoeffsDict.Add("sources", sourcesDict);

            semiImplicitSourceCoeffsDict.Add("h", hDict);

            hDict.Add("explicit table", @"((0 0) (1.076e-4 0)");
            hDict.Add("implicit", @"0");

            return Dict;
        }
    }
}