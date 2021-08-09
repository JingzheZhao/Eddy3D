using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    internal class DecomposeParDict : GenericDict
    {
        public List<String> InternalDict = new List<string>();

        public DecomposeParDict()
        {
            this.DictionaryName = "decomposeParDict";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader2(this);

            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetBodyDict()));

            string[] parts = {
               this.Header, "\n",
         String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static Dictionary<string, dynamic> GetBodyDict()
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            //Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            //Dictionary<string, dynamic> FunctionObjectlDict = new Dictionary<string, dynamic>();

            Dict.Add("method", "scotch");
            Dict.Add("numberOfSubdomains", "8");
            Dict.Add("scotchCoeffs", "{"+"\n" + "}");

            return Dict;
        }

    }
}