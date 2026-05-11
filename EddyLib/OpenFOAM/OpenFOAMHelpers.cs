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
        /// Deletes solver-generated phi files from the 0/ folder of each wind-direction case.
        /// OpenFOAM operations such as potentialFoam or decomposePar may leave a phi
        /// (surfaceScalarField) in the initial-conditions folder. If present when the main
        /// solver starts, it can cause field-type conflicts or stale data, so we remove it
        /// during case setup.
        /// </summary>
        /// <returns>True if all targeted files were removed (or already absent); false if any deletion failed.</returns>
        public static bool DeletePhi(OFMeshSettings meshSettings, OFBaseDomain domain)
        {
            if (meshSettings == null || domain?.BCond?.WindDirections == null)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(meshSettings.baseWorkingDir))
            {
                return true;
            }

            bool success = true;

            foreach (int dir in domain.BCond.WindDirections)
            {
                string phiPath = Path.Combine(meshSettings.baseWorkingDir, dir.ToString(), "0", "phi");

                if (!File.Exists(phiPath))
                {
                    continue;
                }

                try
                {
                    File.Delete(phiPath);
                }
                catch
                {
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Checks whether a case directory contains any non-zero time-step iteration
        /// folders, indicating a previous simulation run that can be continued.
        /// Handles both single-CPU layout (numeric folders in the case root) and
        /// multi-CPU layout (numeric folders inside processorX directories).
        /// </summary>
        public static bool HasIterationFolders(string caseDirectory)
        {
            if (string.IsNullOrWhiteSpace(caseDirectory) || !Directory.Exists(caseDirectory))
                return false;

            foreach (var dir in Directory.GetDirectories(caseDirectory))
            {
                if (IsNonZeroIterationFolder(Path.GetFileName(dir)))
                    return true;
            }

            foreach (var dir in Directory.GetDirectories(caseDirectory, "processor*"))
            {
                var procName = Path.GetFileName(dir);
                if (procName == null || !Regex.IsMatch(procName, @"^processor\d+$", RegexOptions.IgnoreCase))
                    continue;
                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    if (IsNonZeroIterationFolder(Path.GetFileName(subDir)))
                        return true;
                }
            }

            return false;
        }

        private static bool IsNonZeroIterationFolder(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return name.All(char.IsDigit) && int.TryParse(name, out int val) && val > 0;
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
        /// Resolves the CPU count for OpenFOAM execution.
        /// - If requestedCpus is -1, returns 75% of physical cores.
        /// - If requestedCpus is less than 1, returns 1.
        /// - Otherwise returns requestedCpus.
        /// </summary>
        public static int CalculateOptimalCPUs(string meshWorkingDirectory, int requestedCpus)
        {
            if (requestedCpus == -1)
            {
                return Utilities.GetAutoCpuCount();
            }

            return requestedCpus < 1 ? 1 : requestedCpus;
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

            if (!Directory.Exists(caseRoot))
            {
                return true;
            }

            // Remove transient root files
            foreach (var pattern in TransientFilePatterns)
            {
                foreach (var file in Directory.GetFiles(caseRoot, pattern, SearchOption.TopDirectoryOnly))
                {
                    try { File.Delete(file); }
                    catch { success = false; }
                }
            }

            // Remove transient directories at top level
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

            // Recursively find and delete ALL processor* directories (including nested ones in mesh folder)
            success &= DeleteProcessorDirectoriesRecursive(caseRoot);

            // Delete reconstructed polyMesh in constant folder (the result of reconstructParMesh)
            var polyMeshPath = Path.Combine(caseRoot, "constant", "polyMesh");
            if (Directory.Exists(polyMeshPath))
            {
                try { Directory.Delete(polyMeshPath, true); }
                catch { success = false; }
            }

            // Delete generated feature-edge folders from surfaceFeatureExtract.
            success &= DeleteExtendedFeatureEdgeMeshDirectories(caseRoot);

            // Remove generated function-object fields from 0 folders while preserving setup fields.
            success &= CleanGeneratedFilesInZeroFolder(caseRoot);

            return success;
        }

        /// <summary>
        /// Removes Eddy probe cache binaries from the root postProcessing folder.
        /// Cache files are stored as *.bin in &lt;workingDirectory&gt;/postProcessing.
        /// </summary>
        /// <param name="workingDirectory">Eddy working directory that contains the root postProcessing folder.</param>
        /// <returns>True if all matching files were removed successfully.</returns>
        public static bool CleanProbeCacheFiles(string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                return true;
            }

            var rootPostProcessing = Path.Combine(workingDirectory, "postProcessing");
            if (!Directory.Exists(rootPostProcessing))
            {
                return true;
            }

            bool success = true;
            foreach (var cacheFile in Directory.GetFiles(rootPostProcessing, "*.bin", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    File.Delete(cacheFile);
                }
                catch
                {
                    success = false;
                }
            }

            return success;
        }

        /// <summary>
        /// Recursively finds and deletes all processor* directories.
        /// </summary>
        private static bool DeleteProcessorDirectoriesRecursive(string rootPath)
        {
            bool success = true;

            try
            {
                // Find all processor directories recursively
                var processorDirs = Directory.GetDirectories(rootPath, "processor*", SearchOption.AllDirectories)
                    .Where(d => ProcessorDirRegex.IsMatch(Path.GetFileName(d)))
                    .OrderByDescending(d => d.Length) // Delete deepest first to avoid parent-first issues
                    .ToList();

                foreach (var dir in processorDirs)
                {
                    try { Directory.Delete(dir, true); }
                    catch { success = false; }
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }

        private static bool ShouldDeleteDirectory(string name)
        {
            return TimeDirRegex.IsMatch(name)
                || ProcessorDirRegex.IsMatch(name)
                || name.Equals("postProcessing", StringComparison.OrdinalIgnoreCase)
                || name.Equals("dynamicCode", StringComparison.OrdinalIgnoreCase)
                || name.Equals("logs", StringComparison.OrdinalIgnoreCase)
                || name.Equals("extendedFeatureEdgeMesh", StringComparison.OrdinalIgnoreCase);
        }

        private static bool DeleteExtendedFeatureEdgeMeshDirectories(string caseRoot)
        {
            bool success = true;

            try
            {
                var candidates = new List<string>();

                // Sometimes generated at case root.
                candidates.Add(Path.Combine(caseRoot, "extendedFeatureEdgeMesh"));

                var constantDir = Path.Combine(caseRoot, "constant");
                candidates.Add(Path.Combine(constantDir, "extendedFeatureEdgeMesh"));

                if (Directory.Exists(constantDir))
                {
                    foreach (var subDir in Directory.GetDirectories(constantDir, "*", SearchOption.TopDirectoryOnly))
                    {
                        candidates.Add(Path.Combine(subDir, "extendedFeatureEdgeMesh"));
                    }
                }

                foreach (var dir in candidates
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Where(Directory.Exists))
                {
                    try { Directory.Delete(dir, true); }
                    catch { success = false; }
                }
            }
            catch
            {
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Removes generated files from the case "0" folder.
        /// Uses 0.org/0.orig as a baseline when available, and falls back to known function-object patterns.
        /// </summary>
        private static bool CleanGeneratedFilesInZeroFolder(string caseRoot)
        {
            bool success = true;
            var zeroDir = Path.Combine(caseRoot, "0");
            if (!Directory.Exists(zeroDir))
            {
                return true;
            }

            var referenceDir = GetZeroReferenceDirectory(caseRoot);
            var preserve = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(referenceDir) && Directory.Exists(referenceDir))
            {
                foreach (var file in Directory.GetFiles(referenceDir, "*", SearchOption.TopDirectoryOnly))
                {
                    preserve.Add(Path.GetFileName(file));
                }

                foreach (var dir in Directory.GetDirectories(referenceDir, "*", SearchOption.TopDirectoryOnly))
                {
                    preserve.Add(Path.GetFileName(dir));
                }
            }

            foreach (var file in Directory.GetFiles(zeroDir, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                if (ShouldDeleteZeroFolderEntry(name, preserve))
                {
                    try { File.Delete(file); }
                    catch { success = false; }
                }
            }

            foreach (var dir in Directory.GetDirectories(zeroDir, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(dir);
                if (ShouldDeleteZeroFolderEntry(name, preserve))
                {
                    try { Directory.Delete(dir, true); }
                    catch { success = false; }
                }
            }

            return success;
        }

        private static string GetZeroReferenceDirectory(string caseRoot)
        {
            var zeroOrg = Path.Combine(caseRoot, "0.org");
            if (Directory.Exists(zeroOrg))
            {
                return zeroOrg;
            }

            var zeroOrig = Path.Combine(caseRoot, "0.orig");
            if (Directory.Exists(zeroOrig))
            {
                return zeroOrig;
            }

            return null;
        }

        private static bool ShouldDeleteZeroFolderEntry(string entryName, HashSet<string> preserve)
        {
            if (preserve.Count > 0)
            {
                return !preserve.Contains(entryName);
            }

            // Fallback when no reference folder exists.
            return IsKnownFunctionObjectOutput(entryName);
        }

        private static bool IsKnownFunctionObjectOutput(string entryName)
        {
            if (string.IsNullOrWhiteSpace(entryName))
            {
                return false;
            }

            return entryName.IndexOf("_coeff", StringComparison.OrdinalIgnoreCase) >= 0
                || entryName.IndexOf('(') >= 0
                || entryName.IndexOf(')') >= 0;
        }
    }
}
