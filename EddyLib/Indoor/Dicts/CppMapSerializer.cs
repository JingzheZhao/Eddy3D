using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.Dicts
{
    public class CppMapSerializer
    {
        public static string Serialize(Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> dicts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var outerKey in dicts.Keys)
            {
                sb.AppendLine();
                sb.AppendLine(outerKey);
                sb.AppendLine("{");

                foreach (var dict in dicts[outerKey])
                {
                    foreach (var key in dict.Keys)
                    {
                        DictToString(key, dict[key], ref sb);
                    }
                }
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        public static string Serialize(List<Dictionary<string, Dictionary<string, string>>> dicts)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var dict in dicts)
            {
                foreach (var key in dict.Keys)
                {
                    DictToString(key, dict[key], ref sb);
                }
            }

            return sb.ToString();
        }

        public static string Serialize(Dictionary<string, string> dict)
        {
            StringBuilder sb = new StringBuilder();

            DictToString(dict, ref sb);

            return sb.ToString();
        }

        public static string Serialize(Dictionary<string, Dictionary<string, string>> dicts)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var key in dicts.Keys)
            {
                DictToString(key, dicts[key], ref sb);
            }

            return sb.ToString();
        }

        private static void DictToString(string name, Dictionary<string, string> dict, ref StringBuilder sb)
        {
            sb.AppendLine("\t" + name);

            sb.AppendLine("\t" + "{");

            foreach (var key in dict.Keys)
            {
                sb.AppendLine("\t" + "\t" + key + "\t" + dict[key] + ";");
            }

            sb.AppendLine("\t" + "}");
        }

        private static void DictToString(Dictionary<string, string> dict, ref StringBuilder sb)
        {
            foreach (var key in dict.Keys)
            {
                sb.AppendLine("\t" + "\t" + key + "\t" + dict[key] + ";");
            }
        }
    }
}