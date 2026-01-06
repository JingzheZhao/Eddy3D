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
        #region Batch Files

        private static void WriteBatchFiles(string workDir, OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings)
        {
            DictFileWriter.WriteBatchFile(workDir, "run_mesh.bat",
                Strings.BatFiles.Run_Mesh(runSettings, meshSettings, domain, Strings.OFExecutionMode.Meshing));
            DictFileWriter.WriteBatchFile(workDir, "run.bat",
                Strings.BatFiles.Run(domain, meshSettings));
            DictFileWriter.WriteBatchFile(workDir, "run_sim_all.bat",
                Strings.BatFiles.RunSimOnly(domain, meshSettings));
            DictFileWriter.WriteBatchFile(workDir, "run_divU_all.bat",
                Strings.BatFiles.RunDivU_Only(domain, meshSettings));
            DictFileWriter.WriteBatchFile(workDir, "run_make_trees.bat",
                Strings.BatFiles.Run_Make_Trees(runSettings, meshSettings, domain, Strings.OFExecutionMode.Meshing));
            DictFileWriter.WriteBatchFile(workDir, "symbolic_link_creator.bat",
                Strings.BatFiles.SymbolicLinkCreatorBatch());
        }

        private static void WritePerDirectionBatchFiles(string workDir, OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings)
        {
            for (int i = 0; i < domain.BCond.WindDirections.Count; i++)
            {
                int windDir = domain.BCond.WindDirections[i];
                var caseDir = Path.Combine(workDir, windDir.ToString());

                DictFileWriter.WriteBatchFile(workDir, $"{windDir}_run_sim.bat",
                    Strings.BatFiles.Run_sim(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));
                DictFileWriter.WriteBatchFile(workDir, $"{windDir}_run_sim_continue.bat",
                    Strings.BatFiles.Run_sim_continue(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));
                DictFileWriter.WriteBatchFile(workDir, $"{windDir}_run_divU.bat",
                    Strings.BatFiles.Run_divU(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));

                WriteGnuplotScript(caseDir, windDir);
            }
        }

        private static void WriteGnuplotScript(string caseDir, int windDir)
        {
            string residualsPath = Path.Combine(caseDir, "postProcessing", "residuals", "0", "residuals.dat");
            string outputPng = Path.Combine(caseDir, "residuals.png");
            string gnuplotScript = Path.Combine(caseDir, "plot_residuals.plt");

            DictFileWriter.WriteDict(gnuplotScript, Strings.PlotResiduals.GenerateGnuplotScript(residualsPath, outputPng));
        }

        #endregion
    }
}
