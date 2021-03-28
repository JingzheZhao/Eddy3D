using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.Dicts
{
    class ViralEmitterInternalDict : GenericDict
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

            Dict4.Add("Covid19", "(1.076e-4 0)");

            return Dict1;
        }
    }
}
