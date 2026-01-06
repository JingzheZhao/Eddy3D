namespace EddyLib.Domain
{
    /// <summary>
    /// Constants used in domain calculations.
    /// Centralizes magic numbers for maintainability and configuration.
    /// </summary>
    public static partial class DomainConstants
    {
        #region Box Domain

        /// <summary>
        /// Downstream length multiplier relative to building height.
        /// </summary>
        public const double DownstreamMultiplier = 15.0;

        /// <summary>
        /// Upstream length multiplier relative to building height.
        /// </summary>
        public const double UpstreamMultiplier = 5.0;

        #endregion
    }
}
