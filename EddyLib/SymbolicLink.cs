using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace EddyLib
{
    public class SymlinkCreator
    {
        public static void Create(string simDir, string meshDir)
        {
            string strCmdText;

            strCmdText = "/c MKLINK /J " + "\"" + simDir + "\"" + " " + "\"" + meshDir + "\"";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"C:\Windows\System32\cmd.exe";
            startInfo.Arguments = strCmdText;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            Process SymLinks = new Process();
            SymLinks.StartInfo = startInfo;
            SymLinks.EnableRaisingEvents = true;

            if (!Directory.Exists(simDir))
            {
                SymLinks.Start();

                //SymLinks.WaitForExit();
                // Check this later. This was added since some links sometimes were not created after deleting.
                Thread.Sleep(500);
            }
        }

        public static void Delete(string simDir)
        {
            System.Diagnostics.Process delete = new System.Diagnostics.Process();
            delete.StartInfo.FileName = @"C:\Windows\System32\cmd.exe";
            delete.StartInfo.UseShellExecute = false;
            delete.StartInfo.RedirectStandardInput = true;
            delete.StartInfo.RedirectStandardError = true;
            delete.StartInfo.RedirectStandardOutput = true;
            delete.StartInfo.CreateNoWindow = true;
            delete.Start();
            StreamWriter sw = delete.StandardInput;
            String strInputText = "rd " + simDir;

            sw.WriteLine(strInputText);

            sw.Flush();

            sw.Close();
        }

        public static bool IsSymbolic(string path)
        {
            FileInfo pathInfo = new FileInfo(path);
            return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
        }
    }
}