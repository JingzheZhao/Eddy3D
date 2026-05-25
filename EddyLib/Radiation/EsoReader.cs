using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

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
        private static readonly HashSet<int> ReservedIds =
            new HashSet<int> { 1, 2, 3, 4, 5, 6 };

        private static readonly NumberStyles NumberStyle =
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;

        // Patterns to remove from zone names
        private static readonly string[] ZoneCleanupPatterns =
        {
            "THERMALCHIMNEYSYSTEM",
            "PEOPLE "
        };

        /// <summary>
        /// Loads and parses an ESO file using a single-pass optimized reader.
        /// </summary>
        public static List<EsoResult> LoadEsoFile(string path)
        {
            if (!File.Exists(path)) return null;

            var results = new Dictionary<int, EsoResult>();

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            using (var sr = new StreamReader(fs))
            {
                sr.ReadLine(); // Skip first line (Program Version)

                bool inDictionary = true;
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    ReadOnlySpan<char> span = line.AsSpan().Trim();
                    if (span.IsEmpty) continue;

                    if (inDictionary)
                    {
                        if (span.SequenceEqual("End of Data Dictionary".AsSpan()))
                        {
                            inDictionary = false;
                            continue;
                        }

                        // Dictionary lines are comma-separated: ID,Count,Zone,Variable...
                        int comma1 = span.IndexOf(',');
                        if (comma1 == -1) continue;

                        if (int.TryParse(span.Slice(0, comma1), out int id))
                        {
                            if (ReservedIds.Contains(id)) continue;

                            // Find next commas
                            var remainder = span.Slice(comma1 + 1);
                            int comma2 = remainder.IndexOf(',');
                            if (comma2 == -1) continue;

                            var remainder2 = remainder.Slice(comma2 + 1);
                            int comma3 = remainder2.IndexOf(',');
                            if (comma3 == -1) continue;

                            string zone = remainder2.Slice(0, comma3).ToString();
                            foreach (var pattern in ZoneCleanupPatterns)
                            {
                                zone = zone.Replace(pattern, "");
                            }

                            // Variable info is in the 4th field: "Variable Name [unit] !Resolution"
                            var varInfo = remainder2.Slice(comma3 + 1);
                            int bangIndex = varInfo.IndexOf('!');
                            if (bangIndex == -1) continue;

                            var nameAndUnit = varInfo.Slice(0, bangIndex);
                            var resolutionPart = varInfo.Slice(bangIndex + 1).Trim();
                            int spaceIdx = resolutionPart.IndexOf(' ');
                            string resolution = spaceIdx == -1 ? resolutionPart.ToString() : resolutionPart.Slice(0, spaceIdx).ToString();

                            string unit = "";
                            int bracketOpen = nameAndUnit.IndexOf('[');
                            int bracketClose = nameAndUnit.IndexOf(']');
                            if (bracketOpen != -1 && bracketClose != -1 && bracketClose > bracketOpen)
                            {
                                unit = nameAndUnit.Slice(bracketOpen + 1, bracketClose - bracketOpen - 1).ToString();
                            }

                            string variable = bracketOpen == -1 ? nameAndUnit.Trim().ToString() : nameAndUnit.Slice(0, bracketOpen).Trim().ToString();

                            results.Add(id, new EsoResult(zone, variable, unit, resolution));
                        }
                    }
                    else
                    {
                        if (span.SequenceEqual("End of Data".AsSpan())) break;

                        // Data lines: ID,Value
                        int commaIndex = span.IndexOf(',');
                        if (commaIndex == -1) continue;

                        if (int.TryParse(span.Slice(0, commaIndex), out int id))
                        {
                            if (results.TryGetValue(id, out var result))
                            {
                                var valueSpan = span.Slice(commaIndex + 1);
                                // Check for trailing commas in case of multi-value lines (we only want the first data value)
                                int nextComma = valueSpan.IndexOf(',');
                                if (nextComma != -1) valueSpan = valueSpan.Slice(0, nextComma);

                                if (double.TryParse(valueSpan, NumberStyle, CultureInfo.InvariantCulture, out double val))
                                {
                                    result.values.Add(val);
                                }
                            }
                        }
                    }
                }
            }

            return results.Values.ToList();
        }
    }
}
