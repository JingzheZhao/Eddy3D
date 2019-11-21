using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib
{
    public static class EddyVersion
    {
        public const string ProductVersion = "0.3.4.0";
        public const string Name = "Eddy3D";

        public static string toString()
        {
            return @"
" + Name + " " + ProductVersion;
        }
    }
}