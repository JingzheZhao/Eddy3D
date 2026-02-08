using EddyLib.BCs;
using EddyLib.OpenFOAM;
using System.IO;

namespace EddyLib
{
    /// <summary>
    /// Generates OpenFOAM simulation case files for each wind direction.
    /// </summary>
    public static partial class RunFoamSimulation
    {
        /// <summary>
        /// Generates complete OpenFOAM case files for all wind directions.
        /// </summary>
        /// <param name="domain">Domain geometry and boundary conditions.</param>
        /// <param name="meshSettings">Mesh generation settings.</param>
        /// <param name="runSettings">Simulation run settings.</param>
        /// <param name="workDir">Working directory for output.</param>
        public static void Run(OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings, string workDir)
        {
            if (!Utilities.CheckLicence())
            {
                return;
            }

            // Resolve CPUs (-1 = auto, invalid values clamp to 1)
            if (runSettings.CPUs <= 0)
            {
                runSettings.CPUs = Utilities.CalcOptimCPU(meshSettings.meshWorkingDir, runSettings.CPUs);
            }

            // Generate case files for each wind direction
            for (int i = 0; i < domain.BCond.WindDirections.Count; i++)
            {
                int windDir = domain.BCond.WindDirections[i];
                var paths = OpenFOAMPaths.CreateCasePaths(workDir, windDir);

                WriteControlAndFoam(runSettings, domain, i, paths);
                EnsurePolyMeshLink(paths, meshSettings, runSettings);
                WriteConstantFiles(paths, runSettings);

                Utilities.DeletePhi(meshSettings, domain);

                WriteBoundaryFiles(domain, i, paths);
                WriteSystemFiles(domain, meshSettings, runSettings, paths.SystemDir);
            }

            WriteBatchFiles(workDir, domain, meshSettings, runSettings);
            WritePerDirectionBatchFiles(workDir, domain, meshSettings, runSettings);
        }

    }
}
