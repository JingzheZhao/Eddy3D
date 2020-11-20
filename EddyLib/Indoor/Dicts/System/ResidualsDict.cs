using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    public class ResidualsDict : GenericDict
    {
        public ResidualsDict()
        {
            this.DictionaryName = "residuals";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            this.FullDictString = @"type            residuals;
libs            (""libutilityFunctionObjects.so"");

writeControl timeStep;
writeInterval   1;

fields (    p_rgh   U  h k omega AoA   );

// ************************************************************************* //";
        }
    }
}