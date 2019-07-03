using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EddyLib
{
    internal class Settings
    {
        public static int getCurrentRAM()
        {
            string currentRAM = @"Get-VMMemory MobyLinuxVM";

            ProcessStartInfo psiCurrentRAM = new ProcessStartInfo(@"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe");
            psiCurrentRAM.Verb = "runas";
            //psiCurrentRAM.CreateNoWindow = true;
            psiCurrentRAM.Arguments = currentRAM;
            psiCurrentRAM.RedirectStandardError = true;
            psiCurrentRAM.RedirectStandardOutput = true;
            psiCurrentRAM.UseShellExecute = false;

            Process pCurrentRAM = new Process();

            pCurrentRAM.StartInfo = psiCurrentRAM;
            pCurrentRAM.Start();
            string vms = pCurrentRAM.StandardOutput.ReadToEnd();
            string vms1 = pCurrentRAM.StandardOutput.ReadToEnd();
            //File.WriteAllText(@"C:\OF2\RAM", vms);
            pCurrentRAM.WaitForExit();

            int result = 0;
            //int[] numbers = (from Match m in Regex.Matches(vms, @"\d+") select int.Parse(m.Value)).ToArray();
            //result = numbers[1];

            return result;
        }

        public static int getCurrentCPUs(OFBoxDomain DOM)
        {
            string currentRAM = @"Get-VMProcessor MobyLinuxVM";

            ProcessStartInfo psiCurrentRAM = new ProcessStartInfo(@"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe");
            psiCurrentRAM.Verb = "runas";
            psiCurrentRAM.CreateNoWindow = true;
            psiCurrentRAM.Arguments = currentRAM;
            psiCurrentRAM.RedirectStandardError = true;
            psiCurrentRAM.RedirectStandardOutput = true;
            psiCurrentRAM.UseShellExecute = false;

            Process pCurrentRAM = new Process();

            pCurrentRAM.StartInfo = psiCurrentRAM;
            pCurrentRAM.Start();
            string vms = pCurrentRAM.StandardOutput.ReadToEnd();
            File.WriteAllText(@"C:\OF2\RAM", vms);
            pCurrentRAM.WaitForExit();

            int result = 0;
            int[] numbers = (from Match m in Regex.Matches(vms, @"\d+") select int.Parse(m.Value)).ToArray();
            result = numbers[0];

            return result;
        }
    }
}