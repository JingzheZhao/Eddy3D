using System;

namespace EddyLib
{
    /// <summary>
    /// Configuration settings for OpenFOAM simulation runs.
    /// </summary>
    public partial class OFRunSettings
    {
        #region Simulation Settings

        /// <summary>Number of CPUs for parallel execution (-1 = auto: 75% of physical cores).</summary>
        public int CPUs { get; set; }

        /// <summary>Whether to initialize with potentialFoam.</summary>
        public bool potentialFoamInit { get; set; }

        /// <summary>Whether to calculate age of air.</summary>
        public bool aoa_domain { get; set; }

        /// <summary>Number of iterations to run.</summary>
        public int endTime { get; set; }

        /// <summary>Number of timesteps to keep.</summary>
        public int purgeWrite { get; set; }

        /// <summary>Relaxation factor preset.</summary>
        public RelaxationFactors relaxationFactors { get; set; }

        /// <summary>Finite volume schemes preset.</summary>
        public fvSchemes schemes { get; set; }

        /// <summary>Simulation engine to use.</summary>
        public SimEngine simEngine { get; set; }

        /// <summary>Turbulence model.</summary>
        public TurbModel turbModel { get; set; }

        /// <summary>Write interval for results.</summary>
        public int writeInterval { get; set; }

        /// <summary>Whether to enable optional function-object field limiters for stability.</summary>
        public bool stabilityLimiters { get; set; }

        #endregion
    }
}
