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
        private static string _radianceDir = Path.Combine(_baseDir, @"Radiance_012cb178_Windows\bin");
        private static string _radianceLibDir = Path.Combine(_baseDir, @"Radiance_012cb178_Windows\lib");
        private static string _energyPlusDir = @"C:\EnergyPlusV9-4-0";
        private static string _blueCfdDir = @"C:\Program Files\blueCFD-Core-2020";

        /// <summary>
        /// Base directory for Eddy3D files in AppData Roaming.
        /// </summary>
        public static string Eddy3DInstallDir => _baseDir;

        /// <summary>
        /// Path to Radiance binaries directory.
        /// </summary>
        public static string RadianceDir
        {
            get => _radianceDir;
            set => _radianceDir = string.IsNullOrWhiteSpace(value) ? Path.Combine(_baseDir, @"Radiance_012cb178_Windows\bin") : value;
        }

        /// <summary>
        /// Alias for RadianceDir (bin).
        /// </summary>
        public static string RadianceBinDir => RadianceDir;

        /// <summary>
        /// Path to Radiance library directory.
        /// </summary>
        public static string RadianceLibDir
        {
            get => _radianceLibDir;
            set => _radianceLibDir = string.IsNullOrWhiteSpace(value) ? Path.Combine(_baseDir, @"Radiance_012cb178_Windows\lib") : value;
        }

        /// <summary>
        /// Path to EnergyPlus installation directory.
        /// </summary>
        public static string EnergyPlusDir
        {
            get => _energyPlusDir;
            set => _energyPlusDir = string.IsNullOrWhiteSpace(value) ? @"C:\EnergyPlusV9-4-0" : value;
        }

        /// <summary>
        /// Path to BlueCFD installation directory.
        /// Default: C:\Program Files\blueCFD-Core-2020
        /// </summary>
        public static string BlueCfdDir
        {
            get => _blueCfdDir;
            set => _blueCfdDir = string.IsNullOrWhiteSpace(value) ? @"C:\Program Files\blueCFD-Core-2020" : value;
        }

        /// <summary>
        /// Path to weather files directory.
        /// </summary>
        public static readonly string WeatherDir = Path.Combine(_baseDir, "Weather");

        /// <summary>
        /// Default weather file path (New York LaGuardia).
        /// </summary>
      //  public static readonly string DefaultWeather = Path.Combine(_baseDir, @"Weather\USA_NY_New.York-LaGuardia.AP.725030_TMY3.epw");
    }
}