using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace EddyLib
{
    /// <summary>
    /// Default directory paths for external tools and resources.
    /// These paths can be overridden at runtime via component inputs.
    /// </summary>
    public static class DefaultDirectoriesAndPaths
    {
        // Backing fields with default values
        private static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        private static readonly string RoamingEddy3DDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Eddy3D");
        private static readonly string LocalEddy3DDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Eddy3D");
        private static string _baseDir = IsWindows ? LocalEddy3DDir : RoamingEddy3DDir;
        private static readonly string WindowsCasesRootDir = LocalEddy3DDir;
        private static readonly string _radianceDirDefault = IsWindows
            ? Path.Combine(_baseDir, "Radiance_012cb178_Windows")
            : Path.Combine(_baseDir, "Radiance_012cb178_OSX", "radiance");
        private static string _radianceDir = _radianceDirDefault;
        private static string _energyPlusDir = IsWindows
            ? @"C:\EnergyPlusV9-4-0"
            : "/Applications/EnergyPlus-9-4-0";
        private static string _blueCfdDir = @"C:\Program Files\blueCFD-Core-2020";
        private static readonly object AutoCasePathLock = new object();
        private static readonly Dictionary<string, string> AutoCasePathByInput =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Base directory for installed Eddy3D engines/resources.
        /// On Windows this is %LocalAppData%\Eddy3D.
        /// On macOS this remains under the user ApplicationData location.
        /// </summary>
        public static string Eddy3DInstallDir => _baseDir;

        /// <summary>
        /// Default directory for simulation cases.
        /// On macOS: ~/Eddy3D/Cases (avoids spaces in path — Docker volume mounts break with spaces).
        /// On Windows: %LocalAppData%\Eddy3D\Cases.
        /// </summary>
        public static string CasesDir => IsWindows
            ? Path.Combine(WindowsCasesRootDir, "Cases")
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Eddy3D", "Cases");

        /// <summary>
        /// Resolves a working directory path. If the input is a simple name (no path separators),
        /// it will be placed under the default CasesDir. Otherwise, the path is returned as-is.
        /// </summary>
        /// <param name="dirInput">User-provided directory or case name.</param>
        /// <returns>Full resolved path to the case directory.</returns>
        public static string ResolveWorkingDirectory(string dirInput)
        {
            if (string.IsNullOrWhiteSpace(dirInput))
            {
                // Empty input: generate an auto case path once per app session.
                return ResolveOrCreateAutoCasePath("__EMPTY_WORKING_DIR__");
            }

            string trimmed = TrimWrappingQuotes(dirInput.Trim());

            // Cross-platform migration: a GH file authored on one OS may carry absolute paths from another OS.
            // Map those foreign absolute paths back into the local Eddy3D Cases root.
            if (TryMapForeignWorkingDirectoryToLocalCases(trimmed, out string mappedForeignPath))
            {
                return mappedForeignPath;
            }

            // Check if this is a simple name (no path separators, no drive letter)
            bool isSimpleName = !ContainsAnyDirectorySeparator(trimmed)
                             && !LooksLikeWindowsDrivePath(trimmed)
                             && !Path.IsPathRooted(trimmed);

            if (isSimpleName)
            {
                // Simple case name - place under CasesDir
                return Path.Combine(CasesDir, trimmed);
            }

            // Full path provided - use as-is
            return trimmed;
        }

        /// <summary>
        /// Path to Radiance base directory.
        /// </summary>
        public static string RadianceDir
        {
            get => _radianceDir;
            set => _radianceDir = NormalizeEnginePath(value, _radianceDirDefault);
        }

        /// <summary>
        /// Path to Radiance binaries directory.
        /// </summary>
        public static string RadianceBinDir => Path.Combine(RadianceDir, "bin");

        /// <summary>
        /// Path to Radiance library directory.
        /// </summary>
        public static string RadianceLibDir => Path.Combine(RadianceDir, "lib");

        /// <summary>
        /// Path to EnergyPlus installation directory.
        /// </summary>
        public static string EnergyPlusDir
        {
            get => _energyPlusDir;
            set => _energyPlusDir = NormalizeEnginePath(value,
                IsWindows ? @"C:\EnergyPlusV9-4-0" : "/Applications/EnergyPlus-9-4-0");
        }

        /// <summary>
        /// Path to BlueCFD installation directory.
        /// Default: C:\Program Files\blueCFD-Core-2020
        /// </summary>
        public static string BlueCfdDir
        {
            get => _blueCfdDir;
            set => _blueCfdDir = NormalizeEnginePath(value, @"C:\Program Files\blueCFD-Core-2020");
        }

        private static string NormalizeEnginePath(string path, string defaultPath)
        {
            if (string.IsNullOrWhiteSpace(path)) return defaultPath;
            
            // Clean up basic formatting
            string normalized = TrimWrappingQuotes(path.Trim()).TrimEnd('\\', '/');

            // Reject foreign absolute paths from another OS (common when sharing Grasshopper files).
            if (IsForeignAbsolutePath(normalized))
            {
                return defaultPath;
            }

            // Handle Grasshopper boolean strings ("True"/"False") from legacy template wire crossings
            if (normalized.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                normalized.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return defaultPath;
            }

            // If user accidentally pointed to the bin folder, go up one level
            if (normalized.EndsWith(@"\bin", StringComparison.OrdinalIgnoreCase) || 
                normalized.EndsWith(@"/bin", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("bin", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string parent = Path.GetDirectoryName(normalized);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        normalized = parent;
                    }
                }
                catch { /* Ignore invalid paths, let CheckRadiance catch them */ }
            }

            return normalized;
        }

        private static bool TryMapForeignWorkingDirectoryToLocalCases(string rawPath, out string mappedPath)
        {
            mappedPath = null;
            if (!LooksLikeForeignPathArtifact(rawPath))
            {
                return false;
            }

            // Foreign/malformed path artifacts should never become literal case names
            // (for example, "C:\\Users\\..."), so generate a clean local case path.
            string cacheKey = "FOREIGN::" + NormalizeCacheKey(rawPath);
            mappedPath = ResolveOrCreateAutoCasePath(cacheKey);
            return true;
        }

        private static string ResolveOrCreateAutoCasePath(string cacheKey)
        {
            string normalizedKey = string.IsNullOrWhiteSpace(cacheKey)
                ? "__EMPTY_KEY__"
                : cacheKey.Trim();

            lock (AutoCasePathLock)
            {
                if (AutoCasePathByInput.TryGetValue(normalizedKey, out string existing))
                {
                    return existing;
                }

                string generated = CreateUniqueAutoCasePath();
                AutoCasePathByInput[normalizedKey] = generated;
                return generated;
            }
        }

        private static string CreateUniqueAutoCasePath()
        {
            string casesRoot = Path.GetFullPath(CasesDir);
            Directory.CreateDirectory(casesRoot);

            for (int attempts = 0; attempts < 128; attempts++)
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
                string randomSuffix = Guid.NewGuid().ToString("N").Substring(0, 4);
                string candidate = Path.Combine(casesRoot, "Case_" + timestamp + "_" + randomSuffix);
                if (!Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            // Extremely unlikely collision fallback.
            return Path.Combine(casesRoot, "Case_" + Guid.NewGuid().ToString("N"));
        }

        private static string NormalizeCacheKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return TrimWrappingQuotes(value.Trim())
                .Replace('\\', '/')
                .Trim()
                .ToUpperInvariant();
        }

        private static bool LooksLikeForeignPathArtifact(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (IsForeignAbsolutePath(value))
            {
                return true;
            }

            if (IsWindows)
            {
                return false;
            }

            // Cross-platform GH migration artifacts on macOS:
            // - escaped Windows paths (contain backslashes),
            // - malformed drive forms like "C/\Users\..."
            if (value.IndexOf('\\') >= 0)
            {
                return true;
            }

            return value.Length >= 3
                && char.IsLetter(value[0])
                && (value[1] == '/' || value[1] == '\\')
                && (value[2] == '\\' || value[2] == '/');
        }

        private static bool ContainsAnyDirectorySeparator(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return value.IndexOf('/') >= 0 || value.IndexOf('\\') >= 0;
        }

        private static bool LooksLikeWindowsDrivePath(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Length >= 2
                && char.IsLetter(value[0])
                && value[1] == ':';
        }

        private static bool LooksLikeWindowsUncPath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.StartsWith(@"\\", StringComparison.Ordinal)
                || value.StartsWith("//", StringComparison.Ordinal);
        }

        private static bool LooksLikeWindowsAbsolutePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (LooksLikeWindowsUncPath(value))
            {
                return true;
            }

            if (!LooksLikeWindowsDrivePath(value))
            {
                return false;
            }

            return value.Length == 2
                || value.Length == 3
                || value[2] == '\\'
                || value[2] == '/';
        }

        private static bool LooksLikeUnixAbsolutePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.StartsWith("/", StringComparison.Ordinal)
                || value.StartsWith("~/", StringComparison.Ordinal)
                || value.Equals("~", StringComparison.Ordinal);
        }

        private static bool IsForeignAbsolutePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (IsWindows)
            {
                return LooksLikeUnixAbsolutePath(value) && !LooksLikeWindowsAbsolutePath(value);
            }

            return LooksLikeWindowsAbsolutePath(value);
        }

        private static string TrimWrappingQuotes(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value ?? string.Empty;
            }

            string trimmed = value.Trim();
            if (trimmed.Length >= 2 && trimmed.StartsWith("\"", StringComparison.Ordinal) && trimmed.EndsWith("\"", StringComparison.Ordinal))
            {
                return trimmed.Substring(1, trimmed.Length - 2);
            }

            return trimmed;
        }

        /// <summary>
        /// Path to weather files directory.
        /// </summary>
        public static readonly string WeatherDir = Path.Combine(_baseDir, "Weather");

        /// <summary>
        /// Default weather file path (New York LaGuardia).
        /// </summary>
        //  public static readonly string DefaultWeather = Path.Combine(_baseDir, @"Weather\USA_NY_New.York-LaGuardia.AP.725030_TMY3.epw");

        /// <summary>
        /// Verifies Radiance installation and throws if missing.
        /// </summary>
        public static void CheckRadiance(string customPath = null)
        {
            string baseDir = string.IsNullOrWhiteSpace(customPath) ? RadianceDir : customPath;
            string binDir = string.IsNullOrWhiteSpace(customPath) ? RadianceBinDir : Path.Combine(customPath, "bin");

            if (!Directory.Exists(baseDir))
            {
                throw new FileNotFoundException(
                    string.Format("Radiance base directory not found at: {0}. Please install Radiance via the 'Install Engines' component.", baseDir));
            }

            string radExeName = IsWindows ? "rad.exe" : "rad";
            string radExe = Path.Combine(binDir, radExeName);
            if (!File.Exists(radExe))
            {
                throw new FileNotFoundException(
                    string.Format("Radiance executable ({0}) not found in: {1}. Please ensure Radiance is correctly installed.", radExeName, binDir));
            }
        }

        /// <summary>
        /// Verifies EnergyPlus installation and throws if missing.
        /// </summary>
        public static void CheckEnergyPlus()
        {
            if (string.IsNullOrWhiteSpace(EnergyPlusDir) || !Directory.Exists(EnergyPlusDir))
            {
                throw new FileNotFoundException(
                    string.Format("EnergyPlus directory not found at: {0}. Please install EnergyPlus v9.4.0.", EnergyPlusDir ?? "null"));
            }

            string epExeName = IsWindows ? "energyplus.exe" : "energyplus";
            string epExe = Path.Combine(EnergyPlusDir, epExeName);
            if (!File.Exists(epExe))
            {
                throw new FileNotFoundException(
                    string.Format("EnergyPlus executable ({0}) not found in: {1}. Please ensure EnergyPlus v9.4.0 is correctly installed.", epExeName, EnergyPlusDir));
            }
        }

        /// <summary>
        /// Verifies Docker is available and the daemon is running.
        /// </summary>
        public static void CheckDocker()
        {
            var dockerPath = Docker.DockerEnvironment.GetDockerPath();
            if (string.IsNullOrEmpty(dockerPath))
            {
                throw new FileNotFoundException(
                    "Docker not found. Please install Docker Desktop from https://www.docker.com/products/docker-desktop/");
            }

            if (!Docker.DockerEnvironment.IsDockerAvailable())
            {
                throw new InvalidOperationException(
                    "Docker is installed but not running. Please start Docker Desktop.");
            }
        }

        /// <summary>
        /// Verifies blueCFD installation and throws if missing.
        /// </summary>
        public static void CheckBlueCfd()
        {
            if (string.IsNullOrWhiteSpace(BlueCfdDir) || !Directory.Exists(BlueCfdDir))
            {
                throw new FileNotFoundException(
                    string.Format("blueCFD directory not found at: {0}. Please install blueCFD-Core 2020-1.", BlueCfdDir ?? "null"));
            }

            // Based on user feedback for blueCFD-Core 2020
            string setvars = Path.Combine(BlueCfdDir, "setvars_OF8.bat");
            string readme = Path.Combine(BlueCfdDir, "README.TXT");

            if (!File.Exists(setvars) && !File.Exists(readme))
            {
                throw new FileNotFoundException(
                    string.Format("blueCFD core files not found in: {0}. Please ensure blueCFD-Core 2020-1 is correctly installed.", BlueCfdDir));
            }
        }
    }
}
