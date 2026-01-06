using System.IO;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Manages OpenFOAM directory paths for mesh and simulation cases.
    /// Consolidates path management and directory creation.
    /// </summary>
    public static partial class OpenFOAMPaths
    {
        #region Mesh Paths

        /// <summary>
        /// Creates mesh directory structure and returns paths.
        /// </summary>
        public static MeshPaths CreateMeshPaths(OFMeshSettings meshSettings, bool ensureDirectories = true)
        {
            var paths = new MeshPaths(meshSettings);
            if (ensureDirectories)
            {
                paths.EnsureDirectories();
            }
            return paths;
        }

        #endregion
    }
}
