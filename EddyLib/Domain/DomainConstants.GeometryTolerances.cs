namespace EddyLib.Domain
{
    /// <summary>
    /// Constants used in domain calculations.
    /// Centralizes magic numbers for maintainability and configuration.
    /// </summary>
    public static partial class DomainConstants
    {
        #region Geometry Tolerances

        /// <summary>
        /// Default mesh tolerance.
        /// </summary>
        public const double DefaultMeshTolerance = 0.01;

        /// <summary>
        /// Weld angle for mesh vertices (radians).
        /// </summary>
        public const double WeldAngle = System.Math.PI;

        /// <summary>
        /// Ray length for intersection tests.
        /// </summary>
        public const double DefaultRayLength = 9999.0;

        #endregion
    }
}
