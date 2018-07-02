using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System;

namespace EddyLib
{
  public  class SymlinkCreator
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
            }

           
            
           
            
        }

        public static void Delete(string simDir)
        {
            string strCmdText;


            strCmdText = "rmdir" + simDir;

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



            if (Directory.Exists(simDir))
            {
                SymLinks.Start();
                //SymLinks.WaitForExit();
            }



        }
    }
}