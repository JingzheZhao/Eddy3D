using Rhino.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;


namespace EddyLib
{
    public static class Utilities
    {
        //static public string hardcodedAssemblyDir = @"C:\Users\Patrick Kastner\Documents\GitHub\WindTunnel\VirtualWindTunnel\bin\";
        //static public string hardcodedAssemblyDir = @"C:\Users\pkastner\Documents\GitHub\WindTunnel\Eddy\bin\";



        public static string AssemblyVersion
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

        public static string AssemblyDirectory
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

        public static object GH_RuntimeMessageLevel { get; private set; }

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


        public static double CalculateRunTimeFromLog(string simulationDirectory, int iter)
        {



            string logFilePath = simulationDirectory + @"\log";

            double timeEnd = 0;

            if (File.Exists(logFilePath))
            {

                try
                {
                    String line;
                    List<String> lines = new List<String>();


                    //var time1 = "0";
                    var time2 = "0";

                    using (var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, System.Text.Encoding.Default))
                    {


                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }


                    foreach (var lline in lines.Select((value, index) => new { value, index }))
                    {
                        // Use x.value and x.index in here


                        if (lline.value.StartsWith("SIMPLE solution converged"))
                        {
                            time2 = lines[lline.index - 3].Split("ClockTime".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[3].Replace("=", "").Replace("s", "").Trim();//.Replace("s", "")

                            //timeElapsed = TimeSpan.FromSeconds(double.Parse(time2));
                        }

                        if (lline.value.EndsWith(iter.ToString()))
                        {
                            time2 = lines[lline.index + 10].Split("ClockTime".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[3].Replace("=", "").Replace("s", "").Trim();//.Replace("s", "")
                            break;


                            //timeElapsed = TimeSpan.FromSeconds(double.Parse(time2));
                        }

                        else
                        {
                            timeEnd = 0;
                        }


                        timeEnd = double.Parse(time2) / 60;
                    }


                }
                catch (Exception)
                {

                    throw;
                }
            }

            return timeEnd;
        }

        public static bool IsDockerRunning(string workingDirectory, OSType ostype)
        {

            bool running = false;
            string fp = workingDirectory + @"\dockerStatus";

            var lines = Utilities.FileReader(fp);

            if (OSType.Windows7 != ostype)
            {

                foreach (string line in lines)
                {
                    if (line.StartsWith("Containers"))
                    {
                        running = true;
                    }
                }
            }
            else
            {
                // Assume that Docker is always running for Windows 7 for now
                running = true;
            }

            return running;
        }

        public static void WriteDockerInfo(string workingDirectory)
        {
            StartProcessCMD(@"docker info > """ + workingDirectory + @"\dockerStatus""",true, true, true);          

        }

        public static void StartProcessCMD(string argument,bool createnowindow,  bool waitforexit = false, bool close = false, string executable = @"C:\Windows\System32\cmd.exe")
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = executable;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;            
            p.StartInfo.CreateNoWindow = createnowindow;
            p.Start();
            StreamWriter sw = p.StandardInput;
            String strInputText = argument;
            sw.WriteLine(strInputText);

            sw.Flush();
            if (waitforexit) { p.WaitForExit(); }
            if (close) { p.Close(); }                    

        }

        public static IEnumerable<List<T>> splitListGen<T>(List<T> locations, int nSize)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        public static List<List<Point3d>> splitPointList(List<Point3d> locations, int nSize)
        {
            var list = new List<List<Point3d>>();

            for (int i = 0; i < locations.Count; i += nSize)
            {
                list.Add(locations.GetRange(i, Math.Min(nSize, locations.Count - i)));
            }

            return list;
        }

        public static Point3d[] RightShift(Point3d[] array)
        {
            // the last element (because we're skipping all but one)... then all but the last one.
            return array.Skip(array.Length - 1).Concat(array.Take(array.Length - 1)).ToArray();
        }

