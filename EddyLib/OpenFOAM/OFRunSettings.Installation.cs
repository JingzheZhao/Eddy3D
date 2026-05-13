using System;
using System.IO;

namespace EddyLib
{
    /// <summary>
    /// Configuration settings for OpenFOAM simulation runs.
    /// </summary>
    public partial class OFRunSettings
    {
        #region Installation Paths

        private static string BlueCfdRoot => DefaultDirectoriesAndPaths.BlueCfdDir;
        private static string BlueCfdGnuplotPath => Path.Combine(BlueCfdRoot, "msys64", "mingw64", "bin", "gnuplot.exe");
        private static string BlueCfdParaviewPath => Path.Combine(BlueCfdRoot, "AddOns", "ParaView", "bin", "paraview.exe");
        private const string WindowsGnuplotPath = @"C:\Program Files\gnuplot\bin\gnuplot.exe";
        private const string MpiWindows32Path = @"C:\Windows\System32\msmpi.dll";
        private static string MpiBlueCfdPath => FindBlueCfdMpiDll();

        private static string FindBlueCfdMpiDll()
        {
            if (string.IsNullOrWhiteSpace(BlueCfdRoot) || !Directory.Exists(BlueCfdRoot))
            {
                return string.Empty;
            }

            var candidates = new[]
            {
                Path.Combine(BlueCfdRoot, DefaultDirectoriesAndPaths.BlueCfdThirdPartyFolderName, "platforms", "mingw_w64Gcc122", DefaultDirectoriesAndPaths.BlueCfdMsMpiFolderName, "bin", "msmpi.dll"),
                Path.Combine(BlueCfdRoot, DefaultDirectoriesAndPaths.BlueCfdThirdPartyFolderName, "platforms", "mingw_w64Gcc122", "MS-MPI-10.1", "bin", "msmpi.dll")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            try
            {
                foreach (var candidate in Directory.EnumerateFiles(BlueCfdRoot, "msmpi.dll", SearchOption.AllDirectories))
                {
                    if (candidate.IndexOf(DefaultDirectoriesAndPaths.BlueCfdThirdPartyFolderName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return candidate;
                    }
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        #endregion
    }
}
