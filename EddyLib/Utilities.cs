using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EddyLib
{
    public static class Utilities
    {
        //static public string hardcodedAssemblyDir = @"C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\VirtualWindTunnel\bin\";
        //static public string hardcodedAssemblyDir = @"C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\bin\";

        

        static public string AssemblyVersion
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fvi.FileVersion;
            }
        }

        public static string GetDirectoryPath(this Assembly assembly)
        {
            string filePath = new Uri(assembly.CodeBase).LocalPath;
            return Path.GetDirectoryName(filePath);
        }

        static public string AssemblyDirectory
        {
            get
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                var localDir = Assembly.GetExecutingAssembly().GetDirectoryPath();
                var dir1 = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);


                var bla1 = Assembly.GetEntryAssembly();    //gives you the entrypoint assembly for the process.
                var bla2 = Assembly.GetCallingAssembly();   // gives you the assembly from which the current method was called.
                var bla3 = Assembly.GetExecutingAssembly(); // gives you the assembly in which the currently executing code is defined
                var bla4 = Assembly.GetAssembly(typeof(OFBaseDomain));  // gives you the assembly in which the specified type is defined.
                var loc = bla4.Location;
                string path2 = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        //(c) Vasian Cepa 2005
        // Version 2 http://www.codeproject.com/Articles/11016/Numeric-String-Sort-in-C


        public static string FixDirectories(string dir)
        {
            if (!dir.EndsWith(@"\"))
            {
                dir = dir + @"\";
            }
            return dir;
        }


        public static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }



        public static bool CheckLicence()
        {
            bool licence = false;
            //DateTime dateNow = Utilities.GetNistTime();
            DateTime dateCompile = new DateTime(2018, 5, 21, 0, 00, 00).ToUniversalTime();
            TimeSpan licenceDuration = new TimeSpan(60, 0, 0, 0);
            DateTime expiresAt = dateCompile.Add(licenceDuration);

            //try
            //{

            //    DateTime dateTime = DateTime.MinValue;
            //    DateTime dateTimeUTC = DateTime.MinValue;

            //    System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://nist.time.gov/actualtime.cgi?lzbc=siqm9b");
            //    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            //    request.Method = "GET";
            //    request.Accept = "text/html, application/xhtml+xml, */*";
            //    request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; Trident/6.0)";
            //    request.ContentType = "application/x-www-form-urlencoded";
            //    //request.ProtocolVersion = HttpVersion.Version11;            
            //    //request.CachePolicy = new RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore); //No caching
            //    System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            //    if (response.StatusCode == (System.Net.HttpStatusCode.OK))
            //    {
            //        System.IO.StreamReader stream = new StreamReader(response.GetResponseStream());
            //        string html = stream.ReadToEnd();//<timestamp time=\"1395772696469995\" delay=\"1395772696469995\"/>
            //        string time = System.Text.RegularExpressions.Regex.Match(html, @"(?<=\btime="")[^""]*").Value;
            //        double milliseconds = Convert.ToInt64(time) / 1000.0;
            //        dateTime = new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();
            //        dateTimeUTC = dateTime.ToUniversalTime();
            //    }

            //    if ((dateTimeUTC - dateCompile) > licenceDuration)
            //    {
            //        licence = false;
            //    }
            //    else
            //    {
            //        licence = true;
            //    }
            //}
            //catch(Exception e) {

            //    Debug.WriteLine(e.Message);

            //    if (DateTime.Now > expiresAt) licence = false;
            //    else licence = true;

            //}


            if (DateTime.Now > expiresAt) licence = false;
            else licence = true;


            return licence;
        }


        public class NumericComparer : IComparer
        {
            public NumericComparer()
            { }

            public int Compare(object x, object y)
            {
                if ((x is string) && (y is string))
                {
                    return StringLogicalComparer.Compare((string)x, (string)y);
                }
                return -1;
            }
        }//EOC



        // emulates StrCmpLogicalW, but not fully
        public class StringLogicalComparer
        {
            public static int Compare(string s1, string s2)
            {
                //get rid of special cases
                if ((s1 == null) && (s2 == null)) return 0;
                else if (s1 == null) return -1;
                else if (s2 == null) return 1;

                if ((s1.Equals(string.Empty) && (s2.Equals(string.Empty)))) return 0;
                else if (s1.Equals(string.Empty)) return -1;
                else if (s2.Equals(string.Empty)) return -1;

                //WE style, special case
                bool sp1 = Char.IsLetterOrDigit(s1, 0);
                bool sp2 = Char.IsLetterOrDigit(s2, 0);
                if (sp1 && !sp2) return 1;
                if (!sp1 && sp2) return -1;

                int i1 = 0, i2 = 0; //current index
                int r = 0; // temp result
                while (true)
                {
                    bool c1 = Char.IsDigit(s1, i1);
                    bool c2 = Char.IsDigit(s2, i2);
                    if (!c1 && !c2)
                    {
                        bool letter1 = Char.IsLetter(s1, i1);
                        bool letter2 = Char.IsLetter(s2, i2);
                        if ((letter1 && letter2) || (!letter1 && !letter2))
                        {
                            if (letter1 && letter2)
                            {
                                r = Char.ToLower(s1[i1]).CompareTo(Char.ToLower(s2[i2]));
                            }
                            else
                            {
                                r = s1[i1].CompareTo(s2[i2]);
                            }
                            if (r != 0) return r;
                        }
                        else if (!letter1 && letter2) return -1;
                        else if (letter1 && !letter2) return 1;
                    }
                    else if (c1 && c2)
                    {
                        r = CompareNum(s1, ref i1, s2, ref i2);
                        if (r != 0) return r;
                    }
                    else if (c1)
                    {
                        return -1;
                    }
                    else if (c2)
                    {
                        return 1;
                    }
                    i1++;
                    i2++;
                    if ((i1 >= s1.Length) && (i2 >= s2.Length))
                    {
                        return 0;
                    }
                    else if (i1 >= s1.Length)
                    {
                        return -1;
                    }
                    else if (i2 >= s2.Length)
                    {
                        return -1;
                    }
                }
            }

            private static int CompareNum(string s1, ref int i1, string s2, ref int i2)
            {
                int nzStart1 = i1, nzStart2 = i2; // nz = non zero
                int end1 = i1, end2 = i2;

                ScanNumEnd(s1, i1, ref end1, ref nzStart1);
                ScanNumEnd(s2, i2, ref end2, ref nzStart2);
                int start1 = i1; i1 = end1 - 1;
                int start2 = i2; i2 = end2 - 1;

                int nzLength1 = end1 - nzStart1;
                int nzLength2 = end2 - nzStart2;

                if (nzLength1 < nzLength2) return -1;
                else if (nzLength1 > nzLength2) return 1;

                for (int j1 = nzStart1, j2 = nzStart2; j1 <= i1; j1++, j2++)
                {
                    int r = s1[j1].CompareTo(s2[j2]);
                    if (r != 0) return r;
                }
                // the nz parts are equal
                int length1 = end1 - start1;
                int length2 = end2 - start2;
                if (length1 == length2) return 0;
                if (length1 > length2) return -1;
                return 1;
            }

            //lookahead
            private static void ScanNumEnd(string s, int start, ref int end, ref int nzStart)
            {
                nzStart = start;
                end = start;
                bool countZeros = true;
                while (Char.IsDigit(s, end))
                {
                    if (countZeros && s[end].Equals('0'))
                    {
                        nzStart++;
                    }
                    else countZeros = false;
                    end++;
                    if (end >= s.Length) break;
                }
            }

        }//EOC
    }

}

