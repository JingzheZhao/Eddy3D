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

        private const string BlueCfdRoot = @"C:\Program Files\blueCFD-Core-2020";
        private static readonly string BlueCfdGnuplotPath = Path.Combine(BlueCfdRoot, "msys64", "mingw64", "bin", "gnuplot.exe");
        private static readonly string BlueCfdParaviewPath = Path.Combine(BlueCfdRoot, "AddOns", "ParaView", "bin", "paraview.exe");
        private const string WindowsGnuplotPath = @"C:\Program Files\gnuplot\bin\gnuplot.exe";
        private const string MpiWindows32Path = @"C:\Windows\System32\msmpi.dll";
        private static readonly string MpiBlueCfdPath = Path.Combine(BlueCfdRoot, "ThirdParty-8", "platforms", "mingw_w64Gcc", "MS-MPI-7.1", "bin", "msmpi.dll");

        #endregion
    }
}
