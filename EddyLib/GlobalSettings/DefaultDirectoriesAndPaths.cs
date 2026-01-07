namespace EddyLib
{
    /// <summary>
    /// Default directory paths for external tools and resources.
    /// These paths can be overridden at runtime via component inputs.
    /// </summary>
    public static class DefaultDirectoriesAndPaths
    {
        // Backing fields with default values
        private static string _radianceDir = @"C:\Eddy3D\Common\Radiance\bin";
        private static string _radianceLibDir = @"C:\Eddy3D\Common\Radiance\lib";
        private static string _energyPlusDir = @"C:\Eddy3D\Common\EnergyPlusV9-4-0";
        private static string _blueCfdDir = @"C:\Program Files\blueCFD-Core-2020";

        /// <summary>
        /// Path to Radiance binaries directory.
        /// Default: C:\Eddy3D\Common\Radiance\bin
        /// </summary>
        public static string RadianceDir
        {
            get => _radianceDir;
            set => _radianceDir = string.IsNullOrWhiteSpace(value) ? @"C:\Eddy3D\Common\Radiance\bin" : value;
        }

        /// <summary>
        /// Path to Radiance library directory.
        /// Default: C:\Eddy3D\Common\Radiance\lib
        /// </summary>
        public static string RadianceLibDir
        {
            get => _radianceLibDir;
            set => _radianceLibDir = string.IsNullOrWhiteSpace(value) ? @"C:\Eddy3D\Common\Radiance\lib" : value;
        }

        /// <summary>
        /// Path to EnergyPlus installation directory.
        /// Default: C:\Eddy3D\Common\EnergyPlusV9-4-0
        /// </summary>
        public static string EnergyPlusDir
        {
            get => _energyPlusDir;
            set => _energyPlusDir = string.IsNullOrWhiteSpace(value) ? @"C:\Eddy3D\Common\EnergyPlusV9-4-0" : value;
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
        public static readonly string WeatherDir = @"C:\Eddy3D\Weather";

        /// <summary>
        /// Default weather file path (New York LaGuardia).
        /// </summary>
        public static readonly string DefaultWeather = @"C:\Eddy3D\Weather\USA_NY_New.York-LaGuardia.AP.725030_TMY3.epw";
    }
}