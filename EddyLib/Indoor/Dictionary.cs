using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib.Indoor
{
    /// <summary>
    /// Represents OpenFOAM field dictionaries for indoor simulations.
    /// </summary>
    public class Dicts
    {
        public enum FieldClass
        {
            volVectorField,
            volScalarField
        }

        /// <summary>
        /// Defines how boundary conditions are applied to inlet/outlet/wall boundaries.
        /// </summary>
        public enum BCStrategy
        {
            FixedValue,
            ZeroGradient,
            InletOutlet
        }

        /// <summary>
        /// Configuration for a field dictionary.
        /// </summary>
        public class FieldConfig
        {
            public string Name { get; set; }
            public string Dimensions { get; set; }
            public string InternalField { get; set; }
            public FieldClass FieldClass { get; set; }
            public BCStrategy InletBC { get; set; }
            public BCStrategy OutletBC { get; set; }
            public BCStrategy WallBC { get; set; }
        }

        #region Predefined Field Configurations

        public static readonly FieldConfig UConfig = new FieldConfig
        {
            Name = "U",
            Dimensions = "dimensions      [0 1 -1 0 0 0 0];",
            InternalField = "internalField   uniform (0 0 0);",
            FieldClass = FieldClass.volVectorField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.FixedValue,
            WallBC = BCStrategy.FixedValue
        };

        public static readonly FieldConfig TConfig = new FieldConfig
        {
            Name = "T",
            Dimensions = "dimensions      [0 0 0 1 0 0 0];",
            InternalField = "internalField   uniform 300;",
            FieldClass = FieldClass.volScalarField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.ZeroGradient,
            WallBC = BCStrategy.ZeroGradient
        };

        public static readonly FieldConfig alphatConfig = new FieldConfig
        {
            Name = "alphat",
            Dimensions = "dimensions      [0 0 1 0 0 0 0]",
            InternalField = "internalField   uniform 0;",
            FieldClass = FieldClass.volScalarField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.ZeroGradient,
            WallBC = BCStrategy.ZeroGradient
        };

        public static readonly FieldConfig AoAConfig = new FieldConfig
        {
            Name = "AoA",
            Dimensions = "dimensions      [0 0 0 1 0 0 0];",
            InternalField = "internalField   uniform 0;",
            FieldClass = FieldClass.volScalarField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.ZeroGradient,
            WallBC = BCStrategy.ZeroGradient
        };

        public static readonly FieldConfig kConfig = new FieldConfig
        {
            Name = "k",
            Dimensions = "dimensions      [0 2 -2 0 0 0 0];",
            InternalField = "internalField   uniform 0;",
            FieldClass = FieldClass.volScalarField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.ZeroGradient,
            WallBC = BCStrategy.ZeroGradient
        };

        public static readonly FieldConfig nutConfig = new FieldConfig
        {
            Name = "nut",
            Dimensions = "dimensions      [0 2 -1 0 0 0 0];",
            InternalField = "internalField   uniform 0;",
            FieldClass = FieldClass.volScalarField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.ZeroGradient,
            WallBC = BCStrategy.ZeroGradient
        };

        public static readonly FieldConfig p_rghConfig = new FieldConfig
        {
            Name = "p_rgh",
            Dimensions = "dimensions      [1 -1 -2 0 0 0 0];",
            InternalField = "internalField   uniform 101325;",
            FieldClass = FieldClass.volScalarField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.ZeroGradient,
            WallBC = BCStrategy.ZeroGradient
        };

        public static readonly FieldConfig omegaConfig = new FieldConfig
        {
            Name = "omega",
            Dimensions = "dimensions      [0 0 -1 0 0 0 0];",
            InternalField = "internalField   uniform 0;",
            FieldClass = FieldClass.volScalarField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.ZeroGradient,
            WallBC = BCStrategy.ZeroGradient
        };

        public static readonly FieldConfig pConfig = new FieldConfig
        {
            Name = "p",
            Dimensions = "dimensions [1 -1 -2 0 0 0 0];",
            InternalField = "internalField uniform 101325;",
            FieldClass = FieldClass.volScalarField,
            InletBC = BCStrategy.FixedValue,
            OutletBC = BCStrategy.ZeroGradient,
            WallBC = BCStrategy.ZeroGradient
        };

        #endregion

        #region Instance Properties

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

        private FieldClass fc;

        #endregion

        #region Factory Method

        /// <summary>
        /// Creates a field dictionary from the given configuration and boundary conditions.
        /// </summary>
        public static Dicts CreateField(FieldConfig config, List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
        {
            var dict = new Dicts
            {
                internalDict = new List<Dictionary<string, Dictionary<string, string>>>(),
                inlet = inlet,
                outlet = outlet,
                wall = wall,
                fc = config.FieldClass,
                Name = config.Name,
                dimensions = config.Dimensions,
                internalField = config.InternalField
            };

            dict.location = @"\0\" + dict.Name;
            dict.header = GetHeader(dict);

            // Apply BC strategies
            foreach (var i in inlet) { dict.internalDict.Add(ApplyBC(i, config.InletBC)); }
            foreach (var o in outlet) { dict.internalDict.Add(ApplyBC(o, config.OutletBC)); }
            foreach (var w in wall) { dict.internalDict.Add(ApplyBC(w, config.WallBC)); }

            return dict;
        }

        private static Dictionary<string, Dictionary<string, string>> ApplyBC(IndoorBCs bc, BCStrategy strategy)
        {
            return strategy switch
            {
                BCStrategy.FixedValue => GetFixedValue(bc),
                BCStrategy.ZeroGradient => GetZeroGradient(bc),
                BCStrategy.InletOutlet => GetInletOutlet(bc),
                _ => GetZeroGradient(bc)
            };
        }

        #endregion

        #region Deprecated Nested Classes (For Backward Compatibility)

        [Obsolete("Use Dicts.CreateField(Dicts.UConfig, ...) instead.")]
        public class U : Dicts
        {
            public U(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(UConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        [Obsolete("Use Dicts.CreateField(Dicts.TConfig, ...) instead.")]
        public class T : Dicts
        {
            public T(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(TConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        [Obsolete("Use Dicts.CreateField(Dicts.alphatConfig, ...) instead.")]
        public class alphat : Dicts
        {
            public alphat(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(alphatConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        [Obsolete("Use Dicts.CreateField(Dicts.AoAConfig, ...) instead.")]
        public class AoA : Dicts
        {
            public AoA(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(AoAConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        [Obsolete("Use Dicts.CreateField(Dicts.kConfig, ...) instead.")]
        public class k : Dicts
        {
            public k(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(kConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        [Obsolete("Use Dicts.CreateField(Dicts.nutConfig, ...) instead.")]
        public class nut : Dicts
        {
            public nut(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(nutConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        [Obsolete("Use Dicts.CreateField(Dicts.p_rghConfig, ...) instead.")]
        public class p_rgh : Dicts
        {
            public p_rgh(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(p_rghConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        [Obsolete("Use Dicts.CreateField(Dicts.omegaConfig, ...) instead.")]
        public class omega : Dicts
        {
            public omega(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(omegaConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        [Obsolete("Use Dicts.CreateField(Dicts.pConfig, ...) instead.")]
        public class p : Dicts
        {
            public p(List<IndoorBCs.Inlet> inlet, List<IndoorBCs.Outlet> outlet, List<IndoorBCs.Wall> wall)
            {
                var temp = CreateField(pConfig, inlet, outlet, wall);
                CopyFrom(temp);
            }
        }

        private void CopyFrom(Dicts source)
        {
            this.internalDict = source.internalDict;
            this.inlet = source.inlet;
            this.outlet = source.outlet;
            this.wall = source.wall;
            this.fc = source.fc;
            this.Name = source.Name;
            this.location = source.location;
            this.header = source.header;
            this.dimensions = source.dimensions;
            this.internalField = source.internalField;
            this.boundaryFieldDict = source.boundaryFieldDict;
        }

        #endregion

        #region Helper Methods

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
            var Dict = new Dictionary<string, Dictionary<string, string>>();
            var InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            if (input is IndoorBCs.Inlet ii)
            {
                InternalDict.Add("type", "fixedValue");
                InternalDict.Add("value", "uniform (" + ii.Velocity.ToString().Trim(',') + ")");
            }

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetInletOutlet(IndoorBCs input)
        {
            var Dict = new Dictionary<string, Dictionary<string, string>>();
            var InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);

            InternalDict.Add("type", "inletOutlet");
            InternalDict.Add("inletValue", "uniform (0 0 0)");
            InternalDict.Add("value", "uniform (0 0 0)");

            return Dict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetZeroGradient(IndoorBCs input)
        {
            var Dict = new Dictionary<string, Dictionary<string, string>>();
            var InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name, InternalDict);
            InternalDict.Add("type", "zeroGradient");

            return Dict;
        }

        private string Serialize(Dicts dict)
        {
            var sb = new StringBuilder();
            sb.Append(dict.header);
            sb.Append(dict.dimensions);
            sb.Append(dict.internalField);
            sb.Append(ToCPPDict(dict.boundaryFieldDict));
            return sb.ToString();
        }

        public void Export(string baseWorkingDir)
        {
            var path = baseWorkingDir + this.location;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText(path, this.Serialize(this));
        }

        private string ToCPPDict(Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> boundaryFieldDict)
        {
            return JsonConvert.SerializeObject(boundaryFieldDict);
        }

        #endregion
    }
}
