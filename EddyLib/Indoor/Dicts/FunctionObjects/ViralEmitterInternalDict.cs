using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.Indoor.Dicts
{
    internal class ViralEmitterInternalDict : GenericDict
    {
        public List<String> InternalDict = new List<string>();

        //public ViralEmitterInternalDict(ViralEmitter viralEm, Point3d PointInsideDomain)
        public ViralEmitterInternalDict(List<ViralEmitter> viralEmitters, Point3d PointInsideDomain)
        {
            this.DictionaryName = "viralEmitters";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            //this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalViralDict(viralEm)));
            foreach (ViralEmitter i in viralEmitters) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalViralDict(i))); }

            string[] parts = {
               String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static Dictionary<string, dynamic> GetInternalViralDict(ViralEmitter input)
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