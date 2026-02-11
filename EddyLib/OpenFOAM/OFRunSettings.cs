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
            int endTime = 1000,
            int writeInterval = 10,
            int purgeWrite = 3,
            fvSchemes schemes = fvSchemes.Optimized,
            int CPUs = -1,
            SimEngine simEngine = SimEngine.BlueCFD,
            TurbModel turbmodel = TurbModel.RNGkEpsilon,
            RelaxationFactors relaxationFactors = RelaxationFactors.Optimized,
            bool potentialFoamInit = false,
            bool aoa = false,
            bool stabilityLimiters = true,
            bool debugMode = false,
            bool simpleConsistent = false)
        {
            this.endTime = endTime;
            this.writeInterval = writeInterval;
            this.purgeWrite = purgeWrite;
            this.schemes = schemes;
            this.CPUs = CPUs;
            this.simEngine = simEngine;
            this.turbModel = turbmodel;
            this.relaxationFactors = relaxationFactors;
            this.potentialFoamInit = potentialFoamInit;
            this.aoa_domain = aoa;
            this.stabilityLimiters = stabilityLimiters;
            this.debugMode = debugMode;
            this.simpleConsistent = simpleConsistent;

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
iter = {endTime}
writeInterval = {writeInterval}
keepTimeSteps = {purgeWrite}
fvScheme = {schemes}
turb = {turbModel}
CPUs = {CPUs}
Engine = {simEngine}
Relaxation Factors = {relaxationFactors}
potentialFoam initialization = {potentialFoamInit}
age of air = {aoa_domain}
stability limiters = {stabilityLimiters}
debug diagnostics = {debugMode}
SIMPLEC (consistent) = {simpleConsistent}";
        }

    }
}
