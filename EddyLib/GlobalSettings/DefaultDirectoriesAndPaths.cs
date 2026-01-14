using System;
using System.IO;

namespace EddyLib
{
    /// <summary>
    /// Default directory paths for external tools and resources.
    /// These paths can be overridden at runtime via component inputs.
    /// </summary>
    public static class DefaultDirectoriesAndPaths
    {
        // Backing fields with default values
        private static string _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Eddy3D");
        private static string _radianceDir = Path.Combine(_baseDir, @"Radiance_012cb178_Windows"); // Specific Version from 2020
        private static string _energyPlusDir = @"C:\EnergyPlusV9-4-0";
        private static string _blueCfdDir = @"C:\Program Files\blueCFD-Core-2020";

        /// <summary>
        /// Base directory for Eddy3D files in AppData Roaming.
        /// </summary>
        public static string Eddy3DInstallDir => _baseDir;

        /// <summary>
        /// Path to Radiance base directory.
        /// </summary>
        public static string RadianceDir
        {
            get => _radianceDir;
            set => _radianceDir = NormalizeEnginePath(value, Path.Combine(_baseDir, @"Radiance_012cb178_Windows"));
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
            set => _energyPlusDir = NormalizeEnginePath(value, @"C:\EnergyPlusV9-4-0");
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
            string normalized = path.Trim().TrimEnd('\\', '/');

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
                throw new FileNotFoundException($"Radiance base directory not found at: {baseDir}. Please install Radiance via the 'Install Engines' component.");
            }

            string radExe = Path.Combine(binDir, "rad.exe");
            if (!File.Exists(radExe))
            {
                throw new FileNotFoundException($"Radiance executable (rad.exe) not found in: {binDir}. checked path: {radExe}. Please ensure Radiance is correctly installed.");
            }
        }

        /// <summary>
        /// Verifies EnergyPlus installation and throws if missing.
        /// </summary>
        public static void CheckEnergyPlus()
        {
            if (string.IsNullOrWhiteSpace(EnergyPlusDir) || !Directory.Exists(EnergyPlusDir))
            {
                throw new FileNotFoundException($"EnergyPlus directory not found at: {EnergyPlusDir ?? "null"}. Please install EnergyPlus v9.4.0 to {EnergyPlusDir}.");
            }

            string epExe = Path.Combine(EnergyPlusDir, "energyplus.exe");
            if (!File.Exists(epExe))
            {
                throw new FileNotFoundException($"EnergyPlus executable (energyplus.exe) not found in: {EnergyPlusDir}. Please ensure EnergyPlus v9.4.0 is correctly installed.");
            }
        }

        /// <summary>
        /// Verifies blueCFD installation and throws if missing.
        /// </summary>
        public static void CheckBlueCfd()
        {
            if (string.IsNullOrWhiteSpace(BlueCfdDir) || !Directory.Exists(BlueCfdDir))
            {
                throw new FileNotFoundException($"blueCFD directory not found at: {BlueCfdDir ?? "null"}. Please install blueCFD-Core 2020-1 to {BlueCfdDir}.");
            }

            // Based on user feedback for blueCFD-Core 2020
            string setvars = Path.Combine(BlueCfdDir, "setvars_OF8.bat");
            string readme = Path.Combine(BlueCfdDir, "README.TXT");

            if (!File.Exists(setvars) && !File.Exists(readme))
            {
                throw new FileNotFoundException($"blueCFD core files not found in: {BlueCfdDir}. Checked for setvars_OF8.bat and README.TXT. Please ensure blueCFD-Core 2020-1 is correctly installed.");
            }
        }
    }
}