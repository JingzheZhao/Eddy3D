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
            var scriptsDir = Path.Combine(workDir, "Scripts");
            if (!Directory.Exists(scriptsDir)) Directory.CreateDirectory(scriptsDir);

            DictFileWriter.WriteBatchFile(scriptsDir, "delete_processor_folders.bat",
                Strings.BatFiles.DeleteProcessorFolders());

            DictFileWriter.WriteBatchFile(scriptsDir, "run_mesh.bat",
                Strings.BatFiles.Run_Mesh(runSettings, meshSettings, domain, Strings.OFExecutionMode.Meshing));
            DictFileWriter.WriteBatchFile(scriptsDir, "run.bat",
                Strings.BatFiles.Run(domain, meshSettings));
            DictFileWriter.WriteBatchFile(scriptsDir, "run_sim_all.bat",
                Strings.BatFiles.RunSimOnly(domain, meshSettings));
            DictFileWriter.WriteBatchFile(scriptsDir, "run_postprocess_U_all.bat",
                Strings.BatFiles.RunPostProcessU_Only(domain, meshSettings));
            DictFileWriter.WriteBatchFile(scriptsDir, "run_make_trees.bat",
                Strings.BatFiles.Run_Make_Trees(runSettings, meshSettings, domain, Strings.OFExecutionMode.Meshing));
            DictFileWriter.WriteBatchFile(scriptsDir, "symbolic_link_creator.bat",
                Strings.BatFiles.SymbolicLinkCreatorBatch());
            DictFileWriter.WriteBatchFile(scriptsDir, "use_all_cores.bat",
                Strings.BatFiles.UpdateCoresBatch());
            DictFileWriter.WriteBatchFile(scriptsDir, "update_cores.bat",
                Strings.BatFiles.UpdateCoresInteractiveBatch());
        }

        private static void WritePerDirectionBatchFiles(string workDir, OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings)
        {
            var scriptsDir = Path.Combine(workDir, "Scripts");
            if (!Directory.Exists(scriptsDir)) Directory.CreateDirectory(scriptsDir);

            for (int i = 0; i < domain.BCond.WindDirections.Count; i++)
            {
                int windDir = domain.BCond.WindDirections[i];
                var caseDir = Path.Combine(workDir, windDir.ToString());

                DictFileWriter.WriteBatchFile(scriptsDir, $"{windDir}_run_sim.bat",
                    Strings.BatFiles.Run_sim(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));
                DictFileWriter.WriteBatchFile(scriptsDir, $"{windDir}_run_sim_continue.bat",
                    Strings.BatFiles.Run_sim_continue(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));
                DictFileWriter.WriteBatchFile(scriptsDir, $"{windDir}_run_postprocess_U.bat",
                    Strings.BatFiles.Run_postprocess_U(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));

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
