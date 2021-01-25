using System.Collections.Generic;
using System.IO;

namespace EddyLib.Indoor.Dicts
{
    public class FunctionObjectDict : GenericDict

    {
        public Dictionary<string, string> Type = new Dictionary<string, string>();

        public Dictionary<string, string> Libs = new Dictionary<string, string>();

        public new string DictionaryName = "fvOptions";
        public new DictLocation Location = DictLocation.system;
        public new readonly FieldClass FC = FieldClass.dictionary;
    }
}