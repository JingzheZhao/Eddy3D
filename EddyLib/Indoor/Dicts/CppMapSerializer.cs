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
                sb.AppendLine(key + "\t" + dict[key] + ";");
            }
        }
    }

    public class CppMapSerializerDyn
    {
        public static string Serialize(Dictionary<string, dynamic> dict)
        {
            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, dynamic> evl in dict)
            {
                // 4. Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> dicts

                if (evl.Key is String && evl.Value is List<Dictionary<string, dynamic>>)
                {
                    sb.AppendLine(Serialize(evl.Value));
                }

                // 1. Dictionary<string, string> dict
                else if (evl.Key is String && evl.Value is String || evl.Value is Int32)
                {
                    DictToString(evl, ref sb);
                }

                //2.Dictionary<string, Dictionary<string, string>> dicts
                else if (evl.Key is String && evl.Value is Dictionary<string, dynamic>)
                {
                    DictToString(evl.Key, evl.Value, ref sb);
                }

                //// 3. Dictionary<string, int> dicts
                //else
                //{
                //    DictToString(evl, ref sb);
                //}
            }

            return sb.ToString();
        }

        public static string Serialize(List<Dictionary<string, dynamic>> dicts)
        {
            StringBuilder sb = new StringBuilder();

            // 3. List<Dictionary<string, Dictionary<string, string>>> dicts
            foreach (var dict in dicts)
            {
                foreach (KeyValuePair<string, dynamic> evl in dict)
                {
                    // 1. Dictionary<string, string> dict
                    if (evl.Key is String && evl.Value is String)
                    {
                        DictToString(evl, ref sb);
                    }

                    // 2. Dictionary<string, Dictionary<string, string>> dicts
                    else if (evl.Key is String && evl.Value is Dictionary<string, dynamic>)
                    {
                        DictToString(evl.Value, ref sb);
                    }
                }
            }

            return sb.ToString();
        }

        public static string Serialize(Dictionary<string, List<Dictionary<string, dynamic>>> dictss)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var list in dictss)
            {
                // 3. List<Dictionary<string, Dictionary<string,string>>> dicts
                foreach (var dict in list.Value)
                {
                    foreach (KeyValuePair<string, dynamic> evl in dict)
                    {
                        // 1. Dictionary<string, string> dict
                        if (evl.Key is String && evl.Value is String)
                        {
                            DictToString(evl, ref sb);
                        }

                        // 2. Dictionary<string, Dictionary<string, string>> dicts
                        else if (evl.Key is String && evl.Value is Dictionary<string, dynamic>)
                        {

                            DictToString(evl.Value, ref sb);
                         
                        }
                    }
                }
            }

            return sb.ToString();
        }


        private static void DictToString(List<Dictionary<string, dynamic>> dicts, ref StringBuilder sb)
        {
            foreach (var dict in dicts)
            {
                foreach (KeyValuePair<string, dynamic> evl in dict)
                {
                    sb.AppendLine();
                    sb.AppendLine(evl.Key);
                    sb.AppendLine("{");

                    //  foreach (var dict in dict[evl.Key]) {
                    if (evl.Key is String && evl.Value is String)
                    {
                        DictToString(evl.Key, evl.Value, ref sb);
                    }

                    // }
                    sb.AppendLine("}");
                }
            }
        }

        private static void DictToString(string name, Dictionary<string, dynamic> dict, ref StringBuilder sb)
        {
            sb.AppendLine("\t" + name);

            sb.AppendLine("\t" + "{");

            foreach (KeyValuePair<string, dynamic> evl in dict)
            {
                if (evl.Key is String && evl.Value is String)
                {
                    sb.AppendLine("\t" + "\t" + evl.Key + "\t" + evl.Value + ";");
                }
                else if (evl.Key is String && evl.Value is Int32)
                {
                    sb.AppendLine("\t" + "\t" + evl.Key + "\t" + evl.Value.ToString() + ";");
                }
                else
                {
                    sb.AppendLine("\t" + "\t" + evl.Key);

                    sb.AppendLine("\t" + "\t" + "{");

                    sb.AppendLine("\t" + "\t" + "\t" + Serialize(evl.Value));

                    sb.AppendLine("\t" + "\t" + "}");
                }
            }

            sb.AppendLine("\t" + "}");
        }

        private static void DictToString(KeyValuePair<string, dynamic> evl, ref StringBuilder sb)
        {
            if (evl.Key is String)
            {
                if (evl.Value is String)
                {
                    sb.AppendLine(evl.Key + "\t " + evl.Value + ";");
                }
                else
                {
                    sb.AppendLine(evl.Key + "\t " + evl.Value.ToString() + ";");
                }
            }
        }
    }
}