        public static string ReformatWorkingDir(string workingDirectory)
        {
            string output = workingDirectory.Replace(@"\", @"/");
            output = output.Replace(@":", @"/");

            //output = "//c//" + output;
            output = "//" + output;
            output = output.Replace(@"//C//", @"//c//");
            return output;
        }

        public static bool IsWindows7 => (Environment.OSVersion.Version.Major == 6 &
                  Environment.OSVersion.Version.Minor == 1);


        public static string GetOSInfo()
        {
            //Get Operating system information.
            OperatingSystem os = Environment.OSVersion;
            //Get version information about the os.
            Version vs = os.Version;

            //Variable to hold our return value
            string operatingSystem = "";

            if (os.Platform == PlatformID.Win32Windows)
            {
                //This is a pre-NT version of Windows
                switch (vs.Minor)
                {
                    case 0:
                        operatingSystem = "95";
                        break;
                    case 10:
                        if (vs.Revision.ToString() == "2222A")
                        {
                            operatingSystem = "98SE";
                        }
                        else
                        {
                            operatingSystem = "98";
                        }

                        break;
                    case 90:
                        operatingSystem = "Me";
                        break;
                    default:
                        break;
                }
            }
            else if (os.Platform == PlatformID.Win32NT)
            {
                switch (vs.Major)
                {
                    case 3:
                        operatingSystem = "NT 3.51";
                        break;
                    case 4:
                        operatingSystem = "NT 4.0";
                        break;
                    case 5:
                        if (vs.Minor == 0)
                        {
                            operatingSystem = "2000";
                        }
                        else
                        {
                            operatingSystem = "XP";
                        }

                        break;
                    case 6:
                        if (vs.Minor == 0)
                        {
                            operatingSystem = "Vista";
                        }
                        else if (vs.Minor == 1)
                        {
                            operatingSystem = "7";
                        }
                        else if (vs.Minor == 2)
                        {
                            operatingSystem = "8";
                        }
                        else
                        {
                            operatingSystem = "8.1";
                        }

                        break;
                    case 10:
                        operatingSystem = "10";
                        break;
                    default:
                        break;
                }
            }
            //Make sure we actually got something in our OS check
            //We don't want to just return " Service Pack 2" or " 32-bit"
            //That information is useless without the OS version.
            if (operatingSystem != "")
            {
                //Got something.  Let's prepend "Windows" and get more info.
                operatingSystem = "Windows " + operatingSystem;
                ////See if there's a service pack installed.
                //if (os.ServicePack != "")
                //{
                //    //Append it to the OS name.  i.e. "Windows XP Service Pack 3"
                //    operatingSystem += " " + os.ServicePack;
                //}
                ////Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"
                ////operatingSystem += " " + getOSArchitecture().ToString() + "-bit";
            }
            //Return the information we've gathered.
            return operatingSystem;
        }


        public static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }


        public static List<Point3d> DiscardPointsOutsideDomain(List<Point3d> listOfPoints, OFBaseDomain DOM)
        {

            // Inclusion check for probes

            // Filter the list
            int kept = 0;
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                // Test whether this is an element that we want to keep.
                if (DOM.inputBreps.IsPointInside(listOfPoints[i], 0.01, true) == false)
                {
                    // Add it to the list of kept elements.
                    listOfPoints[kept] = listOfPoints[i];
                    kept++;
                }
            }
            // Unfortunately IList has no Resize method. So instead we
            // remove the last element of the list until: elements.Count == kept.
            while (kept < listOfPoints.Count)
            {
                listOfPoints.RemoveAt(listOfPoints.Count - 1);
            }
            return listOfPoints;
        }

        public static List<string> GetDirectories(string path, string searchPattern = "*",   SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
                return Directory.GetDirectories(path, searchPattern).ToList();

            var directories = new List<string>(GetDirectories(path, searchPattern));

            for (var i = 0; i < directories.Count; i++)
                directories.AddRange(GetDirectories(directories[i], searchPattern));

            return directories;
        }

