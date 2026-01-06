namespace EddyLib
{
    /// <summary>
    /// Default directory paths for external tools and resources.
    /// </summary>
    public static class DefaultDirectoriesAndPaths
    {
        /// <summary>
        /// Path to Radiance binaries directory.
        /// </summary>
        public static readonly string RadianceDir = @"C:\Eddy3D\Common\Radiance\bin";

        /// <summary>
        /// Path to Radiance library directory.
        /// </summary>
        public static readonly string RadianceLibDir = @"C:\Eddy3D\Common\Radiance\lib";

        /// <summary>
        /// Path to EnergyPlus installation directory.
        /// </summary>
        public static readonly string EnergyPlusDir = @"C:\Eddy3D\Common\EnergyPlusV9-4-0";

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