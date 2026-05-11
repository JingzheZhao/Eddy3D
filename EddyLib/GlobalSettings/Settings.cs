using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EddyLib
{
    /// <summary>
    /// System settings and resource detection utilities.
    /// </summary>
    internal class Settings
    {
        /// <summary>
        /// Path to Windows PowerShell executable.
        /// </summary>
        private const string PowerShellPath = @"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe";

        /// <summary>
        /// Gets the current CPU count allocated to the Docker VM.
        /// </summary>
        public static int getCurrentCPUs(OFBoxDomain DOM)
        {
            string output = ExecutePowerShellCommand("Get-VMProcessor MobyLinuxVM");

            int result = 0;
            int[] numbers = (from Match m in Regex.Matches(output, @"\d+") select int.Parse(m.Value)).ToArray();
            if (numbers.Length > 0)
            {
                result = numbers[0];
            }

            return result;
        }

        /// <summary>
        /// Executes a PowerShell command and returns the standard output.
        /// </summary>
        private static string ExecutePowerShellCommand(string command)
        {
            var psi = new ProcessStartInfo(PowerShellPath)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add(command);

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }
    }
}