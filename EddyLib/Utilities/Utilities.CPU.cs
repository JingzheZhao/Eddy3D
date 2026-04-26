using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace EddyLib
{
    public static partial class Utilities
    {
        private static readonly Lazy<int> CachedPhysicalCoreCount =
            new Lazy<int>(DetectPhysicalCoreCount);

        /// <summary>
        /// Gets the number of physical CPU cores (not logical threads).
        /// Falls back to logical processor count if detection fails.
        /// </summary>
        public static int GetPhysicalCoreCount()
        {
            return CachedPhysicalCoreCount.Value;
        }

        /// <summary>
        /// Gets Eddy's auto CPU count: 75% of physical cores, minimum 1.
        /// </summary>
        public static int GetAutoCpuCount()
        {
            int physical = GetPhysicalCoreCount();
            return Math.Max(1, (int)Math.Floor(physical * 0.75));
        }

        private static int DetectPhysicalCoreCount()
        {
            try
            {
                int detected = 0;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    detected = DetectWindowsPhysicalCores();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    detected = DetectMacPhysicalCores();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    detected = DetectLinuxPhysicalCores();
                }

                if (detected > 0)
                {
                    return detected;
                }
            }
            catch
            {
                // Fallback below
            }

            return Math.Max(1, Environment.ProcessorCount);
        }

        private static int DetectWindowsPhysicalCores()
        {
            string psOut = RunProcessForOutput(
                "powershell",
                "-NoProfile -NonInteractive -Command \"(Get-CimInstance Win32_Processor | Measure-Object -Property NumberOfCores -Sum).Sum\"");

            if (TryParsePositiveInt(psOut, out int coresFromPowerShell))
            {
                return coresFromPowerShell;
            }

            // Fallback for older hosts where PowerShell CIM may fail.
            string wmicOut = RunProcessForOutput("wmic", "cpu get NumberOfCores");
            var matches = Regex.Matches(wmicOut ?? string.Empty, @"\d+");
            int sum = 0;
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) && value > 0)
                {
                    sum += value;
                }
            }

            return sum;
        }

        private static int DetectMacPhysicalCores()
        {
            string output = RunProcessForOutput("sysctl", "-n hw.physicalcpu");
            if (TryParsePositiveInt(output, out int cores))
            {
                return cores;
            }

            return 0;
        }

        private static int DetectLinuxPhysicalCores()
        {
            string output = RunProcessForOutput("lscpu", "-p=Core,Socket");
            if (string.IsNullOrWhiteSpace(output))
            {
                return 0;
            }

            var uniqueCoreSocketPairs = new HashSet<string>(StringComparer.Ordinal);
            foreach (string raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = raw.Trim();
                if (line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                uniqueCoreSocketPairs.Add(line);
            }

            return uniqueCoreSocketPairs.Count;
        }

        private static string RunProcessForOutput(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        return string.Empty;
                    }

                    if (!process.WaitForExit(5000))
                    {
                        try { process.Kill(); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                        return string.Empty;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        output = process.StandardError.ReadToEnd();
                    }

                    return (output ?? string.Empty).Trim();
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool TryParsePositiveInt(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var match = Regex.Match(text, @"\d+");
            if (!match.Success)
            {
                return false;
            }

            if (!int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return false;
            }

            if (parsed <= 0)
            {
                return false;
            }

            value = parsed;
            return true;
        }
    }
}
