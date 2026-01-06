using System;

namespace EddyLib
{
    /// <summary>
    /// Configuration settings for OpenFOAM simulation runs.
    /// </summary>
    public partial class OFRunSettings
    {

        /// <summary>
        /// Creates run settings with specified parameters.
        /// </summary>
        public OFRunSettings(
            int iter = 1000,
            int writeInterval = 10,
            int keepTimeSteps = 3,
            fvSchemes schemes = fvSchemes.Optimized,
            int CPUs = 1,
            SimEngine simEngine = SimEngine.BlueCFD,
            OSType ostype = OSType.Windows10,
            TurbModel turbmodel = TurbModel.kEpsilon,
            RelaxationFactors relaxationFactors = RelaxationFactors.Optimized,
            bool potentialFoamInit = false,
            bool aoa = false)
        {
            this.iter = iter;
            this.writeInterval = writeInterval;
            this.keepTimeSteps = keepTimeSteps;
            this.schemes = schemes;
            this.CPUs = CPUs;
            this.simEngine = simEngine;
            this.ostype = ostype;
            this.turbModel = turbmodel;
            this.relaxationFactors = relaxationFactors;
            this.potentialFoamInit = potentialFoamInit;
            this.aoa_domain = aoa;

            InitializeEnvironmentFlags();
        }

        /// <summary>
        /// Refreshes environment detection flags.
        /// </summary>
        public void RefreshEnvironmentFlags()
        {
            InitializeEnvironmentFlags();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $@"
iter = {iter}
writeInterval = {writeInterval}
keepTimeSteps = {keepTimeSteps}
fvScheme = {schemes}
turb = {turbModel}
CPUs = {CPUs}
Engine = {simEngine}
OS = {ostype}
Relaxation Factors = {relaxationFactors}
potentialFoam initialization = {potentialFoamInit}
age of air = {aoa_domain}";
        }

    }
}
