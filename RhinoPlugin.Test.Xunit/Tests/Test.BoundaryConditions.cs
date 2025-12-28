using EddyLib;
using EddyLib.BCs;
using Rhino.Geometry;
using RhinoPlugin.Test.Xunit.Tests;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class OFBoundaryConditionTests

    {
        [Fact]
        public void SetUpBCWithBox_ReturnCorrectBC()
        {
            // Arrange (your existing values)

            // Expected values from your setup
            const double expectedUref = 13.0;
            const double expectedZref = 6.2;
            const double expectedZ0 = 7.9;
            const int expectedWDir = 23;

            var bc1 = new ABL(expectedWDir, expectedUref, expectedZref, expectedZ0);

            var bcColl = new BCCollection(bc1);

            var caseDir = Path.Combine(Path.GetTempPath(), $"testcase-box-{Guid.NewGuid():N}\\");

            Directory.CreateDirectory(caseDir);

            var meshSettings = new OFMeshSettings
            {
                accBuildings = 3,
                accFeatures = 4,
                accGround = 3
            };
            meshSettings.SetDirectories(caseDir);

            var runSettings = new OFRunSettings
            {
                iter = 3000,
                CPUs = 6,
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Optimized
            };

            var domBox = new OFBoxDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunBox(domBox, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domBox, meshSettings, runSettings, out var logfileOutput);
            RunFoamSimulation.Run(domBox, meshSettings, runSettings, caseDir);

            // var result = OFExecutionTests.RunBatchFileInteractive(caseDir, "run.bat");

            // Point to the file your plugin wrote (absolute path you gave)
            var ablPath = caseDir + $@"{expectedWDir}\0\ABLConditions";
            Assert.True(File.Exists(ablPath), "Expected ABLConditions file not found at: " + ablPath);

            // Read without touching the file
            var kv = ABLConditionsReader.Read(ablPath);

            // Check required numeric entries with tolerance (xUnit: precision = # of decimal places)
            Assert.Equal(expectedUref, ABLConditionsReader.GetDouble(kv, "Uref"), 2);
            Assert.Equal(expectedZref, ABLConditionsReader.GetDouble(kv, "Zref"), 2);
            Assert.Equal(expectedZ0, ABLConditionsReader.GetDouble(kv, "Z0"), 2);
        }

        [Fact]
        public void SetUpBCWithCyl_ReturnCorrectBC()
        {
            // Arrange (your existing values)

            // Expected values from your setup
            const double expectedUref = 13.9;
            const double expectedZref = 6.82;
            const double expectedZ0 = 7.1;
            const int expectedWDir = 28;

            var bc1 = new ABL(expectedWDir, expectedUref, expectedZref, expectedZ0);

            var bcList = new List<BC> { bc1 };
            var bcColl = new BCCollection(bcList);

            var caseDir = Path.Combine(Path.GetTempPath(), $"testcase-cyl-{Guid.NewGuid():N}\\");

            // Clean up the directory and all contents at the beginning for debugging
            //if (Directory.Exists(caseDir))
            //{
            //    Directory.Delete(caseDir, true);
            //}
            Directory.CreateDirectory(caseDir);

            var meshSettings = new OFMeshSettings
            {
                accBuildings = 3,
                accFeatures = 4,
                accGround = 3
            };
            meshSettings.SetDirectories(caseDir);

            var runSettings = new OFRunSettings
            {
                iter = 3000,
                CPUs = 6,
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Optimized
            };

            var domCyl = new OFCylDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);

            // Act: Generate the OpenFOAM case and batch files
            RunBlockMesh.RunCyl(domCyl, meshSettings, runSettings, caseDir);
            RunSnappy.Run(domCyl, meshSettings, runSettings, out var logfileOutput);
            RunFoamSimulation.Run(domCyl, meshSettings, runSettings, caseDir);

            // var result = OFExecutionTests.RunBatchFileInteractive(caseDir, "run.bat");

            // Point to the file your plugin wrote (absolute path you gave)
            var ablPath = caseDir + $@"{expectedWDir}\0\ABLConditions";
            Assert.True(File.Exists(ablPath), "Expected ABLConditions file not found at: " + ablPath);

            // Read without touching the file
            var kv = ABLConditionsReader.Read(ablPath);

            // Check required numeric entries with tolerance (xUnit: precision = # of decimal places)
            Assert.Equal(expectedUref, ABLConditionsReader.GetDouble(kv, "Uref"), 2);
            Assert.Equal(expectedZref, ABLConditionsReader.GetDouble(kv, "Zref"), 2);
            Assert.Equal(expectedZ0, ABLConditionsReader.GetDouble(kv, "Z0"), 2);
        }

        private static class ABLConditionsReader
        {
            private static readonly Regex FoamPair = new Regex(
                @"^\s*\{\s*\[\s*(?<key>[^,\]]+)\s*,\s*(?<val>[^;]+)\s*;\s*\]\s*\}\s*$",
                RegexOptions.Compiled);

            private static readonly Regex WsPair = new Regex(
                @"^\s*(?<key>\S+)\s+(?<val>.+?)\s*$",
                RegexOptions.Compiled);

            // number like 7, 7.9, -0.391, 1e-3, -2.5E+02
            private static readonly Regex FirstNumber = new Regex(
                @"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?",
                RegexOptions.Compiled);

            public static Dictionary<string, string> Read(string ablConditionsPath)
            {
                if (!File.Exists(ablConditionsPath))
                    throw new FileNotFoundException("ABLConditions file not found.", ablConditionsPath);

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var lines = File.ReadAllLines(ablConditionsPath);

                foreach (var raw in lines)
                {
                    var l = raw.Trim();
                    if (l.Length == 0) continue;               // blank
                    if (l.StartsWith("#")) continue;           // comment

                    // Prefer OpenFOAM-style pair: {[ key, value; ]}
                    var m = FoamPair.Match(l);
                    if (m.Success)
                    {
                        var key = m.Groups["key"].Value.Trim();
                        var val = m.Groups["val"].Value.Trim();
                        dict[key] = val;
                        continue;
                    }

                    // Fallback: plain "key   value"
                    var m2 = WsPair.Match(l);
                    if (m2.Success)
                    {
                        var key = m2.Groups["key"].Value.Trim();
                        var val = m2.Groups["val"].Value.Trim();
                        dict[key] = val;
                    }
                }

                return dict;
            }

            public static double GetDouble(IDictionary<string, string> kv, string key)
            {
                if (!kv.ContainsKey(key))
                    throw new KeyNotFoundException("Missing key '" + key + "' in ABLConditions.");

                var s = kv[key];
                var num = FirstNumber.Match(s);
                if (!num.Success)
                    throw new FormatException("No numeric value found for key '" + key + "': '" + s + "'");

                return double.Parse(num.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            }

            public static int[] GetDirectionsIfPresent(IDictionary<string, string> kv)
            {
                if (!kv.ContainsKey("Directions")) return null;

                // Directions can be "0,45,..." OR "(0 45 ...)" — handle both
                var raw = kv["Directions"].Trim();

                // If parenthesized vector, replace whitespace with commas
                if (raw.StartsWith("(") && raw.EndsWith(")"))
                    raw = raw.Substring(1, raw.Length - 2).Trim().Replace("\t", " ");

                // Split on comma OR whitespace
                var parts = Regex.Split(raw, @"\s*,\s*|\s+")
                                 .Where(p => p.Length > 0)
                                 .ToArray();

                var result = new int[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    result[i] = int.Parse(parts[i], CultureInfo.InvariantCulture);
                return result;
            }
        }
    }
}