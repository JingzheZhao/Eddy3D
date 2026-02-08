namespace EddyLib
{
    /// <summary>
    /// Relaxation factor presets for OpenFOAM solvers.
    /// </summary>
    public enum RelaxationFactors
    {
        /// <summary>Fast convergence, may be unstable.</summary>
        Fast,

        /// <summary>Fluent-like settings.</summary>
        Fluent,

        /// <summary>More stable, slower convergence.</summary>
        Robust,

        /// <summary>Balanced settings.</summary>
        Optimized
    }

    /// <summary>
    /// Simulation engine options.
    /// </summary>
    public enum SimEngine
    {
        /// <summary>Run via Docker container.</summary>
        Docker,

        /// <summary>Run via BlueCFD-Core installation.</summary>
        BlueCFD
    }

    /// <summary>
    /// Turbulence models available in OpenFOAM.
    /// </summary>
    public enum TurbModel
    {
        /// <summary>Laminar flow (no turbulence model).</summary>
        laminar,

        /// <summary>Standard k-epsilon model.</summary>
        kEpsilon,

        /// <summary>RNG k-epsilon model.</summary>
        RNGkEpsilon,

        /// <summary>Realizable k-epsilon model.</summary>
        realizableKE,

        /// <summary>k-omega SST model.</summary>
        kOmegaSST
    }

    /// <summary>
    /// Finite volume scheme presets.
    /// </summary>
    public enum fvSchemes
    {
        /// <summary>Default OpenFOAM schemes.</summary>
        Default,

        /// <summary>Optimized schemes for urban microclimate.</summary>
        Optimized
    }
}
