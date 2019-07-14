using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using Deedle;
using Rhino.Geometry;

namespace EddyLib
{
    public static class Utilities
    {
        public class StartProcess
        {
            public static void StartProcessCMD(string argument, bool createnowindow, bool waitforexit = false, bool close = false, string executable = @"C:\Windows\System32\cmd.exe")
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = executable;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = createnowindow;
                p.Start();
                StreamWriter sw = p.StandardInput;
                string strInputText = argument;
                sw.WriteLine(strInputText);

                sw.Flush();
                if (waitforexit) { p.WaitForExit(); }
                if (close) { p.Close(); }
            }

            public static void StartProcessCMDNT(string argument, bool createnowindow, bool waitforexit = true, bool close = false, bool startInNewThread = false, string executable = @"C:\Windows\System32\cmd.exe")
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = executable;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                //p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = createnowindow;
                //p.Start();

                ThreadStart ths = new ThreadStart(() =>
                {
                    p.Start();

                    StreamWriter sw = p.StandardInput;
                    String strInputText = argument;
                    sw.WriteLine(strInputText);

                    // Window doesn't close with
                    //sw.Flush();
                });

                Thread th = new Thread(ths);
                th.Start();

                if (waitforexit)
                {
                    //Console.ReadLine();
                    p.WaitForExit();
                }
                if (close) { p.Close(); }
            }
        }

        public class Directories
        {
            public static string FixDirectories(string dir)
            {
                if (!dir.EndsWith(@"\"))
                {
                    dir = dir + @"\";
                }
                return dir;
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

            public static bool IsDirectoryEmpty(string path)
            {
                return !Directory.EnumerateFileSystemEntries(path).Any();
            }

            public static List<string> GetDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
            {
                if (searchOption == SearchOption.TopDirectoryOnly)
                {
                    return Directory.GetDirectories(path, searchPattern).ToList();
                }

                List<string> directories = new List<string>(GetDirectories(path, searchPattern));

                for (int i = 0; i < directories.Count; i++)
                {
                    directories.AddRange(GetDirectories(directories[i], searchPattern));
                }

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

            public static string InsertDoubleBackslashes(string input)
            {
                string output;

                output = input.Replace(@"\", @"\\");
                output = output.Replace(@"\\\", @"\\");
                output = output.Replace(@"\\\\", @"\\");
                return output;
            }

            public static bool processDirectory(string startLocation, bool simDir)
            {
                bool result = true;
                foreach (string directory in Directory.GetDirectories(startLocation))
                {
                    if (simDir)
                    {
                        if (directory.EndsWith("polyMesh"))
                        {
                            result = false;
                            continue;
                        }
                    }

                    bool directoryResult = processDirectory(directory, simDir);
                    result &= directoryResult;

                    //if (Directory.GetFiles(directory, "*.dvr").Any())
                    //{
                    //    result = false;
                    //    continue;
                    //}

                    foreach (string file in Directory.GetFiles(directory))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (IOException)
                        {
                            // error handling
                            result = directoryResult = false;
                        }
                    }

                    if (!directoryResult)
                    {
                        continue;
                    }

                    try
                    {
                        Directory.Delete(directory, false);
                    }
                    catch (IOException)
                    {
                        // error handling
                        result = false;
                    }
                }

                return result;
            }
        }

        public class Docker
        {
            public static bool IsDockerRunning(string workingDirectory, OSType ostype)
            {
                bool running = false;
                string fp = workingDirectory + @"\dockerStatus";

                List<string> lines = Utilities.FileReader(fp);

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
                StartProcess.StartProcessCMD(@"docker info > """ + workingDirectory + @"\dockerStatus""", true, false, false);

                //StartProcessCMD(@"docker info > """ + workingDirectory + @"\dockerStatus""", true, true, true);
            }
        }

        //public class Directories
        //{
        //}

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
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string localDir = Assembly.GetExecutingAssembly().GetDirectoryPath();
                string dir1 = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);

                Assembly bla1 = Assembly.GetEntryAssembly();    //gives you the entrypoint assembly for the process.
                Assembly bla2 = Assembly.GetCallingAssembly();   // gives you the assembly from which the current method was called.
                Assembly bla3 = Assembly.GetExecutingAssembly(); // gives you the assembly in which the currently executing code is defined
                Assembly bla4 = Assembly.GetAssembly(typeof(OFBaseDomain));  // gives you the assembly in which the specified type is defined.
                string loc = bla4.Location;
                string path2 = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static object GH_RuntimeMessageLevel { get; private set; }

        public static void DeletePhi(OFMeshSettings MeshSettings, OFBaseDomain DOM)
        {
            foreach (int dir in DOM.BCond.windDirs)
            {
                string phiPath = MeshSettings.baseWorkingDir + dir + @"\0\phi";
                if (File.Exists(phiPath)) { File.Delete(phiPath); }
                //string logPath = MeshSettings.baseWorkingDir + dir + @"\log";
                //if (File.Exists(logPath)) { File.Delete(logPath); }
            }
        }

        public static T[,] TransposeRowsAndColumns<T>(this T[,] arr)
        {
            int rowCount = arr.GetLength(0);
            int columnCount = arr.GetLength(1);
            T[,] transposed = new T[columnCount, rowCount];
            if (rowCount == columnCount)
            {
                transposed = (T[,])arr.Clone();
                for (int i = 1; i < rowCount; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        T temp = transposed[i, j];
                        transposed[i, j] = transposed[j, i];
                        transposed[j, i] = temp;
                    }
                }
            }
            else
            {
                for (int column = 0; column < columnCount; column++)
                {
                    for (int row = 0; row < rowCount; row++)
                    {
                        transposed[column, row] = arr[row, column];
                    }
                }
            }
            return transposed;
        }

        // This doesnt work atm because tee.exe puts write lock on log file
        //public static double CalculateRunTimeFromLog(string simulationDirectory, int iter)
        //{
        //    string logFilePath = simulationDirectory + @"\log";

        // double timeEnd = 0;

        // if (File.Exists(logFilePath)) { try { string line; List<string> lines = new List<string>();

        // //var time1 = "0"; string time2 = "0";

        // // This causes issues if the logfile isn't there

        // using (FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read,
        // FileShare.ReadWrite)) using (StreamReader sr = new StreamReader(fs,
        // System.Text.Encoding.Default)) { while ((line = sr.ReadLine()) != null) { lines.Add(line);
        // } }

        // foreach (var lline in lines.Select((value, index) => new { value, index })) { // Use
        // x.value and x.index in here

        // if (lline.value.StartsWith("SIMPLE solution converged")) { time2 = lines[lline.index -
        // 3].Split("ClockTime".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[3].Replace("=",
        // "").Replace("s", "").Trim();//.Replace("s", "")

        // //timeElapsed = TimeSpan.FromSeconds(double.Parse(time2)); }

        // if (lline.value.EndsWith(iter.ToString())) { time2 = lines[lline.index +
        // 10].Split("ClockTime".ToCharArray(),
        // StringSplitOptions.RemoveEmptyEntries)[3].Replace("=", "").Replace("s",
        // "").Trim();//.Replace("s", "") break;

        // //timeElapsed = TimeSpan.FromSeconds(double.Parse(time2)); }

        // else { timeEnd = 0; }

        // timeEnd = double.Parse(time2) / 60; }

        // } catch (Exception e) { throw new System.ArgumentException(e.Message); } }

        //    return timeEnd;
        //}

        public static IEnumerable<List<T>> SplitListGen<T>(List<T> locations, int nSize)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        public static List<List<Point3d>> SplitPointList(List<Point3d> locations, int nSize)
        {
            List<List<Point3d>> list = new List<List<Point3d>>();

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

        public static List<Point3d> DiscardPoints(List<Point3d> listOfPoints, OFBaseDomain DOM)
        {
            List<Point3d> newList = new List<Point3d>();

            for (int i = 0; i < listOfPoints.Count; i++)
            {
                if (DOM.DomainMesh.IsPointInside(listOfPoints[i], 0.001, true))
                {
                    if (!DOM.BuildingGeometry.IsPointInside(listOfPoints[i], 0.001, true))
                    {
                        newList.Add(listOfPoints[i]);
                    }
                }
            }

            return newList;
        }

        public static void CleanDirectory(string path)
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(path);

            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
            {
                dir.Delete(true);
            }
        }

        public static string GetFileNameWithHighestEnumerator(string folder)
        {
            var path = Directory.GetFiles(folder, "*.dat").Select(fn => new FileInfo(fn)).OrderBy(f => f.Name).Last();
            return path.ToString();
        }

        public static int GetLastIterationFromDirectory(string simWorkingDirectory)
        {
            simWorkingDirectory = Directories.ReplaceDoubleBackslashes(simWorkingDirectory);

            // Full path
            List<string> directoriesInDir = Directories.GetDirectories(simWorkingDirectory);

            // Without trailing path
            List<string> listOfDirs = new List<string>();
            foreach (string str in directoriesInDir)
            {
                listOfDirs.Add(new DirectoryInfo(str).Name);
            }

            IEnumerable<string> filteredNumbers = listOfDirs.Where(s => s.All(char.IsDigit));

            string lastIteration = filteredNumbers.Max();
            int lastIterationInt = int.Parse(lastIteration);

            return lastIterationInt;
        }

        public static Point3d[] Probes2Point3D(double[][] input)
        {
            int numberOfProbes = input.Count();

            var outputList = new Point3d[numberOfProbes];

            for (int i = 0; i < numberOfProbes; i++)
            {
                outputList[i] = new Point3d(input[i][0], input[i][1], input[i][2]);
            }

            return outputList;
        }

        public static Vector3d[] CSVVectorComponents2Vector3D(double[,] input)
        {
            //double[hours, windDirs]

            int numberOfDirs = input.GetUpperBound(1);
            int numberOfSensors = input.GetUpperBound(0);

            var outputList = new Vector3d[numberOfDirs];

            for (int p = 0; p < numberOfSensors; p++)
            {
                for (int dir = 0; dir < numberOfDirs; dir++)
                {
                    outputList[p] = new Vector3d(input[p, dir + 0], input[p, dir + 1], input[p, dir + 2]);
                }
            }

            return outputList;
        }

        public static int CalcOptimCPU(string meshWorkingDirectory, int CPUSetByUser)
        {
            int CPU = CPUSetByUser;
            int numberOfCellsInMesh = 0;
            int numberOfCPUsOnMachine = System.Environment.ProcessorCount;

            if (File.Exists(meshWorkingDirectory + @"\log"))
            {
                string[] logFile = File.ReadAllLines(meshWorkingDirectory + @"\log");
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
                    string line;
                    List<string> lines = new List<string>();

                    using (FileStream fs = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default))
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
                catch (Exception e)
                {
                    throw new System.ArgumentException(e.Message);
                }
            }
            return processGotKilled;
        }

        public static List<string> FileReader(string filePath)
        {
            string line;
            List<string> lines = new List<string>();

            if (File.Exists(filePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (StreamReader sr = new StreamReader(fs, System.Text.Encoding.Default))
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

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
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

        //private static int GetNumberOfHours(Interval inter)
        //{
        //    //Interval inter = new Interval(1000, 2000);
        //    int numberOfHours = (int)(inter.T1 - inter.T0);
        //    return numberOfHours;
        //}

        //public static List<int> ConcatAllLists(List<List<int>> inputList)
        //{
        //    var finalList = new List<int>();

        //    for (int i = 0; i < inputList.Count; i++)
        //    {
        //        for (int j = 0; j < inputList[i].Count; i++)
        //        {
        //            finalList.Add(j);
        //        }
        //    }
        //    return finalList;
        //}

        //public static List<List<int>> GetFullHoursListFromLB(List<List<string>> LBanalysisList)
        //{
        //    var fullHoursList = new List<List<int>>();
        //    foreach (List<string> LBobj in LBanalysisList)
        //    {
        //        fullHoursList.Add(GetEvalHoursFromLB(LBobj));
        //    }
        //    return fullHoursList;
        //}

        //public static List<List<int>> GetFullHoursListFromInt(List<Interval> list)
        //{
        //    var fullHoursList = new List<List<int>>();
        //    foreach (Interval inter in list)
        //    {
        //        fullHoursList.Add(GetEvalHoursFromInterval(inter));
        //    }
        //    return fullHoursList;
        //}

        public static List<int> GetFullHoursListFromLB(List<string> LBanalysisList)
        {
            List<int> fullHoursList = new List<int>();
            //foreach (string LBobj in LBanalysisList)
            //{
            foreach (int hour in GetEvalHoursFromLB(LBanalysisList))
            {
                fullHoursList.Add(hour);
            }
            //}
            return fullHoursList;
        }

        public static List<int> GetFullHoursListFromInt(List<Interval> list)
        {
            List<int> fullHoursList = new List<int>();
            foreach (Interval inter in list)
            {
                foreach (int hour in GetEvalHoursFromInterval(inter))
                {
                    fullHoursList.Add(hour);
                }
            }
            return fullHoursList;
        }

        private static List<int> GetEvalHoursFromInterval(Interval inter)
        {
            List<int> evalHours = new List<int>();

            int startHour = (int)inter.T0;
            int endHour = (int)inter.T1;

            int numberOfHours = endHour - startHour;

            for (int i = startHour; i < startHour + numberOfHours; i++)
            {
                evalHours.Add(i);
            }
            return evalHours;
        }

        public static bool CheckForDuplicates(List<GeometryBase> geo)
        {
            bool equal = false;

            for (int i = 0; i < geo.Count - 1; i++)
            {
                for (int j = 0; j < geo.Count; j++)
                {
                    if (i != j)
                    {
                        equal = GeometryBase.GeometryEquals(geo[i], geo[j]);
                        if (equal == true)
                        {
                            break;
                        }
                    }
                }
            }

            return equal;
        }

        public static List<int> GetEvalHoursFromLB(List<string> LBanalysis)
        {
            List<int> hoursToEvaluate = new List<int>();

            int month_start = int.Parse(LBanalysis[0].Split(',')[0].Split('(')[1]) - 1;
            int month_end = int.Parse(LBanalysis[1].Split(',')[0].Split('(')[1]);
            int day_start = int.Parse(LBanalysis[0].Split(',')[1]) - 1;
            int day_end = int.Parse(LBanalysis[1].Split(',')[1]);
            int hour_start = int.Parse(LBanalysis[0].Split(',')[2].Split(')')[0]) - 1;
            int hour_end = int.Parse(LBanalysis[1].Split(',')[2].Split(')')[0]);

            if (month_end > 12) { month_end = 12; }
            if (day_end > 31) { day_end = 31; }
            if (hour_end > 24) { hour_end = 24; }

            int cnt = 0;

            int hours = hour_end - hour_start;

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

        public static void ParseABLConditionsFromCaseFolder(string ABLConditionsFilePath, out double URef, out double z0, out double zref)
        {
            URef = 0.0;
            zref = 0.0;
            z0 = 0.0;

            string[] lines = File.ReadAllLines(ABLConditionsFilePath);

            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];

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

        public static string PrepareParaviewLoadScript(String baseWorkingDir, List<int> dirs)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("from paraview.simple import *");

            // build strings

            // building and ground

            sb.AppendLine(@"building = OpenDataFile(""" + Utilities.Directories.InsertDoubleBackslashes(baseWorkingDir) + @"mesh\\constant\\triSurface\\building.stl"")");
            sb.AppendLine(@"ground = OpenDataFile(""" + Utilities.Directories.InsertDoubleBackslashes(baseWorkingDir) + @"mesh\\constant\\triSurface\\ground.stl"")");

            foreach (int dir in dirs)
            {
                sb.AppendLine("case_" + dir + @" = OpenDataFile(""" + Utilities.Directories.InsertDoubleBackslashes(baseWorkingDir) + dir + @"\\" + dir + @".foam"")");
            }

            sb.AppendLine("Show(building)");
            sb.AppendLine("Show(ground)");

            foreach (int dir in dirs)
            {
                sb.AppendLine("Show(case_" + dir + @")");
            }

            sb.AppendLine(@"from paraview.simple import *
#### disable automatic camera reset on 'Show'
paraview.simple._DisableFirstRenderCameraReset()

# find source
sTLReader1 = FindSource('STLReader1')

# find source
sTLReader2 = FindSource('STLReader2')

# get active source.
openFOAMReader1 = GetActiveSource()

# Properties modified on openFOAMReader1
openFOAMReader1.CellArrays = ['U']

# get active view
renderView1 = GetActiveViewOrCreate('RenderView')
# uncomment following to set a specific view size
# renderView1.ViewSize = [2135, 550]

# get display properties
openFOAMReader1Display = GetDisplayProperties(openFOAMReader1, view = renderView1)

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.SelectScaleArray = 'None'

# get color transfer function/color map for 'p'
pLUT = GetColorTransferFunction('p')

# get opacity transfer function/opacity map for 'p'
pPWF = GetOpacityTransferFunction('p')

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.GlyphTableIndexArray = 'None'

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.SetScaleArray = ['POINTS', 'U']

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.OpacityArray = ['POINTS', 'U']

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.OSPRayScaleArray = 'U'

# get animation scene
animationScene1 = GetAnimationScene()

# update animation scene based on data timesteps
animationScene1.UpdateAnimationUsingDataTimeSteps()

# update the view to ensure updated data information
renderView1.Update()

# Properties modified on openFOAMReader1
openFOAMReader1.Adddimensionalunitstoarraynames = 1

# update the view to ensure updated data information
renderView1.Update()

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.SelectOrientationVectors = 'None'

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.SetScaleArray = ['POINTS', 'U [m/s]']

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.OpacityArray = ['POINTS', 'U [m/s]']

# Properties modified on openFOAMReader1Display
openFOAMReader1Display.OSPRayScaleArray = 'U [m/s]'

# set scalar coloring
ColorBy(openFOAMReader1Display, ('POINTS', 'U [m/s]', 'Magnitude'))

# Hide the scalar bar for this color map if no visible data is colored by it.
HideScalarBarIfNotNeeded(pLUT, renderView1)

# rescale color and/or opacity maps used to include current data range
openFOAMReader1Display.RescaleTransferFunctionToDataRange(True, False)

# show color bar/color legend
openFOAMReader1Display.SetScalarBarVisibility(renderView1, True)

# get color transfer function/color map for 'Ums'
umsLUT = GetColorTransferFunction('Ums')

# get opacity transfer function/opacity map for 'Ums'
umsPWF = GetOpacityTransferFunction('Ums')

# reset view to fit data
renderView1.ResetCamera()

# Properties modified on renderView1
renderView1.Background = [1.0, 1.0, 1.0]

# get the material library
materialLibrary1 = GetMaterialLibrary()

# Apply a preset using its name. Note this may not work as expected when presets have duplicate names.
umsLUT.ApplyPreset('Viridis (matplotlib)', True)

# get color legend/bar for umsLUT in view renderView1
umsLUTColorBar = GetScalarBar(umsLUT, renderView1)

# Properties modified on umsLUTColorBar
umsLUTColorBar.TitleColor = [0.0, 0.0, 0.0]
umsLUTColorBar.TitleBold = 1
umsLUTColorBar.LabelColor = [0.0, 0.0, 0.0]
umsLUTColorBar.LabelBold = 1
umsLUTColorBar.AutomaticLabelFormat = 0
umsLUTColorBar.LabelFormat = '%-#6.1f'
umsLUTColorBar.RangeLabelFormat = '%-#6.1f'

#### saving camera placements for all active views

# current camera placement for renderView1
renderView1.CameraPosition = [-109.98370361328125, 1374.7207336425781, 8420.956940089278]
renderView1.CameraFocalPoint = [-109.98370361328125, 1374.7207336425781, 594.7585678100586]
renderView1.CameraParallelScale = 2025.5691894962097
renderView1.CameraParallelProjection = 1

#### uncomment the following to render all views
# RenderAllViews()
# alternatively, if you want to write images, you can use SaveScreenshot(...).
");

            return sb.ToString();
        }

        public static string GetParaviewPath(int version)
        {
            string matchingvalues = "";

            string str4 = @"C:\Program Files (x86)\";
            string str5 = @"C:\Program Files\";
            string para = "ParaView";

            string paraviewPath = "";

            if (version == 4)
            {
                DirectoryInfo[] di = new DirectoryInfo(str4).GetDirectories();
                List<string> list = new List<string>();

                foreach (DirectoryInfo d in di)
                {
                    list.Add(d.ToString());
                }
                matchingvalues = list.LastOrDefault(stringToCheck => stringToCheck.StartsWith(para));
                paraviewPath = str4 + matchingvalues + @"\bin\paraview.exe";
            }
            else
            {
                DirectoryInfo[] di = new DirectoryInfo(str5).GetDirectories();
                List<string> list = new List<string>();

                foreach (DirectoryInfo d in di)
                {
                    list.Add(d.ToString());
                }
                matchingvalues = list.LastOrDefault(stringToCheck => stringToCheck.StartsWith(para));
                paraviewPath = str5 + matchingvalues + @"\bin\paraview.exe";
            }

            return paraviewPath;
        }

        public static bool HasWhiteSpace(string input)
        {
            bool hasWhiteSpace = false;

            foreach (char ch in input)
            {
                if (Char.IsWhiteSpace(ch))
                {
                    hasWhiteSpace = true;
                }
            }
            return hasWhiteSpace;
        }

        public static bool CheckLicence()
        {
            bool licence = false;
            //DateTime dateNow = Utilities.GetNistTime();
            DateTime dateCompile = new DateTime(2019, 2, 25, 0, 00, 00).ToUniversalTime();
            TimeSpan licenceDuration = new TimeSpan(240, 0, 0, 0);
            DateTime expiresAt = dateCompile.Add(licenceDuration);

            //try
            //{
            //    DateTime dateTime = DateTime.MinValue;
            //    DateTime dateTimeUTC = DateTime.MinValue;

            // System.Net.HttpWebRequest request =
            // (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://nist.time.gov/actualtime.cgi?lzbc=siqm9b");
            // ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 |
            // SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            // request.Method = "GET"; request.Accept = "text/html, application/xhtml+xml, */*";
            // request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1;
            // Trident/6.0)"; request.ContentType = "application/x-www-form-urlencoded";
            // //request.ProtocolVersion = HttpVersion.Version11; //request.CachePolicy = new
            // RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore); //No caching
            // System.Net.HttpWebResponse response =
            // (System.Net.HttpWebResponse)request.GetResponse(); if (response.StatusCode ==
            // (System.Net.HttpStatusCode.OK)) { System.IO.StreamReader stream = new
            // StreamReader(response.GetResponseStream()); string html =
            // stream.ReadToEnd();//<timestamp time=\"1395772696469995\" delay=\"1395772696469995\"/>
            // string time = System.Text.RegularExpressions.Regex.Match(html,
            // @"(?<=\btime="")[^""]*").Value; double milliseconds = Convert.ToInt64(time) / 1000.0;
            // dateTime = new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();
            // dateTimeUTC = dateTime.ToUniversalTime(); }

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

            // if (DateTime.Now > expiresAt) licence = false; else licence = true;

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

        // for (int i = 0; i < 4;i++) { if( GeometryToCheck.Vertices[i].X <
        // GeometryToCheckAgainst.Vertices[i].X) { } }

        //    return ShapeInsideBrep;
        //}

        public static void DownLoadFile(string URL, string FilePath)
        {
            WebClient webClient = new WebClient();
            webClient.DownloadFile(URL, FilePath);
        }

        public static double Rad2Deg(Vector3d windVec)
        {
            Vector3d vec1 = new Vector3d(0, 1, 0);
            Vector3d vec2 = windVec;

            double rad = Math.Acos(vec1 * vec2 / vec1.Length * vec2.Length);
            double ang = rad * 180 / Math.PI;
            return ang;
        }

        public static double Vec2Dir(Vector3d vec)
        {
            var res = Math.Atan2(vec.Y, vec.X) * 180 / Math.PI;
            return res;
        }

        public static int Vec2DirOFCoord(Vector3d vec)
        {
            // Standard 0 deg is plus X

            var transform = (Math.Atan2(vec.Y, vec.X) * 180 / Math.PI) + 90;

            var deg = 0.0;

            if (transform < 0)

            {
                deg = -1 * transform;
            }
            else if (transform <= 270 && transform > 0)
            {
                deg = 360 - transform;
            }
            else
            { deg = transform; }

            return (int)Math.Round(deg);
        }

        public static List<int> NormalizeWindDirs(List<int> windDir)
        {
            if (windDir.Count == 0)
            {
                windDir.Add(0);
            }

            // Translate dirs > 359 into correct format

            for (int i = 0; i < windDir.Count; i++)
            {
                if (windDir[i] > 359)
                {
                    int j = windDir[i] / 360;
                    windDir[i] = windDir[i] - (360 * j);
                }
                else { windDir[i] = windDir[i]; }
            }

            return windDir;
        }

        public static Vector3d Dir2Vec(double d)
        {
            return new Vector3d(-1 * Math.Sin(d * Math.PI / 180), -1 * Math.Cos(d * Math.PI / 180), 0);
        }

        public static double AngleBetweenVectors(Vector3d vector1, Vector3d vector2)
        {
            double sin = vector1.X * vector2.Y - vector2.X * vector1.Y;
            double cos = vector1.X * vector2.X + vector1.Y * vector2.Y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);
        }

        public static Point3d CenterBottomBoundingBox(Mesh geometry)
        {
            BoundingBox empty = BoundingBox.Empty;
            var box = geometry.GetBoundingBox(true);
            empty.Union(box);
            Point3d CenterGround = empty.Center + 0.5 * -Vector3d.ZAxis * (empty.Max.Z - empty.Min.Z);

            return CenterGround;
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
                bool sp1 = char.IsLetterOrDigit(s1, 0);
                bool sp2 = char.IsLetterOrDigit(s2, 0);
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
                    bool c1 = char.IsDigit(s1, i1);
                    bool c2 = char.IsDigit(s2, i2);
                    if (!c1 && !c2)
                    {
                        bool letter1 = char.IsLetter(s1, i1);
                        bool letter2 = char.IsLetter(s2, i2);
                        if ((letter1 && letter2) || (!letter1 && !letter2))
                        {
                            if (letter1 && letter2)
                            {
                                r = char.ToLower(s1[i1]).CompareTo(char.ToLower(s2[i2]));
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
                while (char.IsDigit(s, end))
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

        public static double rad2deg(double angleRad)
        {
            return (180.0 * angleRad / Math.PI);
        }

        public static double deg2rad(double angleDeg)
        {
            return Math.PI * angleDeg / 180.0;
        }
    }
}