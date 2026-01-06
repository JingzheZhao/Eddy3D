namespace EddyLib.Domain
{
    /// <summary>
    /// Constants used in domain calculations.
    /// Centralizes magic numbers for maintainability and configuration.
    /// </summary>
    public static partial class DomainConstants
    {
        #region Height Calculations

        /// <summary>
        /// Default multiplier for domain height relative to building height.
        /// </summary>
        public const double DefaultHeightMultiplier = 6.0;

        /// <summary>
        /// Height multiplier for box domain (5x building height).
        /// </summary>
        public const double BoxHeightMultiplier = 5.0;

        #endregion
    }
}
