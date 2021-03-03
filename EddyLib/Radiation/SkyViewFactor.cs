using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib.Radiation
{
    public class SkyViewFactor
    {
        public int[] hitCounts;

        private string sunRaysRes = "sunRays.res";

        private string sunRaysFile = "sunRays.pts";

        private string radFile = "geometry.rad";

        private string octreeFile = "geometry.oct";

        private string fileNameExport = "SkyViewFactors.bin";

        private Point3d[] sensors;

        private string workingDir;

        private string subDir;

        public bool wrongNumberOfProbes;

        public bool resultPrecalculated;

        public double[] Values { get; set; }

        public Mesh BuildingsAndGround { get; set; }

        public SkyViewFactor(string workingDir, Mesh BuildingsAndGround, Point3d[] sensors, bool recalc)
        {
            this.hitCounts = new int[sensors.Length];
            this.Values = new double[sensors.Length];
            this.BuildingsAndGround = BuildingsAndGround;

            this.sensors = sensors;
            this.workingDir = workingDir;
            this.subDir = @"\Rad\ViewFactors\";

            var binSVF = subDir + @"SkyViewFactors.bin";

            var numberOfProbes = sensors.Length;

            // Add other files here
            if (recalc == false && File.Exists(binSVF))
            {
                // Load radiation datasets [x][] time [][x] points

                var tempValues = RadianceFiles.loadBin1D(binSVF);

                int sensorPointCountExisting = tempValues.GetLength(1);

                if (sensorPointCountExisting != numberOfProbes)
                {
                    this.wrongNumberOfProbes = true;
                    return;
                }
                else
                {
                    this.Values = tempValues;
                }
            }
            else if (recalc == true)
            {
                if (File.Exists(binSVF))
                {
                    File.Delete(binSVF);
                }

                Run();

                RadianceFiles.writeBin1D(workingDir + subDir + @"\" + fileNameExport, this.Values);
            }
        }

        private void Run()
        {
            string sunRaysResPath = workingDir + subDir + this.sunRaysRes;
            string sunRaysFilePath = workingDir + subDir + this.sunRaysFile;
            string radFilePath = workingDir + subDir + this.radFile;
            string octreeFilePath = workingDir + subDir + this.octreeFile;

            // 0. Number of rays

            var numRays = equiSolidAngleVectors4PI().Length;

            // 1. Add Mat

            //foreach (Mesh m in BuildingsAndGround)
            //{
            AddMat(BuildingGroundTemplate(), BuildingsAndGround);

            //this.BuildingsAndGround = BuildingsAndGround;

            //}

            // 2. RadFile

            var matlist = new HashSet<string>();

            StringBuilder radFile = new StringBuilder();

            StringBuilder radFileString = new StringBuilder();

            int id = 0;

            //foreach (GeometryBase g in BuildingsAndGround)
            //{
            string mat = BuildingsAndGround.UserDictionary["RadMat"].ToString().Trim();
            matlist.Add(mat);

            string matName = mat.Split(' ')[2];

            //Print(matName);

            //Mesh m = (Mesh)g;

            radFileString.AppendLine(Mesh2Rad(BuildingsAndGround, matName, id.ToString()));

            //id++;
            //}

            foreach (string s in matlist)
            {
                radFile.AppendLine(s);
            }
            radFile.AppendLine("");
            radFile.AppendLine(radFileString.ToString());

            Directory.CreateDirectory(Path.GetDirectoryName(radFilePath));
            File.WriteAllText(radFilePath, radFile.ToString());

            //A = "Final File Length: " + finalFile.Length;

            // 3. Octree

            RunOconv(radFilePath, octreeFilePath);

            // 4. RaysFile

            StringBuilder sunRaysFile = new StringBuilder();

            foreach (Point3d p in sensors)
            {
                sunRaysFile.AppendLine(Rays(p, equiSolidAngleVectors4PI().ToList()));
            }

            File.WriteAllText(sunRaysFilePath, sunRaysFile.ToString());

            //A = "Final File Length: " + finalFile.Length;

            // 5. RayCast

            RunRayCastMat(octreeFilePath, sunRaysFilePath, sunRaysResPath);

            // RunRayCastSurf(Oct, Pts, Path);

            // 6. LoadResultsFile

            var HCnt = equiSolidAngleVectors4PI().Length;

            var lines = File.ReadAllLines(sunRaysResPath);

            var ptCnt = lines.Length / HCnt;

            var result = new int[ptCnt];

            //A = "Lines: " + lines.Length + " Points: " + ptCnt;
            if (lines.Length == 0) return;

            int lindex = 0;
            for (int pt = 0; pt < ptCnt; pt++)
            {
                for (int h = 0; h < HCnt; h++)
                {
                    // var m = Regex.Match(lines[lindex].Trim(), @"^\d");

                    // Everything that is not hit is the Sky
                    // We are counting the ones that don't hit anything
                    var m = lines[lindex].Trim().StartsWith("*");

                    if (m)
                    {
                        //if(m.Success) {
                        result[pt]++;
                    }

                    lindex++;
                }
            }

            //B = result.ToList();

            this.hitCounts = result;

            for (int pt = 0; pt < ptCnt; pt++)
            {
                this.Values[pt] = (double)hitCounts[pt] / numRays;
            }
        }

        #region 6. LoadResultFile

        public static int[] LoadResultFile(int HCnt, string Path, bool Run)

        {
            if (!Run) { }

            var lines = File.ReadAllLines(Path);

            var ptCnt = lines.Length / HCnt;
            var result = new int[ptCnt];

            //A = "Lines: " + lines.Length + " Points: " + ptCnt;
            if (lines.Length == 0) { }

            int lindex = 0;
            for (int pt = 0; pt < ptCnt; pt++)
            {
                for (int h = 0; h < HCnt; h++)
                {
                    // var m = Regex.Match(lines[lindex].Trim(), @"^\d");
                    var m = lines[lindex].Trim().StartsWith("*");

                    if (m)
                    {
                        //if(m.Success) {
                        result[pt]++;
                    }

                    lindex++;
                }
            }

            return result;
        }

        #endregion 6. LoadResultFile

        #region 5. RunRayCast

        //if(Run){
        //  RunRayCastMat(Oct, Pts, Path);
        //    // RunRayCastSurf(Oct, Pts, Path);
        //}

        public enum OSType
        {
            Windows = 0,

            Unix = 1,
        }

        public enum RuntimeType
        {
            NET = 0,

            Mono = 1,
        }

        #endregion 5. RunRayCast

        #region 4. RaysFile

        public static string SensorPoints(List<Point3d> pts, List<Vector3d> pts_norm)
        {
            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < pts.Count; k++)
            {
                sb.AppendLine(FormatPointAndNormal(pts[k], pts_norm[k]));
            }
            return sb.ToString();
        }

        public static string Rays(Point3d pt, List<Vector3d> pts_norm)
        {
            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < pts_norm.Count; k++)
            {
                sb.AppendLine(FormatPointAndNormal(pt, pts_norm[k]));
            }
            return sb.ToString();
        }

        private static string FormatPointAndNormal(Point3d p, Vector3d n)
        {
            return String.Format(radianceCulture, "{0:0.000} {1:0.000} {2:0.000} {3:0.000} {4:0.000} {5:0.000}", p.X, p.Y, p.Z, n.X, n.Y, n.Z);
        }

        #endregion 4. RaysFile

        #region 3. Octree

        //     if(Run){
        //  RunOconv(Rad, Oct);
        //}

        public class ExecutingPlatform
        {
            public OSType OS;

            public RuntimeType Runtime;

            public string RootDir;

            public string RadDir;

            public string RadBinDir;

            public string DaysimBinDir;

            private const string windows_root = @"C:\DIVA";

            private const string linux_root = "/opt/diva";

            private const string linux_rad = "/opt/diva/Linux";

            /// <summary> gets current executing platform
            /// </summary>
            public ExecutingPlatform()
            {
                // get os and root/rad directories
                int p = (int)Environment.OSVersion.Platform;
                if ((p == 4) || (p == 6) || (p == 128))
                {
                    this.OS = OSType.Unix;
                    this.RootDir = linux_root;
                    this.RadDir = Path.Combine(linux_rad, "Radiance");
                    this.RadBinDir = Path.Combine(linux_rad, "Radiance", "bin_64");
                    if (!Environment.Is64BitOperatingSystem)
                    {
                        // 32 bit binaries (Linux)
                        string bin32 = Path.Combine(linux_rad, "Radiance", "bin_32");
                        if (Directory.Exists(bin32)) this.RadBinDir = bin32;
                    }
                    this.DaysimBinDir = Path.Combine(linux_rad, "DaysimBinaries");
                }
                else
                {
                    this.OS = OSType.Windows;
                    this.RootDir = windows_root;
                    this.RadDir = Path.Combine(windows_root, "Radiance");
                    this.RadBinDir = Path.Combine(windows_root, "Radiance", "bin_64");
                    if (!Environment.Is64BitOperatingSystem)
                    {
                        // 32 bit binaries (Windows)
                        string bin32 = Path.Combine(windows_root, "Radiance", "bin_32");
                        if (Directory.Exists(bin32)) this.RadBinDir = bin32;
                    }
                    this.DaysimBinDir = Path.Combine(windows_root, "DaysimBinaries");
                }

                // get runtime
                Type t = Type.GetType("Mono.Runtime");
                if (t != null)
                {
                    this.Runtime = RuntimeType.Mono;
                }
                else
                {
                    this.Runtime = RuntimeType.NET;
                }
            }
        }

        public static void RunOconv(string radFilePath, string octFilePath)
        {
            // get executing platform
            var platform = new ExecutingPlatform();

            // add environmental variables
            string radbin = platform.RadBinDir;
            string radlib = Path.Combine(platform.RadDir, "lib");
            string daybin = platform.DaysimBinDir;
            char ps = (platform.OS == OSType.Windows) ? ';' : ':';
            Environment.SetEnvironmentVariable("PATH", "." + ps + radlib + ps + radbin + ps + daybin + ps + "$PATH");
            Environment.SetEnvironmentVariable("RAYPATH", "." + ps + radlib + ps + radbin + ps + daybin + ps + "$RAYPATH");

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c oconv " + radFilePath + " > " + octFilePath);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = Path.GetDirectoryName(octFilePath);

            Process p = Process.Start(psi);
            p.WaitForExit();
            p.Close();
        }

        public static void RunRayCastMat(string octree_path, string pts_path, string output_path)
        {
            // get executing platform
            var platform = new ExecutingPlatform();

            // public static string RadiancePath = @"C:\DIVA\Radiance\bin_64";
            // add environmental variables
            string radbin = platform.RadBinDir;
            string radlib = Path.Combine(platform.RadDir, "lib");
            string daybin = platform.DaysimBinDir;
            char ps = (platform.OS == OSType.Windows) ? ';' : ':';
            Environment.SetEnvironmentVariable("PATH", "." + ps + radlib + ps + radbin + ps + daybin + ps + "$PATH");
            Environment.SetEnvironmentVariable("RAYPATH", "." + ps + radlib + ps + radbin + ps + daybin + ps + "$RAYPATH");

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c rtrace -oM " + "-h " + "-ab 1 " + octree_path + " < " + pts_path + " > " + output_path);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.RedirectStandardInput = true;
            psi.WorkingDirectory = Path.GetDirectoryName(octree_path);

            Process p = Process.Start(psi);

            //if (!p.WaitForExit(2000000))
            //{
            //    p.Kill();
            //}
            p.WaitForExit();
            p.Close();
        }

        public static void RunRayCastSurf(string octree_path, string pts_path, string output_path)
        {
            // get executing platform
            var platform = new ExecutingPlatform();

            // public static string RadiancePath = @"C:\DIVA\Radiance\bin_64";
            // add environmental variables
            string radbin = platform.RadBinDir;
            string radlib = Path.Combine(platform.RadDir, "lib");
            string daybin = platform.DaysimBinDir;
            char ps = (platform.OS == OSType.Windows) ? ';' : ':';
            Environment.SetEnvironmentVariable("PATH", "." + ps + radlib + ps + radbin + ps + daybin + ps + "$PATH");
            Environment.SetEnvironmentVariable("RAYPATH", "." + ps + radlib + ps + radbin + ps + daybin + ps + "$RAYPATH");

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c rtrace -os " + "-h " + "-ab 1 " + octree_path + " < " + pts_path + " > " + output_path);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.RedirectStandardInput = true;
            psi.WorkingDirectory = Path.GetDirectoryName(octree_path);

            Process p = Process.Start(psi);

            //if (!p.WaitForExit(2000000))
            //{
            //    p.Kill();
            //}
            p.WaitForExit();
            p.Close();
        }

        #endregion 3. Octree

        #region 2. RadFile

        public static void RadFile(List<GeometryBase> Geo, string Path)// RadFilePath
        {
            var matlist = new HashSet<string>();

            StringBuilder finalFile = new StringBuilder();

            StringBuilder radFileString = new StringBuilder();

            int id = 0;

            foreach (GeometryBase g in Geo)
            {
                string mat = g.UserDictionary["RadMat"].ToString().Trim();
                matlist.Add(mat);

                string matName = mat.Split(' ')[2];

                //Print(matName);

                Mesh m = (Mesh)g;

                radFileString.AppendLine(Mesh2Rad(m, matName, id.ToString()));
                id++;
            }

            foreach (string s in matlist)
            {
                finalFile.AppendLine(s);
            }
            finalFile.AppendLine("");
            finalFile.AppendLine(radFileString.ToString());

            File.WriteAllText(Path, finalFile.ToString());

            // A = "Final File Length: " + finalFile.Length;
        }

        public static string Mesh2Rad(Mesh m, string RadianceMaterial, string id)
        {
            StringBuilder s = new StringBuilder();

            // string s = "";
            try
            {
                for (int i = 0; i < m.Faces.Count; ++i)
                {
                    if (m.Faces[i].IsTriangle)
                    {
                        s.AppendLine(RadianceMaterial + " polygon " + id + "_" + i);
                        s.AppendLine("0");
                        s.AppendLine("0");
                        s.AppendLine("9");

                        int v0 = m.Faces[i].A;
                        int v1 = m.Faces[i].B;
                        int v2 = m.Faces[i].C;

                        s.AppendLine(FormatPoint(m.Vertices[v0]));
                        s.AppendLine(FormatPoint(m.Vertices[v1]));
                        s.AppendLine(FormatPoint(m.Vertices[v2]));
                        s.AppendLine();
                    }
                    else
                    {
                        s.AppendLine(RadianceMaterial + " polygon " + id + "_" + i);
                        s.AppendLine("0");
                        s.AppendLine("0");
                        s.AppendLine("12");

                        int v0 = m.Faces[i].A;
                        int v1 = m.Faces[i].B;
                        int v2 = m.Faces[i].C;
                        int v3 = m.Faces[i].D;

                        s.AppendLine(FormatPoint(m.Vertices[v0]));
                        s.AppendLine(FormatPoint(m.Vertices[v1]));
                        s.AppendLine(FormatPoint(m.Vertices[v2]));
                        s.AppendLine(FormatPoint(m.Vertices[v3]));
                        s.AppendLine();
                    }
                }
            }
            catch (Exception ex) { RhinoApp.WriteLine("Mesh2Rad failed " + ex.Message); }
            return s.ToString();
        }

        private static readonly CultureInfo radianceCulture = new CultureInfo("en-US");

        private static string FormatPoint(Point3f p)
        {
            return String.Format(radianceCulture, "{0:0.000} {1:0.000} {2:0.000}", p.X, p.Y, p.Z);
        }

        #endregion 2. RadFile

        #region 1. AddMat

        public static Mesh AddMat(string mat, Mesh m)

        {
            string matClean = DeleteComments(mat.Trim());
            string matSingleLine = Regex.Replace(matClean, @"\s+", " ", RegexOptions.Multiline);

            m.UserDictionary.Set("RadMat", matSingleLine);

            return m;
        }

        private static string BuildingGroundTemplate()
        {
            return @"void plastic OutsideGround_10
0
0
5 0.1 0.1 0.1 0 0";
        }

        public static string DeleteComments(string s)
        {
            StringBuilder sb = new StringBuilder();
            string[] lines = s.Split(Environment.NewLine.ToCharArray());
            foreach (var l in lines)
            {
                if (!l.Trim().StartsWith("#")) sb.AppendLine(l);
            }

            return sb.ToString();
        }

        #endregion 1. AddMat

        #region 0. 4PI Array

        private Vector3d[] equiSolidAngleVectors4PI()
        {
            Vector3d[] equiSolidAngleVectors4PI = new Vector3d[] { new Vector3d(0.0000, 0.0000, -1.0000), new Vector3d(0.7236, -0.5257, -0.4472), new Vector3d(-0.2764, -0.8506, -0.4472), new Vector3d(0.7236, 0.5257, -0.4472), new Vector3d(-0.8944, 0.0000, -0.4472), new Vector3d(-0.2764, 0.8506, -0.4472), new Vector3d(0.8944, 0.0000, 0.4472), new Vector3d(0.2764, -0.8506, 0.4472), new Vector3d(-0.7236, -0.5257, 0.4472), new Vector3d(-0.7236, 0.5257, 0.4472), new Vector3d(0.2764, 0.8506, 0.4472), new Vector3d(0.0000, 0.0000, 1.0000), new Vector3d(0.2629, -0.8090, -0.5257), new Vector3d(-0.1625, -0.5000, -0.8507), new Vector3d(0.4253, -0.3090, -0.8507), new Vector3d(0.4253, 0.3090, -0.8507), new Vector3d(0.8506, 0.0000, -0.5257), new Vector3d(-0.6882, -0.5000, -0.5257), new Vector3d(-0.5257, 0.0000, -0.8507), new Vector3d(-0.6882, 0.5000, -0.5257), new Vector3d(-0.1625, 0.5000, -0.8507), new Vector3d(0.2629, 0.8090, -0.5257), new Vector3d(0.9511, 0.3090, -0.0000), new Vector3d(0.9511, -0.3090, -0.0000), new Vector3d(0.5878, -0.8090, 0.0000), new Vector3d(0.0000, -1.0000, 0.0000), new Vector3d(-0.5878, -0.8090, -0.0000), new Vector3d(-0.9511, -0.3090, 0.0000), new Vector3d(-0.9511, 0.3090, 0.0000), new Vector3d(-0.5878, 0.8090, -0.0000), new Vector3d(0.0000, 1.0000, 0.0000), new Vector3d(0.5878, 0.8090, 0.0000), new Vector3d(0.6882, -0.5000, 0.5257), new Vector3d(-0.2629, -0.8090, 0.5257), new Vector3d(-0.8506, 0.0000, 0.5257), new Vector3d(-0.2629, 0.8090, 0.5257), new Vector3d(0.6882, 0.5000, 0.5257), new Vector3d(0.5257, 0.0000, 0.8507), new Vector3d(0.1625, -0.5000, 0.8507), new Vector3d(-0.4253, -0.3090, 0.8507), new Vector3d(-0.4253, 0.3090, 0.8507), new Vector3d(0.1625, 0.5000, 0.8507), new Vector3d(0.5128, -0.6938, -0.5057), new Vector3d(-0.0844, -0.2599, -0.9619), new Vector3d(0.2211, -0.1606, -0.9619), new Vector3d(0.2211, 0.1606, -0.9619), new Vector3d(0.8183, -0.2733, -0.5057), new Vector3d(-0.5014, -0.7020, -0.5057), new Vector3d(-0.2733, 0.0000, -0.9619), new Vector3d(-0.8226, 0.2599, -0.5057), new Vector3d(-0.0844, 0.2599, -0.9619), new Vector3d(0.5128, 0.6938, -0.5057), new Vector3d(0.8705, 0.4339, -0.2325), new Vector3d(0.8705, -0.4339, -0.2325), new Vector3d(0.6816, -0.6938, -0.2325), new Vector3d(-0.1437, -0.9619, -0.2325), new Vector3d(-0.4492, -0.8627, -0.2325), new Vector3d(-0.9593, -0.1606, -0.2325), new Vector3d(-0.9593, 0.1606, -0.2325), new Vector3d(-0.4492, 0.8627, -0.2325), new Vector3d(-0.1437, 0.9619, -0.2325), new Vector3d(0.6816, 0.6938, -0.2325), new Vector3d(0.8226, -0.2599, 0.5057), new Vector3d(0.0070, -0.8627, 0.5057), new Vector3d(-0.8183, -0.2733, 0.5057), new Vector3d(-0.5128, 0.6938, 0.5057), new Vector3d(0.8226, 0.2599, 0.5057), new Vector3d(0.7382, 0.0000, 0.6746), new Vector3d(0.2281, -0.7020, 0.6746), new Vector3d(-0.5972, -0.4339, 0.6746), new Vector3d(-0.5972, 0.4339, 0.6746), new Vector3d(0.2281, 0.7020, 0.6746), new Vector3d(-0.0070, -0.8627, -0.5057), new Vector3d(-0.2281, -0.7020, -0.6746), new Vector3d(0.5972, -0.4339, -0.6746), new Vector3d(0.5972, 0.4339, -0.6746), new Vector3d(0.8183, 0.2733, -0.5057), new Vector3d(-0.8226, -0.2599, -0.5057), new Vector3d(-0.7382, 0.0000, -0.6746), new Vector3d(-0.5014, 0.7020, -0.5057), new Vector3d(-0.2281, 0.7020, -0.6746), new Vector3d(-0.0070, 0.8627, -0.5057), new Vector3d(0.9593, 0.1606, 0.2325), new Vector3d(0.9593, -0.1606, 0.2325), new Vector3d(0.4492, -0.8627, 0.2325), new Vector3d(0.1437, -0.9619, 0.2325), new Vector3d(-0.6816, -0.6938, 0.2325), new Vector3d(-0.8705, -0.4339, 0.2325), new Vector3d(-0.8705, 0.4339, 0.2325), new Vector3d(-0.6816, 0.6938, 0.2325), new Vector3d(0.1437, 0.9619, 0.2325), new Vector3d(0.4492, 0.8627, 0.2325), new Vector3d(0.5014, -0.7020, 0.5057), new Vector3d(-0.5128, -0.6938, 0.5057), new Vector3d(-0.8183, 0.2733, 0.5057), new Vector3d(0.0070, 0.8627, 0.5057), new Vector3d(0.5014, 0.7020, 0.5057), new Vector3d(0.2733, 0.0000, 0.9619), new Vector3d(0.0844, -0.2599, 0.9619), new Vector3d(-0.2211, -0.1606, 0.9619), new Vector3d(-0.2211, 0.1606, 0.9619), new Vector3d(0.0844, 0.2599, 0.9619), new Vector3d(0.1382, -0.4253, -0.8944), new Vector3d(0.3618, -0.5878, -0.7236), new Vector3d(0.0528, -0.6882, -0.7236), new Vector3d(0.6708, -0.1625, -0.7236), new Vector3d(0.4472, 0.0000, -0.8944), new Vector3d(0.6708, 0.1625, -0.7236), new Vector3d(-0.3618, -0.2629, -0.8944), new Vector3d(-0.4472, -0.5257, -0.7236), new Vector3d(-0.6382, -0.2629, -0.7236), new Vector3d(-0.3618, 0.2629, -0.8944), new Vector3d(-0.6382, 0.2629, -0.7236), new Vector3d(-0.4472, 0.5257, -0.7236), new Vector3d(0.1382, 0.4253, -0.8944), new Vector3d(0.0528, 0.6882, -0.7236), new Vector3d(0.3618, 0.5878, -0.7236), new Vector3d(0.9472, -0.1625, -0.2764), new Vector3d(0.9472, 0.1625, -0.2764), new Vector3d(1.0000, 0.0000, -0.0000), new Vector3d(0.1382, -0.9511, -0.2764), new Vector3d(0.4472, -0.8506, -0.2764), new Vector3d(0.3090, -0.9511, 0.0000), new Vector3d(-0.8618, -0.4253, -0.2764), new Vector3d(-0.6708, -0.6882, -0.2764), new Vector3d(-0.8090, -0.5878, 0.0000), new Vector3d(-0.6708, 0.6882, -0.2764), new Vector3d(-0.8618, 0.4253, -0.2764), new Vector3d(-0.8090, 0.5878, 0.0000), new Vector3d(0.4472, 0.8506, -0.2764), new Vector3d(0.1382, 0.9511, -0.2764), new Vector3d(0.3090, 0.9511, 0.0000), new Vector3d(0.8090, -0.5878, -0.0000), new Vector3d(0.8618, -0.4253, 0.2764), new Vector3d(0.6708, -0.6882, 0.2764), new Vector3d(-0.3090, -0.9511, -0.0000), new Vector3d(-0.1382, -0.9511, 0.2764), new Vector3d(-0.4472, -0.8506, 0.2764), new Vector3d(-1.0000, 0.0000, 0.0000), new Vector3d(-0.9472, -0.1625, 0.2764), new Vector3d(-0.9472, 0.1625, 0.2764), new Vector3d(-0.3090, 0.9511, -0.0000), new Vector3d(-0.4472, 0.8506, 0.2764), new Vector3d(-0.1382, 0.9511, 0.2764), new Vector3d(0.8090, 0.5878, -0.0000), new Vector3d(0.6708, 0.6882, 0.2764), new Vector3d(0.8618, 0.4253, 0.2764), new Vector3d(0.4472, -0.5257, 0.7236), new Vector3d(0.6382, -0.2629, 0.7236), new Vector3d(0.3618, -0.2629, 0.8944), new Vector3d(-0.3618, -0.5878, 0.7236), new Vector3d(-0.0528, -0.6882, 0.7236), new Vector3d(-0.1382, -0.4253, 0.8944), new Vector3d(-0.6708, 0.1625, 0.7236), new Vector3d(-0.6708, -0.1625, 0.7236), new Vector3d(-0.4472, 0.0000, 0.8944), new Vector3d(-0.0528, 0.6882, 0.7236), new Vector3d(-0.3618, 0.5878, 0.7236), new Vector3d(-0.1382, 0.4253, 0.8944), new Vector3d(0.6382, 0.2629, 0.7236), new Vector3d(0.4472, 0.5257, 0.7236), new Vector3d(0.3618, 0.2629, 0.8944), new Vector3d(0.6241, -0.6156, -0.4811), new Vector3d(-0.0426, -0.1312, -0.9904), new Vector3d(0.1116, -0.0811, -0.9904), new Vector3d(0.1116, 0.0811, -0.9904), new Vector3d(0.7784, -0.4034, -0.4811), new Vector3d(-0.3926, -0.7838, -0.4811), new Vector3d(-0.1380, 0.0000, -0.9904), new Vector3d(-0.8668, 0.1312, -0.4811), new Vector3d(-0.0426, 0.1312, -0.9904), new Vector3d(0.6241, 0.6156, -0.4811), new Vector3d(0.8047, 0.4844, -0.3431), new Vector3d(0.8047, -0.4844, -0.3431), new Vector3d(0.7094, -0.6156, -0.3431), new Vector3d(-0.2121, -0.9150, -0.3431), new Vector3d(-0.3663, -0.8649, -0.3431), new Vector3d(-0.9358, -0.0811, -0.3431), new Vector3d(-0.9358, 0.0811, -0.3431), new Vector3d(-0.3663, 0.8649, -0.3431), new Vector3d(-0.2121, 0.9150, -0.3431), new Vector3d(0.7094, 0.6156, -0.3431), new Vector3d(0.8668, -0.1312, 0.4811), new Vector3d(0.1431, -0.8649, 0.4811), new Vector3d(-0.7784, -0.4034, 0.4811), new Vector3d(-0.6241, 0.6156, 0.4811), new Vector3d(0.8668, 0.1312, 0.4811), new Vector3d(0.8242, 0.0000, 0.5663), new Vector3d(0.2547, -0.7838, 0.5663), new Vector3d(-0.6668, -0.4844, 0.5663), new Vector3d(-0.6668, 0.4844, 0.5663), new Vector3d(0.2547, 0.7838, 0.5663), new Vector3d(-0.1431, -0.8649, -0.4811), new Vector3d(-0.2547, -0.7838, -0.5663), new Vector3d(0.6668, -0.4844, -0.5663), new Vector3d(0.6668, 0.4844, -0.5663), new Vector3d(0.7784, 0.4034, -0.4811), new Vector3d(-0.8668, -0.1312, -0.4811), new Vector3d(-0.8242, 0.0000, -0.5663), new Vector3d(-0.3926, 0.7838, -0.4811), new Vector3d(-0.2547, 0.7838, -0.5663), new Vector3d(-0.1431, 0.8649, -0.4811), new Vector3d(0.9358, 0.0811, 0.3431), new Vector3d(0.9358, -0.0811, 0.3431), new Vector3d(0.3663, -0.8649, 0.3431), new Vector3d(0.2121, -0.9150, 0.3431), new Vector3d(-0.7094, -0.6156, 0.3431), new Vector3d(-0.8047, -0.4844, 0.3431), new Vector3d(-0.8047, 0.4844, 0.3431), new Vector3d(-0.7094, 0.6156, 0.3431), new Vector3d(0.2121, 0.9150, 0.3431), new Vector3d(0.3663, 0.8649, 0.3431), new Vector3d(0.3926, -0.7838, 0.4811), new Vector3d(-0.6241, -0.6156, 0.4811), new Vector3d(-0.7784, 0.4034, 0.4811), new Vector3d(0.1431, 0.8649, 0.4811), new Vector3d(0.3926, 0.7838, 0.4811), new Vector3d(0.1380, 0.0000, 0.9904), new Vector3d(0.0426, -0.1312, 0.9904), new Vector3d(-0.1116, -0.0811, 0.9904), new Vector3d(-0.1116, 0.0811, 0.9904), new Vector3d(0.0426, 0.1312, 0.9904), new Vector3d(-0.0123, -0.4684, -0.8834), new Vector3d(0.3162, -0.7071, -0.6325), new Vector3d(0.1598, -0.7579, -0.6325), new Vector3d(0.5549, -0.2387, -0.7969), new Vector3d(0.4417, -0.1564, -0.8834), new Vector3d(0.5549, 0.2387, -0.7969), new Vector3d(-0.2654, -0.3862, -0.8834), new Vector3d(-0.3086, -0.5193, -0.7969), new Vector3d(-0.6715, -0.3862, -0.6325), new Vector3d(-0.4493, 0.1331, -0.8834), new Vector3d(-0.5892, 0.1331, -0.7969), new Vector3d(-0.5748, 0.5193, -0.6325), new Vector3d(0.2853, 0.3717, -0.8834), new Vector3d(-0.0555, 0.6015, -0.7969), new Vector3d(0.3985, 0.4540, -0.7969), new Vector3d(0.9101, -0.0822, -0.4061), new Vector3d(0.9101, 0.0822, -0.4061), new Vector3d(0.9877, 0.1564, -0.0000), new Vector3d(0.2030, -0.8910, -0.4061), new Vector3d(0.3595, -0.8402, -0.4061), new Vector3d(0.4540, -0.8910, 0.0000), new Vector3d(-0.7847, -0.4684, -0.4061), new Vector3d(-0.6880, -0.6015, -0.4061), new Vector3d(-0.7071, -0.7071, 0.0000), new Vector3d(-0.6880, 0.6015, -0.4061), new Vector3d(-0.7847, 0.4684, -0.4061), new Vector3d(-0.8910, 0.4540, 0.0000), new Vector3d(0.3595, 0.8402, -0.4061), new Vector3d(0.2030, 0.8910, -0.4061), new Vector3d(0.1564, 0.9877, 0.0000), new Vector3d(0.8910, -0.4540, -0.0000), new Vector3d(0.9177, -0.3717, 0.1399), new Vector3d(0.6371, -0.7579, 0.1399), new Vector3d(-0.1564, -0.9877, -0.0000), new Vector3d(-0.0700, -0.9877, 0.1399), new Vector3d(-0.5240, -0.8402, 0.1399), new Vector3d(-0.9877, -0.1564, 0.0000), new Vector3d(-0.9610, -0.2387, 0.1399), new Vector3d(-0.9610, 0.2387, 0.1399), new Vector3d(-0.4540, 0.8910, -0.0000), new Vector3d(-0.5240, 0.8402, 0.1399), new Vector3d(-0.0700, 0.9877, 0.1399), new Vector3d(0.8910, 0.4540, -0.0000), new Vector3d(0.6371, 0.7579, 0.1399), new Vector3d(0.9177, 0.3717, 0.1399), new Vector3d(0.5748, -0.5193, 0.6325), new Vector3d(0.6715, -0.3862, 0.6325), new Vector3d(0.4493, -0.1331, 0.8834), new Vector3d(-0.3162, -0.7071, 0.6325), new Vector3d(-0.1598, -0.7579, 0.6325), new Vector3d(0.0123, -0.4684, 0.8834), new Vector3d(-0.7702, 0.0822, 0.6325), new Vector3d(-0.7702, -0.0822, 0.6325), new Vector3d(-0.4417, -0.1564, 0.8834), new Vector3d(-0.1598, 0.7579, 0.6325), new Vector3d(-0.3162, 0.7071, 0.6325), new Vector3d(-0.2853, 0.3717, 0.8834), new Vector3d(0.6715, 0.3862, 0.6325), new Vector3d(0.5748, 0.5193, 0.6325), new Vector3d(0.4493, 0.1331, 0.8834), new Vector3d(0.3916, -0.7586, -0.5207), new Vector3d(-0.1246, -0.3836, -0.9150), new Vector3d(0.3263, -0.2371, -0.9150), new Vector3d(0.3263, 0.2371, -0.9150), new Vector3d(0.8425, -0.1380, -0.5207), new Vector3d(-0.6005, -0.6068, -0.5207), new Vector3d(-0.4034, 0.0000, -0.9150), new Vector3d(-0.7627, 0.3836, -0.5207), new Vector3d(-0.1246, 0.3836, -0.9150), new Vector3d(0.3916, 0.7586, -0.5207), new Vector3d(0.9196, 0.3750, -0.1174), new Vector3d(0.9196, -0.3750, -0.1174), new Vector3d(0.6408, -0.7587, -0.1174), new Vector3d(-0.0725, -0.9904, -0.1174), new Vector3d(-0.5235, -0.8439, -0.1174), new Vector3d(-0.9644, -0.2371, -0.1173), new Vector3d(-0.9644, 0.2371, -0.1173), new Vector3d(-0.5235, 0.8439, -0.1174), new Vector3d(-0.0725, 0.9904, -0.1174), new Vector3d(0.6408, 0.7587, -0.1174), new Vector3d(0.7627, -0.3836, 0.5207), new Vector3d(-0.1292, -0.8439, 0.5207), new Vector3d(-0.8425, -0.1380, 0.5207), new Vector3d(-0.3916, 0.7586, 0.5207), new Vector3d(0.7627, 0.3836, 0.5207), new Vector3d(0.6381, 0.0000, 0.7700), new Vector3d(0.1972, -0.6068, 0.7700), new Vector3d(-0.5162, -0.3750, 0.7700), new Vector3d(-0.5162, 0.3750, 0.7700), new Vector3d(0.1972, 0.6068, 0.7700), new Vector3d(0.1292, -0.8439, -0.5207), new Vector3d(-0.1972, -0.6068, -0.7700), new Vector3d(0.5162, -0.3750, -0.7700), new Vector3d(0.5162, 0.3750, -0.7700), new Vector3d(0.8425, 0.1380, -0.5207), new Vector3d(-0.7627, -0.3836, -0.5207), new Vector3d(-0.6381, 0.0000, -0.7700), new Vector3d(-0.6005, 0.6068, -0.5207), new Vector3d(-0.1972, 0.6068, -0.7700), new Vector3d(0.1292, 0.8439, -0.5207), new Vector3d(0.9644, 0.2371, 0.1173), new Vector3d(0.9644, -0.2371, 0.1173), new Vector3d(0.5235, -0.8439, 0.1174), new Vector3d(0.0725, -0.9904, 0.1174), new Vector3d(-0.6408, -0.7587, 0.1174), new Vector3d(-0.9196, -0.3750, 0.1174), new Vector3d(-0.9196, 0.3750, 0.1174), new Vector3d(-0.6408, 0.7587, 0.1174), new Vector3d(0.0725, 0.9904, 0.1174), new Vector3d(0.5235, 0.8439, 0.1174), new Vector3d(0.6005, -0.6068, 0.5207), new Vector3d(-0.3916, -0.7586, 0.5207), new Vector3d(-0.8425, 0.1380, 0.5207), new Vector3d(-0.1292, 0.8439, 0.5207), new Vector3d(0.6005, 0.6068, 0.5207), new Vector3d(0.4034, 0.0000, 0.9150), new Vector3d(0.1246, -0.3836, 0.9150), new Vector3d(-0.3263, -0.2371, 0.9150), new Vector3d(-0.3263, 0.2371, 0.9150), new Vector3d(0.1246, 0.3836, 0.9150), new Vector3d(0.2853, -0.3717, -0.8834), new Vector3d(0.3985, -0.4540, -0.7969), new Vector3d(-0.0555, -0.6015, -0.7969), new Vector3d(0.7702, -0.0822, -0.6325), new Vector3d(0.4417, 0.1564, -0.8834), new Vector3d(0.7702, 0.0822, -0.6325), new Vector3d(-0.4493, -0.1331, -0.8834), new Vector3d(-0.5748, -0.5193, -0.6325), new Vector3d(-0.5892, -0.1331, -0.7969), new Vector3d(-0.2654, 0.3862, -0.8834), new Vector3d(-0.6715, 0.3862, -0.6325), new Vector3d(-0.3086, 0.5193, -0.7969), new Vector3d(-0.0123, 0.4684, -0.8834), new Vector3d(0.1598, 0.7579, -0.6325), new Vector3d(0.3162, 0.7071, -0.6325), new Vector3d(0.9610, -0.2387, -0.1399), new Vector3d(0.9610, 0.2387, -0.1399), new Vector3d(0.9877, -0.1564, -0.0000), new Vector3d(0.0700, -0.9877, -0.1399), new Vector3d(0.5240, -0.8402, -0.1399), new Vector3d(0.1564, -0.9877, 0.0000), new Vector3d(-0.9177, -0.3717, -0.1399), new Vector3d(-0.6371, -0.7579, -0.1399), new Vector3d(-0.8910, -0.4540, 0.0000), new Vector3d(-0.6371, 0.7579, -0.1399), new Vector3d(-0.9177, 0.3717, -0.1399), new Vector3d(-0.7071, 0.7071, 0.0000), new Vector3d(0.5240, 0.8402, -0.1399), new Vector3d(0.0700, 0.9877, -0.1399), new Vector3d(0.4540, 0.8910, 0.0000), new Vector3d(0.7071, -0.7071, -0.0000), new Vector3d(0.7847, -0.4684, 0.4061), new Vector3d(0.6880, -0.6015, 0.4061), new Vector3d(-0.4540, -0.8910, -0.0000), new Vector3d(-0.2030, -0.8910, 0.4061), new Vector3d(-0.3595, -0.8402, 0.4061), new Vector3d(-0.9877, 0.1564, 0.0000), new Vector3d(-0.9101, -0.0822, 0.4061), new Vector3d(-0.9101, 0.0822, 0.4061), new Vector3d(-0.1564, 0.9877, -0.0000), new Vector3d(-0.3595, 0.8402, 0.4061), new Vector3d(-0.2030, 0.8910, 0.4061), new Vector3d(0.7071, 0.7071, -0.0000), new Vector3d(0.6880, 0.6015, 0.4061), new Vector3d(0.7847, 0.4684, 0.4061), new Vector3d(0.3086, -0.5193, 0.7969), new Vector3d(0.5892, -0.1331, 0.7969), new Vector3d(0.2654, -0.3862, 0.8834), new Vector3d(-0.3985, -0.4540, 0.7969), new Vector3d(0.0555, -0.6015, 0.7969), new Vector3d(-0.2853, -0.3717, 0.8834), new Vector3d(-0.5549, 0.2387, 0.7969), new Vector3d(-0.5549, -0.2387, 0.7969), new Vector3d(-0.4417, 0.1564, 0.8834), new Vector3d(0.0555, 0.6015, 0.7969), new Vector3d(-0.3985, 0.4540, 0.7969), new Vector3d(0.0123, 0.4684, 0.8834), new Vector3d(0.5892, 0.1331, 0.7969), new Vector3d(0.3086, 0.5193, 0.7969), new Vector3d(0.2654, 0.3862, 0.8834), new Vector3d(0.0692, -0.2130, -0.9746), new Vector3d(0.1815, -0.2960, -0.9378), new Vector3d(0.0272, -0.3462, -0.9378), new Vector3d(0.4845, -0.5161, -0.7063), new Vector3d(0.5623, -0.5712, -0.5979), new Vector3d(0.4418, -0.6474, -0.6210), new Vector3d(-0.0886, -0.7023, -0.7063), new Vector3d(0.0231, -0.7834, -0.6210), new Vector3d(-0.1191, -0.7926, -0.5979), new Vector3d(0.2534, -0.5134, -0.8199), new Vector3d(0.2101, -0.6466, -0.7334), new Vector3d(0.0968, -0.5643, -0.8199), new Vector3d(0.7170, -0.3582, -0.5979), new Vector3d(0.6406, -0.3013, -0.7063), new Vector3d(0.7522, -0.2201, -0.6210), new Vector3d(0.3376, -0.0811, -0.9378), new Vector3d(0.2240, 0.0000, -0.9746), new Vector3d(0.3376, 0.0811, -0.9378), new Vector3d(0.7522, 0.2201, -0.6210), new Vector3d(0.6406, 0.3013, -0.7063), new Vector3d(0.7170, 0.3582, -0.5979), new Vector3d(0.5665, -0.0823, -0.8199), new Vector3d(0.5665, 0.0823, -0.8199), new Vector3d(0.6798, 0.0000, -0.7334), new Vector3d(-0.1812, -0.1317, -0.9746), new Vector3d(-0.2254, -0.2641, -0.9378), new Vector3d(-0.3208, -0.1328, -0.9378), new Vector3d(-0.3412, -0.6202, -0.7063), new Vector3d(-0.3695, -0.7113, -0.5979), new Vector3d(-0.4792, -0.6202, -0.6210), new Vector3d(-0.6953, -0.1328, -0.7063), new Vector3d(-0.7380, -0.2641, -0.6210), new Vector3d(-0.7907, -0.1317, -0.5979), new Vector3d(-0.4100, -0.3996, -0.8199), new Vector3d(-0.5500, -0.3996, -0.7334), new Vector3d(-0.5067, -0.2664, -0.8199), new Vector3d(-0.1812, 0.1317, -0.9746), new Vector3d(-0.3208, 0.1328, -0.9378), new Vector3d(-0.2254, 0.2641, -0.9378), new Vector3d(-0.6953, 0.1328, -0.7063), new Vector3d(-0.7907, 0.1317, -0.5979), new Vector3d(-0.7380, 0.2641, -0.6210), new Vector3d(-0.3412, 0.6202, -0.7063), new Vector3d(-0.4792, 0.6202, -0.6210), new Vector3d(-0.3695, 0.7113, -0.5979), new Vector3d(-0.5067, 0.2664, -0.8199), new Vector3d(-0.5500, 0.3996, -0.7334), new Vector3d(-0.4100, 0.3996, -0.8199), new Vector3d(0.0692, 0.2130, -0.9746), new Vector3d(0.0272, 0.3462, -0.9378), new Vector3d(0.1815, 0.2960, -0.9378), new Vector3d(-0.0886, 0.7023, -0.7063), new Vector3d(-0.1191, 0.7926, -0.5979), new Vector3d(0.0231, 0.7834, -0.6210), new Vector3d(0.4845, 0.5161, -0.7063), new Vector3d(0.4418, 0.6474, -0.6210), new Vector3d(0.5623, 0.5712, -0.5979), new Vector3d(0.0968, 0.5643, -0.8199), new Vector3d(0.2101, 0.6466, -0.7334), new Vector3d(0.2534, 0.5134, -0.8199), new Vector3d(0.8555, -0.3582, -0.3739), new Vector3d(0.8919, -0.2201, -0.3951), new Vector3d(0.9182, -0.3013, -0.2571), new Vector3d(0.8919, 0.2201, -0.3951), new Vector3d(0.8555, 0.3582, -0.3739), new Vector3d(0.9182, 0.3013, -0.2571), new Vector3d(0.9898, -0.0811, 0.1174), new Vector3d(0.9898, 0.0811, 0.1174), new Vector3d(0.9719, 0.0000, 0.2355), new Vector3d(0.9600, 0.0000, -0.2801), new Vector3d(0.9867, 0.0823, -0.1401), new Vector3d(0.9867, -0.0823, -0.1401), new Vector3d(-0.0763, -0.9243, -0.3739), new Vector3d(0.0663, -0.9162, -0.3951), new Vector3d(-0.0028, -0.9664, -0.2571), new Vector3d(0.4850, -0.7802, -0.3951), new Vector3d(0.6051, -0.7029, -0.3739), new Vector3d(0.5703, -0.7802, -0.2571), new Vector3d(0.2287, -0.9664, 0.1174), new Vector3d(0.3830, -0.9162, 0.1174), new Vector3d(0.3003, -0.9243, 0.2355), new Vector3d(0.2966, -0.9130, -0.2801), new Vector3d(0.3832, -0.9130, -0.1401), new Vector3d(0.2266, -0.9639, -0.1401), new Vector3d(-0.9027, -0.2130, -0.3739), new Vector3d(-0.8509, -0.3462, -0.3951), new Vector3d(-0.9199, -0.2960, -0.2571), new Vector3d(-0.5922, -0.7023, -0.3951), new Vector3d(-0.4815, -0.7926, -0.3739), new Vector3d(-0.5658, -0.7835, -0.2571), new Vector3d(-0.8484, -0.5161, 0.1174), new Vector3d(-0.7530, -0.6474, 0.1174), new Vector3d(-0.7863, -0.5712, 0.2355), new Vector3d(-0.7766, -0.5643, -0.2801), new Vector3d(-0.7499, -0.6466, -0.1401), new Vector3d(-0.8467, -0.5134, -0.1401), new Vector3d(-0.4815, 0.7926, -0.3739), new Vector3d(-0.5922, 0.7023, -0.3951), new Vector3d(-0.5658, 0.7835, -0.2571), new Vector3d(-0.8509, 0.3462, -0.3951), new Vector3d(-0.9027, 0.2130, -0.3739), new Vector3d(-0.9199, 0.2960, -0.2571), new Vector3d(-0.7530, 0.6474, 0.1174), new Vector3d(-0.8484, 0.5161, 0.1174), new Vector3d(-0.7863, 0.5712, 0.2355), new Vector3d(-0.7766, 0.5643, -0.2801), new Vector3d(-0.8467, 0.5134, -0.1401), new Vector3d(-0.7499, 0.6466, -0.1401), new Vector3d(0.6051, 0.7029, -0.3739), new Vector3d(0.4850, 0.7802, -0.3951), new Vector3d(0.5703, 0.7802, -0.2571), new Vector3d(0.0663, 0.9162, -0.3951), new Vector3d(-0.0763, 0.9243, -0.3739), new Vector3d(-0.0028, 0.9664, -0.2571), new Vector3d(0.3830, 0.9162, 0.1174), new Vector3d(0.2287, 0.9664, 0.1174), new Vector3d(0.3003, 0.9243, 0.2355), new Vector3d(0.2966, 0.9130, -0.2801), new Vector3d(0.2266, 0.9639, -0.1401), new Vector3d(0.3832, 0.9130, -0.1401), new Vector3d(0.7863, -0.5712, -0.2355), new Vector3d(0.8484, -0.5161, -0.1174), new Vector3d(0.7530, -0.6474, -0.1174), new Vector3d(0.9199, -0.2960, 0.2571), new Vector3d(0.9027, -0.2130, 0.3739), new Vector3d(0.8509, -0.3462, 0.3951), new Vector3d(0.5658, -0.7835, 0.2571), new Vector3d(0.5922, -0.7023, 0.3951), new Vector3d(0.4815, -0.7926, 0.3739), new Vector3d(0.8467, -0.5134, 0.1401), new Vector3d(0.7766, -0.5643, 0.2801), new Vector3d(0.7499, -0.6466, 0.1401), new Vector3d(-0.3003, -0.9243, -0.2355), new Vector3d(-0.2287, -0.9664, -0.1174), new Vector3d(-0.3830, -0.9162, -0.1174), new Vector3d(0.0028, -0.9664, 0.2571), new Vector3d(0.0763, -0.9243, 0.3739), new Vector3d(-0.0663, -0.9162, 0.3951), new Vector3d(-0.5703, -0.7802, 0.2571), new Vector3d(-0.4850, -0.7802, 0.3951), new Vector3d(-0.6051, -0.7029, 0.3739), new Vector3d(-0.2266, -0.9639, 0.1401), new Vector3d(-0.2966, -0.9130, 0.2801), new Vector3d(-0.3832, -0.9130, 0.1401), new Vector3d(-0.9719, 0.0000, -0.2355), new Vector3d(-0.9898, -0.0811, -0.1174), new Vector3d(-0.9898, 0.0811, -0.1174), new Vector3d(-0.9182, -0.3013, 0.2571), new Vector3d(-0.8555, -0.3582, 0.3739), new Vector3d(-0.8919, -0.2201, 0.3951), new Vector3d(-0.9182, 0.3013, 0.2571), new Vector3d(-0.8919, 0.2201, 0.3951), new Vector3d(-0.8555, 0.3582, 0.3739), new Vector3d(-0.9867, -0.0823, 0.1401), new Vector3d(-0.9600, 0.0000, 0.2801), new Vector3d(-0.9867, 0.0823, 0.1401), new Vector3d(-0.3003, 0.9243, -0.2355), new Vector3d(-0.3830, 0.9162, -0.1174), new Vector3d(-0.2287, 0.9664, -0.1174), new Vector3d(-0.5703, 0.7802, 0.2571), new Vector3d(-0.6051, 0.7029, 0.3739), new Vector3d(-0.4850, 0.7802, 0.3951), new Vector3d(0.0028, 0.9664, 0.2571), new Vector3d(-0.0663, 0.9162, 0.3951), new Vector3d(0.0763, 0.9243, 0.3739), new Vector3d(-0.3832, 0.9130, 0.1401), new Vector3d(-0.2966, 0.9130, 0.2801), new Vector3d(-0.2266, 0.9639, 0.1401), new Vector3d(0.7863, 0.5712, -0.2355), new Vector3d(0.7530, 0.6474, -0.1174), new Vector3d(0.8484, 0.5161, -0.1174), new Vector3d(0.5658, 0.7835, 0.2571), new Vector3d(0.4815, 0.7926, 0.3739), new Vector3d(0.5922, 0.7023, 0.3951), new Vector3d(0.9199, 0.2960, 0.2571), new Vector3d(0.8509, 0.3462, 0.3951), new Vector3d(0.9027, 0.2130, 0.3739), new Vector3d(0.7499, 0.6466, 0.1401), new Vector3d(0.7766, 0.5643, 0.2801), new Vector3d(0.8467, 0.5134, 0.1401), new Vector3d(0.3695, -0.7113, 0.5979), new Vector3d(0.4792, -0.6202, 0.6210), new Vector3d(0.3412, -0.6202, 0.7063), new Vector3d(0.7380, -0.2641, 0.6210), new Vector3d(0.7907, -0.1317, 0.5979), new Vector3d(0.6953, -0.1328, 0.7063), new Vector3d(0.2254, -0.2641, 0.9378), new Vector3d(0.3208, -0.1328, 0.9378), new Vector3d(0.1812, -0.1317, 0.9746), new Vector3d(0.5500, -0.3996, 0.7334), new Vector3d(0.5067, -0.2664, 0.8199), new Vector3d(0.4100, -0.3996, 0.8199), new Vector3d(-0.5623, -0.5712, 0.5979), new Vector3d(-0.4418, -0.6474, 0.6210), new Vector3d(-0.4845, -0.5161, 0.7063), new Vector3d(-0.0231, -0.7834, 0.6210), new Vector3d(0.1191, -0.7926, 0.5979), new Vector3d(0.0886, -0.7023, 0.7063), new Vector3d(-0.1815, -0.2960, 0.9378), new Vector3d(-0.0272, -0.3462, 0.9378), new Vector3d(-0.0692, -0.2130, 0.9746), new Vector3d(-0.2101, -0.6466, 0.7334), new Vector3d(-0.0968, -0.5643, 0.8199), new Vector3d(-0.2534, -0.5134, 0.8199), new Vector3d(-0.7170, 0.3582, 0.5979), new Vector3d(-0.7522, 0.2201, 0.6210), new Vector3d(-0.6406, 0.3013, 0.7063), new Vector3d(-0.7522, -0.2201, 0.6210), new Vector3d(-0.7170, -0.3582, 0.5979), new Vector3d(-0.6406, -0.3013, 0.7063), new Vector3d(-0.3376, 0.0811, 0.9378), new Vector3d(-0.3376, -0.0811, 0.9378), new Vector3d(-0.2240, 0.0000, 0.9746), new Vector3d(-0.6798, 0.0000, 0.7334), new Vector3d(-0.5665, -0.0823, 0.8199), new Vector3d(-0.5665, 0.0823, 0.8199), new Vector3d(0.1191, 0.7926, 0.5979), new Vector3d(-0.0231, 0.7834, 0.6210), new Vector3d(0.0886, 0.7023, 0.7063), new Vector3d(-0.4418, 0.6474, 0.6210), new Vector3d(-0.5623, 0.5712, 0.5979), new Vector3d(-0.4845, 0.5161, 0.7063), new Vector3d(-0.0272, 0.3462, 0.9378), new Vector3d(-0.1815, 0.2960, 0.9378), new Vector3d(-0.0692, 0.2130, 0.9746), new Vector3d(-0.2101, 0.6466, 0.7334), new Vector3d(-0.2534, 0.5134, 0.8199), new Vector3d(-0.0968, 0.5643, 0.8199), new Vector3d(0.7907, 0.1317, 0.5979), new Vector3d(0.7380, 0.2641, 0.6210), new Vector3d(0.6953, 0.1328, 0.7063), new Vector3d(0.4792, 0.6202, 0.6210), new Vector3d(0.3695, 0.7113, 0.5979), new Vector3d(0.3412, 0.6202, 0.7063), new Vector3d(0.3208, 0.1328, 0.9378), new Vector3d(0.2254, 0.2641, 0.9378), new Vector3d(0.1812, 0.1317, 0.9746), new Vector3d(0.5500, 0.3996, 0.7334), new Vector3d(0.4100, 0.3996, 0.8199), new Vector3d(0.5067, 0.2664, 0.8199), new Vector3d(0.6755, -0.5720, -0.4653), new Vector3d(-0.0214, -0.0658, -0.9976), new Vector3d(0.0559, -0.0406, -0.9976), new Vector3d(0.0559, 0.0406, -0.9976), new Vector3d(0.7528, -0.4657, -0.4653), new Vector3d(-0.3353, -0.8192, -0.4653), new Vector3d(-0.0691, 0.0000, -0.9976), new Vector3d(-0.8827, 0.0658, -0.4653), new Vector3d(-0.0214, 0.0658, -0.9976), new Vector3d(0.6755, 0.5720, -0.4653), new Vector3d(0.7660, 0.5063, -0.3961), new Vector3d(0.7660, -0.5063, -0.3961), new Vector3d(0.7182, -0.5721, -0.3961), new Vector3d(-0.2448, -0.8850, -0.3961), new Vector3d(-0.3221, -0.8598, -0.3961), new Vector3d(-0.9173, -0.0406, -0.3961), new Vector3d(-0.9173, 0.0406, -0.3961), new Vector3d(-0.3221, 0.8598, -0.3961), new Vector3d(-0.2448, 0.8850, -0.3961), new Vector3d(0.7182, 0.5721, -0.3961), new Vector3d(0.8827, -0.0658, 0.4653), new Vector3d(0.2102, -0.8598, 0.4653), new Vector3d(-0.7528, -0.4657, 0.4653), new Vector3d(-0.6755, 0.5720, 0.4653), new Vector3d(0.8827, 0.0658, 0.4653), new Vector3d(0.8614, 0.0000, 0.5080), new Vector3d(0.2662, -0.8192, 0.5080), new Vector3d(-0.6969, -0.5063, 0.5080), new Vector3d(-0.6969, 0.5063, 0.5080), new Vector3d(0.2662, 0.8192, 0.5080), new Vector3d(-0.2102, -0.8598, -0.4653), new Vector3d(-0.2662, -0.8192, -0.5080), new Vector3d(0.6969, -0.5063, -0.5080), new Vector3d(0.6969, 0.5063, -0.5080), new Vector3d(0.7528, 0.4657, -0.4653), new Vector3d(-0.8827, -0.0658, -0.4653), new Vector3d(-0.8614, 0.0000, -0.5080), new Vector3d(-0.3353, 0.8192, -0.4653), new Vector3d(-0.2662, 0.8192, -0.5080), new Vector3d(-0.2102, 0.8598, -0.4653), new Vector3d(0.9173, 0.0406, 0.3961), new Vector3d(0.9173, -0.0406, 0.3961), new Vector3d(0.3221, -0.8598, 0.3961), new Vector3d(0.2448, -0.8850, 0.3961), new Vector3d(-0.7182, -0.5721, 0.3961), new Vector3d(-0.7660, -0.5063, 0.3961), new Vector3d(-0.7660, 0.5063, 0.3961), new Vector3d(-0.7182, 0.5721, 0.3961), new Vector3d(0.2448, 0.8850, 0.3961), new Vector3d(0.3221, 0.8598, 0.3961), new Vector3d(0.3353, -0.8192, 0.4653), new Vector3d(-0.6755, -0.5720, 0.4653), new Vector3d(-0.7528, 0.4657, 0.4653), new Vector3d(0.2102, 0.8598, 0.4653), new Vector3d(0.3353, 0.8192, 0.4653), new Vector3d(0.0691, 0.0000, 0.9976), new Vector3d(0.0214, -0.0658, 0.9976), new Vector3d(-0.0559, -0.0406, 0.9976), new Vector3d(-0.0559, 0.0406, 0.9976), new Vector3d(0.0214, 0.0658, 0.9976), new Vector3d(-0.0876, -0.4857, -0.8697), new Vector3d(0.2904, -0.7604, -0.5809), new Vector3d(0.2120, -0.7859, -0.5809), new Vector3d(0.4916, -0.2747, -0.8263), new Vector3d(0.4349, -0.2334, -0.8697), new Vector3d(0.4916, 0.2747, -0.8263), new Vector3d(-0.2146, -0.4445, -0.8697), new Vector3d(-0.2363, -0.5112, -0.8263), new Vector3d(-0.6819, -0.4445, -0.5809), new Vector3d(-0.4890, 0.0667, -0.8697), new Vector3d(-0.5592, 0.0667, -0.8263), new Vector3d(-0.6334, 0.5112, -0.5809), new Vector3d(0.3564, 0.3414, -0.8697), new Vector3d(-0.1093, 0.5525, -0.8263), new Vector3d(0.4132, 0.3827, -0.8263), new Vector3d(0.8831, -0.0412, -0.4673), new Vector3d(0.8831, 0.0412, -0.4673), new Vector3d(0.9724, 0.2334, -0.0000), new Vector3d(0.2337, -0.8526, -0.4673), new Vector3d(0.3121, -0.8271, -0.4673), new Vector3d(0.5225, -0.8526, 0.0000), new Vector3d(-0.7387, -0.4857, -0.4673), new Vector3d(-0.6902, -0.5525, -0.4673), new Vector3d(-0.6494, -0.7604, 0.0000), new Vector3d(-0.6902, 0.5525, -0.4673), new Vector3d(-0.7387, 0.4857, -0.4673), new Vector3d(-0.9239, 0.3827, 0.0000), new Vector3d(0.3121, 0.8271, -0.4673), new Vector3d(0.2337, 0.8526, -0.4673), new Vector3d(0.0785, 0.9969, 0.0000), new Vector3d(0.9239, -0.3827, -0.0000), new Vector3d(0.9373, -0.3414, 0.0702), new Vector3d(0.6144, -0.7859, 0.0702), new Vector3d(-0.0785, -0.9969, -0.0000), new Vector3d(-0.0351, -0.9969, 0.0702), new Vector3d(-0.5576, -0.8271, 0.0702), new Vector3d(-0.9724, -0.2334, 0.0000), new Vector3d(-0.9590, -0.2747, 0.0702), new Vector3d(-0.9590, 0.2747, 0.0702), new Vector3d(-0.5225, 0.8526, -0.0000), new Vector3d(-0.5576, 0.8271, 0.0702), new Vector3d(-0.0351, 0.9969, 0.0702), new Vector3d(0.9239, 0.3827, -0.0000), new Vector3d(0.6144, 0.7859, 0.0702), new Vector3d(0.9373, 0.3414, 0.0702), new Vector3d(0.6334, -0.5112, 0.5809), new Vector3d(0.6819, -0.4445, 0.5809), new Vector3d(0.4890, -0.0667, 0.8697), new Vector3d(-0.2904, -0.7604, 0.5809), new Vector3d(-0.2120, -0.7859, 0.5809), new Vector3d(0.0876, -0.4857, 0.8697), new Vector3d(-0.8129, 0.0412, 0.5809), new Vector3d(-0.8129, -0.0412, 0.5809), new Vector3d(-0.4349, -0.2334, 0.8697), new Vector3d(-0.2120, 0.7859, 0.5809), new Vector3d(-0.2904, 0.7604, 0.5809), new Vector3d(-0.3564, 0.3414, 0.8697), new Vector3d(0.6819, 0.4445, 0.5809), new Vector3d(0.6334, 0.5112, 0.5809), new Vector3d(0.4890, 0.0667, 0.8697), new Vector3d(0.3280, -0.7857, -0.5245), new Vector3d(-0.1439, -0.4429, -0.8850), new Vector3d(0.3767, -0.2737, -0.8850), new Vector3d(0.3767, 0.2737, -0.8850), new Vector3d(0.8486, -0.0691, -0.5245), new Vector3d(-0.6459, -0.5547, -0.5245), new Vector3d(-0.4657, 0.0000, -0.8850), new Vector3d(-0.7272, 0.4429, -0.5245), new Vector3d(-0.1439, 0.4429, -0.8850), new Vector3d(0.3280, 0.7857, -0.5245), new Vector3d(0.9375, 0.3428, -0.0588), new Vector3d(0.9375, -0.3428, -0.0588), new Vector3d(0.6158, -0.7857, -0.0588), new Vector3d(-0.0363, -0.9976, -0.0588), new Vector3d(-0.5570, -0.8284, -0.0588), new Vector3d(-0.9600, -0.2737, -0.0588), new Vector3d(-0.9600, 0.2737, -0.0588), new Vector3d(-0.5570, 0.8284, -0.0588), new Vector3d(-0.0363, 0.9976, -0.0588), new Vector3d(0.6158, 0.7857, -0.0588), new Vector3d(0.7272, -0.4429, 0.5245), new Vector3d(-0.1965, -0.8284, 0.5245), new Vector3d(-0.8486, -0.0691, 0.5245), new Vector3d(-0.3280, 0.7857, 0.5245), new Vector3d(0.7272, 0.4429, 0.5245), new Vector3d(0.5833, 0.0000, 0.8123), new Vector3d(0.1802, -0.5547, 0.8123), new Vector3d(-0.4719, -0.3428, 0.8123), new Vector3d(-0.4719, 0.3428, 0.8123), new Vector3d(0.1802, 0.5547, 0.8123), new Vector3d(0.1965, -0.8284, -0.5245), new Vector3d(-0.1802, -0.5547, -0.8123), new Vector3d(0.4719, -0.3428, -0.8123), new Vector3d(0.4719, 0.3428, -0.8123), new Vector3d(0.8486, 0.0691, -0.5245), new Vector3d(-0.7272, -0.4429, -0.5245), new Vector3d(-0.5833, 0.0000, -0.8123), new Vector3d(-0.6459, 0.5547, -0.5245), new Vector3d(-0.1802, 0.5547, -0.8123), new Vector3d(0.1965, 0.8284, -0.5245), new Vector3d(0.9600, 0.2737, 0.0588), new Vector3d(0.9600, -0.2737, 0.0588), new Vector3d(0.5570, -0.8284, 0.0588), new Vector3d(0.0363, -0.9976, 0.0588), new Vector3d(-0.6158, -0.7857, 0.0588), new Vector3d(-0.9375, -0.3428, 0.0588), new Vector3d(-0.9375, 0.3428, 0.0588), new Vector3d(-0.6158, 0.7857, 0.0588), new Vector3d(0.0363, 0.9976, 0.0588), new Vector3d(0.5570, 0.8284, 0.0588), new Vector3d(0.6459, -0.5547, 0.5245), new Vector3d(-0.3280, -0.7857, 0.5245), new Vector3d(-0.8486, 0.0691, 0.5245), new Vector3d(-0.1965, 0.8284, 0.5245), new Vector3d(0.6459, 0.5547, 0.5245), new Vector3d(0.4657, 0.0000, 0.8850), new Vector3d(0.1439, -0.4429, 0.8850), new Vector3d(-0.3767, -0.2737, 0.8850), new Vector3d(-0.3767, 0.2737, 0.8850), new Vector3d(0.1439, 0.4429, 0.8850), new Vector3d(0.3564, -0.3414, -0.8697), new Vector3d(0.4132, -0.3827, -0.8263), new Vector3d(-0.1093, -0.5525, -0.8263), new Vector3d(0.8129, -0.0412, -0.5809), new Vector3d(0.4349, 0.2334, -0.8697), new Vector3d(0.8129, 0.0412, -0.5809), new Vector3d(-0.4890, -0.0667, -0.8697), new Vector3d(-0.6334, -0.5112, -0.5809), new Vector3d(-0.5592, -0.0667, -0.8263), new Vector3d(-0.2146, 0.4445, -0.8697), new Vector3d(-0.6819, 0.4445, -0.5809), new Vector3d(-0.2363, 0.5112, -0.8263), new Vector3d(-0.0876, 0.4857, -0.8697), new Vector3d(0.2120, 0.7859, -0.5809), new Vector3d(0.2904, 0.7604, -0.5809), new Vector3d(0.9590, -0.2747, -0.0702), new Vector3d(0.9590, 0.2747, -0.0702), new Vector3d(0.9724, -0.2334, -0.0000), new Vector3d(0.0351, -0.9969, -0.0702), new Vector3d(0.5576, -0.8271, -0.0702), new Vector3d(0.0785, -0.9969, 0.0000), new Vector3d(-0.9373, -0.3414, -0.0702), new Vector3d(-0.6144, -0.7859, -0.0702), new Vector3d(-0.9239, -0.3827, 0.0000), new Vector3d(-0.6144, 0.7859, -0.0702), new Vector3d(-0.9373, 0.3414, -0.0702), new Vector3d(-0.6494, 0.7604, 0.0000), new Vector3d(0.5576, 0.8271, -0.0702), new Vector3d(0.0351, 0.9969, -0.0702), new Vector3d(0.5225, 0.8526, 0.0000), new Vector3d(0.6494, -0.7604, -0.0000), new Vector3d(0.7387, -0.4857, 0.4673), new Vector3d(0.6902, -0.5525, 0.4673), new Vector3d(-0.5225, -0.8526, -0.0000), new Vector3d(-0.2337, -0.8526, 0.4673), new Vector3d(-0.3121, -0.8271, 0.4673), new Vector3d(-0.9724, 0.2334, 0.0000), new Vector3d(-0.8831, -0.0412, 0.4673), new Vector3d(-0.8831, 0.0412, 0.4673), new Vector3d(-0.0785, 0.9969, -0.0000), new Vector3d(-0.3121, 0.8271, 0.4673), new Vector3d(-0.2337, 0.8526, 0.4673), new Vector3d(0.6494, 0.7604, -0.0000), new Vector3d(0.6902, 0.5525, 0.4673), new Vector3d(0.7387, 0.4857, 0.4673), new Vector3d(0.2363, -0.5112, 0.8263), new Vector3d(0.5592, -0.0667, 0.8263), new Vector3d(0.2146, -0.4445, 0.8697), new Vector3d(-0.4132, -0.3827, 0.8263), new Vector3d(0.1093, -0.5525, 0.8263), new Vector3d(-0.3564, -0.3414, 0.8697), new Vector3d(-0.4916, 0.2747, 0.8263), new Vector3d(-0.4916, -0.2747, 0.8263), new Vector3d(-0.4349, 0.2334, 0.8697), new Vector3d(0.1093, 0.5525, 0.8263), new Vector3d(-0.4132, 0.3827, 0.8263), new Vector3d(0.0876, 0.4857, 0.8697), new Vector3d(0.5592, 0.0667, 0.8263), new Vector3d(0.2363, 0.5112, 0.8263), new Vector3d(0.2146, 0.4445, 0.8697), new Vector3d(-0.0076, -0.2372, -0.9714), new Vector3d(0.2018, -0.2289, -0.9523), new Vector3d(-0.0287, -0.3038, -0.9523), new Vector3d(0.5422, -0.4762, -0.6923), new Vector3d(0.5393, -0.6346, -0.5536), new Vector3d(0.4785, -0.6723, -0.5648), new Vector3d(-0.1587, -0.7040, -0.6923), new Vector3d(0.0081, -0.8252, -0.5648), new Vector3d(-0.0633, -0.8304, -0.5536), new Vector3d(0.1964, -0.4709, -0.8600), new Vector3d(0.2869, -0.6192, -0.7309), new Vector3d(0.1179, -0.4964, -0.8600), new Vector3d(0.7702, -0.3168, -0.5536), new Vector3d(0.6205, -0.3685, -0.6923), new Vector3d(0.7873, -0.2473, -0.5648), new Vector3d(0.2801, -0.1212, -0.9523), new Vector3d(0.2233, -0.0806, -0.9714), new Vector3d(0.2801, 0.1212, -0.9523), new Vector3d(0.7873, 0.2473, -0.5648), new Vector3d(0.6205, 0.3685, -0.6923), new Vector3d(0.6593, 0.3973, -0.6383), new Vector3d(0.6207, -0.1228, -0.7743), new Vector3d(0.5086, 0.0413, -0.8600), new Vector3d(0.6776, -0.0815, -0.7309), new Vector3d(-0.1333, -0.1964, -0.9714), new Vector3d(-0.1553, -0.2627, -0.9523), new Vector3d(-0.2978, -0.0666, -0.9523), new Vector3d(-0.2854, -0.6628, -0.6923), new Vector3d(-0.4369, -0.7090, -0.5536), new Vector3d(-0.4915, -0.6628, -0.5648), new Vector3d(-0.7186, -0.0666, -0.6923), new Vector3d(-0.7823, -0.2627, -0.5648), new Vector3d(-0.8093, -0.1964, -0.5536), new Vector3d(-0.3872, -0.3323, -0.8600), new Vector3d(-0.5003, -0.4642, -0.7309), new Vector3d(-0.4357, -0.2655, -0.8600), new Vector3d(-0.2280, 0.0660, -0.9714), new Vector3d(-0.2978, 0.0666, -0.9523), new Vector3d(-0.1553, 0.2627, -0.9523), new Vector3d(-0.7186, 0.0666, -0.6923), new Vector3d(-0.8093, 0.1964, -0.5536), new Vector3d(-0.7823, 0.2627, -0.5648), new Vector3d(-0.2854, 0.6628, -0.6923), new Vector3d(-0.4915, 0.6628, -0.5648), new Vector3d(-0.4369, 0.7090, -0.5536), new Vector3d(-0.4357, 0.2655, -0.8600), new Vector3d(-0.5961, 0.3323, -0.7309), new Vector3d(-0.3872, 0.3323, -0.8600), new Vector3d(0.1456, 0.1874, -0.9714), new Vector3d(-0.0287, 0.3038, -0.9523), new Vector3d(0.2018, 0.2289, -0.9523), new Vector3d(-0.1587, 0.7040, -0.6923), new Vector3d(-0.1742, 0.7498, -0.6383), new Vector3d(0.0081, 0.8252, -0.5648), new Vector3d(0.5422, 0.4762, -0.6923), new Vector3d(0.4785, 0.6723, -0.5648), new Vector3d(0.5393, 0.6346, -0.5536), new Vector3d(0.1179, 0.4964, -0.8600), new Vector3d(0.1319, 0.6696, -0.7309), new Vector3d(0.1964, 0.4709, -0.8600), new Vector3d(0.8396, -0.3168, -0.4413), new Vector3d(0.8573, -0.2473, -0.4516), new Vector3d(0.8967, -0.3685, -0.2454), new Vector3d(0.8573, 0.2473, -0.4516), new Vector3d(0.8658, 0.3973, -0.3042), new Vector3d(0.8967, 0.3685, -0.2454), new Vector3d(0.9770, -0.1212, 0.1754), new Vector3d(0.9770, 0.1212, 0.1754), new Vector3d(0.9687, 0.0806, 0.2347), new Vector3d(0.9568, -0.0815, -0.2792), new Vector3d(0.9702, 0.1228, -0.2089), new Vector3d(0.9702, -0.1228, -0.2089), new Vector3d(-0.1104, -0.9462, -0.3042), new Vector3d(0.0297, -0.8917, -0.4516), new Vector3d(-0.0734, -0.9666, -0.2454), new Vector3d(0.5001, -0.7389, -0.4516), new Vector3d(0.5607, -0.7006, -0.4413), new Vector3d(0.6276, -0.7389, -0.2454), new Vector3d(0.1867, -0.9666, 0.1754), new Vector3d(0.4172, -0.8917, 0.1754), new Vector3d(0.3760, -0.8964, 0.2348), new Vector3d(0.2181, -0.9351, -0.2792), new Vector3d(0.4166, -0.8848, -0.2089), new Vector3d(0.1830, -0.9607, -0.2089), new Vector3d(-0.9340, -0.1874, -0.3042), new Vector3d(-0.8389, -0.3038, -0.4516), new Vector3d(-0.9420, -0.2289, -0.2454), new Vector3d(-0.5482, -0.7040, -0.4516), new Vector3d(-0.4931, -0.7498, -0.4413), new Vector3d(-0.5088, -0.8252, -0.2454), new Vector3d(-0.8617, -0.4762, 0.1754), new Vector3d(-0.7192, -0.6723, 0.1754), new Vector3d(-0.7363, -0.6346, 0.2348), new Vector3d(-0.8220, -0.4964, -0.2792), new Vector3d(-0.7127, -0.6696, -0.2089), new Vector3d(-0.8571, -0.4709, -0.2089), new Vector3d(-0.4669, 0.8304, -0.3042), new Vector3d(-0.5482, 0.7040, -0.4516), new Vector3d(-0.5088, 0.8252, -0.2454), new Vector3d(-0.8389, 0.3038, -0.4516), new Vector3d(-0.8655, 0.2372, -0.4413), new Vector3d(-0.9420, 0.2289, -0.2454), new Vector3d(-0.7192, 0.6723, 0.1754), new Vector3d(-0.8617, 0.4762, 0.1754), new Vector3d(-0.8311, 0.5042, 0.2348), new Vector3d(-0.7261, 0.6283, -0.2792), new Vector3d(-0.8571, 0.4709, -0.2089), new Vector3d(-0.7127, 0.6696, -0.2089), new Vector3d(0.5607, 0.7006, -0.4413), new Vector3d(0.5001, 0.7389, -0.4516), new Vector3d(0.6276, 0.7389, -0.2454), new Vector3d(0.0297, 0.8917, -0.4516), new Vector3d(-0.1104, 0.9462, -0.3042), new Vector3d(-0.0734, 0.9666, -0.2454), new Vector3d(0.4172, 0.8917, 0.1754), new Vector3d(0.1867, 0.9666, 0.1754), new Vector3d(0.2227, 0.9462, 0.2348), new Vector3d(0.3732, 0.8848, -0.2792), new Vector3d(0.1830, 0.9607, -0.2089), new Vector3d(0.4166, 0.8848, -0.2089), new Vector3d(0.8311, -0.5042, -0.2348), new Vector3d(0.8617, -0.4762, -0.1754), new Vector3d(0.7192, -0.6723, -0.1754), new Vector3d(0.9420, -0.2289, 0.2454), new Vector3d(0.8655, -0.2372, 0.4413), new Vector3d(0.8389, -0.3038, 0.4516), new Vector3d(0.5088, -0.8252, 0.2454), new Vector3d(0.5482, -0.7040, 0.4516), new Vector3d(0.4669, -0.8304, 0.3042), new Vector3d(0.8306, -0.5524, 0.0703), new Vector3d(0.8220, -0.4964, 0.2792), new Vector3d(0.7821, -0.6192, 0.0703), new Vector3d(-0.2227, -0.9462, -0.2348), new Vector3d(-0.1867, -0.9666, -0.1754), new Vector3d(-0.4172, -0.8917, -0.1754), new Vector3d(0.0734, -0.9666, 0.2454), new Vector3d(0.0418, -0.8964, 0.4413), new Vector3d(-0.0297, -0.8917, 0.4516), new Vector3d(-0.6276, -0.7389, 0.2454), new Vector3d(-0.5001, -0.7389, 0.4516), new Vector3d(-0.6454, -0.7006, 0.3042), new Vector3d(-0.2687, -0.9607, 0.0703), new Vector3d(-0.2181, -0.9351, 0.2792), new Vector3d(-0.3473, -0.9351, 0.0703), new Vector3d(-0.9687, -0.0806, -0.2347), new Vector3d(-0.9770, -0.1212, -0.1754), new Vector3d(-0.9770, 0.1212, -0.1754), new Vector3d(-0.8967, -0.3685, 0.2454), new Vector3d(-0.8396, -0.3168, 0.4413), new Vector3d(-0.8573, -0.2473, 0.4516), new Vector3d(-0.8967, 0.3685, 0.2454), new Vector3d(-0.8573, 0.2473, 0.4516), new Vector3d(-0.8658, 0.3973, 0.3042), new Vector3d(-0.9967, -0.0413, 0.0703), new Vector3d(-0.9568, -0.0815, 0.2792), new Vector3d(-0.9967, 0.0413, 0.0703), new Vector3d(-0.3760, 0.8964, -0.2348), new Vector3d(-0.4172, 0.8917, -0.1754), new Vector3d(-0.1867, 0.9666, -0.1754), new Vector3d(-0.6276, 0.7389, 0.2454), new Vector3d(-0.5607, 0.7006, 0.4413), new Vector3d(-0.5001, 0.7389, 0.4516), new Vector3d(0.0734, 0.9666, 0.2454), new Vector3d(-0.0297, 0.8917, 0.4516), new Vector3d(0.1104, 0.9462, 0.3042), new Vector3d(-0.3473, 0.9351, 0.0703), new Vector3d(-0.3732, 0.8848, 0.2792), new Vector3d(-0.2687, 0.9607, 0.0703), new Vector3d(0.8311, 0.5042, -0.2348), new Vector3d(0.7192, 0.6723, -0.1754), new Vector3d(0.8617, 0.4762, -0.1754), new Vector3d(0.5088, 0.8252, 0.2454), new Vector3d(0.4669, 0.8304, 0.3042), new Vector3d(0.5482, 0.7040, 0.4516), new Vector3d(0.9420, 0.2289, 0.2454), new Vector3d(0.8389, 0.3038, 0.4516), new Vector3d(0.8655, 0.2372, 0.4413), new Vector3d(0.7821, 0.6192, 0.0703), new Vector3d(0.7261, 0.6283, 0.2792), new Vector3d(0.8306, 0.5524, 0.0703), new Vector3d(0.2998, -0.7090, 0.6383), new Vector3d(0.4915, -0.6628, 0.5648), new Vector3d(0.2854, -0.6628, 0.6923), new Vector3d(0.7823, -0.2627, 0.5648), new Vector3d(0.8093, -0.1964, 0.5536), new Vector3d(0.7186, -0.0666, 0.6923), new Vector3d(0.1553, -0.2627, 0.9523), new Vector3d(0.2978, -0.0666, 0.9523), new Vector3d(0.2280, -0.0660, 0.9714), new Vector3d(0.5003, -0.4642, 0.7309), new Vector3d(0.5744, -0.2655, 0.7743), new Vector3d(0.4300, -0.4642, 0.7743), new Vector3d(-0.5816, -0.5042, 0.6383), new Vector3d(-0.4785, -0.6723, 0.5648), new Vector3d(-0.5422, -0.4762, 0.6923), new Vector3d(-0.0081, -0.8252, 0.5648), new Vector3d(0.0633, -0.8304, 0.5536), new Vector3d(0.1587, -0.7040, 0.6923), new Vector3d(-0.2018, -0.2289, 0.9523), new Vector3d(0.0287, -0.3038, 0.9523), new Vector3d(0.0076, -0.2372, 0.9714), new Vector3d(-0.2869, -0.6192, 0.7309), new Vector3d(-0.0750, -0.6283, 0.7743), new Vector3d(-0.3086, -0.5524, 0.7743), new Vector3d(-0.6593, 0.3973, 0.6383), new Vector3d(-0.7873, 0.2473, 0.5648), new Vector3d(-0.6205, 0.3685, 0.6923), new Vector3d(-0.7873, -0.2473, 0.5648), new Vector3d(-0.7702, -0.3168, 0.5536), new Vector3d(-0.6205, -0.3685, 0.6923), new Vector3d(-0.2801, 0.1212, 0.9523), new Vector3d(-0.2801, -0.1212, 0.9523), new Vector3d(-0.2233, -0.0806, 0.9714), new Vector3d(-0.6776, 0.0815, 0.7309), new Vector3d(-0.6207, -0.1228, 0.7743), new Vector3d(-0.6207, 0.1228, 0.7743), new Vector3d(0.1742, 0.7498, 0.6383), new Vector3d(-0.0081, 0.8252, 0.5648), new Vector3d(0.1587, 0.7040, 0.6923), new Vector3d(-0.4785, 0.6723, 0.5648), new Vector3d(-0.5393, 0.6346, 0.5536), new Vector3d(-0.5422, 0.4762, 0.6923), new Vector3d(0.0287, 0.3038, 0.9523), new Vector3d(-0.2018, 0.2289, 0.9523), new Vector3d(-0.1456, 0.1874, 0.9714), new Vector3d(-0.1319, 0.6696, 0.7309), new Vector3d(-0.3086, 0.5524, 0.7743), new Vector3d(-0.0750, 0.6283, 0.7743), new Vector3d(0.8093, 0.1964, 0.5536), new Vector3d(0.7823, 0.2627, 0.5648), new Vector3d(0.7186, 0.0666, 0.6923), new Vector3d(0.4915, 0.6628, 0.5648), new Vector3d(0.2998, 0.7090, 0.6383), new Vector3d(0.2854, 0.6628, 0.6923), new Vector3d(0.2978, 0.0666, 0.9523), new Vector3d(0.1553, 0.2627, 0.9523), new Vector3d(0.2280, 0.0660, 0.9714), new Vector3d(0.5961, 0.3323, 0.7309), new Vector3d(0.4300, 0.4642, 0.7743), new Vector3d(0.5744, 0.2655, 0.7743), new Vector3d(0.5698, -0.6563, -0.4946), new Vector3d(-0.0637, -0.1960, -0.9785), new Vector3d(0.1667, -0.1211, -0.9785), new Vector3d(0.1667, 0.1211, -0.9785), new Vector3d(0.8002, -0.3391, -0.4946), new Vector3d(-0.4481, -0.7447, -0.4946), new Vector3d(-0.2061, 0.0000, -0.9785), new Vector3d(-0.8467, 0.1960, -0.4946), new Vector3d(-0.0637, 0.1960, -0.9785), new Vector3d(0.5698, 0.6563, -0.4946), new Vector3d(0.8396, 0.4603, -0.2885), new Vector3d(0.8396, -0.4603, -0.2885), new Vector3d(0.6972, -0.6563, -0.2885), new Vector3d(-0.1783, -0.9407, -0.2885), new Vector3d(-0.4087, -0.8659, -0.2885), new Vector3d(-0.9498, -0.1211, -0.2885), new Vector3d(-0.9498, 0.1211, -0.2885), new Vector3d(-0.4087, 0.8659, -0.2885), new Vector3d(-0.1783, 0.9407, -0.2885), new Vector3d(0.6972, 0.6563, -0.2885), new Vector3d(0.8467, -0.1960, 0.4946), new Vector3d(0.0752, -0.8659, 0.4946), new Vector3d(-0.8002, -0.3391, 0.4946), new Vector3d(-0.5698, 0.6563, 0.4946), new Vector3d(0.8467, 0.1960, 0.4946), new Vector3d(0.7831, 0.0000, 0.6220), new Vector3d(0.2420, -0.7447, 0.6220), new Vector3d(-0.6335, -0.4603, 0.6220), new Vector3d(-0.6335, 0.4603, 0.6220), new Vector3d(0.2420, 0.7447, 0.6220), new Vector3d(-0.0752, -0.8659, -0.4946), new Vector3d(-0.2420, -0.7447, -0.6220), new Vector3d(0.6335, -0.4603, -0.6220), new Vector3d(0.6335, 0.4603, -0.6220), new Vector3d(0.8002, 0.3391, -0.4946), new Vector3d(-0.8467, -0.1960, -0.4946), new Vector3d(-0.7831, 0.0000, -0.6220), new Vector3d(-0.4481, 0.7447, -0.4946), new Vector3d(-0.2420, 0.7447, -0.6220), new Vector3d(-0.0752, 0.8659, -0.4946), new Vector3d(0.9498, 0.1211, 0.2885), new Vector3d(0.9498, -0.1211, 0.2885), new Vector3d(0.4087, -0.8659, 0.2885), new Vector3d(0.1783, -0.9407, 0.2885), new Vector3d(-0.6972, -0.6563, 0.2885), new Vector3d(-0.8396, -0.4603, 0.2885), new Vector3d(-0.8396, 0.4603, 0.2885), new Vector3d(-0.6972, 0.6563, 0.2885), new Vector3d(0.1783, 0.9407, 0.2885), new Vector3d(0.4087, 0.8659, 0.2885), new Vector3d(0.4481, -0.7447, 0.4946), new Vector3d(-0.5698, -0.6563, 0.4946), new Vector3d(-0.8002, 0.3391, 0.4946), new Vector3d(0.0752, 0.8659, 0.4946), new Vector3d(0.4481, 0.7447, 0.4946), new Vector3d(0.2061, 0.0000, 0.9785), new Vector3d(0.0637, -0.1960, 0.9785), new Vector3d(-0.1667, -0.1211, 0.9785), new Vector3d(-0.1667, 0.1211, 0.9785), new Vector3d(0.0637, 0.1960, 0.9785), new Vector3d(0.0632, -0.4483, -0.8917), new Vector3d(0.3401, -0.6494, -0.6801), new Vector3d(0.1066, -0.7253, -0.6801), new Vector3d(0.6148, -0.2012, -0.7626), new Vector3d(0.4458, -0.0785, -0.8917), new Vector3d(0.6148, 0.2012, -0.7626), new Vector3d(-0.3146, -0.3255, -0.8917), new Vector3d(-0.3791, -0.5241, -0.7626), new Vector3d(-0.6569, -0.3255, -0.6801), new Vector3d(-0.4068, 0.1986, -0.8917), new Vector3d(-0.6156, 0.1986, -0.7626), new Vector3d(-0.5126, 0.5241, -0.6801), new Vector3d(0.2124, 0.3998, -0.8917), new Vector3d(-0.0014, 0.6468, -0.7626), new Vector3d(0.3813, 0.5225, -0.7626), new Vector3d(0.9315, -0.1227, -0.3423), new Vector3d(0.9315, 0.1227, -0.3423), new Vector3d(0.9969, 0.0785, -0.0000), new Vector3d(0.1711, -0.9239, -0.3423), new Vector3d(0.4046, -0.8480, -0.3423), new Vector3d(0.3827, -0.9239, 0.0000), new Vector3d(-0.8258, -0.4483, -0.3423), new Vector3d(-0.6815, -0.6468, -0.3423), new Vector3d(-0.7604, -0.6494, 0.0000), new Vector3d(-0.6815, 0.6468, -0.3423), new Vector3d(-0.8258, 0.4483, -0.3423), new Vector3d(-0.8526, 0.5225, 0.0000), new Vector3d(0.4046, 0.8480, -0.3423), new Vector3d(0.1711, 0.9239, -0.3423), new Vector3d(0.2334, 0.9724, 0.0000), new Vector3d(0.8526, -0.5225, -0.0000), new Vector3d(0.8925, -0.3998, 0.2088), new Vector3d(0.6560, -0.7253, 0.2088), new Vector3d(-0.2334, -0.9724, -0.0000), new Vector3d(-0.1044, -0.9724, 0.2088), new Vector3d(-0.4871, -0.8480, 0.2088), new Vector3d(-0.9969, -0.0785, 0.0000), new Vector3d(-0.9570, -0.2012, 0.2088), new Vector3d(-0.9570, 0.2012, 0.2088), new Vector3d(-0.3827, 0.9239, -0.0000), new Vector3d(-0.4871, 0.8480, 0.2088), new Vector3d(-0.1044, 0.9724, 0.2088), new Vector3d(0.8526, 0.5225, -0.0000), new Vector3d(0.6560, 0.7253, 0.2088), new Vector3d(0.8925, 0.3998, 0.2088), new Vector3d(0.5126, -0.5241, 0.6801), new Vector3d(0.6569, -0.3255, 0.6801), new Vector3d(0.4068, -0.1986, 0.8917), new Vector3d(-0.3401, -0.6494, 0.6801), new Vector3d(-0.1066, -0.7253, 0.6801), new Vector3d(-0.0632, -0.4483, 0.8917), new Vector3d(-0.7227, 0.1227, 0.6801), new Vector3d(-0.7227, -0.1227, 0.6801), new Vector3d(-0.4458, -0.0785, 0.8917), new Vector3d(-0.1066, 0.7253, 0.6801), new Vector3d(-0.3401, 0.6494, 0.6801), new Vector3d(-0.2124, 0.3998, 0.8917), new Vector3d(0.6569, 0.3255, 0.6801), new Vector3d(0.5126, 0.5241, 0.6801), new Vector3d(0.4068, 0.1986, 0.8917), new Vector3d(0.4532, -0.7280, -0.5145), new Vector3d(-0.1048, -0.3225, -0.9407), new Vector3d(0.2744, -0.1993, -0.9407), new Vector3d(0.2744, 0.1993, -0.9407), new Vector3d(0.8324, -0.2061, -0.5144), new Vector3d(-0.5523, -0.6560, -0.5144), new Vector3d(-0.3391, 0.0000, -0.9407), new Vector3d(-0.7946, 0.3225, -0.5144), new Vector3d(-0.1048, 0.3225, -0.9407), new Vector3d(0.4532, 0.7280, -0.5145), new Vector3d(0.8972, 0.4054, -0.1753), new Vector3d(0.8972, -0.4054, -0.1753), new Vector3d(0.6628, -0.7280, -0.1753), new Vector3d(-0.1084, -0.9785, -0.1753), new Vector3d(-0.4875, -0.8553, -0.1753), new Vector3d(-0.9641, -0.1993, -0.1753), new Vector3d(-0.9641, 0.1993, -0.1753), new Vector3d(-0.4875, 0.8553, -0.1753), new Vector3d(-0.1084, 0.9785, -0.1753), new Vector3d(0.6628, 0.7280, -0.1753), new Vector3d(0.7946, -0.3225, 0.5144), new Vector3d(-0.0612, -0.8553, 0.5145), new Vector3d(-0.8324, -0.2061, 0.5144), new Vector3d(-0.4532, 0.7280, 0.5145), new Vector3d(0.7946, 0.3225, 0.5144), new Vector3d(0.6898, 0.0000, 0.7240), new Vector3d(0.2131, -0.6560, 0.7240), new Vector3d(-0.5580, -0.4054, 0.7240), new Vector3d(-0.5580, 0.4054, 0.7240), new Vector3d(0.2131, 0.6560, 0.7240), new Vector3d(0.0612, -0.8553, -0.5145), new Vector3d(-0.2131, -0.6560, -0.7240), new Vector3d(0.5580, -0.4054, -0.7240), new Vector3d(0.5580, 0.4054, -0.7240), new Vector3d(0.8324, 0.2061, -0.5144), new Vector3d(-0.7946, -0.3225, -0.5144), new Vector3d(-0.6898, 0.0000, -0.7240), new Vector3d(-0.5523, 0.6560, -0.5144), new Vector3d(-0.2131, 0.6560, -0.7240), new Vector3d(0.0612, 0.8553, -0.5145), new Vector3d(0.9641, 0.1993, 0.1753), new Vector3d(0.9641, -0.1993, 0.1753), new Vector3d(0.4875, -0.8553, 0.1753), new Vector3d(0.1084, -0.9785, 0.1753), new Vector3d(-0.6628, -0.7280, 0.1753), new Vector3d(-0.8972, -0.4054, 0.1753), new Vector3d(-0.8972, 0.4054, 0.1753), new Vector3d(-0.6628, 0.7280, 0.1753), new Vector3d(0.1084, 0.9785, 0.1753), new Vector3d(0.4875, 0.8553, 0.1753), new Vector3d(0.5523, -0.6560, 0.5144), new Vector3d(-0.4532, -0.7280, 0.5145), new Vector3d(-0.8324, 0.2061, 0.5144), new Vector3d(-0.0612, 0.8553, 0.5145), new Vector3d(0.5523, 0.6560, 0.5144), new Vector3d(0.3391, 0.0000, 0.9407), new Vector3d(0.1048, -0.3225, 0.9407), new Vector3d(-0.2744, -0.1993, 0.9407), new Vector3d(-0.2744, 0.1993, 0.9407), new Vector3d(0.1048, 0.3225, 0.9407), new Vector3d(0.2124, -0.3998, -0.8917), new Vector3d(0.3813, -0.5225, -0.7626), new Vector3d(-0.0014, -0.6468, -0.7626), new Vector3d(0.7227, -0.1227, -0.6801), new Vector3d(0.4458, 0.0785, -0.8917), new Vector3d(0.7227, 0.1227, -0.6801), new Vector3d(-0.4068, -0.1986, -0.8917), new Vector3d(-0.5126, -0.5241, -0.6801), new Vector3d(-0.6156, -0.1986, -0.7626), new Vector3d(-0.3146, 0.3255, -0.8917), new Vector3d(-0.6569, 0.3255, -0.6801), new Vector3d(-0.3791, 0.5241, -0.7626), new Vector3d(0.0632, 0.4483, -0.8917), new Vector3d(0.1066, 0.7253, -0.6801), new Vector3d(0.3401, 0.6494, -0.6801), new Vector3d(0.9570, -0.2012, -0.2088), new Vector3d(0.9570, 0.2012, -0.2088), new Vector3d(0.9969, -0.0785, -0.0000), new Vector3d(0.1044, -0.9724, -0.2088), new Vector3d(0.4871, -0.8480, -0.2088), new Vector3d(0.2334, -0.9724, 0.0000), new Vector3d(-0.8925, -0.3998, -0.2088), new Vector3d(-0.6560, -0.7253, -0.2088), new Vector3d(-0.8526, -0.5225, 0.0000), new Vector3d(-0.6560, 0.7253, -0.2088), new Vector3d(-0.8925, 0.3998, -0.2088), new Vector3d(-0.7604, 0.6494, 0.0000), new Vector3d(0.4871, 0.8480, -0.2088), new Vector3d(0.1044, 0.9724, -0.2088), new Vector3d(0.3827, 0.9239, 0.0000), new Vector3d(0.7604, -0.6494, -0.0000), new Vector3d(0.8258, -0.4483, 0.3423), new Vector3d(0.6815, -0.6468, 0.3423), new Vector3d(-0.3827, -0.9239, -0.0000), new Vector3d(-0.1711, -0.9239, 0.3423), new Vector3d(-0.4046, -0.8480, 0.3423), new Vector3d(-0.9969, 0.0785, 0.0000), new Vector3d(-0.9315, -0.1227, 0.3423), new Vector3d(-0.9315, 0.1227, 0.3423), new Vector3d(-0.2334, 0.9724, -0.0000), new Vector3d(-0.4046, 0.8480, 0.3423), new Vector3d(-0.1711, 0.9239, 0.3423), new Vector3d(0.7604, 0.6494, -0.0000), new Vector3d(0.6815, 0.6468, 0.3423), new Vector3d(0.8258, 0.4483, 0.3423), new Vector3d(0.3791, -0.5241, 0.7626), new Vector3d(0.6156, -0.1986, 0.7626), new Vector3d(0.3146, -0.3255, 0.8917), new Vector3d(-0.3813, -0.5225, 0.7626), new Vector3d(0.0014, -0.6468, 0.7626), new Vector3d(-0.2124, -0.3998, 0.8917), new Vector3d(-0.6148, 0.2012, 0.7626), new Vector3d(-0.6148, -0.2012, 0.7626), new Vector3d(-0.4458, 0.0785, 0.8917), new Vector3d(0.0014, 0.6468, 0.7626), new Vector3d(-0.3813, 0.5225, 0.7626), new Vector3d(-0.0632, 0.4483, 0.8917), new Vector3d(0.6156, 0.1986, 0.7626), new Vector3d(0.3791, 0.5241, 0.7626), new Vector3d(0.3146, 0.3255, 0.8917), new Vector3d(0.1456, -0.1874, -0.9714), new Vector3d(0.1603, -0.3616, -0.9185), new Vector3d(0.0829, -0.3867, -0.9185), new Vector3d(0.4242, -0.5534, -0.7168), new Vector3d(0.5816, -0.5042, -0.6383), new Vector3d(0.4028, -0.6192, -0.6740), new Vector3d(-0.0179, -0.6970, -0.7168), new Vector3d(0.0381, -0.7377, -0.6740), new Vector3d(-0.1742, -0.7498, -0.6383), new Vector3d(0.3086, -0.5524, -0.7743), new Vector3d(0.1319, -0.6696, -0.7309), new Vector3d(0.0750, -0.6283, -0.7743), new Vector3d(0.6593, -0.3973, -0.6383), new Vector3d(0.6574, -0.2325, -0.7168), new Vector3d(0.7134, -0.1918, -0.6740), new Vector3d(0.3934, -0.0407, -0.9185), new Vector3d(0.2233, 0.0806, -0.9714), new Vector3d(0.3934, 0.0407, -0.9185), new Vector3d(0.7134, 0.1918, -0.6740), new Vector3d(0.6574, 0.2325, -0.7168), new Vector3d(0.7702, 0.3168, -0.5536), new Vector3d(0.5086, -0.0413, -0.8600), new Vector3d(0.6207, 0.1228, -0.7743), new Vector3d(0.6776, 0.0815, -0.7309), new Vector3d(-0.2280, -0.0660, -0.9714), new Vector3d(-0.2944, -0.2642, -0.9185), new Vector3d(-0.3422, -0.1983, -0.9185), new Vector3d(-0.3952, -0.5745, -0.7168), new Vector3d(-0.2998, -0.7090, -0.6383), new Vector3d(-0.4644, -0.5745, -0.6740), new Vector3d(-0.6685, -0.1983, -0.7168), new Vector3d(-0.6898, -0.2642, -0.6740), new Vector3d(-0.7669, -0.0660, -0.6383), new Vector3d(-0.4300, -0.4642, -0.7743), new Vector3d(-0.5961, -0.3323, -0.7309), new Vector3d(-0.5744, -0.2655, -0.7743), new Vector3d(-0.1333, 0.1964, -0.9714), new Vector3d(-0.3422, 0.1983, -0.9185), new Vector3d(-0.2944, 0.2642, -0.9185), new Vector3d(-0.6685, 0.1983, -0.7168), new Vector3d(-0.7669, 0.0660, -0.6383), new Vector3d(-0.6898, 0.2642, -0.6740), new Vector3d(-0.3952, 0.5745, -0.7168), new Vector3d(-0.4644, 0.5745, -0.6740), new Vector3d(-0.2998, 0.7090, -0.6383), new Vector3d(-0.5744, 0.2655, -0.7743), new Vector3d(-0.5003, 0.4642, -0.7309), new Vector3d(-0.4300, 0.4642, -0.7743), new Vector3d(-0.0076, 0.2372, -0.9714), new Vector3d(0.0829, 0.3867, -0.9185), new Vector3d(0.1603, 0.3616, -0.9185), new Vector3d(-0.0179, 0.6970, -0.7168), new Vector3d(-0.0633, 0.8304, -0.5536), new Vector3d(0.0381, 0.7377, -0.6740), new Vector3d(0.4242, 0.5534, -0.7168), new Vector3d(0.4028, 0.6192, -0.6740), new Vector3d(0.5816, 0.5042, -0.6383), new Vector3d(0.0750, 0.6283, -0.7743), new Vector3d(0.2869, 0.6192, -0.7309), new Vector3d(0.3086, 0.5524, -0.7743), new Vector3d(0.8658, -0.3973, -0.3042), new Vector3d(0.9219, -0.1918, -0.3366), new Vector3d(0.9351, -0.2325, -0.2674), new Vector3d(0.9219, 0.1918, -0.3366), new Vector3d(0.8396, 0.3168, -0.4413), new Vector3d(0.9351, 0.2325, -0.2674), new Vector3d(0.9974, -0.0407, 0.0589), new Vector3d(0.9974, 0.0407, 0.0589), new Vector3d(0.9687, -0.0806, 0.2347), new Vector3d(0.9568, 0.0815, -0.2792), new Vector3d(0.9967, 0.0413, -0.0703), new Vector3d(0.9967, -0.0413, -0.0703), new Vector3d(-0.0418, -0.8964, -0.4413), new Vector3d(0.1025, -0.9360, -0.3366), new Vector3d(0.0679, -0.9612, -0.2674), new Vector3d(0.4673, -0.8175, -0.3366), new Vector3d(0.6454, -0.7006, -0.3042), new Vector3d(0.5100, -0.8175, -0.2674), new Vector3d(0.2695, -0.9612, 0.0589), new Vector3d(0.3469, -0.9360, 0.0589), new Vector3d(0.2227, -0.9462, 0.2348), new Vector3d(0.3732, -0.8848, -0.2792), new Vector3d(0.3473, -0.9351, -0.0703), new Vector3d(0.2687, -0.9607, -0.0703), new Vector3d(-0.8655, -0.2372, -0.4413), new Vector3d(-0.8586, -0.3867, -0.3366), new Vector3d(-0.8932, -0.3616, -0.2674), new Vector3d(-0.6331, -0.6970, -0.3366), new Vector3d(-0.4669, -0.8304, -0.3042), new Vector3d(-0.6199, -0.7377, -0.2674), new Vector3d(-0.8309, -0.5534, 0.0589), new Vector3d(-0.7830, -0.6192, 0.0589), new Vector3d(-0.8311, -0.5042, 0.2348), new Vector3d(-0.7261, -0.6283, -0.2792), new Vector3d(-0.7821, -0.6192, -0.0703), new Vector3d(-0.8306, -0.5524, -0.0703), new Vector3d(-0.4931, 0.7498, -0.4413), new Vector3d(-0.6331, 0.6970, -0.3366), new Vector3d(-0.6199, 0.7377, -0.2674), new Vector3d(-0.8586, 0.3867, -0.3366), new Vector3d(-0.9340, 0.1874, -0.3042), new Vector3d(-0.8932, 0.3616, -0.2674), new Vector3d(-0.7830, 0.6192, 0.0589), new Vector3d(-0.8309, 0.5534, 0.0589), new Vector3d(-0.7363, 0.6346, 0.2348), new Vector3d(-0.8220, 0.4964, -0.2792), new Vector3d(-0.8306, 0.5524, -0.0703), new Vector3d(-0.7821, 0.6192, -0.0703), new Vector3d(0.6454, 0.7006, -0.3042), new Vector3d(0.4673, 0.8175, -0.3366), new Vector3d(0.5100, 0.8175, -0.2674), new Vector3d(0.1025, 0.9360, -0.3366), new Vector3d(-0.0418, 0.8964, -0.4413), new Vector3d(0.0679, 0.9612, -0.2674), new Vector3d(0.3469, 0.9360, 0.0589), new Vector3d(0.2695, 0.9612, 0.0589), new Vector3d(0.3760, 0.8964, 0.2348), new Vector3d(0.2181, 0.9351, -0.2792), new Vector3d(0.2687, 0.9607, -0.0703), new Vector3d(0.3473, 0.9351, -0.0703), new Vector3d(0.7363, -0.6346, -0.2348), new Vector3d(0.8309, -0.5534, -0.0589), new Vector3d(0.7830, -0.6192, -0.0589), new Vector3d(0.8932, -0.3616, 0.2674), new Vector3d(0.9340, -0.1874, 0.3042), new Vector3d(0.8586, -0.3867, 0.3366), new Vector3d(0.6199, -0.7377, 0.2674), new Vector3d(0.6331, -0.6970, 0.3366), new Vector3d(0.4931, -0.7498, 0.4413), new Vector3d(0.8571, -0.4709, 0.2089), new Vector3d(0.7261, -0.6283, 0.2792), new Vector3d(0.7127, -0.6696, 0.2089), new Vector3d(-0.3760, -0.8964, -0.2348), new Vector3d(-0.2695, -0.9612, -0.0589), new Vector3d(-0.3469, -0.9360, -0.0589), new Vector3d(-0.0679, -0.9612, 0.2674), new Vector3d(0.1104, -0.9462, 0.3042), new Vector3d(-0.1025, -0.9360, 0.3366), new Vector3d(-0.5100, -0.8175, 0.2674), new Vector3d(-0.4673, -0.8175, 0.3366), new Vector3d(-0.5607, -0.7006, 0.4413), new Vector3d(-0.1830, -0.9607, 0.2089), new Vector3d(-0.3732, -0.8848, 0.2792), new Vector3d(-0.4166, -0.8848, 0.2089), new Vector3d(-0.9687, 0.0806, -0.2347), new Vector3d(-0.9974, -0.0407, -0.0589), new Vector3d(-0.9974, 0.0407, -0.0589), new Vector3d(-0.9351, -0.2325, 0.2674), new Vector3d(-0.8658, -0.3973, 0.3042), new Vector3d(-0.9219, -0.1918, 0.3366), new Vector3d(-0.9351, 0.2325, 0.2674), new Vector3d(-0.9219, 0.1918, 0.3366), new Vector3d(-0.8396, 0.3168, 0.4413), new Vector3d(-0.9702, -0.1228, 0.2089), new Vector3d(-0.9568, 0.0815, 0.2792), new Vector3d(-0.9702, 0.1228, 0.2089), new Vector3d(-0.2227, 0.9462, -0.2348), new Vector3d(-0.3469, 0.9360, -0.0589), new Vector3d(-0.2695, 0.9612, -0.0589), new Vector3d(-0.5100, 0.8175, 0.2674), new Vector3d(-0.6454, 0.7006, 0.3042), new Vector3d(-0.4673, 0.8175, 0.3366), new Vector3d(-0.0679, 0.9612, 0.2674), new Vector3d(-0.1025, 0.9360, 0.3366), new Vector3d(0.0418, 0.8964, 0.4413), new Vector3d(-0.4166, 0.8848, 0.2089), new Vector3d(-0.2181, 0.9351, 0.2792), new Vector3d(-0.1830, 0.9607, 0.2089), new Vector3d(0.7363, 0.6346, -0.2348), new Vector3d(0.7830, 0.6192, -0.0589), new Vector3d(0.8309, 0.5534, -0.0589), new Vector3d(0.6199, 0.7377, 0.2674), new Vector3d(0.4931, 0.7498, 0.4413), new Vector3d(0.6331, 0.6970, 0.3366), new Vector3d(0.8932, 0.3616, 0.2674), new Vector3d(0.8586, 0.3867, 0.3366), new Vector3d(0.9340, 0.1874, 0.3042), new Vector3d(0.7127, 0.6696, 0.2089), new Vector3d(0.8220, 0.4964, 0.2792), new Vector3d(0.8571, 0.4709, 0.2089), new Vector3d(0.4369, -0.7090, 0.5536), new Vector3d(0.4644, -0.5745, 0.6740), new Vector3d(0.3952, -0.5745, 0.7168), new Vector3d(0.6898, -0.2642, 0.6740), new Vector3d(0.7669, -0.0660, 0.6383), new Vector3d(0.6685, -0.1983, 0.7168), new Vector3d(0.2944, -0.2642, 0.9185), new Vector3d(0.3422, -0.1983, 0.9185), new Vector3d(0.1333, -0.1964, 0.9714), new Vector3d(0.5961, -0.3323, 0.7309), new Vector3d(0.4357, -0.2655, 0.8600), new Vector3d(0.3872, -0.3323, 0.8600), new Vector3d(-0.5393, -0.6346, 0.5536), new Vector3d(-0.4028, -0.6192, 0.6740), new Vector3d(-0.4242, -0.5534, 0.7168), new Vector3d(-0.0381, -0.7377, 0.6740), new Vector3d(0.1742, -0.7498, 0.6383), new Vector3d(0.0179, -0.6970, 0.7168), new Vector3d(-0.1603, -0.3616, 0.9185), new Vector3d(-0.0829, -0.3867, 0.9185), new Vector3d(-0.1456, -0.1874, 0.9714), new Vector3d(-0.1319, -0.6696, 0.7309), new Vector3d(-0.1179, -0.4964, 0.8600), new Vector3d(-0.1964, -0.4709, 0.8600), new Vector3d(-0.7702, 0.3168, 0.5536), new Vector3d(-0.7134, 0.1918, 0.6740), new Vector3d(-0.6574, 0.2325, 0.7168), new Vector3d(-0.7134, -0.1918, 0.6740), new Vector3d(-0.6593, -0.3973, 0.6383), new Vector3d(-0.6574, -0.2325, 0.7168), new Vector3d(-0.3934, 0.0407, 0.9185), new Vector3d(-0.3934, -0.0407, 0.9185), new Vector3d(-0.2233, 0.0806, 0.9714), new Vector3d(-0.6776, -0.0815, 0.7309), new Vector3d(-0.5086, -0.0413, 0.8600), new Vector3d(-0.5086, 0.0413, 0.8600), new Vector3d(0.0633, 0.8304, 0.5536), new Vector3d(-0.0381, 0.7377, 0.6740), new Vector3d(0.0179, 0.6970, 0.7168), new Vector3d(-0.4028, 0.6192, 0.6740), new Vector3d(-0.5816, 0.5042, 0.6383), new Vector3d(-0.4242, 0.5534, 0.7168), new Vector3d(-0.0829, 0.3867, 0.9185), new Vector3d(-0.1603, 0.3616, 0.9185), new Vector3d(0.0076, 0.2372, 0.9714), new Vector3d(-0.2869, 0.6192, 0.7309), new Vector3d(-0.1964, 0.4709, 0.8600), new Vector3d(-0.1179, 0.4964, 0.8600), new Vector3d(0.7669, 0.0660, 0.6383), new Vector3d(0.6898, 0.2642, 0.6740), new Vector3d(0.6685, 0.1983, 0.7168), new Vector3d(0.4644, 0.5745, 0.6740), new Vector3d(0.4369, 0.7090, 0.5536), new Vector3d(0.3952, 0.5745, 0.7168), new Vector3d(0.3422, 0.1983, 0.9185), new Vector3d(0.2944, 0.2642, 0.9185), new Vector3d(0.1333, 0.1964, 0.9714), new Vector3d(0.5003, 0.4642, 0.7309), new Vector3d(0.3872, 0.3323, 0.8600), new Vector3d(0.4357, 0.2655, 0.8600), new Vector3d(0.0346, -0.1065, -0.9937), new Vector3d(0.0906, -0.1474, -0.9849), new Vector3d(0.0133, -0.1725, -0.9849), new Vector3d(0.2547, -0.2674, -0.9293), new Vector3d(0.3066, -0.3052, -0.9016), new Vector3d(0.2340, -0.3347, -0.9128), new Vector3d(-0.0489, -0.3660, -0.9293), new Vector3d(0.0075, -0.4083, -0.9128), new Vector3d(-0.0686, -0.4271, -0.9016), new Vector3d(0.1257, -0.2552, -0.9587), new Vector3d(0.1047, -0.3221, -0.9409), new Vector3d(0.0483, -0.2803, -0.9587), new Vector3d(0.4585, -0.4156, -0.7855), new Vector3d(0.5019, -0.4470, -0.7405), new Vector3d(0.4425, -0.4862, -0.7535), new Vector3d(0.6160, -0.5291, -0.5836), new Vector3d(0.6476, -0.5519, -0.5254), new Vector3d(0.5947, -0.5949, -0.5408), new Vector3d(0.3799, -0.6789, -0.6283), new Vector3d(0.4180, -0.7052, -0.5727), new Vector3d(0.3548, -0.7348, -0.5781), new Vector3d(0.5247, -0.5451, -0.6538), new Vector3d(0.5034, -0.6109, -0.6111), new Vector3d(0.4647, -0.5837, -0.6659), new Vector3d(-0.1267, -0.6057, -0.7855), new Vector3d(-0.0722, -0.6535, -0.7535), new Vector3d(-0.1433, -0.6566, -0.7405), new Vector3d(0.0917, -0.7726, -0.6283), new Vector3d(0.1449, -0.8030, -0.5781), new Vector3d(0.0764, -0.8162, -0.5727), new Vector3d(-0.1874, -0.7902, -0.5836), new Vector3d(-0.1314, -0.8308, -0.5408), new Vector3d(-0.1995, -0.8271, -0.5254), new Vector3d(-0.0328, -0.7453, -0.6659), new Vector3d(-0.0481, -0.7901, -0.6111), new Vector3d(-0.1041, -0.7494, -0.6538), new Vector3d(0.3430, -0.4143, -0.8430), new Vector3d(0.3269, -0.4852, -0.8110), new Vector3d(0.2702, -0.4440, -0.8544), new Vector3d(0.2640, -0.6790, -0.6851), new Vector3d(0.2388, -0.7350, -0.6346), new Vector3d(0.1855, -0.7045, -0.6851), new Vector3d(0.0424, -0.5180, -0.8544), new Vector3d(0.0207, -0.5847, -0.8110), new Vector3d(-0.0340, -0.5368, -0.8430), new Vector3d(0.2325, -0.5819, -0.7793), new Vector3d(0.1540, -0.6075, -0.7793), new Vector3d(0.1757, -0.5406, -0.8227), new Vector3d(0.7250, -0.4454, -0.5254), new Vector3d(0.6936, -0.4224, -0.5836), new Vector3d(0.7495, -0.3817, -0.5408), new Vector3d(0.5802, -0.3392, -0.7405), new Vector3d(0.5369, -0.3077, -0.7855), new Vector3d(0.5992, -0.2706, -0.7535), new Vector3d(0.7999, -0.1796, -0.5727), new Vector3d(0.7631, -0.1515, -0.6283), new Vector3d(0.8085, -0.1104, -0.5781), new Vector3d(0.6806, -0.3306, -0.6538), new Vector3d(0.6987, -0.2615, -0.6659), new Vector3d(0.7366, -0.2899, -0.6111), new Vector3d(0.3850, -0.1973, -0.9016), new Vector3d(0.3330, -0.1596, -0.9293), new Vector3d(0.3906, -0.1191, -0.9128), new Vector3d(0.1682, -0.0406, -0.9849), new Vector3d(0.1120, 0.0000, -0.9937), new Vector3d(0.1682, 0.0406, -0.9849), new Vector3d(0.3906, 0.1191, -0.9128), new Vector3d(0.3330, 0.1596, -0.9293), new Vector3d(0.3850, 0.1973, -0.9016), new Vector3d(0.2815, -0.0407, -0.9587), new Vector3d(0.2815, 0.0407, -0.9587), new Vector3d(0.3387, 0.0000, -0.9409), new Vector3d(0.8085, 0.1104, -0.5781), new Vector3d(0.7631, 0.1515, -0.6283), new Vector3d(0.7999, 0.1796, -0.5727), new Vector3d(0.5992, 0.2706, -0.7535), new Vector3d(0.5369, 0.3077, -0.7855), new Vector3d(0.5802, 0.3392, -0.7405), new Vector3d(0.7495, 0.3817, -0.5408), new Vector3d(0.6936, 0.4224, -0.5836), new Vector3d(0.7250, 0.4454, -0.5254), new Vector3d(0.6987, 0.2615, -0.6659), new Vector3d(0.6806, 0.3306, -0.6538), new Vector3d(0.7366, 0.2899, -0.6111), new Vector3d(0.5000, -0.1982, -0.8430), new Vector3d(0.5057, -0.1198, -0.8543), new Vector3d(0.5625, -0.1610, -0.8110), new Vector3d(0.5057, 0.1198, -0.8543), new Vector3d(0.5000, 0.1982, -0.8430), new Vector3d(0.5625, 0.1610, -0.8110), new Vector3d(0.7273, -0.0413, -0.6851), new Vector3d(0.7273, 0.0413, -0.6851), new Vector3d(0.7728, 0.0000, -0.6346), new Vector3d(0.5685, 0.0000, -0.8227), new Vector3d(0.6253, 0.0413, -0.7793), new Vector3d(0.6253, -0.0413, -0.7793), new Vector3d(-0.0906, -0.0658, -0.9937), new Vector3d(-0.1122, -0.1317, -0.9849), new Vector3d(-0.1600, -0.0660, -0.9849), new Vector3d(-0.1756, -0.3249, -0.9293), new Vector3d(-0.1955, -0.3859, -0.9016), new Vector3d(-0.2460, -0.3259, -0.9128), new Vector3d(-0.3632, -0.0666, -0.9293), new Vector3d(-0.3860, -0.1333, -0.9128), new Vector3d(-0.4274, -0.0667, -0.9016), new Vector3d(-0.2039, -0.1984, -0.9587), new Vector3d(-0.2740, -0.1991, -0.9409), new Vector3d(-0.2517, -0.1326, -0.9587), new Vector3d(-0.2536, -0.5645, -0.7855), new Vector3d(-0.2700, -0.6155, -0.7405), new Vector3d(-0.3257, -0.5711, -0.7535), new Vector3d(-0.3129, -0.7494, -0.5836), new Vector3d(-0.3247, -0.7864, -0.5254), new Vector3d(-0.3820, -0.7494, -0.5408), new Vector3d(-0.5283, -0.5711, -0.6283), new Vector3d(-0.5416, -0.6155, -0.5727), new Vector3d(-0.5892, -0.5645, -0.5781), new Vector3d(-0.3563, -0.6675, -0.6538), new Vector3d(-0.4255, -0.6675, -0.6111), new Vector3d(-0.4115, -0.6223, -0.6659), new Vector3d(-0.6152, -0.0667, -0.7855), new Vector3d(-0.6438, -0.1333, -0.7535), new Vector3d(-0.6688, -0.0666, -0.7405), new Vector3d(-0.7064, -0.3259, -0.6283), new Vector3d(-0.7189, -0.3859, -0.5781), new Vector3d(-0.7527, -0.3249, -0.5727), new Vector3d(-0.8094, -0.0660, -0.5836), new Vector3d(-0.8308, -0.1317, -0.5408), new Vector3d(-0.8483, -0.0658, -0.5254), new Vector3d(-0.7190, -0.1991, -0.6659), new Vector3d(-0.7663, -0.1984, -0.6111), new Vector3d(-0.7449, -0.1326, -0.6538), new Vector3d(-0.2880, -0.4543, -0.8430), new Vector3d(-0.3604, -0.4609, -0.8110), new Vector3d(-0.3387, -0.3941, -0.8543), new Vector3d(-0.5642, -0.4609, -0.6851), new Vector3d(-0.6252, -0.4543, -0.6346), new Vector3d(-0.6127, -0.3941, -0.6851), new Vector3d(-0.4795, -0.2004, -0.8543), new Vector3d(-0.5497, -0.2004, -0.8110), new Vector3d(-0.5210, -0.1335, -0.8430), new Vector3d(-0.4816, -0.4010, -0.7793), new Vector3d(-0.5302, -0.3341, -0.7793), new Vector3d(-0.4599, -0.3341, -0.8227), new Vector3d(-0.0906, 0.0658, -0.9937), new Vector3d(-0.1600, 0.0660, -0.9849), new Vector3d(-0.1122, 0.1317, -0.9849), new Vector3d(-0.3632, 0.0666, -0.9293), new Vector3d(-0.4274, 0.0667, -0.9016), new Vector3d(-0.3860, 0.1333, -0.9128), new Vector3d(-0.1756, 0.3249, -0.9293), new Vector3d(-0.2460, 0.3259, -0.9128), new Vector3d(-0.1955, 0.3859, -0.9016), new Vector3d(-0.2517, 0.1326, -0.9587), new Vector3d(-0.2740, 0.1991, -0.9409), new Vector3d(-0.2039, 0.1984, -0.9587), new Vector3d(-0.6152, 0.0667, -0.7855), new Vector3d(-0.6688, 0.0666, -0.7405), new Vector3d(-0.6438, 0.1333, -0.7535), new Vector3d(-0.8094, 0.0660, -0.5836), new Vector3d(-0.8483, 0.0658, -0.5254), new Vector3d(-0.8308, 0.1317, -0.5408), new Vector3d(-0.7064, 0.3259, -0.6283), new Vector3d(-0.7527, 0.3249, -0.5727), new Vector3d(-0.7189, 0.3859, -0.5781), new Vector3d(-0.7449, 0.1326, -0.6538), new Vector3d(-0.7663, 0.1984, -0.6111), new Vector3d(-0.7190, 0.1991, -0.6659), new Vector3d(-0.2536, 0.5645, -0.7855), new Vector3d(-0.3257, 0.5711, -0.7535), new Vector3d(-0.2700, 0.6155, -0.7405), new Vector3d(-0.5283, 0.5711, -0.6283), new Vector3d(-0.5892, 0.5645, -0.5781), new Vector3d(-0.5416, 0.6155, -0.5727), new Vector3d(-0.3129, 0.7494, -0.5836), new Vector3d(-0.3820, 0.7494, -0.5408), new Vector3d(-0.3247, 0.7864, -0.5254), new Vector3d(-0.4115, 0.6223, -0.6659), new Vector3d(-0.4255, 0.6675, -0.6111), new Vector3d(-0.3563, 0.6675, -0.6538), new Vector3d(-0.5210, 0.1335, -0.8430), new Vector3d(-0.5497, 0.2004, -0.8110), new Vector3d(-0.4795, 0.2004, -0.8543), new Vector3d(-0.6127, 0.3941, -0.6851), new Vector3d(-0.6252, 0.4543, -0.6346), new Vector3d(-0.5642, 0.4609, -0.6851), new Vector3d(-0.3387, 0.3941, -0.8543), new Vector3d(-0.3604, 0.4609, -0.8110), new Vector3d(-0.2880, 0.4543, -0.8430), new Vector3d(-0.5302, 0.3341, -0.7793), new Vector3d(-0.4816, 0.4010, -0.7793), new Vector3d(-0.4599, 0.3341, -0.8227), new Vector3d(0.0346, 0.1065, -0.9937), new Vector3d(0.0133, 0.1725, -0.9849), new Vector3d(0.0906, 0.1474, -0.9849), new Vector3d(-0.0489, 0.3660, -0.9293), new Vector3d(-0.0686, 0.4271, -0.9016), new Vector3d(0.0075, 0.4083, -0.9128), new Vector3d(0.2547, 0.2674, -0.9293), new Vector3d(0.2340, 0.3347, -0.9128), new Vector3d(0.3066, 0.3052, -0.9016), new Vector3d(0.0483, 0.2803, -0.9587), new Vector3d(0.1047, 0.3221, -0.9409), new Vector3d(0.1257, 0.2552, -0.9587), new Vector3d(-0.1267, 0.6057, -0.7855), new Vector3d(-0.1433, 0.6566, -0.7405), new Vector3d(-0.0722, 0.6535, -0.7535), new Vector3d(-0.1874, 0.7902, -0.5836), new Vector3d(-0.1995, 0.8271, -0.5254), new Vector3d(-0.1314, 0.8308, -0.5408), new Vector3d(0.0917, 0.7726, -0.6283), new Vector3d(0.0764, 0.8162, -0.5727), new Vector3d(0.1449, 0.8030, -0.5781), new Vector3d(-0.1041, 0.7494, -0.6538), new Vector3d(-0.0481, 0.7901, -0.6111), new Vector3d(-0.0328, 0.7453, -0.6659), new Vector3d(0.4585, 0.4156, -0.7855), new Vector3d(0.4425, 0.4862, -0.7535), new Vector3d(0.5019, 0.4470, -0.7405), new Vector3d(0.3799, 0.6789, -0.6283), new Vector3d(0.3548, 0.7348, -0.5781), new Vector3d(0.4180, 0.7052, -0.5727), new Vector3d(0.6160, 0.5291, -0.5836), new Vector3d(0.5947, 0.5949, -0.5408), new Vector3d(0.6476, 0.5519, -0.5254), new Vector3d(0.4647, 0.5837, -0.6659), new Vector3d(0.5034, 0.6109, -0.6111), new Vector3d(0.5247, 0.5451, -0.6538), new Vector3d(-0.0340, 0.5368, -0.8430), new Vector3d(0.0207, 0.5847, -0.8110), new Vector3d(0.0424, 0.5180, -0.8544), new Vector3d(0.1855, 0.7045, -0.6851), new Vector3d(0.2388, 0.7350, -0.6346), new Vector3d(0.2640, 0.6790, -0.6851), new Vector3d(0.2702, 0.4440, -0.8544), new Vector3d(0.3269, 0.4852, -0.8110), new Vector3d(0.3430, 0.4143, -0.8430), new Vector3d(0.1540, 0.6075, -0.7793), new Vector3d(0.2325, 0.5819, -0.7793), new Vector3d(0.1757, 0.5406, -0.8227), new Vector3d(0.7942, -0.4454, -0.4135), new Vector3d(0.8189, -0.3817, -0.4286), new Vector3d(0.8321, -0.4224, -0.3594), new Vector3d(0.8699, -0.1796, -0.4593), new Vector3d(0.8786, -0.1104, -0.4646), new Vector3d(0.9032, -0.1515, -0.4016), new Vector3d(0.9218, -0.3392, -0.1878), new Vector3d(0.9419, -0.2706, -0.1990), new Vector3d(0.9427, -0.3077, -0.1290), new Vector3d(0.8760, -0.2899, -0.3855), new Vector3d(0.9081, -0.2615, -0.3272), new Vector3d(0.8892, -0.3306, -0.3163), new Vector3d(0.8786, 0.1104, -0.4646), new Vector3d(0.8699, 0.1796, -0.4593), new Vector3d(0.9032, 0.1515, -0.4016), new Vector3d(0.8189, 0.3817, -0.4286), new Vector3d(0.7942, 0.4454, -0.4135), new Vector3d(0.8321, 0.4224, -0.3594), new Vector3d(0.9419, 0.2706, -0.1990), new Vector3d(0.9218, 0.3392, -0.1878), new Vector3d(0.9427, 0.3077, -0.1290), new Vector3d(0.8760, 0.2899, -0.3855), new Vector3d(0.8892, 0.3306, -0.3163), new Vector3d(0.9081, 0.2615, -0.3272), new Vector3d(0.9786, -0.1973, 0.0588), new Vector3d(0.9911, -0.1191, 0.0589), new Vector3d(0.9801, -0.1596, 0.1178), new Vector3d(0.9911, 0.1191, 0.0589), new Vector3d(0.9786, 0.1973, 0.0588), new Vector3d(0.9801, 0.1596, 0.1178), new Vector3d(0.9562, -0.0406, 0.2900), new Vector3d(0.9562, 0.0406, 0.2900), new Vector3d(0.9389, 0.0000, 0.3442), new Vector3d(0.9930, 0.0000, 0.1178), new Vector3d(0.9834, 0.0407, 0.1769), new Vector3d(0.9834, -0.0407, 0.1769), new Vector3d(0.9132, 0.0000, -0.4074), new Vector3d(0.9380, 0.0413, -0.3442), new Vector3d(0.9380, -0.0413, -0.3442), new Vector3d(0.9769, 0.1610, -0.1404), new Vector3d(0.9776, 0.1982, -0.0702), new Vector3d(0.9903, 0.1198, -0.0703), new Vector3d(0.9769, -0.1610, -0.1404), new Vector3d(0.9903, -0.1198, -0.0703), new Vector3d(0.9776, -0.1982, -0.0702), new Vector3d(0.9767, 0.0413, -0.2108), new Vector3d(0.9901, 0.0000, -0.1405), new Vector3d(0.9767, -0.0413, -0.2108), new Vector3d(-0.1782, -0.8929, -0.4135), new Vector3d(-0.1100, -0.8968, -0.4286), new Vector3d(-0.1445, -0.9219, -0.3594), new Vector3d(0.0980, -0.8828, -0.4593), new Vector3d(0.1665, -0.8697, -0.4646), new Vector3d(0.1350, -0.9058, -0.4016), new Vector3d(-0.0378, -0.9815, -0.1878), new Vector3d(0.0337, -0.9794, -0.1990), new Vector3d(-0.0013, -0.9916, -0.1290), new Vector3d(-0.0050, -0.9227, -0.3855), new Vector3d(0.0319, -0.9444, -0.3272), new Vector3d(-0.0397, -0.9478, -0.3163), new Vector3d(0.3765, -0.8015, -0.4646), new Vector3d(0.4396, -0.7718, -0.4593), new Vector3d(0.4232, -0.8122, -0.4016), new Vector3d(0.6161, -0.6609, -0.4286), new Vector3d(0.6690, -0.6177, -0.4135), new Vector3d(0.6588, -0.6609, -0.3594), new Vector3d(0.5484, -0.8122, -0.1990), new Vector3d(0.6075, -0.7718, -0.1878), new Vector3d(0.5839, -0.8015, -0.1290), new Vector3d(0.5464, -0.7435, -0.3855), new Vector3d(0.5892, -0.7435, -0.3163), new Vector3d(0.5294, -0.7828, -0.3272), new Vector3d(0.1148, -0.9916, 0.0588), new Vector3d(0.1930, -0.9794, 0.0589), new Vector3d(0.1511, -0.9815, 0.1178), new Vector3d(0.4195, -0.9058, 0.0589), new Vector3d(0.4900, -0.8697, 0.0588), new Vector3d(0.4547, -0.8828, 0.1178), new Vector3d(0.2568, -0.9219, 0.2900), new Vector3d(0.3341, -0.8968, 0.2900), new Vector3d(0.2901, -0.8929, 0.3443), new Vector3d(0.3069, -0.9444, 0.1178), new Vector3d(0.3426, -0.9227, 0.1769), new Vector3d(0.2652, -0.9478, 0.1769), new Vector3d(0.2822, -0.8685, -0.4074), new Vector3d(0.3291, -0.8793, -0.3442), new Vector3d(0.2506, -0.9048, -0.3442), new Vector3d(0.4550, -0.8793, -0.1404), new Vector3d(0.4906, -0.8685, -0.0702), new Vector3d(0.4199, -0.9048, -0.0703), new Vector3d(0.1488, -0.9789, -0.1404), new Vector3d(0.1921, -0.9789, -0.0703), new Vector3d(0.1136, -0.9910, -0.0702), new Vector3d(0.3411, -0.9161, -0.2108), new Vector3d(0.3060, -0.9416, -0.1405), new Vector3d(0.2625, -0.9416, -0.2108), new Vector3d(-0.9043, -0.1065, -0.4135), new Vector3d(-0.8869, -0.1725, -0.4286), new Vector3d(-0.9215, -0.1474, -0.3594), new Vector3d(-0.8093, -0.3660, -0.4593), new Vector3d(-0.7757, -0.4271, -0.4646), new Vector3d(-0.8198, -0.4083, -0.4016), new Vector3d(-0.9451, -0.2674, -0.1878), new Vector3d(-0.9211, -0.3347, -0.1990), new Vector3d(-0.9435, -0.3052, -0.1290), new Vector3d(-0.8791, -0.2803, -0.3855), new Vector3d(-0.8884, -0.3221, -0.3272), new Vector3d(-0.9137, -0.2552, -0.3163), new Vector3d(-0.6459, -0.6057, -0.4646), new Vector3d(-0.5982, -0.6566, -0.4593), new Vector3d(-0.6416, -0.6535, -0.4016), new Vector3d(-0.4381, -0.7902, -0.4286), new Vector3d(-0.3807, -0.8271, -0.4135), new Vector3d(-0.4249, -0.8308, -0.3594), new Vector3d(-0.6029, -0.7726, -0.1990), new Vector3d(-0.5463, -0.8162, -0.1878), new Vector3d(-0.5818, -0.8030, -0.1290), new Vector3d(-0.5382, -0.7494, -0.3855), new Vector3d(-0.5250, -0.7901, -0.3163), new Vector3d(-0.5809, -0.7453, -0.3272), new Vector3d(-0.9076, -0.4156, 0.0588), new Vector3d(-0.8718, -0.4862, 0.0589), new Vector3d(-0.8868, -0.4470, 0.1178), new Vector3d(-0.7319, -0.6789, 0.0589), new Vector3d(-0.6757, -0.7348, 0.0588), new Vector3d(-0.6991, -0.7052, 0.1178), new Vector3d(-0.7974, -0.5291, 0.2900), new Vector3d(-0.7497, -0.5949, 0.2900), new Vector3d(-0.7596, -0.5519, 0.3443), new Vector3d(-0.8034, -0.5837, 0.1178), new Vector3d(-0.7717, -0.6109, 0.1769), new Vector3d(-0.8195, -0.5451, 0.1769), new Vector3d(-0.7388, -0.5368, -0.4074), new Vector3d(-0.7346, -0.5847, -0.3442), new Vector3d(-0.7831, -0.5180, -0.3442), new Vector3d(-0.6957, -0.7045, -0.1404), new Vector3d(-0.6744, -0.7350, -0.0702), new Vector3d(-0.7308, -0.6790, -0.0702), new Vector3d(-0.8850, -0.4440, -0.1404), new Vector3d(-0.8716, -0.4852, -0.0702), new Vector3d(-0.9074, -0.4143, -0.0702), new Vector3d(-0.7659, -0.6075, -0.2108), new Vector3d(-0.8010, -0.5820, -0.1405), new Vector3d(-0.8144, -0.5406, -0.2108), new Vector3d(-0.3807, 0.8271, -0.4135), new Vector3d(-0.4381, 0.7902, -0.4286), new Vector3d(-0.4249, 0.8308, -0.3594), new Vector3d(-0.5982, 0.6566, -0.4593), new Vector3d(-0.6459, 0.6057, -0.4646), new Vector3d(-0.6416, 0.6535, -0.4016), new Vector3d(-0.5463, 0.8162, -0.1878), new Vector3d(-0.6029, 0.7726, -0.1990), new Vector3d(-0.5818, 0.8030, -0.1290), new Vector3d(-0.5382, 0.7494, -0.3855), new Vector3d(-0.5809, 0.7453, -0.3272), new Vector3d(-0.5250, 0.7901, -0.3163), new Vector3d(-0.7757, 0.4271, -0.4646), new Vector3d(-0.8093, 0.3660, -0.4593), new Vector3d(-0.8198, 0.4083, -0.4016), new Vector3d(-0.8869, 0.1725, -0.4286), new Vector3d(-0.9043, 0.1065, -0.4135), new Vector3d(-0.9215, 0.1474, -0.3594), new Vector3d(-0.9211, 0.3347, -0.1990), new Vector3d(-0.9451, 0.2674, -0.1878), new Vector3d(-0.9435, 0.3052, -0.1290), new Vector3d(-0.8791, 0.2803, -0.3855), new Vector3d(-0.9137, 0.2552, -0.3163), new Vector3d(-0.8884, 0.3221, -0.3272), new Vector3d(-0.6757, 0.7348, 0.0588), new Vector3d(-0.7319, 0.6789, 0.0589), new Vector3d(-0.6991, 0.7052, 0.1178), new Vector3d(-0.8718, 0.4862, 0.0589), new Vector3d(-0.9076, 0.4156, 0.0588), new Vector3d(-0.8868, 0.4470, 0.1178), new Vector3d(-0.7497, 0.5949, 0.2900), new Vector3d(-0.7974, 0.5291, 0.2900), new Vector3d(-0.7596, 0.5519, 0.3443), new Vector3d(-0.8034, 0.5837, 0.1178), new Vector3d(-0.8195, 0.5451, 0.1769), new Vector3d(-0.7717, 0.6109, 0.1769), new Vector3d(-0.7388, 0.5368, -0.4074), new Vector3d(-0.7831, 0.5180, -0.3442), new Vector3d(-0.7346, 0.5847, -0.3442), new Vector3d(-0.8850, 0.4440, -0.1404), new Vector3d(-0.9074, 0.4143, -0.0702), new Vector3d(-0.8716, 0.4852, -0.0702), new Vector3d(-0.6957, 0.7045, -0.1404), new Vector3d(-0.7308, 0.6790, -0.0702), new Vector3d(-0.6744, 0.7350, -0.0702), new Vector3d(-0.8144, 0.5406, -0.2108), new Vector3d(-0.8010, 0.5820, -0.1405), new Vector3d(-0.7659, 0.6075, -0.2108), new Vector3d(0.6690, 0.6177, -0.4135), new Vector3d(0.6161, 0.6609, -0.4286), new Vector3d(0.6588, 0.6609, -0.3594), new Vector3d(0.4396, 0.7718, -0.4593), new Vector3d(0.3765, 0.8015, -0.4646), new Vector3d(0.4232, 0.8122, -0.4016), new Vector3d(0.6075, 0.7718, -0.1878), new Vector3d(0.5484, 0.8122, -0.1990), new Vector3d(0.5839, 0.8015, -0.1290), new Vector3d(0.5464, 0.7435, -0.3855), new Vector3d(0.5294, 0.7828, -0.3272), new Vector3d(0.5892, 0.7435, -0.3163), new Vector3d(0.1665, 0.8697, -0.4646), new Vector3d(0.0980, 0.8828, -0.4593), new Vector3d(0.1350, 0.9058, -0.4016), new Vector3d(-0.1100, 0.8968, -0.4286), new Vector3d(-0.1782, 0.8929, -0.4135), new Vector3d(-0.1445, 0.9219, -0.3594), new Vector3d(0.0337, 0.9794, -0.1990), new Vector3d(-0.0378, 0.9815, -0.1878), new Vector3d(-0.0013, 0.9916, -0.1290), new Vector3d(-0.0050, 0.9227, -0.3855), new Vector3d(-0.0397, 0.9478, -0.3163), new Vector3d(0.0319, 0.9444, -0.3272), new Vector3d(0.4900, 0.8697, 0.0588), new Vector3d(0.4195, 0.9058, 0.0589), new Vector3d(0.4547, 0.8828, 0.1178), new Vector3d(0.1930, 0.9794, 0.0589), new Vector3d(0.1148, 0.9916, 0.0588), new Vector3d(0.1511, 0.9815, 0.1178), new Vector3d(0.3341, 0.8968, 0.2900), new Vector3d(0.2568, 0.9219, 0.2900), new Vector3d(0.2901, 0.8929, 0.3443), new Vector3d(0.3069, 0.9444, 0.1178), new Vector3d(0.2652, 0.9478, 0.1769), new Vector3d(0.3426, 0.9227, 0.1769), new Vector3d(0.2822, 0.8685, -0.4074), new Vector3d(0.2506, 0.9048, -0.3442), new Vector3d(0.3291, 0.8793, -0.3442), new Vector3d(0.1488, 0.9789, -0.1404), new Vector3d(0.1136, 0.9910, -0.0702), new Vector3d(0.1921, 0.9789, -0.0703), new Vector3d(0.4550, 0.8793, -0.1404), new Vector3d(0.4199, 0.9048, -0.0703), new Vector3d(0.4906, 0.8685, -0.0702), new Vector3d(0.2625, 0.9416, -0.2108), new Vector3d(0.3060, 0.9416, -0.1405), new Vector3d(0.3411, 0.9161, -0.2108), new Vector3d(0.7596, -0.5519, -0.3443), new Vector3d(0.7974, -0.5291, -0.2900), new Vector3d(0.7497, -0.5949, -0.2900), new Vector3d(0.8868, -0.4470, -0.1178), new Vector3d(0.9076, -0.4156, -0.0588), new Vector3d(0.8718, -0.4862, -0.0589), new Vector3d(0.6991, -0.7052, -0.1178), new Vector3d(0.7319, -0.6789, -0.0589), new Vector3d(0.6757, -0.7348, -0.0588), new Vector3d(0.8195, -0.5451, -0.1769), new Vector3d(0.8034, -0.5837, -0.1178), new Vector3d(0.7717, -0.6109, -0.1769), new Vector3d(0.9435, -0.3052, 0.1290), new Vector3d(0.9451, -0.2674, 0.1878), new Vector3d(0.9211, -0.3347, 0.1990), new Vector3d(0.9215, -0.1474, 0.3594), new Vector3d(0.9043, -0.1065, 0.4135), new Vector3d(0.8869, -0.1725, 0.4286), new Vector3d(0.8198, -0.4083, 0.4016), new Vector3d(0.8093, -0.3660, 0.4593), new Vector3d(0.7757, -0.4271, 0.4646), new Vector3d(0.9137, -0.2552, 0.3163), new Vector3d(0.8791, -0.2803, 0.3855), new Vector3d(0.8884, -0.3221, 0.3272), new Vector3d(0.5818, -0.8030, 0.1290), new Vector3d(0.6029, -0.7726, 0.1990), new Vector3d(0.5463, -0.8162, 0.1878), new Vector3d(0.6416, -0.6535, 0.4016), new Vector3d(0.6459, -0.6057, 0.4646), new Vector3d(0.5982, -0.6566, 0.4593), new Vector3d(0.4249, -0.8308, 0.3594), new Vector3d(0.4381, -0.7902, 0.4286), new Vector3d(0.3807, -0.8271, 0.4135), new Vector3d(0.5809, -0.7453, 0.3272), new Vector3d(0.5382, -0.7494, 0.3855), new Vector3d(0.5250, -0.7901, 0.3163), new Vector3d(0.9074, -0.4143, 0.0702), new Vector3d(0.8850, -0.4440, 0.1404), new Vector3d(0.8716, -0.4852, 0.0702), new Vector3d(0.7831, -0.5180, 0.3442), new Vector3d(0.7388, -0.5368, 0.4074), new Vector3d(0.7346, -0.5847, 0.3442), new Vector3d(0.7308, -0.6790, 0.0702), new Vector3d(0.6957, -0.7045, 0.1404), new Vector3d(0.6744, -0.7350, 0.0702), new Vector3d(0.8144, -0.5406, 0.2108), new Vector3d(0.7659, -0.6075, 0.2108), new Vector3d(0.8010, -0.5820, 0.1405), new Vector3d(-0.2901, -0.8929, -0.3443), new Vector3d(-0.2568, -0.9219, -0.2900), new Vector3d(-0.3341, -0.8968, -0.2900), new Vector3d(-0.1511, -0.9815, -0.1178), new Vector3d(-0.1148, -0.9916, -0.0588), new Vector3d(-0.1930, -0.9794, -0.0589), new Vector3d(-0.4547, -0.8828, -0.1178), new Vector3d(-0.4195, -0.9058, -0.0589), new Vector3d(-0.4900, -0.8697, -0.0588), new Vector3d(-0.2652, -0.9478, -0.1769), new Vector3d(-0.3069, -0.9444, -0.1178), new Vector3d(-0.3426, -0.9227, -0.1769), new Vector3d(0.0013, -0.9916, 0.1290), new Vector3d(0.0378, -0.9815, 0.1878), new Vector3d(-0.0337, -0.9794, 0.1990), new Vector3d(0.1445, -0.9219, 0.3594), new Vector3d(0.1782, -0.8929, 0.4135), new Vector3d(0.1100, -0.8968, 0.4286), new Vector3d(-0.1350, -0.9058, 0.4016), new Vector3d(-0.0980, -0.8828, 0.4593), new Vector3d(-0.1665, -0.8697, 0.4646), new Vector3d(0.0397, -0.9478, 0.3163), new Vector3d(0.0050, -0.9227, 0.3855), new Vector3d(-0.0319, -0.9444, 0.3272), new Vector3d(-0.5839, -0.8015, 0.1290), new Vector3d(-0.5484, -0.8122, 0.1990), new Vector3d(-0.6075, -0.7718, 0.1878), new Vector3d(-0.4232, -0.8122, 0.4016), new Vector3d(-0.3765, -0.8015, 0.4646), new Vector3d(-0.4396, -0.7718, 0.4593), new Vector3d(-0.6588, -0.6609, 0.3594), new Vector3d(-0.6161, -0.6609, 0.4286), new Vector3d(-0.6690, -0.6177, 0.4135), new Vector3d(-0.5294, -0.7828, 0.3272), new Vector3d(-0.5464, -0.7435, 0.3855), new Vector3d(-0.5892, -0.7435, 0.3163), new Vector3d(-0.1136, -0.9910, 0.0702), new Vector3d(-0.1488, -0.9789, 0.1404), new Vector3d(-0.1921, -0.9789, 0.0703), new Vector3d(-0.2506, -0.9048, 0.3442), new Vector3d(-0.2822, -0.8685, 0.4074), new Vector3d(-0.3291, -0.8793, 0.3442), new Vector3d(-0.4199, -0.9048, 0.0703), new Vector3d(-0.4550, -0.8793, 0.1404), new Vector3d(-0.4906, -0.8685, 0.0702), new Vector3d(-0.2625, -0.9416, 0.2108), new Vector3d(-0.3411, -0.9161, 0.2108), new Vector3d(-0.3060, -0.9416, 0.1405), new Vector3d(-0.9389, 0.0000, -0.3442), new Vector3d(-0.9562, -0.0406, -0.2900), new Vector3d(-0.9562, 0.0406, -0.2900), new Vector3d(-0.9801, -0.1596, -0.1178), new Vector3d(-0.9786, -0.1973, -0.0588), new Vector3d(-0.9911, -0.1191, -0.0589), new Vector3d(-0.9801, 0.1596, -0.1178), new Vector3d(-0.9911, 0.1191, -0.0589), new Vector3d(-0.9786, 0.1973, -0.0588), new Vector3d(-0.9834, -0.0407, -0.1769), new Vector3d(-0.9930, 0.0000, -0.1178), new Vector3d(-0.9834, 0.0407, -0.1769), new Vector3d(-0.9427, -0.3077, 0.1290), new Vector3d(-0.9218, -0.3392, 0.1878), new Vector3d(-0.9419, -0.2706, 0.1990), new Vector3d(-0.8321, -0.4224, 0.3594), new Vector3d(-0.7942, -0.4454, 0.4135), new Vector3d(-0.8189, -0.3817, 0.4286), new Vector3d(-0.9032, -0.1515, 0.4016), new Vector3d(-0.8699, -0.1796, 0.4593), new Vector3d(-0.8786, -0.1104, 0.4646), new Vector3d(-0.8892, -0.3306, 0.3163), new Vector3d(-0.8760, -0.2899, 0.3855), new Vector3d(-0.9081, -0.2615, 0.3272), new Vector3d(-0.9427, 0.3077, 0.1290), new Vector3d(-0.9419, 0.2706, 0.1990), new Vector3d(-0.9218, 0.3392, 0.1878), new Vector3d(-0.9032, 0.1515, 0.4016), new Vector3d(-0.8786, 0.1104, 0.4646), new Vector3d(-0.8699, 0.1796, 0.4593), new Vector3d(-0.8321, 0.4224, 0.3594), new Vector3d(-0.8189, 0.3817, 0.4286), new Vector3d(-0.7942, 0.4454, 0.4135), new Vector3d(-0.9081, 0.2615, 0.3272), new Vector3d(-0.8760, 0.2899, 0.3855), new Vector3d(-0.8892, 0.3306, 0.3163), new Vector3d(-0.9776, -0.1982, 0.0702), new Vector3d(-0.9769, -0.1610, 0.1404), new Vector3d(-0.9903, -0.1198, 0.0703), new Vector3d(-0.9380, -0.0413, 0.3442), new Vector3d(-0.9132, 0.0000, 0.4074), new Vector3d(-0.9380, 0.0413, 0.3442), new Vector3d(-0.9903, 0.1198, 0.0703), new Vector3d(-0.9769, 0.1610, 0.1404), new Vector3d(-0.9776, 0.1982, 0.0702), new Vector3d(-0.9767, -0.0413, 0.2108), new Vector3d(-0.9767, 0.0413, 0.2108), new Vector3d(-0.9901, 0.0000, 0.1405), new Vector3d(-0.2901, 0.8929, -0.3443), new Vector3d(-0.3341, 0.8968, -0.2900), new Vector3d(-0.2568, 0.9219, -0.2900), new Vector3d(-0.4547, 0.8828, -0.1178), new Vector3d(-0.4900, 0.8697, -0.0588), new Vector3d(-0.4195, 0.9058, -0.0589), new Vector3d(-0.1511, 0.9815, -0.1178), new Vector3d(-0.1930, 0.9794, -0.0589), new Vector3d(-0.1148, 0.9916, -0.0588), new Vector3d(-0.3426, 0.9227, -0.1769), new Vector3d(-0.3069, 0.9444, -0.1178), new Vector3d(-0.2652, 0.9478, -0.1769), new Vector3d(-0.5839, 0.8015, 0.1290), new Vector3d(-0.6075, 0.7718, 0.1878), new Vector3d(-0.5484, 0.8122, 0.1990), new Vector3d(-0.6588, 0.6609, 0.3594), new Vector3d(-0.6690, 0.6177, 0.4135), new Vector3d(-0.6161, 0.6609, 0.4286), new Vector3d(-0.4232, 0.8122, 0.4016), new Vector3d(-0.4396, 0.7718, 0.4593), new Vector3d(-0.3765, 0.8015, 0.4646), new Vector3d(-0.5892, 0.7435, 0.3163), new Vector3d(-0.5464, 0.7435, 0.3855), new Vector3d(-0.5294, 0.7828, 0.3272), new Vector3d(0.0013, 0.9916, 0.1290), new Vector3d(-0.0337, 0.9794, 0.1990), new Vector3d(0.0378, 0.9815, 0.1878), new Vector3d(-0.1350, 0.9058, 0.4016), new Vector3d(-0.1665, 0.8697, 0.4646), new Vector3d(-0.0980, 0.8828, 0.4593), new Vector3d(0.1445, 0.9219, 0.3594), new Vector3d(0.1100, 0.8968, 0.4286), new Vector3d(0.1782, 0.8929, 0.4135), new Vector3d(-0.0319, 0.9444, 0.3272), new Vector3d(0.0050, 0.9227, 0.3855), new Vector3d(0.0397, 0.9478, 0.3163), new Vector3d(-0.4906, 0.8685, 0.0702), new Vector3d(-0.4550, 0.8793, 0.1404), new Vector3d(-0.4199, 0.9048, 0.0703), new Vector3d(-0.3291, 0.8793, 0.3442), new Vector3d(-0.2822, 0.8685, 0.4074), new Vector3d(-0.2506, 0.9048, 0.3442), new Vector3d(-0.1921, 0.9789, 0.0703), new Vector3d(-0.1488, 0.9789, 0.1404), new Vector3d(-0.1136, 0.9910, 0.0702), new Vector3d(-0.3411, 0.9161, 0.2108), new Vector3d(-0.2625, 0.9416, 0.2108), new Vector3d(-0.3060, 0.9416, 0.1405), new Vector3d(0.7596, 0.5519, -0.3443), new Vector3d(0.7497, 0.5949, -0.2900), new Vector3d(0.7974, 0.5291, -0.2900), new Vector3d(0.6991, 0.7052, -0.1178), new Vector3d(0.6757, 0.7348, -0.0588), new Vector3d(0.7319, 0.6789, -0.0589), new Vector3d(0.8868, 0.4470, -0.1178), new Vector3d(0.8718, 0.4862, -0.0589), new Vector3d(0.9076, 0.4156, -0.0588), new Vector3d(0.7717, 0.6109, -0.1769), new Vector3d(0.8034, 0.5837, -0.1178), new Vector3d(0.8195, 0.5451, -0.1769), new Vector3d(0.5818, 0.8030, 0.1290), new Vector3d(0.5463, 0.8162, 0.1878), new Vector3d(0.6029, 0.7726, 0.1990), new Vector3d(0.4249, 0.8308, 0.3594), new Vector3d(0.3807, 0.8271, 0.4135), new Vector3d(0.4381, 0.7902, 0.4286), new Vector3d(0.6416, 0.6535, 0.4016), new Vector3d(0.5982, 0.6566, 0.4593), new Vector3d(0.6459, 0.6057, 0.4646), new Vector3d(0.5250, 0.7901, 0.3163), new Vector3d(0.5382, 0.7494, 0.3855), new Vector3d(0.5809, 0.7453, 0.3272), new Vector3d(0.9435, 0.3052, 0.1290), new Vector3d(0.9211, 0.3347, 0.1990), new Vector3d(0.9451, 0.2674, 0.1878), new Vector3d(0.8198, 0.4083, 0.4016), new Vector3d(0.7757, 0.4271, 0.4646), new Vector3d(0.8093, 0.3660, 0.4593), new Vector3d(0.9215, 0.1474, 0.3594), new Vector3d(0.8869, 0.1725, 0.4286), new Vector3d(0.9043, 0.1065, 0.4135), new Vector3d(0.8884, 0.3221, 0.3272), new Vector3d(0.8791, 0.2803, 0.3855), new Vector3d(0.9137, 0.2552, 0.3163), new Vector3d(0.6744, 0.7350, 0.0702), new Vector3d(0.6957, 0.7045, 0.1404), new Vector3d(0.7308, 0.6790, 0.0702), new Vector3d(0.7346, 0.5847, 0.3442), new Vector3d(0.7388, 0.5368, 0.4074), new Vector3d(0.7831, 0.5180, 0.3442), new Vector3d(0.8716, 0.4852, 0.0702), new Vector3d(0.8850, 0.4440, 0.1404), new Vector3d(0.9074, 0.4143, 0.0702), new Vector3d(0.7659, 0.6075, 0.2108), new Vector3d(0.8144, 0.5406, 0.2108), new Vector3d(0.8010, 0.5820, 0.1405), new Vector3d(0.3247, -0.7864, 0.5254), new Vector3d(0.3820, -0.7494, 0.5408), new Vector3d(0.3129, -0.7494, 0.5836), new Vector3d(0.5416, -0.6155, 0.5727), new Vector3d(0.5892, -0.5645, 0.5781), new Vector3d(0.5283, -0.5711, 0.6283), new Vector3d(0.2700, -0.6155, 0.7405), new Vector3d(0.3257, -0.5711, 0.7535), new Vector3d(0.2536, -0.5645, 0.7855), new Vector3d(0.4255, -0.6675, 0.6111), new Vector3d(0.4115, -0.6223, 0.6659), new Vector3d(0.3563, -0.6675, 0.6538), new Vector3d(0.7189, -0.3859, 0.5781), new Vector3d(0.7527, -0.3249, 0.5727), new Vector3d(0.7064, -0.3259, 0.6283), new Vector3d(0.8308, -0.1317, 0.5408), new Vector3d(0.8483, -0.0658, 0.5254), new Vector3d(0.8094, -0.0660, 0.5836), new Vector3d(0.6438, -0.1333, 0.7535), new Vector3d(0.6688, -0.0666, 0.7405), new Vector3d(0.6152, -0.0667, 0.7855), new Vector3d(0.7663, -0.1984, 0.6111), new Vector3d(0.7449, -0.1326, 0.6538), new Vector3d(0.7190, -0.1991, 0.6659), new Vector3d(0.1955, -0.3859, 0.9016), new Vector3d(0.2460, -0.3259, 0.9128), new Vector3d(0.1756, -0.3249, 0.9293), new Vector3d(0.3860, -0.1333, 0.9128), new Vector3d(0.4274, -0.0667, 0.9016), new Vector3d(0.3632, -0.0666, 0.9293), new Vector3d(0.1122, -0.1317, 0.9849), new Vector3d(0.1600, -0.0660, 0.9849), new Vector3d(0.0906, -0.0658, 0.9937), new Vector3d(0.2740, -0.1991, 0.9409), new Vector3d(0.2517, -0.1326, 0.9587), new Vector3d(0.2039, -0.1984, 0.9587), new Vector3d(0.6252, -0.4543, 0.6346), new Vector3d(0.6127, -0.3941, 0.6851), new Vector3d(0.5642, -0.4609, 0.6851), new Vector3d(0.5497, -0.2004, 0.8110), new Vector3d(0.5210, -0.1335, 0.8430), new Vector3d(0.4795, -0.2004, 0.8543), new Vector3d(0.3604, -0.4609, 0.8110), new Vector3d(0.3387, -0.3941, 0.8543), new Vector3d(0.2880, -0.4543, 0.8430), new Vector3d(0.5302, -0.3341, 0.7793), new Vector3d(0.4599, -0.3341, 0.8227), new Vector3d(0.4816, -0.4010, 0.7793), new Vector3d(-0.6476, -0.5519, 0.5254), new Vector3d(-0.5947, -0.5949, 0.5408), new Vector3d(-0.6160, -0.5291, 0.5836), new Vector3d(-0.4180, -0.7052, 0.5727), new Vector3d(-0.3548, -0.7348, 0.5781), new Vector3d(-0.3799, -0.6789, 0.6283), new Vector3d(-0.5019, -0.4470, 0.7405), new Vector3d(-0.4425, -0.4862, 0.7535), new Vector3d(-0.4585, -0.4156, 0.7855), new Vector3d(-0.5034, -0.6109, 0.6111), new Vector3d(-0.4647, -0.5837, 0.6659), new Vector3d(-0.5247, -0.5451, 0.6538), new Vector3d(-0.1449, -0.8030, 0.5781), new Vector3d(-0.0764, -0.8162, 0.5727), new Vector3d(-0.0917, -0.7726, 0.6283), new Vector3d(0.1314, -0.8308, 0.5408), new Vector3d(0.1995, -0.8271, 0.5254), new Vector3d(0.1874, -0.7902, 0.5836), new Vector3d(0.0722, -0.6535, 0.7535), new Vector3d(0.1433, -0.6566, 0.7405), new Vector3d(0.1267, -0.6057, 0.7855), new Vector3d(0.0481, -0.7901, 0.6111), new Vector3d(0.1041, -0.7494, 0.6538), new Vector3d(0.0328, -0.7453, 0.6659), new Vector3d(-0.3066, -0.3052, 0.9016), new Vector3d(-0.2340, -0.3347, 0.9128), new Vector3d(-0.2547, -0.2674, 0.9293), new Vector3d(-0.0075, -0.4083, 0.9128), new Vector3d(0.0686, -0.4271, 0.9016), new Vector3d(0.0489, -0.3660, 0.9293), new Vector3d(-0.0906, -0.1474, 0.9849), new Vector3d(-0.0133, -0.1725, 0.9849), new Vector3d(-0.0346, -0.1065, 0.9937), new Vector3d(-0.1047, -0.3221, 0.9409), new Vector3d(-0.0483, -0.2803, 0.9587), new Vector3d(-0.1257, -0.2552, 0.9587), new Vector3d(-0.2388, -0.7350, 0.6346), new Vector3d(-0.1855, -0.7045, 0.6851), new Vector3d(-0.2640, -0.6790, 0.6851), new Vector3d(-0.0207, -0.5847, 0.8110), new Vector3d(0.0340, -0.5368, 0.8430), new Vector3d(-0.0424, -0.5180, 0.8544), new Vector3d(-0.3269, -0.4852, 0.8110), new Vector3d(-0.2702, -0.4440, 0.8544), new Vector3d(-0.3430, -0.4143, 0.8430), new Vector3d(-0.1540, -0.6075, 0.7793), new Vector3d(-0.1757, -0.5406, 0.8227), new Vector3d(-0.2325, -0.5819, 0.7793), new Vector3d(-0.7250, 0.4454, 0.5254), new Vector3d(-0.7495, 0.3817, 0.5408), new Vector3d(-0.6936, 0.4224, 0.5836), new Vector3d(-0.7999, 0.1796, 0.5727), new Vector3d(-0.8085, 0.1104, 0.5781), new Vector3d(-0.7631, 0.1515, 0.6283), new Vector3d(-0.5802, 0.3392, 0.7405), new Vector3d(-0.5992, 0.2706, 0.7535), new Vector3d(-0.5369, 0.3077, 0.7855), new Vector3d(-0.7366, 0.2899, 0.6111), new Vector3d(-0.6987, 0.2615, 0.6659), new Vector3d(-0.6806, 0.3306, 0.6538), new Vector3d(-0.8085, -0.1104, 0.5781), new Vector3d(-0.7999, -0.1796, 0.5727), new Vector3d(-0.7631, -0.1515, 0.6283), new Vector3d(-0.7495, -0.3817, 0.5408), new Vector3d(-0.7250, -0.4454, 0.5254), new Vector3d(-0.6936, -0.4224, 0.5836), new Vector3d(-0.5992, -0.2706, 0.7535), new Vector3d(-0.5802, -0.3392, 0.7405), new Vector3d(-0.5369, -0.3077, 0.7855), new Vector3d(-0.7366, -0.2899, 0.6111), new Vector3d(-0.6806, -0.3306, 0.6538), new Vector3d(-0.6987, -0.2615, 0.6659), new Vector3d(-0.3850, 0.1973, 0.9016), new Vector3d(-0.3906, 0.1191, 0.9128), new Vector3d(-0.3330, 0.1596, 0.9293), new Vector3d(-0.3906, -0.1191, 0.9128), new Vector3d(-0.3850, -0.1973, 0.9016), new Vector3d(-0.3330, -0.1596, 0.9293), new Vector3d(-0.1682, 0.0406, 0.9849), new Vector3d(-0.1682, -0.0406, 0.9849), new Vector3d(-0.1120, 0.0000, 0.9937), new Vector3d(-0.3387, 0.0000, 0.9409), new Vector3d(-0.2815, -0.0407, 0.9587), new Vector3d(-0.2815, 0.0407, 0.9587), new Vector3d(-0.7728, 0.0000, 0.6346), new Vector3d(-0.7273, -0.0413, 0.6851), new Vector3d(-0.7273, 0.0413, 0.6851), new Vector3d(-0.5625, -0.1610, 0.8110), new Vector3d(-0.5000, -0.1982, 0.8430), new Vector3d(-0.5057, -0.1198, 0.8543), new Vector3d(-0.5625, 0.1610, 0.8110), new Vector3d(-0.5057, 0.1198, 0.8543), new Vector3d(-0.5000, 0.1982, 0.8430), new Vector3d(-0.6253, -0.0413, 0.7793), new Vector3d(-0.5685, 0.0000, 0.8227), new Vector3d(-0.6253, 0.0413, 0.7793), new Vector3d(0.1995, 0.8271, 0.5254), new Vector3d(0.1314, 0.8308, 0.5408), new Vector3d(0.1874, 0.7902, 0.5836), new Vector3d(-0.0764, 0.8162, 0.5727), new Vector3d(-0.1449, 0.8030, 0.5781), new Vector3d(-0.0917, 0.7726, 0.6283), new Vector3d(0.1433, 0.6566, 0.7405), new Vector3d(0.0722, 0.6535, 0.7535), new Vector3d(0.1267, 0.6057, 0.7855), new Vector3d(0.0481, 0.7901, 0.6111), new Vector3d(0.0328, 0.7453, 0.6659), new Vector3d(0.1041, 0.7494, 0.6538), new Vector3d(-0.3548, 0.7348, 0.5781), new Vector3d(-0.4180, 0.7052, 0.5727), new Vector3d(-0.3799, 0.6789, 0.6283), new Vector3d(-0.5947, 0.5949, 0.5408), new Vector3d(-0.6476, 0.5519, 0.5254), new Vector3d(-0.6160, 0.5291, 0.5836), new Vector3d(-0.4425, 0.4862, 0.7535), new Vector3d(-0.5019, 0.4470, 0.7405), new Vector3d(-0.4585, 0.4156, 0.7855), new Vector3d(-0.5034, 0.6109, 0.6111), new Vector3d(-0.5247, 0.5451, 0.6538), new Vector3d(-0.4647, 0.5837, 0.6659), new Vector3d(0.0686, 0.4271, 0.9016), new Vector3d(-0.0075, 0.4083, 0.9128), new Vector3d(0.0489, 0.3660, 0.9293), new Vector3d(-0.2340, 0.3347, 0.9128), new Vector3d(-0.3066, 0.3052, 0.9016), new Vector3d(-0.2547, 0.2674, 0.9293), new Vector3d(-0.0133, 0.1725, 0.9849), new Vector3d(-0.0906, 0.1474, 0.9849), new Vector3d(-0.0346, 0.1065, 0.9937), new Vector3d(-0.1047, 0.3221, 0.9409), new Vector3d(-0.1257, 0.2552, 0.9587), new Vector3d(-0.0483, 0.2803, 0.9587), new Vector3d(-0.2388, 0.7350, 0.6346), new Vector3d(-0.2640, 0.6790, 0.6851), new Vector3d(-0.1855, 0.7045, 0.6851), new Vector3d(-0.3269, 0.4852, 0.8110), new Vector3d(-0.3430, 0.4143, 0.8430), new Vector3d(-0.2702, 0.4440, 0.8544), new Vector3d(-0.0207, 0.5847, 0.8110), new Vector3d(-0.0424, 0.5180, 0.8544), new Vector3d(0.0340, 0.5368, 0.8430), new Vector3d(-0.2325, 0.5819, 0.7793), new Vector3d(-0.1757, 0.5406, 0.8227), new Vector3d(-0.1540, 0.6075, 0.7793), new Vector3d(0.8483, 0.0658, 0.5254), new Vector3d(0.8308, 0.1317, 0.5408), new Vector3d(0.8094, 0.0660, 0.5836), new Vector3d(0.7527, 0.3249, 0.5727), new Vector3d(0.7189, 0.3859, 0.5781), new Vector3d(0.7064, 0.3259, 0.6283), new Vector3d(0.6688, 0.0666, 0.7405), new Vector3d(0.6438, 0.1333, 0.7535), new Vector3d(0.6152, 0.0667, 0.7855), new Vector3d(0.7663, 0.1984, 0.6111), new Vector3d(0.7190, 0.1991, 0.6659), new Vector3d(0.7449, 0.1326, 0.6538), new Vector3d(0.5892, 0.5645, 0.5781), new Vector3d(0.5416, 0.6155, 0.5727), new Vector3d(0.5283, 0.5711, 0.6283), new Vector3d(0.3820, 0.7494, 0.5408), new Vector3d(0.3247, 0.7864, 0.5254), new Vector3d(0.3129, 0.7494, 0.5836), new Vector3d(0.3257, 0.5711, 0.7535), new Vector3d(0.2700, 0.6155, 0.7405), new Vector3d(0.2536, 0.5645, 0.7855), new Vector3d(0.4255, 0.6675, 0.6111), new Vector3d(0.3563, 0.6675, 0.6538), new Vector3d(0.4115, 0.6223, 0.6659), new Vector3d(0.4274, 0.0667, 0.9016), new Vector3d(0.3860, 0.1333, 0.9128), new Vector3d(0.3632, 0.0666, 0.9293), new Vector3d(0.2460, 0.3259, 0.9128), new Vector3d(0.1955, 0.3859, 0.9016), new Vector3d(0.1756, 0.3249, 0.9293), new Vector3d(0.1600, 0.0660, 0.9849), new Vector3d(0.1122, 0.1317, 0.9849), new Vector3d(0.0906, 0.0658, 0.9937), new Vector3d(0.2740, 0.1991, 0.9409), new Vector3d(0.2039, 0.1984, 0.9587), new Vector3d(0.2517, 0.1326, 0.9587), new Vector3d(0.6252, 0.4543, 0.6346), new Vector3d(0.5642, 0.4609, 0.6851), new Vector3d(0.6127, 0.3941, 0.6851), new Vector3d(0.3604, 0.4609, 0.8110), new Vector3d(0.2880, 0.4543, 0.8430), new Vector3d(0.3387, 0.3941, 0.8543), new Vector3d(0.5497, 0.2004, 0.8110), new Vector3d(0.4795, 0.2004, 0.8543), new Vector3d(0.5210, 0.1335, 0.8430), new Vector3d(0.4816, 0.4010, 0.7793), new Vector3d(0.4599, 0.3341, 0.8227), new Vector3d(0.5302, 0.3341, 0.7793) };

            return equiSolidAngleVectors4PI;
        }

        #endregion 0. 4PI Array
    }
}