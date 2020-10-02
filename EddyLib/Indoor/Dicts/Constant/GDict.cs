using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    public class GDict : GenericDict
    {
        public GDict
            (
            )
        {
            this.Name = "g";
            this.Location = DictLocation.constant;

            this.Header = GetHeader(this);

            this.FullDictString = @"FoamFile
{
    version         1912;
    format          ascii;
    class           uniformDimensionedVectorField;
    location        ""constant"";
    object          g;
}

dimensions      [0 1 -2 0 0 0 0];

value           (0 0 -9.81);

";
        }
    }
}