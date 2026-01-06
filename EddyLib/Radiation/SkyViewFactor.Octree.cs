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
    public partial class SkyViewFactor
    {
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

            private const string windows_root = @"C:\Eddy3D\Common\";

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
                    this.RadBinDir = Path.Combine(windows_root, "Radiance", "bin");
                    //if (!Environment.Is64BitOperatingSystem)
                    //{
                    //    // 32 bit binaries (Windows)
                    //    string bin32 = Path.Combine(windows_root, "Radiance", "bin_32");
                    //    if (Directory.Exists(bin32)) this.RadBinDir = bin32;
                    //}
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
    }
}
