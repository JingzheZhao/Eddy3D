using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor
{
    public class IndoorBCDict : GenericDict
    {
        public string dimensions { get; set; }

        public string internalField { get; set; }

        public Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> boundaryFieldDict { get; set; }

        public List<Dictionary<string, Dictionary<string, string>>> internalDict { get; set; }

        public List<IndoorBC.Inlet> inlet { get; set; }

        public List<IndoorBC.Outlet> outlet { get; set; }

        public List<IndoorBC.Wall> wall { get; set; }

        public class U : IndoorBCDict
        {
            public U(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.inlet = inlet;
                this.outlet = outlet;
                this.wall = wall;

                this.fc = GenericDict.fieldClass.volVectorField;
                this.Name = "U";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 1 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform (0 0 0);";

                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetFixedValue(i)); }
            }
        }

        public class T : IndoorBCDict
        {
            public T(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "T";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 0 1 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 300;";

                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class alphat : IndoorBCDict
        {
            public alphat(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "alphat";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 1 0 0 0 0]";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";

                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class AoA : IndoorBCDict
        {
            public AoA(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "AoA";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 0 1 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";

                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class k : IndoorBCDict
        {
            public k(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "k";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 2 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";

                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class nut : IndoorBCDict
        {
            public nut(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "nut";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 2 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";

                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class p_rgh : IndoorBCDict
        {
            public p_rgh(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "p_rgh";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [1 -1 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 101325;";

                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class omega : IndoorBCDict
        {
            public omega(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "omega";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";
                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class p : IndoorBCDict
        {
            public p(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "p";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions [1 -1 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField uniform 101325;";
                foreach (IndoorBC.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        private static Dictionary<string, Dictionary<string, string>> GetFixedValue(IndoorBC input)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            if (input is IndoorBC.Inlet)
            {
                var ii = (IndoorBC.Inlet)input;

                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform (" + ii.velocity.ToString().Trim(',') + ")");
            };

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetInletOutlet(IndoorBC input)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            InternalDict.Add("type", "inletOutlet");
            InternalDict.Add("inletValue", "uniform (0 0 0)");
            InternalDict.Add("value", "uniform (0 0 0)");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetZeroGradient(IndoorBC input)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            InternalDict.Add("type", "zeroGradient");

            return Dict;
        }

        private string Serialize(IndoorBCDict dict)
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
            File.WriteAllText(baseWorkingDir + this.location, this.Serialize(this));
        }

        private string ToCPPDict(Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> boundaryFieldDict)
        {
            string sb = JsonConvert.SerializeObject(boundaryFieldDict);
            return sb;
        }
    }
}