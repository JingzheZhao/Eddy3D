using EddyLib;
using EddyLib.BCs;
using Rhino.Geometry;
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
        public enum DomainKind
        {
            Box,
            Cyl
        }

        [Theory]
        [InlineData(DomainKind.Box, 13.0, 6.2, 7.9, 23)]
        [InlineData(DomainKind.Cyl, 13.9, 6.82, 7.1, 28)]
        public void SetUpBC_ReturnCorrectBC(
            DomainKind domainKind,
            double expectedUref,
            double expectedZref,
            double expectedZ0,
            int expectedWDir)
        {
            var bc = new ABL(expectedWDir, expectedUref, expectedZref, expectedZ0);
            var bcColl = new BCCollection(bc);

            var caseDir = TestFixtures.CreateTestDirectory(domainKind == DomainKind.Box ? "testcase-box" : "testcase-cyl");
            var (meshSettings, runSettings) = CreateSettings(caseDir);

            RunCase(domainKind, bcColl, meshSettings, runSettings, caseDir);

            AssertAblConditions(caseDir, expectedWDir, expectedUref, expectedZref, expectedZ0);
        }

        private static (OFMeshSettings MeshSettings, OFRunSettings RunSettings) CreateSettings(string caseDir)
        {
            var meshSettings = TestFixtures.CreateDefaultMeshSettings(caseDir);
            var runSettings = TestFixtures.CreateDefaultRunSettings();

            return (meshSettings, runSettings);
        }

        private static void RunCase(
            DomainKind domainKind,
            BCCollection bcColl,
            OFMeshSettings meshSettings,
            OFRunSettings runSettings,
            string caseDir)
        {
            switch (domainKind)
            {
                case DomainKind.Box:
                {
                    var domBox = new OFBoxDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);
                    RunBlockMesh.RunBox(domBox, meshSettings, runSettings, caseDir);
                    RunSnappyAndFoam(domBox, meshSettings, runSettings, caseDir);
                    break;
                }
                case DomainKind.Cyl:
                {
                    var domCyl = new OFCylDomain(Setup.SetUpBuildingMesh(), new Mesh(), bcColl, 20);
                    RunBlockMesh.RunCyl(domCyl, meshSettings, runSettings, caseDir);
                    RunSnappyAndFoam(domCyl, meshSettings, runSettings, caseDir);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(domainKind), domainKind, "Unknown domain kind.");
            }
        }

        private static void RunSnappyAndFoam(
            OFBaseDomain domain,
            OFMeshSettings meshSettings,
            OFRunSettings runSettings,
            string caseDir)
        {
            RunSnappy.Run(domain, meshSettings, runSettings, out _);
            RunFoamSimulation.Run(domain, meshSettings, runSettings, caseDir);
        }

        private static void AssertAblConditions(
            string caseDir,
            int expectedWDir,
            double expectedUref,
            double expectedZref,
            double expectedZ0)
        {
            var ablPath = Path.Combine(caseDir, expectedWDir.ToString(), "0", "ABLConditions");
            Assert.True(File.Exists(ablPath), "Expected ABLConditions file not found at: " + ablPath);

            var kv = ABLConditionsReader.Read(ablPath);

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
                if (!kv.TryGetValue(key, out var s))
                    throw new KeyNotFoundException("Missing key '" + key + "' in ABLConditions.");
                var num = FirstNumber.Match(s);
                if (!num.Success)
                    throw new FormatException("No numeric value found for key '" + key + "': '" + s + "'");

                return double.Parse(num.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);
            }

            public static int[] GetDirectionsIfPresent(IDictionary<string, string> kv)
            {
                if (!kv.TryGetValue("Directions", out var raw)) return null;

                // Directions can be "0,45,..." OR "(0 45 ...)" - handle both
                raw = raw.Trim();
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
