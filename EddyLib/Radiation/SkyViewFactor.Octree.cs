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
                    this.RootDir = DefaultDirectoriesAndPaths.Eddy3DInstallDir;
                    this.RadDir = DefaultDirectoriesAndPaths.RadianceDir;
                    this.RadBinDir = DefaultDirectoriesAndPaths.RadianceBinDir;
                    //if (!Environment.Is64BitOperatingSystem)
                    //{
                    //    // 32 bit binaries (Windows)
                    //    // string bin32 = Path.Combine(windows_root, "Radiance", "bin_32");
                    //    // if (Directory.Exists(bin32)) this.RadBinDir = bin32;
                    //}
                    this.DaysimBinDir = Path.Combine(this.RootDir, "DaysimBinaries");
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
            // ✅ GOOD: Validate user-controlled paths before use in process execution
            Utilities.ValidatePathForShell(radFilePath);
            Utilities.ValidatePathForShell(octFilePath);

            // get executing platform
            var platform = new ExecutingPlatform();

            // add environmental variables
            string radbin = platform.RadBinDir;
            string radlib = Path.Combine(platform.RadDir, "lib");
            string daybin = platform.DaysimBinDir;
            char ps = (platform.OS == OSType.Windows) ? ';' : ':';
            Environment.SetEnvironmentVariable("PATH", "." + ps + radlib + ps + radbin + ps + daybin + ps + "$PATH");
            Environment.SetEnvironmentVariable("RAYPATH", "." + ps + radlib + ps + radbin + ps + daybin + ps + "$RAYPATH");

            ProcessStartInfo psi = new ProcessStartInfo("oconv");
            psi.ArgumentList.Add(radFilePath);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.WorkingDirectory = Path.GetDirectoryName(octFilePath);

            Process p = Process.Start(psi);
            var writeTask = System.Threading.Tasks.Task.Run(() =>
            {
                using (var fs = new FileStream(octFilePath, FileMode.Create, FileAccess.Write))
                {
                    p.StandardOutput.BaseStream.CopyTo(fs);
                }
            });
            p.WaitForExit();
            writeTask.Wait();
            p.Close();
        }

        private static void RunRTrace(string o_flag, string octree_path, string pts_path, string output_path)
        {
            // ✅ GOOD: Validate user-controlled paths before use in process execution
            Utilities.ValidatePathForShell(octree_path);
            Utilities.ValidatePathForShell(pts_path);
            Utilities.ValidatePathForShell(output_path);

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

            ProcessStartInfo psi = new ProcessStartInfo("rtrace");
            psi.ArgumentList.Add(o_flag);
            psi.ArgumentList.Add("-h");
            psi.ArgumentList.Add("-ab");
            psi.ArgumentList.Add("1");
            psi.ArgumentList.Add(octree_path);
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.CreateNoWindow = true;
            psi.RedirectStandardInput = true;
            psi.WorkingDirectory = Path.GetDirectoryName(octree_path);

            using var inFs = new FileStream(pts_path, FileMode.Open, FileAccess.Read);
            using var outFs = new FileStream(output_path, FileMode.Create, FileAccess.Write);

            Process p = Process.Start(psi);

            var readTask = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    inFs.CopyTo(p.StandardInput.BaseStream);
                }
                catch (IOException)
                {
                    // Ignore broken pipe if process exits early
                }
                finally
                {
                    p.StandardInput.Close();
                }
            });

            var writeTask = System.Threading.Tasks.Task.Run(() =>
            {
                p.StandardOutput.BaseStream.CopyTo(outFs);
            });
            var errorTask = p.StandardError.ReadToEndAsync();

            p.WaitForExit();
            readTask.Wait();
            writeTask.Wait();
            string error = errorTask.GetAwaiter().GetResult();
            int exitCode = p.ExitCode;
            p.Close();

            if (exitCode != 0)
            {
                throw new InvalidOperationException("rtrace failed with exit code " + exitCode + ": " + error);
            }
        }

        public static void RunRayCastMat(string octree_path, string pts_path, string output_path)
        {
            RunRTrace("-oM", octree_path, pts_path, output_path);
        }

        public static void RunRayCastSurf(string octree_path, string pts_path, string output_path)
        {
            RunRTrace("-os", octree_path, pts_path, output_path);
        }

        #endregion 3. Octree
    }
}
