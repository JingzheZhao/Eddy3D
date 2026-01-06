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
        #region Constant Files

        private static void WriteConstantFiles(CasePaths paths, OFRunSettings runSettings)
        {
            DictFileWriter.WriteDictToDir(paths.ConstantDir, "turbulenceProperties",
                Strings.OFExecDicts.TurbulenceProperties(runSettings));
            DictFileWriter.WriteDictToDir(paths.ConstantDir, "transportProperties",
                Strings.OFExecDicts.TransportProperties());
        }

        #endregion
    }
}
