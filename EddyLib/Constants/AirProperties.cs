namespace EddyLib
{
    /// <summary>
    /// Physical constants for air at standard conditions.
    /// </summary>
    public static class AirProperties
    {
        /// <summary>
        /// Air density at 20°C and 101.325 kPa (kg/m³).
        /// </summary>
        public const double Rho = 1.2041;

        /// <summary>
        /// Dynamic viscosity of air (Pa·s).
        /// </summary>
        public const double Mu = 0.0000181;

        /// <summary>
        /// Kinematic viscosity of air (m²/s).
        /// </summary>
        public const double Nu = 1.5e-05;

        /// <summary>
        /// Standard atmospheric pressure (Pa).
        /// </summary>
        public const double AtmosphericPressure = 101325;
    }
}
