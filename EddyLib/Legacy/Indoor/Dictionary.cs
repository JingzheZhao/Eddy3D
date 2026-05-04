using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor
{
    public class Dicts
    {
        private enum fieldClass
        {
            volVectorField,

            volScalarField
        }

        private fieldClass fc;

        public string header { get; set; }

        public string dimensions { get; set; }

        public string internalField { get; set; }

        public string location { get; set; }

        public string Name { get; set; }

        public Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> boundaryFieldDict { get; set; }

        public List<Dictionary<string, Dictionary<string, string>>> internalDict { get; set; }

        public List<IndoorBCs.Inlet> inlet { get; set; }

        public List<IndoorBCs.Outlet> outlet { get; set; }

        public List<IndoorBCs.Wall> wall { get; set; }

        public class U : Dicts
        {
            public U(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.inlet = inlet;
                this.outlet = outlet;
                this.wall = wall;

                this.fc = fieldClass.volVectorField;
                this.Name = "U";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 1 -1 0 0 0 0];";

                this.internalField = "internalField   uniform (0 0 0);";

                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(wall.Select(i => GetFixedValue(i)));
            }
        }

        public class T : Dicts
        {
            public T(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.fc = fieldClass.volScalarField;
                this.Name = "T";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 0 1 0 0 0];";

                this.internalField = "internalField   uniform 300;";

                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetZeroGradient(i)));
                this.internalDict.AddRange(wall.Select(i => GetZeroGradient(i)));
            }
        }

        public class alphat : Dicts
        {
            public alphat(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.fc = fieldClass.volScalarField;
                this.Name = "alphat";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 1 0 0 0 0]";

                this.internalField = "internalField   uniform 0;";

                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetZeroGradient(i)));
                this.internalDict.AddRange(wall.Select(i => GetZeroGradient(i)));
            }
        }

        public class AoA : Dicts
        {
            public AoA(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.fc = fieldClass.volScalarField;
                this.Name = "AoA";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 0 1 0 0 0];";

                this.internalField = "internalField   uniform 0;";

                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetZeroGradient(i)));
                this.internalDict.AddRange(wall.Select(i => GetZeroGradient(i)));
            }
        }

        public class k : Dicts
        {
            public k(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.fc = fieldClass.volScalarField;
                this.Name = "k";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 2 -2 0 0 0 0];";

                this.internalField = "internalField   uniform 0;";

                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetZeroGradient(i)));
                this.internalDict.AddRange(wall.Select(i => GetZeroGradient(i)));
            }
        }

        public class nut : Dicts
        {
            public nut(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.fc = fieldClass.volScalarField;
                this.Name = "nut";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 2 -1 0 0 0 0];";

                this.internalField = "internalField   uniform 0;";

                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetZeroGradient(i)));
                this.internalDict.AddRange(wall.Select(i => GetZeroGradient(i)));
            }
        }

        public class p_rgh : Dicts
        {
            public p_rgh(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.fc = fieldClass.volScalarField;
                this.Name = "p_rgh";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [1 -1 -2 0 0 0 0];";

                this.internalField = "internalField   uniform 101325;";

                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetZeroGradient(i)));
                this.internalDict.AddRange(wall.Select(i => GetZeroGradient(i)));
            }
        }

        public class omega : Dicts
        {
            public omega(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.fc = fieldClass.volScalarField;
                this.Name = "omega";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 -1 0 0 0 0];";

                this.internalField = "internalField   uniform 0;";
                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetZeroGradient(i)));
                this.internalDict.AddRange(wall.Select(i => GetZeroGradient(i)));
            }
        }

        public class p : Dicts
        {
            public p(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(inlet.Count + outlet.Count + wall.Count);

                this.fc = fieldClass.volScalarField;
                this.Name = "p";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions [1 -1 -2 0 0 0 0];";

                this.internalField = "internalField uniform 101325;";
                this.internalDict.AddRange(inlet.Select(i => GetFixedValue(i)));
                this.internalDict.AddRange(outlet.Select(i => GetZeroGradient(i)));
                this.internalDict.AddRange(wall.Select(i => GetZeroGradient(i)));
            }
        }

        public static string GetHeader(Dicts dict)
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

        private static Dictionary<string, Dictionary<string, string>> GetFixedValue(IndoorBCs input)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            if (input is IndoorBCs.Inlet)
            {
                var ii = (IndoorBCs.Inlet)input;

                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform (" + ii.Velocity.ToString().Trim(',') + ")");
            };

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetInletOutlet(IndoorBCs input)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            InternalDict.Add("type", "inletOutlet");
            InternalDict.Add("inletValue", "uniform (0 0 0)");
            InternalDict.Add("value", "uniform (0 0 0)");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetZeroGradient(IndoorBCs input)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            InternalDict.Add("type", "zeroGradient");

            return Dict;
        }

        private string Serialize(Dicts dict)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(dict.header);
            sb.Append(dict.dimensions);
            sb.Append(dict.internalField);

            sb.Append(ToCPPDict(dict.boundaryFieldDict));

            return sb.ToString();
        }

        public void Export(string baseWorkingDir)
        {
            if (!Directory.Exists(baseWorkingDir + this.location)) {
                Directory.CreateDirectory(baseWorkingDir + this.location);
            }
            File.WriteAllText(baseWorkingDir + this.location, this.Serialize(this));
        }

        private string ToCPPDict(Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> boundaryFieldDict)
        {

            string sb = JsonConvert.SerializeObject(boundaryFieldDict);
            return sb;
        }

    }
}
