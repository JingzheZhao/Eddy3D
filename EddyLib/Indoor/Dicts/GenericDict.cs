using System.Collections.Generic;
using System.IO;

namespace EddyLib.Indoor.Dicts
{
    public class GenericDict

    {
        public enum DictLocation
        {
            zero,

            system,

            constant
        }

        public DictLocation Location;

        public string Header { get; set; }

        public string DictionaryName { get; set; }

        public enum FieldClass
        {
            volVectorField,

            volScalarField,

            dictionary
        }

        public FieldClass FC;

        public string FullDictString;

        public void Export(string baseWorkingDir)

        {
            var path = Path.Combine(baseWorkingDir, PrintLocation(Location));
            Directory.CreateDirectory(path);
            if (!path.EndsWith("\\")) path += "\\";
            File.WriteAllText(path + this.DictionaryName, this.FullDictString);
        }

        public static string GetHeader(GenericDict dict)
        {
            return @"FoamFile
{
    version     2.0;
    format      ascii;
    class       " + dict.FC + @";
    location    """ + PrintLocation(dict.Location) + @""";
    object      " + dict.DictionaryName + @";
}";
        }

        private static string PrintLocation(DictLocation dl)
        {
            string s = "";
            if (dl == DictLocation.zero)
            {
                s = "0";
            }
            else
            {
                s = dl.ToString();
            }

            return s;
        }

        public void RemoveDict(string baseWorkingDir)
        {
            string path = baseWorkingDir + "\\" + this.Location + this.DictionaryName;

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static string InParenthesis(string input)
        {


            return "(" + input + ")";
        }
    }
}