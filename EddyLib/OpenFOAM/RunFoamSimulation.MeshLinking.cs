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

        private static void EnsurePolyMeshLink(CasePaths paths, OFMeshSettings meshSettings)
        {
            string target = Path.Combine(meshSettings.meshConstantDir, "polyMesh");

            if (Directory.Exists(paths.PolyMeshLink) && !SymlinkCreator.IsSymbolic(paths.PolyMeshLink))
            {
                SymlinkCreator.Delete(paths.PolyMeshLink);
            }

            SymlinkCreator.Create(paths.PolyMeshLink, target);
        }

        #endregion
    }
}
