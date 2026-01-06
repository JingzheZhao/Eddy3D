namespace EddyLib.Domain
{
    /// <summary>
    /// Constants used in domain calculations.
    /// Centralizes magic numbers for maintainability and configuration.
    /// </summary>
    public static partial class DomainConstants
    {
        #region Blocking Ratio

        /// <summary>
        /// Target blocking ratio percentage for inlet sizing.
        /// </summary>
        public const double DefaultBlockingRatioPercent = 3.0;

        /// <summary>
        /// Blocking ratio divisor used in box domain calculations.
        /// </summary>
        public const double BlockingRatioDivisor = 2.0;

        #endregion
    }
}
