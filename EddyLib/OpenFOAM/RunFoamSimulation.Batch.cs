using EddyLib.BCs;
using EddyLib.Docker;
using EddyLib.OpenFOAM;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

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

            // On macOS: always generate .command files, never .bat
            bool useDocker = runSettings.simEngine == SimEngine.Docker
                          || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (useDocker)
            {
                WriteDockerCommandFiles(scriptsDir, workDir, domain, meshSettings, runSettings);
            }
            else
            {
                WriteBlueCfdBatchFiles(scriptsDir, domain, meshSettings, runSettings);
            }
        }

        private static void WriteBlueCfdBatchFiles(string scriptsDir, OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings)
        {
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

        private static void WriteDockerCommandFiles(string scriptsDir, string workDir, OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings)
        {
            // Meshing
            var meshCmds = new List<string> { "cd mesh" };
            if (runSettings.CPUs > 1)
            {
                meshCmds.Add("blockMesh");
                meshCmds.Add("surfaceFeatures");
                meshCmds.Add("decomposePar -force");
                meshCmds.Add(string.Format("mpiexec -np {0} snappyHexMesh -overwrite -parallel", runSettings.CPUs));
                meshCmds.Add("reconstructParMesh -constant");
                meshCmds.Add("renumberMesh -overwrite");
            }
            else
            {
                meshCmds.Add("blockMesh");
                meshCmds.Add("surfaceFeatures");
                meshCmds.Add("snappyHexMesh -overwrite");
                meshCmds.Add("renumberMesh -overwrite");
            }
            DictFileWriter.WriteCommandFile(scriptsDir, "run_mesh.command",
                DockerRunner.BuildCommandFileContent(meshCmds, workDir, "Meshing"));

            // Trees
            var treeCmds = new List<string> { "cd mesh", "topoSet", "setsToZones -noFlipMap" };
            DictFileWriter.WriteCommandFile(scriptsDir, "run_make_trees.command",
                DockerRunner.BuildCommandFileContent(treeCmds, workDir, "Make Trees"));

            // Simulation for all wind directions (sequential)
            var simAllCmds = new List<string>();
            for (int i = 0; i < domain.BCond.WindDirections.Count; i++)
            {
                int windDir = domain.BCond.WindDirections[i];
                if (i > 0) simAllCmds.Add("cd " + DockerConfig.CaseMountPoint);
                // Link mesh into wind direction case (symlinks work inside the container)
                simAllCmds.Add(string.Format("ln -sfn {0}/mesh/constant/polyMesh {0}/{1}/constant/polyMesh", DockerConfig.CaseMountPoint, windDir));
                simAllCmds.Add(string.Format("cd {0}", windDir));
                AddSimulationCommands(simAllCmds, runSettings);
            }
            DictFileWriter.WriteCommandFile(scriptsDir, "run_sim_all.command",
                DockerRunner.BuildCommandFileContent(simAllCmds, workDir, "Simulation (all directions)"));

            // Mesh + Sim combined
            var runAllCmds = new List<string>();
            runAllCmds.AddRange(meshCmds);
            runAllCmds.Add("cd " + DockerConfig.CaseMountPoint);
            runAllCmds.AddRange(simAllCmds);
            DictFileWriter.WriteCommandFile(scriptsDir, "run.command",
                DockerRunner.BuildCommandFileContent(runAllCmds, workDir, "Mesh + Simulation"));
        }

        private static void WritePerDirectionBatchFiles(string workDir, OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings)
        {
            var scriptsDir = Path.Combine(workDir, "Scripts");
            if (!Directory.Exists(scriptsDir)) Directory.CreateDirectory(scriptsDir);

            bool useDocker = runSettings.simEngine == SimEngine.Docker
                          || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            for (int i = 0; i < domain.BCond.WindDirections.Count; i++)
            {
                int windDir = domain.BCond.WindDirections[i];
                var caseDir = Path.Combine(workDir, windDir.ToString());

                if (useDocker)
                {
                    var cmds = new List<string>();
                    // Link mesh into wind direction case (symlinks work inside the container)
                    cmds.Add(string.Format("ln -sfn {0}/mesh/constant/polyMesh {0}/{1}/constant/polyMesh", DockerConfig.CaseMountPoint, windDir));
                    cmds.Add(string.Format("cd {0}", windDir));
                    AddSimulationCommands(cmds, runSettings);
                    DictFileWriter.WriteCommandFile(scriptsDir, string.Format("{0}_run_sim.command", windDir),
                        DockerRunner.BuildCommandFileContent(cmds, workDir, string.Format("Simulation (dir {0})", windDir)));
                }
                else
                {
                    DictFileWriter.WriteBatchFile(scriptsDir, string.Format("{0}_run_sim.bat", windDir),
                        Strings.BatFiles.Run_sim(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));
                    DictFileWriter.WriteBatchFile(scriptsDir, string.Format("{0}_run_sim_continue.bat", windDir),
                        Strings.BatFiles.Run_sim_continue(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));
                    DictFileWriter.WriteBatchFile(scriptsDir, string.Format("{0}_run_postprocess_U.bat", windDir),
                        Strings.BatFiles.Run_postprocess_U(meshSettings, runSettings, domain, Strings.OFExecutionMode.Simulation, i));
                }

                WriteGnuplotScript(caseDir, windDir);
            }
        }

        private static void AddSimulationCommands(List<string> cmds, OFRunSettings runSettings)
        {
            if (runSettings.CPUs > 1)
            {
                cmds.Add("decomposePar -force");
                if (runSettings.potentialFoamInit)
                    cmds.Add(string.Format("mpiexec -np {0} potentialFoam -parallel", runSettings.CPUs));
                cmds.Add(string.Format("mpiexec -np {0} simpleFoam -parallel", runSettings.CPUs));
                cmds.Add("reconstructPar -latestTime");
            }
            else
            {
                if (runSettings.potentialFoamInit)
                    cmds.Add("potentialFoam");
                cmds.Add("simpleFoam");
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
