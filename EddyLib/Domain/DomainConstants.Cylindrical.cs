namespace EddyLib.Domain
{
    /// <summary>
    /// Constants used in domain calculations.
    /// Centralizes magic numbers for maintainability and configuration.
    /// </summary>
    public static partial class DomainConstants
    {
        #region Cylindrical Domain

        /// <summary>
        /// Radius multiplier based on building height for cylindrical domains.
        /// </summary>
        public const double RadiusHeightMultiplier = 15.5;

        /// <summary>
        /// Default inner rectangle size ratio relative to outer radius.
        /// </summary>
        public const double DefaultInnerRectRatio = 0.35;

        /// <summary>
        /// Default radial multiplier for block sizing.
        /// </summary>
        public const double DefaultRadialMultiplier = 2.0;

        /// <summary>
        /// Number of angles to sample for frontage area calculation.
        /// </summary>
        public const int FrontageSamplingCount = 9;

        /// <summary>
        /// Angle step for frontage sampling (degrees).
        /// </summary>
        public const int FrontageSamplingStep = 40;

        #endregion
    }
}
