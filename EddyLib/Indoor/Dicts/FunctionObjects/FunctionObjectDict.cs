using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib.Indoor.Dicts
{
    public class FunctionObjectDict : GenericDict

    {
        public new string DictionaryName;
        public new DictLocation Location = DictLocation.system;
        public new readonly FieldClass FC = FieldClass.dictionary;

        public FunctionObjectDict(List<FunctionObjectDictInternal> InternalDicts, string Name)
        {
            this.Header = GetHeader(this);
            this.DictionaryName = Name;

            string[] parts = {
               this.Header, "\n",
         String.Join("\n", InternalDicts.Select(x => x.TopoSetDictString.ToString()).ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }
    }
}