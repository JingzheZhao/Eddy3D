using System.IO;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Manages OpenFOAM directory paths for mesh and simulation cases.
    /// Consolidates path management and directory creation.
    /// </summary>
    public static partial class OpenFOAMPaths
    {
        #region Work Directory

        /// <summary>
        /// Ensures the work directory exists.
        /// </summary>
        public static void EnsureWorkDirectory(string workDir)
        {
            if (!string.IsNullOrWhiteSpace(workDir))
            {
                Directory.CreateDirectory(workDir);
            }
        }

        /// <summary>
        /// Ensures the mesh log file exists.
        /// </summary>
        public static void EnsureMeshLog(string meshWorkingDir)
        {
            if (string.IsNullOrWhiteSpace(meshWorkingDir))
            {
                return;
            }

            var logPath = Path.Combine(meshWorkingDir, "log");
            var logDir = Path.GetDirectoryName(logPath);
            
            if (!string.IsNullOrWhiteSpace(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            if (!File.Exists(logPath))
            {
                File.WriteAllText(logPath, string.Empty);
            }
        }

        #endregion
    }
}
