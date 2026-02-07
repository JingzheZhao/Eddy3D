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
        #region Mesh Linking

        private static void EnsurePolyMeshLink(CasePaths paths, OFMeshSettings meshSettings, OFRunSettings runSettings)
        {
            string target = Path.Combine(meshSettings.meshConstantDir, "polyMesh");

            // Ensure the parent constant/ directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(paths.PolyMeshLink));

            if (runSettings.simEngine == SimEngine.Docker)
            {
                // Docker mode uses copied meshes in each wind directory.
                // Remove legacy symlinks/junctions if present, but keep real directories.
                if (Directory.Exists(paths.PolyMeshLink) && SymlinkCreator.IsSymbolic(paths.PolyMeshLink))
                {
                    SymlinkCreator.Delete(paths.PolyMeshLink);
                }
                return;
            }

            // Remove existing non-symlink directory (stale polyMesh from a previous run)
            if (Directory.Exists(paths.PolyMeshLink) && !SymlinkCreator.IsSymbolic(paths.PolyMeshLink))
            {
                SymlinkCreator.Delete(paths.PolyMeshLink);
            }

            // On macOS: creates a relative symlink (works on host AND inside Docker bind mounts)
            // On Windows: creates a directory junction (works for BlueCFD)
            SymlinkCreator.Create(paths.PolyMeshLink, target);
        }

        #endregion
    }
}