        private static List<string> GetDirectories(string path, string searchPattern)
        {
            try
            {
                return Directory.GetDirectories(path, searchPattern).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<string>();
            }
        }

        public static string ReplaceDoubleBackslashes(string input)
        {
            string output;
            output = input.Replace(@"\\", @"\");
            output = output.Replace(@"\\", @"\");
            output = output.Replace(@"\\", @"\");
            return output;
        }


        public static int GetLastIterationFromDirectory(string simWorkingDirectory)
        {

            simWorkingDirectory = ReplaceDoubleBackslashes(simWorkingDirectory);


            // Full path
            var directoriesInDir = GetDirectories(simWorkingDirectory);


            // Without trailing path
            var listOfDirs = new List<String>();
            foreach (string str in directoriesInDir)
            {
                listOfDirs.Add(new DirectoryInfo(str).Name);
            }


            var filteredNumbers = listOfDirs.Where(s => s.All(char.IsDigit));

            var lastIteration = filteredNumbers.Max();
            int lastIterationInt = int.Parse(lastIteration);


            return lastIterationInt;

        }

       

        public static int CPUAutoCalc(string meshWorkingDirectory, int CPUSetByUser)
        {
            int CPU = CPUSetByUser;
            int numberOfCellsInMesh = 0;
            int numberOfCPUsOnMachine = System.Environment.ProcessorCount;

            if (File.Exists(meshWorkingDirectory + @"\log"))
            {
                var logFile = File.ReadAllLines(meshWorkingDirectory + @"\log");
                foreach (string line in logFile)
                {
                    if (line.StartsWith("    cells:"))
                    {
                        numberOfCellsInMesh = int.Parse(line.Split(':')[1]);

                        if (numberOfCellsInMesh > 50000)
                        {
                            CPU = numberOfCellsInMesh / 50000;
                            if (CPU > numberOfCPUsOnMachine / 2)
                            {
                                CPU = numberOfCPUsOnMachine / 2;
                            }
                        }

                        if (CPU < 1)
                        {
                            CPU = 1;
                        }

                    }
                    else
                    {
                        CPU = numberOfCPUsOnMachine / 2;
                        if (CPU < 1)
                        {
                            CPU = 1;
                        }
                    }

                }

            }
            else
            {
                CPU = numberOfCPUsOnMachine - 2;
                if (CPU < 1)
                {
                    CPU = 1;
                }
            }

            return CPU;
        }

        private static void AddRuntimeMessage(object warning, string v)
        {
            throw new NotImplementedException();
        }


        public static bool DidProcessGetKilled(string workingDirectory)
        {
            bool processGotKilled = false;
            string logFilePath = workingDirectory + @"\log";

            if (File.Exists(logFilePath))
            {
                try
                {
                    String line;
                    List<String> lines = new List<String>();


                    using (var fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, System.Text.Encoding.Default))
                    {


                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }


                    foreach (string lline in lines)
                    {
                        if (lline.EndsWith("(Killed).")) { processGotKilled = true; }
                    }

                }
                catch (Exception)
                {

                    throw;
                }
            }
            return processGotKilled;





        }


        public static List<String> FileReader(string filePath)
        {

            String line;
            List<String> lines = new List<String>();

            if (File.Exists(filePath))
            {
                try
                {



                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs, System.Text.Encoding.Default))
                    {


                        while ((line = sr.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }




                }
                catch (Exception e)
                {

                    throw new System.ArgumentException(e.Message);
                }
            }
            return lines;

        }


        public static string ConvertComputeTimes(long elapsedMilliseconds)
        {
            string elapsedTime = "";

            if (elapsedMilliseconds < 60 * 1000)
            {
                elapsedTime = ("Compute time: " + elapsedMilliseconds / 1000 + " s");
            }
            else
            {
                elapsedTime = ("Compute time: " + elapsedMilliseconds / 1000 + " s or ca. " + elapsedMilliseconds / 1000 / 60 + " min");
            }

            return elapsedTime;


        }

        public static List<int> ExportEvaluationHours(List<string> LBanalysis)
        {
            List<int> hoursToEvaluate = new List<int>();

            int month_start = int.Parse(LBanalysis[0].Split(',')[0].Split('(')[1]) - 1;
            var month_end = int.Parse(LBanalysis[1].Split(',')[0].Split('(')[1]);
            var day_start = int.Parse(LBanalysis[0].Split(',')[1]) - 1;
            var day_end = int.Parse(LBanalysis[1].Split(',')[1]);
            var hour_start = int.Parse(LBanalysis[0].Split(',')[2].Split(')')[0]) - 1;
            var hour_end = int.Parse(LBanalysis[1].Split(',')[2].Split(')')[0]);

            if (month_end > 12) { month_end = 12; }
            if (day_end > 31) { day_end = 31; }
            if (hour_end > 24) { hour_end = 24; }

            int cnt = 0;

            var hours = hour_end - hour_start;

            for (int m = 0; m < 12; m++) // 0-11
            {
                for (int d = 0; d < 31; d++) // 0-30
                {
                    for (int h = 0; h < 24; h++) // 0-23
                    {
                        // Check if already gone through month          

                        if (m == 2 && d > 27) { continue; }

                        else if ((m == 4 || m == 6 || m == 9 || m == 10) && d > 29) { continue; }
                        //

                        cnt++;
                        // Fill list

                        if (m >= month_start && m < month_end && d >= day_start && d < day_end && h >= hour_start && h < hour_end)
                        {
                            hoursToEvaluate.Add(cnt);
                        }




                    }
                }
            }





            return hoursToEvaluate;

        }

        public static double MeshFaceArea(int meshfaceindex, Mesh m)
        {
            //get points into a nice, concise format
            Point3d[] pts = new Point3d[4];
            pts[0] = m.Vertices[m.Faces[meshfaceindex].A];
            pts[1] = m.Vertices[m.Faces[meshfaceindex].B];
            pts[2] = m.Vertices[m.Faces[meshfaceindex].C];
            if (m.Faces[meshfaceindex].IsQuad)
            {
                pts[3] = m.Vertices[m.Faces[meshfaceindex].D];
            }

            //calculate areas of triangles
            double a = pts[0].DistanceTo(pts[1]);
            double b = pts[1].DistanceTo(pts[2]);
            double c = pts[2].DistanceTo(pts[0]);
            double p = 0.5 * (a + b + c);
            double area1 = Math.Sqrt(p * (p - a) * (p - b) * (p - c));

            //if quad, calc area of second triangle
            double area2 = 0;
            if (m.Faces[meshfaceindex].IsQuad)
            {
                a = pts[0].DistanceTo(pts[2]);
                b = pts[2].DistanceTo(pts[3]);
                c = pts[3].DistanceTo(pts[0]);
                p = 0.5 * (a + b + c);
                area2 = Math.Sqrt(p * (p - a) * (p - b) * (p - c));
            }

            return area1 + area2;
        }

        public static double[] FilterExtremeCPs(double[] inputList)
        {
            double[] outputList = new double[inputList.Length];


            for (int i = 0; i < inputList.Length; i++)
            {

                if (inputList[i] < -1)
                {
                    outputList[i] = -1;
                }
                else if (inputList[i] > 1)
                {
                    outputList[i] = 1;
                }
                else
                {
                    outputList[i] = inputList[i];
                }
            }
            return outputList;
        }

        public static Vector3d[] FilterExtremeVectorLengths(Vector3d[] inputList)
        {
            List<Vector3d> outputList = new List<Vector3d>();


            for (int i = 0; i < inputList.Length; i++)
            {

                if (inputList[i].Length < 1000)
                {
                    outputList.Add(inputList[i]);

                }
            }

            return outputList.ToArray();
        }

        public static void ParseABLConditionsFromCaseFolder(string ABLConditionsFilePath, out double URef, out double z0, out double zref)
        {

            URef = 0.0;
            zref = 0.0;
            z0 = 0.0;

            string[] lines = File.ReadAllLines(ABLConditionsFilePath);


                        for (int i = 0; i<lines.Length; i++)
                        {
                            var l = lines[i];



                            if (l.Contains("Uref"))
                            {
                                URef = double.Parse(l.Replace("Uref", "").Replace(";", "").Trim());
                            }

                            if (l.Contains("z0"))
                            {
                                z0 = double.Parse(l.Replace("z0 uniform", "").Replace(";", "").Trim());
                            }

                            if (l.Contains("Zref"))
                            {
                                zref = double.Parse(l.Replace("Zref", "").Replace(";", "").Trim());
                            }
                        }

        }



        public static bool CheckLicence()
        {
            bool licence = false;
            //DateTime dateNow = Utilities.GetNistTime();
            DateTime dateCompile = new DateTime(2019, 2, 25, 0, 00, 00).ToUniversalTime();
            TimeSpan licenceDuration = new TimeSpan(150, 0, 0, 0);
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


            if (DateTime.Now > expiresAt)
            {
                licence = false;
            }
            else
            {
                licence = true;
            }

            return licence;
        }


        //public static bool ArePointsOutsideBrep(Mesh GeometryToCheck, Mesh GeometryToCheckAgainst)
        //{
        //    bool ShapeInsideBrep = false;

        //    for (int i = 0; i < 4;i++)
        //    {
        //        if( GeometryToCheck.Vertices[i].X < GeometryToCheckAgainst.Vertices[i].X)
        //        {

        //        }
        //    }


        //    return ShapeInsideBrep;
        //}



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
                if ((s1 == null) && (s2 == null))
                {
                    return 0;
                }
                else if (s1 == null)
                {
                    return -1;
                }
                else if (s2 == null)
                {
                    return 1;
                }

                if ((s1.Equals(string.Empty) && (s2.Equals(string.Empty))))
                {
                    return 0;
                }
                else if (s1.Equals(string.Empty))
                {
                    return -1;
                }
                else if (s2.Equals(string.Empty))
                {
                    return -1;
                }

                //WE style, special case
                bool sp1 = Char.IsLetterOrDigit(s1, 0);
                bool sp2 = Char.IsLetterOrDigit(s2, 0);
                if (sp1 && !sp2)
                {
                    return 1;
                }

                if (!sp1 && sp2)
                {
                    return -1;
                }

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
                            if (r != 0)
                            {
                                return r;
                            }
                        }
                        else if (!letter1 && letter2)
                        {
                            return -1;
                        }
                        else if (letter1 && !letter2)
                        {
                            return 1;
                        }
                    }
                    else if (c1 && c2)
                    {
                        r = CompareNum(s1, ref i1, s2, ref i2);
                        if (r != 0)
                        {
                            return r;
                        }
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

                if (nzLength1 < nzLength2)
                {
                    return -1;
                }
                else if (nzLength1 > nzLength2)
                {
                    return 1;
                }

                for (int j1 = nzStart1, j2 = nzStart2; j1 <= i1; j1++, j2++)
                {
                    int r = s1[j1].CompareTo(s2[j2]);
                    if (r != 0)
                    {
                        return r;
                    }
                }
                // the nz parts are equal
                int length1 = end1 - start1;
                int length2 = end2 - start2;
                if (length1 == length2)
                {
                    return 0;
                }

                if (length1 > length2)
                {
                    return -1;
                }

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
                    else
                    {
                        countZeros = false;
                    }

                    end++;
                    if (end >= s.Length)
                    {
                        break;
                    }
                }
            }

           



        }//EOC

        // <Custom additional code>
        public static string[][] CreateMatrix(int rows, int columns)
        {
            var matrix = new string[rows][];

            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[i] = new string[columns];
            }

            return matrix;
        }
        public static void DownLoadFile(string URL, string FilePath)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFile(URL, FilePath);

        }
    }



}

