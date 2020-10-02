using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.Dicts
{
    public class GenericDict
    {
        public string header { get; set; }

        public string location { get; set; }

        public string Name { get; set; }

        public enum fieldClass
        {
            volVectorField,

            volScalarField,

            dictionary
        }

        public fieldClass fc;

        public static string GetHeader(GenericDict dict)
        {
            return @"FoamFile
{
    version     2.0;
    format      ascii;
    class       " + dict.fc + @";
    location    " + dict.location + @";
    object      " + dict.Name + @"
}";
        }
    }
}