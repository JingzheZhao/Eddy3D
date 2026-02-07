using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace EddyLib
{
    public class SymlinkCreator
    {
        public static void Create(string simDir, string meshDir)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (Directory.Exists(simDir))
                    return;

                // Windows: use cmd.exe MKLINK /J for directory junction
                string strCmdText = "/c MKLINK /J " + "\"" + simDir + "\"" + " " + "\"" + meshDir + "\"";

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"C:\Windows\System32\cmd.exe";
                startInfo.Arguments = strCmdText;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                Process symLinks = new Process();
                symLinks.StartInfo = startInfo;
                symLinks.EnableRaisingEvents = true;
                symLinks.Start();
                Thread.Sleep(500);
            }
            else
            {
                // macOS/Linux: use ln -sfn with relative path (works inside Docker mounts)
                // -s = symbolic, -f = force overwrite, -n = don't follow existing symlink
                var symlinkParent = Path.GetDirectoryName(simDir);
                var relTarget = Path.GetRelativePath(symlinkParent, meshDir);

                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/ln",
                    Arguments = string.Format("-sfn \"{0}\" \"{1}\"", relTarget, simDir),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit();
                }
            }
        }

        public static void Delete(string simDir)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: use cmd.exe rd to remove directory junction
                Process delete = new Process();
                delete.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
                delete.StartInfo.UseShellExecute = false;
                delete.StartInfo.RedirectStandardInput = true;
                delete.StartInfo.RedirectStandardError = true;
                delete.StartInfo.RedirectStandardOutput = true;
                delete.StartInfo.CreateNoWindow = true;
                delete.Start();
                StreamWriter sw = delete.StandardInput;
                sw.WriteLine("rd " + simDir);
                sw.Flush();
                sw.Close();
            }
            else
            {
                // macOS/Linux: remove symlink
                try
                {
                    if (File.Exists(simDir) || Directory.Exists(simDir))
                    {
                        Directory.Delete(simDir, false);
                    }
                }
                catch
                {
                    // Fallback: use rm
                    var psi = new ProcessStartInfo
                    {
                        FileName = "/bin/rm",
                        Arguments = string.Format("-rf \"{0}\"", simDir),
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var process = Process.Start(psi))
                    {
                        process?.WaitForExit();
                    }
                }
            }
        }

        public static bool IsSymbolic(string path)
        {
            FileInfo pathInfo = new FileInfo(path);
            return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
    }
}
