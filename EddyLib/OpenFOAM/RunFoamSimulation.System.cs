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
        #region System Files

        private static void WriteSystemFiles(OFBaseDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings, string systemDir)
        {
            DictFileWriter.WriteDictToDir(systemDir, "snappyHexMeshDict",
                Strings.OFExecDicts.SnappyHexMeshDict(meshSettings, domain));
            DictFileWriter.WriteDictToDir(systemDir, "surfaceFeaturesDict",
                Strings.OFExecDicts.surfaceFeaturesDict());
            DictFileWriter.WriteDictToDir(systemDir, "fvSchemes",
                Strings.OFExecDicts.FvSchemes(runSettings));
            DictFileWriter.WriteDictToDir(systemDir, "fvSolution",
                Strings.OFExecDicts.FvSolution(runSettings));
            DictFileWriter.WriteDictToDir(systemDir, "residuals",
                Strings.OFExecDicts.ResidualsDict());
            DictFileWriter.WriteDictToDir(systemDir, "decomposeParDict",
                Strings.OFExecDicts.DecomposeParDict(runSettings));
        }

        #endregion
    }
}
