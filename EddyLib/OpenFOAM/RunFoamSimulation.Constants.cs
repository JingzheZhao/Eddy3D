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
            DictFileWriter.WriteDictToDir(paths.ConstantDir, "momentumTransport",
                Strings.OFExecDicts.MomentumTransport(runSettings));
            DictFileWriter.WriteDictToDir(paths.ConstantDir, "physicalProperties",
                Strings.OFExecDicts.PhysicalProperties());
        }

        #endregion
    }
}
