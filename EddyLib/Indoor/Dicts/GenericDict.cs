using System.IO;

namespace EddyLib.Indoor.Dicts
{
    public class GenericDict

    {
        public enum DictLocation
        {
            system,

            constant,

            zero = 0
        }

        public DictLocation Location;

        public string Header { get; set; }

        //  public string Location { get; set; }

        public string Name { get; set; }

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
            var path = Path.Combine(baseWorkingDir, Location.ToString());
            Directory.CreateDirectory(path);
            if (!path.EndsWith("\\")) path += "\\";
            File.WriteAllText(path + this.Name, this.FullDictString);
        }

        public static string GetHeader(GenericDict dict)
        {
            return @"FoamFile
{
    version     2.0;
    format      ascii;
    class       " + dict.FC + @";
    location    " + dict.Location.ToString() + @";
    object      " + dict.Name + @"
}";
        }
    }
}