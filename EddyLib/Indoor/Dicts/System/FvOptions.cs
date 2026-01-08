using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EddyLib.Indoor.Dicts
{
    public class FvOptions : GenericDict
    {
        //public List<String> InternalDict = new List<string>();

        public FvOptions(List<GenericDict> fv)
        {
            this.DictionaryName = "fvOptions";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);

            //this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetddtSchemesDict(IndoorDom)));

            StringBuilder sb = new StringBuilder();

            foreach (var dic in fv)
            {
                if (dic.DictionaryName == "co2Emitters")
                { }
                else { sb.AppendLine(dic.FullDictString); }
            }

            string[] parts = {
               this.Header, "\n",
         String.Join("\n", sb)
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }
    }
}
