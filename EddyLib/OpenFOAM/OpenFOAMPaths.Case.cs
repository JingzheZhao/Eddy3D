using System.IO;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Manages OpenFOAM directory paths for mesh and simulation cases.
    /// Consolidates path management and directory creation.
    /// </summary>
    public static partial class OpenFOAMPaths
    {
        #region Case Paths

        /// <summary>
        /// Creates simulation case directory structure and returns paths.
        /// </summary>
        public static CasePaths CreateCasePaths(string workDir, int windDir, bool ensureDirectories = true)
        {
            var paths = new CasePaths(workDir, windDir);
            if (ensureDirectories)
            {
                paths.EnsureDirectories();
            }
            return paths;
        }

        #endregion
    }
}
