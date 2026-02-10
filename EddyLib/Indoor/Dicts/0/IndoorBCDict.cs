using System.Collections.Generic;
using System.Text;

namespace EddyLib.Indoor.Dicts
{
    public class IndoorBCDict : GenericDict
    {
        private static readonly string[] BoxPatches =
        {
            "Back",
            "Front",
            "Bottom",
            "Top",
            "Right",
            "Left"
        };

        public string Dimensions { get; set; }

        public string InternalField { get; set; }

        public Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> BoundaryFieldDict { get; set; }

        public List<Dictionary<string, Dictionary<string, string>>> InternalDict { get; set; }

        public class U : IndoorBCDict
        {
            public U(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = GenericDict.FieldClass.volVectorField;
                this.DictionaryName = "U";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [0 1 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform (0 0 0);";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValueInlet(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetInletOutlet(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetFixedValue(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetFixedValue(patch, DictionaryName)); }

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
                this.DictionaryName = "T";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [0 0 0 1 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 300;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValueInlet(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetZeroGradient(patch, DictionaryName)); }

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
                this.DictionaryName = "alphat";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [1 -1 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetCalculated(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetCalculated(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetAlphaWallFunction(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetAlphaWallFunction(patch, DictionaryName)); }

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
                this.DictionaryName = "aoa";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [0 0 0 1 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetZeroGradient(patch, DictionaryName)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        public class Covid : IndoorBCDict
        {
            public Covid(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.FC = FieldClass.volScalarField;
                this.DictionaryName = "covid19";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [0 0 0 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetFixedValue(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetZeroGradient(patch, DictionaryName)); }

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
                this.DictionaryName = "k";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [0 2 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0.03375;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetTurbulentIntensityKineticEnergyInlet(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetKqRWallFunction(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetKqRWallFunction(patch, DictionaryName)); }

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
                this.DictionaryName = "nut";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [0 2 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 0;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetCalculated(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetCalculated(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetNutKWallFunction(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetNutKWallFunction(patch, DictionaryName)); }

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
                this.DictionaryName = "p";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions [1 -1 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField uniform 101325;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();
                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetFixedValue(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetCalculated(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetCalculated(patch, DictionaryName)); }

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
                this.DictionaryName = "p_rgh";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [1 -1 -2 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 101325;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();

                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetFixedValue(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetFixFluxPressure(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetFixFluxPressure(patch, DictionaryName)); }

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
                this.DictionaryName = "omega";
                this.Location = DictLocation.zero;
                this.Header = GetHeader0(this);
                this.Dimensions = "dimensions      [0 0 -1 0 0 0 0];";

                // Todo need to pass another class to set internalFieldTemp
                this.InternalField = "internalField   uniform 1.8371173070873834;";

                this.InternalDict = new List<Dictionary<string, Dictionary<string, string>>>();
                foreach (IndoorBC.Inlet i in inlet) { this.InternalDict.Add(GetTurbulentMixingLengthFrequencyInlet(i, DictionaryName)); }
                foreach (IndoorBC.Outlet i in outlet) { this.InternalDict.Add(GetZeroGradient(i, DictionaryName)); }
                foreach (IndoorBC.Wall i in wall) { this.InternalDict.Add(GetOmegaWallFunction(i, DictionaryName)); }
                foreach (string patch in BoxPatches) { this.InternalDict.Add(GetOmegaWallFunction(patch, DictionaryName)); }

                this.BoundaryFieldDict = new Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>>();
                BoundaryFieldDict.Add("boundaryField", InternalDict);

                this.FullDictString = Serialize(this);
            }
        }

        private static Dictionary<string, Dictionary<string, string>> GetFixedValue(IndoorBC input, string DictName)
        {
            return GetFixedValue(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetFixedValue(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            if (DictName == "U")
            {
                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform (0 0 0)");
            }
            else if (DictName == "p" || DictName == "p_rgh")
            {
                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform 101325");
            }
            else if (DictName == "T")
            {
                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform 300");
            }
            else if (DictName == "aoa")
            {
                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform 0");
            }
            else if (DictName == "covid19")
            {
                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform 0");
            }
            ;

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetFixedValueInlet(IndoorBC.Inlet input, string DictName)
        {
            var dict = GetFixedValue(input.Id, DictName);
            if (DictName == "U")
            {
                var internalDict = dict[input.Id];
                internalDict["value"] = "uniform (" + input.Velocity.ToString().Replace(',', ' ') + ")";
            }
            else if (DictName == "T")
            {
                var internalDict = dict[input.Id];
                internalDict["value"] = "uniform " + input.TemperatureK.ToString();
            }

            return dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetInletOutlet(IndoorBC input, string DictName)
        {
            return GetInletOutlet(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetInletOutlet(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            InternalDict.Add("type", "inletOutlet");
            InternalDict.Add("inletValue", "uniform (0 0 0)");
            InternalDict.Add("value", "uniform (0 0 0)");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetZeroGradient(IndoorBC input, string DictName)
        {
            return GetZeroGradient(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetZeroGradient(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            InternalDict.Add("type", "zeroGradient");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetCalculated(IndoorBC input, string DictName)
        {
            return GetCalculated(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetCalculated(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            if (DictName == "p" || DictName == "p_rgh")
            {
                InternalDict.Add("type", "calculated");
                InternalDict.Add("value", "uniform 101325");
            }
            else
            {
                InternalDict.Add("type", "calculated");
                InternalDict.Add("value", "uniform 0");
            }

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetAlphaWallFunction(IndoorBC input, string DictName)
        {
            return GetAlphaWallFunction(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetAlphaWallFunction(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            InternalDict.Add("type", "compressible::alphatJayatillekeWallFunction");
            InternalDict.Add("Prt", "0.85");
            InternalDict.Add("Cmu", "0.09");
            InternalDict.Add("kappa", "0.41");
            InternalDict.Add("E", "9.8");
            InternalDict.Add("value", "uniform 0");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetNutKWallFunction(IndoorBC input, string DictName)
        {
            return GetNutKWallFunction(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetNutKWallFunction(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            InternalDict.Add("type", "nutkWallFunction");

            InternalDict.Add("value", "uniform 0");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetTurbulentIntensityKineticEnergyInlet(IndoorBC input, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Id, InternalDict);

            InternalDict.Add("type", "turbulentIntensityKineticEnergyInlet");

            InternalDict.Add("intensity", "0.05");

            InternalDict.Add("value", "uniform 5");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetKqRWallFunction(IndoorBC input, string DictName)
        {
            return GetKqRWallFunction(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetKqRWallFunction(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            InternalDict.Add("type", "kqRWallFunction");

            InternalDict.Add("value", "uniform 0.03375");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetTurbulentMixingLengthFrequencyInlet(IndoorBC input, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Id, InternalDict);

            InternalDict.Add("type", "turbulentMixingLengthFrequencyInlet");

            InternalDict.Add("mixingLength", "0.1");
            InternalDict.Add("k", "k");
            InternalDict.Add("value", "uniform 9.18");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetOmegaWallFunction(IndoorBC input, string DictName)
        {
            return GetOmegaWallFunction(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetOmegaWallFunction(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            InternalDict.Add("type", "omegaWallFunction");

            InternalDict.Add("value", "uniform 9.18");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetFixFluxPressure(IndoorBC input, string DictName)
        {
            return GetFixFluxPressure(input.Id, DictName);
        }

        private static Dictionary<string, Dictionary<string, string>> GetFixFluxPressure(string patchName, string DictName)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(patchName, InternalDict);

            InternalDict.Add("type", "fixedFluxPressure");
            InternalDict.Add("gradient", "uniform 0");
            InternalDict.Add("value", "uniform 101325");

            return Dict;
        }

        private string Serialize(IndoorBCDict dict)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(dict.Header);
            sb.AppendLine(dict.Dimensions);
            sb.AppendLine(dict.InternalField);

            sb.AppendLine(CppMapSerializer.Serialize(dict.BoundaryFieldDict));

            return sb.ToString();
        }
    }
}
