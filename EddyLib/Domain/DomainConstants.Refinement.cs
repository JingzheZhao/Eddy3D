namespace EddyLib.Domain
{
    /// <summary>
    /// Constants used in domain calculations.
    /// Centralizes magic numbers for maintainability and configuration.
    /// </summary>
    public static partial class DomainConstants
    {
        #region Refinement

        /// <summary>
        /// Default Z padding for refinement cylinder.
        /// </summary>
        public const double RefinementZPadding = 0.3;

        /// <summary>
        /// Minimum radius multiplier to avoid face collapse.
        /// </summary>
        public const double MinRadiusMultiplier = 1.1;

        #endregion
    }
}
