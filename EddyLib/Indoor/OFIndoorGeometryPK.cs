using EddyLib.BCs;
using EddyLib.Indoor;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Indoor
{
    //public enum OFIndoorGeometryType
    //{
    //    Inlet = 0,

    //    Outlet = 1,

    //    Geometry = 2
    //}

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
                this.inlet = inlet;
                this.outlet = outlet;
                this.wall = wall;

                this.fc = fieldClass.volVectorField;
                this.Name = "U";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 1 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform (0 0 0);";

                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetFixedValue(i)); }
            }
        }

        public class T : Dicts
        {
            public T(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "T";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 0 1 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 300;";

                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class alphat : Dicts
        {
            public alphat(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "alphat";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 1 0 0 0 0]";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";

                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class AoA : Dicts
        {
            public AoA(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "AoA";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 0 1 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";

                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class k : Dicts
        {
            public k(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "k";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 2 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";

                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class nut : Dicts
        {
            public nut(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "nut";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 2 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";

                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class p_rgh : Dicts
        {
            public p_rgh(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "p_rgh";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [1 -1 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 101325;";

                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class omega : Dicts
        {
            public omega(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "omega";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions      [0 0 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField   uniform 0;";
                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
            }
        }

        public class p : Dicts
        {
            public p(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                this.fc = fieldClass.volScalarField;
                this.Name = "p";
                this.location = @"\0\" + this.Name;
                this.header = GetHeader(this);
                this.dimensions = "dimensions [1 -1 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.internalField = "internalField uniform 101325;";
                foreach (IndoorBCs.Inlet i in inlet) { this.internalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBCs.Outlet i in outlet) { this.internalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBCs.Wall i in wall) { this.internalDict.Add(GetZeroGradient(i)); }
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
                InternalDict.Add("value", "uniform (" + ii.velocity.ToString().Trim(',') + ")");
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

            sb.Append(dict.boundaryFieldDict.ToString());

            return sb.ToString();
        }

        public void Export(string baseWorkingDir)
        {
            File.WriteAllText(baseWorkingDir + this.location, this.Serialize(this));
        }
    }
}

public class IndoorBCs

{
    //public Dictionary<string, string> U { get; set; }

    //public Dictionary<string, string> T { get; set; }

    //public Dictionary<string, string> alpha { get; set; }

    //public Dictionary<string, string> AoA { get; set; }

    //public Dictionary<string, string> k { get; set; }

    //public Dictionary<string, string> nut { get; set; }

    //public Dictionary<string, string> p_rgh { get; set; }

    //public Dictionary<string, string> omega { get; set; }

    //public Dictionary<string, string> p { get; set; }

    public MeshFaceNormalList Normals { get; set; }

    public Mesh Geometry { get; set; }

    public string Name { get; set; }

    public class Wall : IndoorBCs
    {
        public double TemperatureK { get; set; }

        public double internalFieldTempK { get; set; }

        public Wall(Mesh m, double TemperatureC, double internalFieldTempC)
        {
            this.TemperatureK = TemperatureC + 273.15;

            // this needs to be passed differently
            this.internalFieldTempK = internalFieldTempC + 273.15;
            this.Geometry = m;
            this.Normals = m.FaceNormals;
            this.Name = "Wall";
        }
    }

    public class Inlet : IndoorBCs
    {
        public double TemperatureK { get; set; }

        public Vector3d velocity { get; set; }

        public Inlet(Mesh m, double TemperatureC, Vector3d velocity)
        {
            this.TemperatureK = TemperatureC + 273.15;
            this.velocity = velocity;
            this.Geometry = m;
            this.Normals = m.FaceNormals;
            this.Name = "Inlet";
        }
    }

    public class Outlet : IndoorBCs
    {
        public Outlet(Mesh m)
        {
            this.Geometry = m;
            this.Normals = m.FaceNormals;
            this.Name = "Outlet";
        }
    }

    internal class Emitter : IndoorBCs
    {
        public Emitter(Mesh m)
        {
            this.Geometry = m;
            this.Normals = m.FaceNormals;
            this.Name = "Emitter";
        }
    }
}

//internal class OFIndoorGeometry
//{
//    private string Name { get; set; } // must be unique

//    private double FlowRate { get; set; } = 0;

//    public OFIndoorGeometry(string name, Mesh geo, OFIndoorGeometryType type)
//    {
//        Name = name;

//        if (type == OFIndoorGeometryType.Inlet)
//        {
//            DefineInlet();
//        }
//        else if (type == OFIndoorGeometryType.Outlet)
//        {
//        }
//        else
//        {
//            // normal geometry
//        }
//    }

//    private void DefineInlet()
//    {
//        U = GetFixedValue();
//        T =

//        alpha =

//        AoA =

//        k =

//        nut =

//        p_rgh =

//        p =

//        omega =
//        }
//}