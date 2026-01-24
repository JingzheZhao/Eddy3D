using EddyLib.OpenFOAM;
using System.IO;

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
            // Auto-calculate CPUs if not specified
            if (runSettings.CPUs == -1)
            {
                runSettings.CPUs = Utilities.CalcOptimCPU(meshSettings.meshWorkingDir, runSettings.CPUs);
            }

            var paths = OpenFOAMPaths.CreateMeshPaths(meshSettings);
            OpenFOAMPaths.EnsureMeshLog(paths.WorkingDir);

            WriteSnappyDicts(paths.SystemDir, meshSettings, runSettings, domain);
            WriteBatchFiles(paths.BaseWorkingDir, meshSettings, runSettings, domain);

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

        private static void WriteBatchFiles(string baseWorkingDir, OFMeshSettings meshSettings, OFRunSettings runSettings, OFBaseDomain domain)
        {
            var scriptsDir = Path.Combine(baseWorkingDir, "Scripts");
            if (!Directory.Exists(scriptsDir)) Directory.CreateDirectory(scriptsDir);

            DictFileWriter.WriteBatchFile(
                scriptsDir,
                "run_checkMesh.bat",
                Strings.BatFiles.Run_checkMesh(runSettings, meshSettings, domain, Strings.OFExecutionMode.Meshing));

            DictFileWriter.WriteBatchFile(
                scriptsDir,
                "run_reconstructMesh.bat",
                Strings.BatFiles.Run_reconstructMesh(runSettings, meshSettings, domain, Strings.OFExecutionMode.Meshing));

            DictFileWriter.WriteBatchFile(
                scriptsDir, 
                "delete_processor_folders.bat", 
                Strings.BatFiles.DeleteProcessorFolders());
        }
    }
}
