using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib.Indoor.Dicts
{
    public class TopoSetDict : GenericDict

    {
        public new string DictionaryName = "topoSetDict";
        public new DictLocation Location = DictLocation.system;
        public new readonly FieldClass FC = FieldClass.dictionary;

        public TopoSetDict(List<FunctionObjectDictInternal> InternalDicts, Point3d PointInsideDomain)
        {
            this.Header = GetHeader(this);

            string[] parts = {
               this.Header, "\n", //(This is a subdict and doesn't need a header
         String.Join("\n", InternalDicts.Select(x => x.TopoSetDictString.ToString()).ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }
    }
}