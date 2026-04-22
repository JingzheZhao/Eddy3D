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
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"C:\Windows\System32\cmd.exe";
                startInfo.ArgumentList.Add("/c");
                startInfo.ArgumentList.Add("MKLINK");
                startInfo.ArgumentList.Add("/J");
                startInfo.ArgumentList.Add(simDir);
                startInfo.ArgumentList.Add(meshDir);
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                using (Process symLinks = new Process())
                {
                    symLinks.StartInfo = startInfo;
                    symLinks.EnableRaisingEvents = true;
                    symLinks.Start();
                    symLinks.WaitForExit();
                }
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
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                psi.ArgumentList.Add("-sfn");
                psi.ArgumentList.Add(relTarget);
                psi.ArgumentList.Add(simDir);
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
                delete.StartInfo.ArgumentList.Add("/c");
                delete.StartInfo.ArgumentList.Add("rd");
                delete.StartInfo.ArgumentList.Add(simDir);
                delete.StartInfo.UseShellExecute = false;
                delete.StartInfo.RedirectStandardInput = true;
                delete.StartInfo.RedirectStandardError = true;
                delete.StartInfo.RedirectStandardOutput = true;
                delete.StartInfo.CreateNoWindow = true;
                delete.Start();
                delete.WaitForExit();
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
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    psi.ArgumentList.Add("-rf");
                    psi.ArgumentList.Add(simDir);
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
