using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.Indoor.Dicts
{
    public class ResidualsDict : GenericDict
    {
        public List<String> InternalDict = new List<string>();

        public ResidualsDict()
        {
            this.DictionaryName = "residuals";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetWriteControlDict()));

            string[] parts = {
         //      this.Header, "\n",
         String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static Dictionary<string, dynamic> GetWriteControlDict()
        {
            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            //Dictionary<string, dynamic> FunctionObjectlDict = new Dictionary<string, dynamic>();

            InternalDict.Add("type", "residuals");
            InternalDict.Add("libs", " (\"libutilityFunctionObjects.so\") ");

            InternalDict.Add("writeControl", "timeStep");
            InternalDict.Add("writeInterval", "1");
            InternalDict.Add("fields", "(    p_rgh   U  h k omega AoA   )");

            return InternalDict;
        }
    }
}

//            @"type            residuals;
//libs            (""libutilityFunctionObjects.so"");

//writeControl timeStep;
//writeInterval   1;

//fields (    p_rgh   U  h k omega AoA   );

//// ************************************************************************* //";
//        }