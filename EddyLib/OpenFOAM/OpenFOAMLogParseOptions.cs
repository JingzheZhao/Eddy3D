using System;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Configuration options for parsing OpenFOAM logs.
    /// </summary>
    public sealed class OpenFOAMLogParseOptions
    {
        /// <summary>
        /// Rolling window size for average iteration durations.
        /// </summary>
        public int RollingWindow { get; set; } = 5;

        /// <summary>
        /// Total iterations for simulation logs (e.g., simpleFoam end time).
        /// </summary>
        public double? TotalIterations { get; set; }
    }
}
