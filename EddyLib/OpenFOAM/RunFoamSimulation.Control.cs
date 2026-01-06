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
        #region Control and Foam Files

        private static void WriteControlAndFoam(OFRunSettings runSettings, OFBaseDomain domain, int index, CasePaths paths)
        {
            DictFileWriter.WriteDictToDir(paths.SystemDir, "controlDict",
                Strings.OFExecDicts.ControlDict(runSettings, domain, null, index));
            DictFileWriter.WriteFoamFile(paths.CaseDir, paths.WindDir);
        }

        #endregion
    }
}
