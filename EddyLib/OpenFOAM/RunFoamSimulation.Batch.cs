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
        private const string MeshLogFileName = "snappyHexMesh.log";
        private const string SimulationLogFileName = "simpleFoam.log";

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
                meshCmds.Add(WithDockerLog(
                    string.Format("mpiexec -np {0} snappyHexMesh -overwrite -parallel", runSettings.CPUs),
                    MeshLogFileName));
                meshCmds.Add("reconstructParMesh -constant");
                meshCmds.Add("renumberMesh -overwrite");
            }
            else
            {
                meshCmds.Add("blockMesh");
                meshCmds.Add("surfaceFeatures");
                meshCmds.Add(WithDockerLog("snappyHexMesh -overwrite", MeshLogFileName));
                meshCmds.Add("renumberMesh -overwrite");
            }
            meshCmds.Add("checkMesh -allGeometry -allTopology -writeSets vtk");

            var meshOnlyCmds = new List<string>(meshCmds);
            meshOnlyCmds.Add("cd " + DockerConfig.CaseMountPoint);
            for (int i = 0; i < domain.BCond.WindDirections.Count; i++)
            {
                AddDockerPolyMeshCopyCommands(meshOnlyCmds, domain.BCond.WindDirections[i]);
            }
            WriteDockerScript(scriptsDir, "run_mesh", meshOnlyCmds, workDir, "Meshing");

            // Manual helper: copy mesh/constant/polyMesh into all wind-direction cases
            var copyMeshCmds = BuildDynamicDockerMeshCopyCommands();
            WriteDockerScript(scriptsDir, "copy_mesh_to_wind_dirs", copyMeshCmds, workDir, "Copy Mesh To Wind Dirs");

            // Trees
            var treeCmds = new List<string> { "cd mesh", "topoSet", "setsToZones -noFlipMap" };
            WriteDockerScript(scriptsDir, "run_make_trees", treeCmds, workDir, "Make Trees");

            // Simulation for all wind directions (sequential)
            var simAllCmds = new List<string>();
            for (int i = 0; i < domain.BCond.WindDirections.Count; i++)
            {
                int windDir = domain.BCond.WindDirections[i];
                if (i > 0) simAllCmds.Add("cd " + DockerConfig.CaseMountPoint);
                AddDockerPolyMeshCopyCommands(simAllCmds, windDir);
                simAllCmds.Add(string.Format("cd {0}", windDir));
                AddSimulationCommands(simAllCmds, runSettings);
            }
            WriteDockerScript(scriptsDir, "run_sim_all", simAllCmds, workDir, "Simulation (all directions)");

            // Mesh + Sim combined
            var runAllCmds = new List<string>();
            runAllCmds.AddRange(meshCmds);
            runAllCmds.Add("cd " + DockerConfig.CaseMountPoint);
            runAllCmds.AddRange(simAllCmds);
            WriteDockerScript(scriptsDir, "run", runAllCmds, workDir, "Mesh + Simulation");
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
                    AddDockerPolyMeshCopyCommands(cmds, windDir);
                    cmds.Add(string.Format("cd {0}", windDir));
                    AddSimulationCommands(cmds, runSettings);
                    WriteDockerScript(scriptsDir, string.Format("{0}_run_sim", windDir), cmds, workDir,
                        string.Format("Simulation (dir {0})", windDir));
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

        private static void AddDockerPolyMeshCopyCommands(List<string> cmds, int windDir)
        {
            string source = string.Format("{0}/mesh/constant/polyMesh", DockerConfig.CaseMountPoint);
            string targetConstant = string.Format("{0}/{1}/constant", DockerConfig.CaseMountPoint, windDir);
            string target = string.Format("{0}/polyMesh", targetConstant);

            cmds.Add(string.Format("if [ ! -d \"{0}\" ]; then echo \"ERROR: Mesh source not found at {0}\"; exit 1; fi", source));
            cmds.Add(string.Format("mkdir -p \"{0}\"", targetConstant));
            cmds.Add(string.Format("rm -rf \"{0}\"", target));
            cmds.Add(string.Format("cp -r \"{0}\" \"{1}\"", source, target));
        }

        private static List<string> BuildDynamicDockerMeshCopyCommands()
        {
            string caseRoot = DockerConfig.CaseMountPoint;
            string source = string.Format("{0}/mesh/constant/polyMesh", caseRoot);

            return new List<string>
            {
                string.Format("if [ ! -d \"{0}\" ]; then echo \"ERROR: Mesh source not found at {0}\"; exit 1; fi", source),
                "FOUND=0",
                string.Format("for d in \"{0}\"/*; do n=$(basename \"$d\"); if [ -d \"$d\" ] && [[ \"$n\" =~ ^[0-9]+$ ]]; then mkdir -p \"$d/constant\"; rm -rf \"$d/constant/polyMesh\"; cp -r \"{1}\" \"$d/constant/polyMesh\"; echo \"Copied mesh to $n\"; FOUND=1; fi; done", caseRoot, source),
                "if [ \"$FOUND\" -eq 0 ]; then echo \"No integer wind-direction folders found under /case.\"; fi"
            };
        }

        private static void AddSimulationCommands(List<string> cmds, OFRunSettings runSettings)
        {
            if (runSettings.CPUs > 1)
            {
                cmds.Add("decomposePar -force");
                if (runSettings.potentialFoamInit)
                    cmds.Add(string.Format("mpiexec -np {0} potentialFoam -parallel", runSettings.CPUs));
                cmds.Add(WithDockerLog(
                    string.Format("mpiexec -np {0} simpleFoam -parallel", runSettings.CPUs),
                    SimulationLogFileName));
                cmds.Add("reconstructPar -latestTime");
            }
            else
            {
                if (runSettings.potentialFoamInit)
                    cmds.Add("potentialFoam");
                cmds.Add(WithDockerLog("simpleFoam", SimulationLogFileName));
            }
        }

        private static string WithDockerLog(string command, string logFileName)
        {
            if (string.IsNullOrWhiteSpace(command))
                return command;

            if (string.IsNullOrWhiteSpace(logFileName))
                return command;

            return string.Format("{0} > >(tee -a \"{1}\") 2>&1", command, logFileName);
        }

        private static void WriteDockerScript(string scriptsDir, string baseName, IReadOnlyList<string> commands, string workDir, string title)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var scriptName = baseName + ".sh";
                var scriptPath = Path.Combine(scriptsDir, scriptName);
                DictFileWriter.WriteDict(scriptPath,
                    DockerBatchScriptBuilder.BuildDockerContainerScript(commands));

                var containerScriptPath = DockerConfig.CaseMountPoint + "/Scripts/" + scriptName;
                DictFileWriter.WriteBatchFile(scriptsDir, baseName + ".bat",
                    DockerBatchScriptBuilder.BuildDockerBatWrapperForScript(containerScriptPath, workDir));
            }
            else
            {
                DictFileWriter.WriteCommandFile(scriptsDir, baseName + ".command",
                    DockerRunner.BuildCommandFileContent(commands, workDir, title));
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
