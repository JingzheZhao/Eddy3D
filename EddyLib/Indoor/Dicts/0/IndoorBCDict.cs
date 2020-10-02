using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace EddyLib.Indoor.Dicts
{
    public class IndoorBCDict : GenericDict
    {
        public string Dimensions { get; set; }

        public string InternalField { get; set; }

        public Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> BoundaryFieldDict { get; set; }

        public List<Dictionary<string, Dictionary<string, string>>> InternalDict { get; set; }

        public class U : IndoorBCDict
        {
            public U(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = GenericDict.FieldClass.volVectorField;
                this.Name = "U";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions      [0 1 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform (0 0 0);";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetInletOutlet(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class T : IndoorBCDict
        {
            public T(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.Name = "T";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions      [0 0 0 1 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 300;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class alphat : IndoorBCDict
        {
            public alphat(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.Name = "alphat";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions      [0 0 1 0 0 0 0]";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class AoA : IndoorBCDict
        {
            public AoA(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.Name = "AoA";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions      [0 0 0 1 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class k : IndoorBCDict
        {
            public k(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.Name = "k";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions      [0 2 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class nut : IndoorBCDict
        {
            public nut(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.Name = "nut";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions      [0 2 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class p_rgh : IndoorBCDict
        {
            public p_rgh(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.Name = "p_rgh";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions      [1 -1 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 101325;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class omega : IndoorBCDict
        {
            public omega(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.Name = "omega";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions      [0 0 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();
                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class p : IndoorBCDict
        {
            public p(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.Name = "p";
                this.Location = DictLocation.zero;
                this.Header = GetHeader(this);
                this.Dimensions = "dimensions [1 -1 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField uniform 101325;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();
                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        private static Dictionary<string, Dictionary<string, string>> GetFixedValue(IndoorBC input)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            if (input is IndoorBC.Inlet)
            {
                IndoorBC.Inlet ii = (IndoorBC.Inlet)input;

                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform (" + ii.Velocity.ToString().Trim(',') + ")");
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

            sb.AppendLine(dict.Header);
            sb.AppendLine(dict.Dimensions);
            sb.AppendLine(dict.InternalField);

            //TODO FOR TD: FIX THIS!
            sb.AppendLine(CppMapSerializer.Serialize(dict.BoundaryFieldDict));

            return sb.ToString();
        }
    }
}

///*--------------------------------*- C++ -*----------------------------------*\
//| =========                 |                                                 |
//| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
//|  \\    /   O peration     | Version:  5.x                                   |
//|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
//|    \\/     M anipulation  |                                                 |
//\*---------------------------------------------------------------------------*/
///*   Windows 32 and 64 bit porting by blueCAPE: http://www.bluecape.com.pt   *\
//|  Based on Windows porting (2.0.x v4) by Symscape: http://www.symscape.com   |
//\*---------------------------------------------------------------------------*/
//FoamFile
//{
//    version     2.0;
//    format      binary;
//    class       volScalarField;
//    location    "0";
//    object      T;
//}
//// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

//dimensions      [0 0 0 1 0 0 0];

//internalField   uniform 299.15;

//boundaryField
//{
//    Inlet
//    {
//        type            fixedValue;
//        value           uniform 283.15;
//    }
//    Outlet
//    {
//        type            zeroGradient;
//    }
//    FashioShop_1
//    {
//        type            zeroGradient;
//    }
//}

//// ************************************************************************* //