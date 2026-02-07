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

            if (runSettings.simEngine == SimEngine.Docker)
            {
                // Docker: the .command scripts create symlinks inside the container
                // (ln -sfn works fine within the container filesystem).
                // Just ensure the constant directory exists on the host.
                Directory.CreateDirectory(paths.ConstantDir);
            }
            else
            {
                // BlueCFD on Windows: use directory junctions (symlinks)
                if (Directory.Exists(paths.PolyMeshLink) && !SymlinkCreator.IsSymbolic(paths.PolyMeshLink))
                {
                    SymlinkCreator.Delete(paths.PolyMeshLink);
                }
                SymlinkCreator.Create(paths.PolyMeshLink, target);
            }
        }

        #endregion
    }
}
