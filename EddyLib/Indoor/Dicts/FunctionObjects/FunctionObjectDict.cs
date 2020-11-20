using System.Collections.Generic;
using System.IO;

namespace EddyLib.Indoor.Dicts
{
    public class FucntionObjectDict : GenericDict

    {
        //private Dictionary<string, string> Type = new Dictionary<string, string>();

        //private Dictionary<string, string> Libs = new Dictionary<string, string>();

        //  this.Type = new Dictionary<string, string>() { "type", "volumetricHeatSources;" };

        //  this.Libs = new Dictionary<string, string>() { "libs", @"(""libutilityFunctionObjects.so"");" };

        public string DictionaryName = "fvOptions";
        public DictLocation Location = DictLocation.system;
        public FieldClass FC = FieldClass.dictionary;
    }
}