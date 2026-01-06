using System;
using System.IO;

namespace EddyLib
{
    /// <summary>
    /// Configuration settings for OpenFOAM simulation runs.
    /// </summary>
    public partial class OFRunSettings
    {
        #region Environment Flags

        /// <summary>Whether BlueCFD-Core is installed.</summary>
        public bool BlueCFDIsInstalled { get; set; }

        /// <summary>Whether BlueCFD's Gnuplot is available.</summary>
        public bool BlueCFD_GNU_Plot { get; set; }

        /// <summary>Whether Windows Gnuplot is installed.</summary>
        public bool WindowsGnuplotInstalled { get; set; }

        /// <summary>Whether MPI versions match between Windows and BlueCFD.</summary>
        public bool IdenticalMPI { get; set; }

        /// <summary>Whether the OS is 64-bit.</summary>
        public bool Is64BitOS { get; set; }

        #endregion
        private void InitializeEnvironmentFlags()
        {
            Is64BitOS = Environment.Is64BitOperatingSystem;
            BlueCFDIsInstalled = CheckIfBlueCFDIsInstalled();
            BlueCFD_GNU_Plot = CheckIfBlueCFD_GNU_Plot_IsInstalled();
            WindowsGnuplotInstalled = CheckIfWinGnuplotISInstalled();
            IdenticalMPI = CheckForProperMPIVersions(BlueCFDIsInstalled);
        }

        private bool CheckForProperMPIVersions(bool blueCfdInstalled)
        {
            if (!blueCfdInstalled)
                return false;

            if (!File.Exists(MpiWindows32Path) || !File.Exists(MpiBlueCfdPath))
                return false;

            var windowsMpiLength = new FileInfo(MpiWindows32Path).Length;
            var blueCfdMpiLength = new FileInfo(MpiBlueCfdPath).Length;

            return windowsMpiLength == blueCfdMpiLength;
        }

        private bool CheckIfBlueCFDIsInstalled()
        {
            return File.Exists(BlueCfdGnuplotPath) && File.Exists(BlueCfdParaviewPath);
        }

        private bool CheckIfBlueCFD_GNU_Plot_IsInstalled()
        {
            return File.Exists(BlueCfdGnuplotPath);
        }

        private bool CheckIfWinGnuplotISInstalled()
        {
            return File.Exists(WindowsGnuplotPath);
        }
    }
}
