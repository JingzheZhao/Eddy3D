using EddyLib.Docker;
using EddyLib.OpenFOAM;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace EddyLib
{
    /// <summary>
    /// Generates OpenFOAM snappyHexMesh configuration files.
    /// </summary>
    public static class RunSnappy
    {
        /// <summary>
        /// Generates snappyHexMesh dictionaries and batch files.
        /// </summary>
        /// <param name="domain">Domain geometry.</param>
        /// <param name="meshSettings">Mesh generation settings.</param>
        /// <param name="runSettings">Simulation run settings.</param>
        /// <param name="logFile">Output path for log file (currently unused).</param>
        public static void Run(OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings, out string logFile)
        {
            // Resolve CPUs (-1 = auto, invalid values clamp to 1)
            if (runSettings.CPUs <= 0)
            {
                runSettings.CPUs = Utilities.CalcOptimCPU(meshSettings.meshWorkingDir, runSettings.CPUs);
            }

            var paths = OpenFOAMPaths.CreateMeshPaths(meshSettings);
            OpenFOAMPaths.EnsureMeshLog(paths.WorkingDir);

            WriteSnappyDicts(paths.SystemDir, meshSettings, runSettings, domain);
            WriteScriptFiles(paths.BaseWorkingDir, meshSettings, runSettings, domain);

            logFile = string.Empty;
        }

        private static void WriteSnappyDicts(string systemDir, OFMeshSettings meshSettings, OFRunSettings runSettings, OFBaseDomain domain)
        {
            DictFileWriter.WriteDictToDir(systemDir, "snappyHexMeshDict", Strings.OFExecDicts.SnappyHexMeshDict(meshSettings, domain));
            DictFileWriter.WriteDictToDir(systemDir, "surfaceFeaturesDict", Strings.OFExecDicts.surfaceFeaturesDict());
            DictFileWriter.WriteDictToDir(systemDir, "fvSchemes", Strings.OFExecDicts.FvSchemesDefault());
            DictFileWriter.WriteDictToDir(systemDir, "fvSolution", Strings.OFExecDicts.FvSolution(runSettings));
            DictFileWriter.WriteDictToDir(systemDir, "decomposeParDict", Strings.OFExecDicts.DecomposeParDict(runSettings));
        }

        private static void WriteScriptFiles(string baseWorkingDir, OFMeshSettings meshSettings, OFRunSettings runSettings, OFBaseDomain domain)
        {
            var scriptsDir = Path.Combine(baseWorkingDir, "Scripts");
            if (!Directory.Exists(scriptsDir)) Directory.CreateDirectory(scriptsDir);

            bool useDocker = runSettings.simEngine == SimEngine.Docker
                          || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (useDocker)
            {
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                // Docker scripts (.command on macOS, .bat on Windows)
                var checkMeshCmds = new List<string> { "cd mesh", "checkMesh" };
                var reconstructCmds = new List<string> { "cd mesh", "reconstructPar -constant -latestTime -noFields" };
                var deleteProcCmds = new List<string> { "cd mesh", "rm -rf processor*" };

                if (isWindows)
                {
                    WriteDockerWindowsScript(scriptsDir, "run_checkMesh", checkMeshCmds, baseWorkingDir);
                    WriteDockerWindowsScript(scriptsDir, "run_reconstructMesh", reconstructCmds, baseWorkingDir);
                    WriteDockerWindowsScript(scriptsDir, "delete_processor_folders", deleteProcCmds, baseWorkingDir);
                }
                else
                {
                    DictFileWriter.WriteCommandFile(scriptsDir, "run_checkMesh.command",
                        DockerRunner.BuildCommandFileContent(checkMeshCmds, baseWorkingDir, "Check Mesh"));
                    DictFileWriter.WriteCommandFile(scriptsDir, "run_reconstructMesh.command",
                        DockerRunner.BuildCommandFileContent(reconstructCmds, baseWorkingDir, "Reconstruct Mesh"));
                    DictFileWriter.WriteCommandFile(scriptsDir, "delete_processor_folders.command",
                        DockerRunner.BuildCommandFileContent(deleteProcCmds, baseWorkingDir, "Delete Processor Folders"));
                }
            }
            else
            {
                // BlueCFD .bat files (Windows only)
                DictFileWriter.WriteBatchFile(scriptsDir, "run_checkMesh.bat",
                    Strings.BatFiles.Run_checkMesh(runSettings, meshSettings, domain, Strings.OFExecutionMode.Meshing));

                DictFileWriter.WriteBatchFile(scriptsDir, "run_reconstructMesh.bat",
                    Strings.BatFiles.Run_reconstructMesh(runSettings, meshSettings, domain, Strings.OFExecutionMode.Meshing));

                DictFileWriter.WriteBatchFile(scriptsDir, "delete_processor_folders.bat",
                    Strings.BatFiles.DeleteProcessorFolders());
            }
        }

        private static void WriteDockerWindowsScript(string scriptsDir, string baseName, IReadOnlyList<string> commands, string workDir)
        {
            var scriptName = baseName + ".sh";
            var scriptPath = Path.Combine(scriptsDir, scriptName);
            DictFileWriter.WriteDict(scriptPath,
                DockerBatchScriptBuilder.BuildDockerContainerScript(commands));

            var containerScriptPath = DockerConfig.CaseMountPoint + "/Scripts/" + scriptName;
            DictFileWriter.WriteBatchFile(scriptsDir, baseName + ".bat",
                DockerBatchScriptBuilder.BuildDockerBatWrapperForScript(containerScriptPath, workDir));
        }
    }
}
