using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace EddyLib.Radiation
{
    /// <summary>
    /// Type of EnergyPlus ESO result.
    /// </summary>
    public enum esoType
    {
        Zone,
        Face,
        Facility,
        Environment
    }

    /// <summary>
    /// EnergyPlus ESO file result entry.
    /// </summary>
    [DataContract]
    [ProtoContract]
    public class EsoResult
    {
        #region Properties

        [DataMember, ProtoMember(1)]
        public string tag { get; set; }

        [DataMember, ProtoMember(2, OverwriteList = true)]
        public List<double> values { get; set; }

        [DataMember, ProtoMember(3)]
        public string unit { get; set; }

        [DataMember, ProtoMember(4)]
        public string res { get; set; }

        [DataMember, ProtoMember(5)]
        public string zone { get; set; }

        [DataMember, ProtoMember(6)]
        public esoType typ { get; set; }

        [DataMember, ProtoMember(7)]
        public string faceId { get; set; }

        #endregion

        // Zone name prefixes to strip
        private static readonly string[] ZonePrefixes =
        {
            "IDEAL LOADS AIR SYSTEM",
            "IDEAL LOADS AIR",
            "DHW ",
            "EQUIPMENT 1"
        };

        public EsoResult() { }

        public EsoResult(string zoneInput, string _tag, string _unit, string _res)
        {
            values = new List<double>();
            tag = _tag;
            unit = _unit;
            res = _res;

            ParseZone(zoneInput);
        }

        /// <summary>
        /// Parses zone string and sets zone name, type, and face ID.
        /// </summary>
        private void ParseZone(string zoneInput)
        {
            // Check for known prefixes
            foreach (var prefix in ZonePrefixes)
            {
                if (zoneInput.Contains(prefix))
                {
                    zone = zoneInput.Replace(prefix, "").Trim();
                    typ = esoType.Zone;
                    return;
                }
            }

            // Check for face reference (contains colon)
            if (zoneInput.Contains(":"))
            {
                var parts = zoneInput.Split(':');
                zone = parts[0].Trim();
                typ = esoType.Face;
                faceId = string.Join(":", parts.Skip(1));
                return;
            }

            // Check for environment
            if (zoneInput.Contains("Environment"))
            {
                zone = zoneInput;
                typ = esoType.Environment;
                return;
            }

            // Default to zone type
            zone = zoneInput;
            typ = esoType.Zone;
        }

        public override string ToString()
        {
            return $"EsoResult: {zone},{tag},{unit},{res},{values.Count}";
        }
    }

    /// <summary>
    /// Reads EnergyPlus ESO (raw output) files.
    /// </summary>
    public class EsoReader
    {
        // IDs 1-6 are reserved for timestamp/environment data
        private static readonly HashSet<string> ReservedIds =
            new HashSet<string> { "1", "2", "3", "4", "5", "6" };

        private static readonly NumberStyles NumberStyle =
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;

        // Patterns to remove from zone names
        private static readonly string[] ZoneCleanupPatterns =
        {
            "THERMALCHIMNEYSYSTEM",
            "PEOPLE "
        };

        /// <summary>
        /// Loads and parses an ESO file.
        /// </summary>
        public static List<EsoResult> LoadEsoFile(string path)
        {
            if (!File.Exists(path)) return null;

            var dictVars = new List<string>();
            var dataLines = new List<string>();

            // Read file in two passes: dictionary section and data section
            ReadEsoFileSections(path, dictVars, dataLines);

            // Build result containers from dictionary
            var results = BuildResultContainers(dictVars);

            // Populate values from data lines
            PopulateValues(dataLines, results);

            return results.Values.ToList();
        }

        /// <summary>
        /// Reads ESO file and separates dictionary and data sections.
        /// </summary>
        private static void ReadEsoFileSections(string path, List<string> dictVars, List<string> dataLines)
        {
            using (var fs = File.Open(path, FileMode.Open))
            using (var bs = new BufferedStream(fs))
            using (var sr = new StreamReader(bs))
            {
                sr.ReadLine(); // Skip first line

                bool pastHeader = false;
                bool pastEnd = false;
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (!pastHeader)
                    {
                        if (line == "End of Data Dictionary")
                        {
                            pastHeader = true;
                        }
                        else
                        {
                            dictVars.Add(line);
                        }
                    }
                    else if (!pastEnd)
                    {
                        if (line == "End of Data")
                        {
                            pastEnd = true;
                        }
                        else
                        {
                            dataLines.Add(line);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Builds EsoResult containers from dictionary entries.
        /// </summary>
        private static Dictionary<string, EsoResult> BuildResultContainers(List<string> dictVars)
        {
            var results = new Dictionary<string, EsoResult>();

            foreach (string s in dictVars)
            {
                var parts = s.Split(',');
                string id = parts[0];

                if (ReservedIds.Contains(id)) continue;
                if (parts.Length < 4) continue;

                // Clean up zone name
                string zone = parts[2];
                foreach (var pattern in ZoneCleanupPatterns)
                {
                    zone = zone.Replace(pattern, "");
                }

                // Parse variable info: "Variable Name [unit] !Resolution"
                var varParts = parts[3].Split('!');
                if (varParts.Length < 2) continue;

                string resolution = varParts[1].Split(' ')[0];
                string unit = Regex.Match(varParts[0], @"\[([^\]]*)\]").Groups[1].Value;
                string variable = varParts[0].Split('[')[0].Trim();

                results.Add(id, new EsoResult(zone, variable, unit, resolution));
            }

            return results;
        }

        /// <summary>
        /// Populates result values from data lines.
        /// </summary>
        private static void PopulateValues(List<string> dataLines, Dictionary<string, EsoResult> results)
        {
            foreach (string line in dataLines)
            {
                var parts = line.Split(',');
                if (parts.Length < 2) continue;

                string id = parts[0];
                if (ReservedIds.Contains(id)) continue;
                if (!results.ContainsKey(id)) continue;

                if (double.TryParse(parts[1], NumberStyle, CultureInfo.InvariantCulture, out double val))
                {
                    results[id].values.Add(val);
                }
            }
        }
    }
}