using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Helper utilities specific to OpenFOAM operations.
    /// </summary>
    public static class OpenFOAMHelpers
    {
        /// <summary>
        /// Deletes phi files for all wind directions.
        /// </summary>
        public static void DeletePhi(OFMeshSettings meshSettings, OFBaseDomain domain)
        {
            foreach (int dir in domain.BCond.WindDirections)
            {
                string phiPath = Path.Combine(meshSettings.baseWorkingDir, dir.ToString(), "0", "phi");
                if (File.Exists(phiPath))
                {
                    File.Delete(phiPath);
                }
            }
        }

        /// <summary>
        /// Gets the last iteration number from a simulation directory.
        /// </summary>
        public static int GetLastIterationFromDirectory(string simWorkingDirectory)
        {
            simWorkingDirectory = NormalizeBackslashes(simWorkingDirectory);

            var directories = GetDirectories(simWorkingDirectory);
            var directoryNames = directories.Select(d => new DirectoryInfo(d).Name);
            var numericDirs = directoryNames.Where(s => s.All(char.IsDigit));

            var lastIteration = numericDirs.Max();
            return int.Parse(lastIteration);
        }

        /// <summary>
        /// Calculates the optimal number of CPUs based on mesh size.
        /// </summary>
        public static int CalculateOptimalCPUs(string meshWorkingDirectory, int requestedCpus)
        {
            int cpuCount = requestedCpus;
            int numberOfCellsInMesh = 0;
            int availableCpus = Environment.ProcessorCount;

            var logPath = Path.Combine(meshWorkingDirectory, "log");
            if (File.Exists(logPath))
            {
                var logLines = File.ReadAllLines(logPath);
                foreach (var line in logLines)
                {
                    if (line.Contains("nCells"))
                    {
                        var match = Regex.Match(line, @"\d+");
                        if (match.Success)
                        {
                            numberOfCellsInMesh = int.Parse(match.Value);
                        }
                    }
                }
            }

            // Auto-calculate if not specified or -1
            if (requestedCpus == -1)
            {
                cpuCount = Math.Max(1, Math.Min(availableCpus, numberOfCellsInMesh / 10000));
            }

            return cpuCount;
        }

        private static string NormalizeBackslashes(string input)
        {
            return input.Replace(@"\\", @"\").Replace(@"\\", @"\");
        }

        private static List<string> GetDirectories(string path)
        {
            return Directory.GetDirectories(path).ToList();
        }
    }

    /// <summary>
    /// Cleans OpenFOAM case directories.
    /// </summary>
    public static class FoamCleaner
    {
        private static readonly Regex TimeDirRegex =
            new Regex(@"^\d+(\.\d+)?$", RegexOptions.Compiled);

        private static readonly Regex ProcessorDirRegex =
            new Regex(@"^processor\d+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly HashSet<string> PreserveTopLevel =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "system", "constant", "0", "0.org", "0.orig"
            };

        private static readonly string[] TransientFilePatterns =
        {
            "*.log", "log.*", "*.OpenFOAM", "*.foam", "core", "backtrace.*"
        };

        /// <summary>
        /// Cleans an OpenFOAM case directory, preserving setup files.
        /// </summary>
        /// <param name="caseRoot">Path to the case directory.</param>
        /// <returns>True if all deletions succeeded.</returns>
        public static bool CleanCase(string caseRoot)
        {
            bool success = true;

            // Remove transient root files
            foreach (var pattern in TransientFilePatterns)
            {
                foreach (var file in Directory.GetFiles(caseRoot, pattern, SearchOption.TopDirectoryOnly))
                {
                    try { File.Delete(file); }
                    catch { success = false; }
                }
            }

            // Remove transient directories
            foreach (var dir in Directory.GetDirectories(caseRoot, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(dir);

                if (PreserveTopLevel.Contains(name))
                {
                    continue;
                }

                if (ShouldDeleteDirectory(name))
                {
                    try { Directory.Delete(dir, true); }
                    catch { success = false; }
                }
            }

            return success;
        }

        private static bool ShouldDeleteDirectory(string name)
        {
            return TimeDirRegex.IsMatch(name)
                || ProcessorDirRegex.IsMatch(name)
                || name.Equals("postProcessing", StringComparison.OrdinalIgnoreCase)
                || name.Equals("dynamicCode", StringComparison.OrdinalIgnoreCase)
                || name.Equals("logs", StringComparison.OrdinalIgnoreCase);
        }
    }
}
