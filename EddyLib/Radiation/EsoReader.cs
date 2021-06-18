using ProtoBuf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace EddyLib.Radiation
{
    public enum esoType
    {
        Zone,
        Face,
        Facility,
        Environment
    }

    [DataContract]
    [ProtoContract]
    public class EsoResult
    {
        [DataMember]
        [ProtoMember(1)]
        public string tag { get; set; }

        [DataMember]
        [ProtoMember(2, OverwriteList = true)]
        public List<double> values { get; set; }

        [DataMember]
        [ProtoMember(3)]
        public string unit { get; set; }

        [DataMember]
        [ProtoMember(4)]
        public string res { get; set; }

        [DataMember]
        [ProtoMember(5)]
        public string zone { get; set; }

        [DataMember]
        [ProtoMember(6)]
        public esoType typ { get; set; }

        [DataMember]
        [ProtoMember(7)]
        public string faceId { get; set; }

        public EsoResult()
        { }

        public EsoResult(string _zone, string _tag, string _unit, string _res)
        {
            string zo = "";
            if (_zone.Contains("IDEAL LOADS AIR SYSTEM")) // templates spit out variable with SYSTEM in the end ???
            {
                zo = _zone.Replace("IDEAL LOADS AIR SYSTEM", "").Trim();
                typ = esoType.Zone;
            }
            else if (_zone.Contains("IDEAL LOADS AIR"))
            {
                zo = _zone.Replace("IDEAL LOADS AIR", "").Trim();
                typ = esoType.Zone;
            }
            else if (_zone.Contains("DHW "))
            {
                zo = _zone.Replace("DHW ", "").Trim();
                typ = esoType.Zone;
            }
            else if (_zone.Contains("EQUIPMENT 1"))
            {
                zo = _zone.Replace("EQUIPMENT 1", "").Trim();
                typ = esoType.Zone;
            }
            else if (_zone.Contains(":"))
            {
                var sArr = _zone.Split(':');
                zo = sArr[0].Trim();
                typ = esoType.Face;
                faceId = "";
                for (int i = 1; i < sArr.Length; i++)
                {
                    faceId += ":" + sArr[i];
                }
            }
            else if (_zone.Contains("Environment"))
            {
                zo = _zone;
                typ = esoType.Environment;
            }
            else
            {
                zo = _zone;
                typ = esoType.Zone;
            }

            values = new List<double>();
            zone = zo;
            tag = _tag;
            unit = _unit;
            res = _res;
        }

        public override string ToString()
        {
            return string.Format("EsoResult: {0},{1},{2},{3},{4}", zone, tag, unit, res, values.Count);
        }
    }

    public class EsoReader
    {
        public static List<EsoResult> LoadEsoFile(string path)
        {
            var style = System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowExponent | System.Globalization.NumberStyles.AllowLeadingSign;
            var culture = System.Globalization.CultureInfo.InvariantCulture;

            if (!File.Exists(path)) return null;

            var dictVars = new List<string>();

            //
            // Read in a file line-by-line, and store it all in a List.
            //
            bool pastHeader = false;
            bool pastEnd = false;
            List<string> list = new List<string>();

            using (FileStream fs = File.Open(path, FileMode.Open))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamReader sr = new StreamReader(bs))
            {
                string line;
                sr.ReadLine(); // skip first line
                while ((line = sr.ReadLine()) != null)
                //while ((line = (reader.ReadLine().Trim())) == "End of Data")
                {
                    line.Trim();

                    if (!pastHeader)
                    {
                        if (line == "End of Data Dictionary") { pastHeader = true; }
                        else
                        {
                            dictVars.Add(line); // Add to list.
                        }
                    }
                    else if (!pastEnd)
                    {
                        if (line == "End of Data") { pastEnd = true; }
                        else
                        {
                            list.Add(line); // Add to list.
                        }
                    }
                }
            }

            Dictionary<string, EsoResult> res = new Dictionary<string, EsoResult>();

            // build EsoResult containers
            foreach (string s in dictVars)
            {
                var ss = s.Split(',');

                string id = ss[0];
                // int following = int.Parse(ss[1]);
                if (id == "1") continue;
                if (id == "2") continue;
                if (id == "3") continue;
                if (id == "4") continue;
                if (id == "5") continue;
                if (id == "6") continue;

                string zone = ss[2];
                if (zone.Contains("THERMALCHIMNEYSYSTEM")) zone = zone.Replace("THERMALCHIMNEYSYSTEM", "");
                if (zone.Contains("PEOPLE ")) zone = zone.Replace("PEOPLE ", "");
                //Print("zone: " + zone);

                var sss = ss[3].Split('!');
                string reso = sss[1].Split(' ')[0];
                //Print("reso: " + reso);

                string unit = System.Text.RegularExpressions.Regex.Match(sss[0], @"\[([^)]*)\]").Groups[1].Value;
                //Print("unit: " + unit);

                string vari = sss[0].Split('[')[0].Trim();
                //Print("vari: " + vari);

                res.Add(id, new EsoResult(zone, vari, unit, reso));
            }

            foreach (string s in list)
            {
                var ss = s.Split(',');
                string id = ss[0];

                if (id == "1") continue;
                if (id == "2") continue;
                if (id == "3") continue;
                if (id == "4") continue;
                if (id == "5") continue;
                if (id == "6") continue;

                double val = double.Parse(ss[1], style, culture);

                if (!res.Keys.Contains(id)) continue;

                res[id].values.Add(val);
            }

            return res.Values.ToList();
        }
    }
}