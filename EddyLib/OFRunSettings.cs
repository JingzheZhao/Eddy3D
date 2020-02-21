using System;
using System.IO;

namespace EddyLib
{
    public enum OSType
    {
        Windows10,
        Windows7,
        Linux,
        MaxOS
    }

    public enum RelaxationFactors
    {
        OpenFOAM,
        Fluent,
        OpenFOAMRobust
    }

    public enum SimEngine
    {
        Docker,
        BlueCFD
    }

    public enum TurbModel
    {
        laminar,
        kEpsilon,
        kOmegaSST,
        RNGkEpsilon
    }

    public class OFRunSettings
    {
        public bool BlueCFDIsInstalled;
        public bool WindowsGnuplotInstalled;

        public int CPUs;
        public bool IdenticalMPI;
        public bool Is64BitOS;
        public bool potentialFoamInit;
        public bool renumberMesh;
        public int iter;
        public int keepTimeSteps;
        public OSType ostype;
        public RelaxationFactors relaxationFactors;
        public int Schemes;
        public SimEngine simEngine;

        //public int totalGBRam;
        public TurbModel turbModel;

        public int writeInterval;

        public OFRunSettings(
            int iter = 1000,
            int writeInterval = 10,
            int keepTimeSteps = 3,
            int Schemes = 0,
            int CPUs = 1,
            SimEngine simEngine = SimEngine.BlueCFD,
            OSType ostype = OSType.Windows10,
            TurbModel turbmodel = TurbModel.kEpsilon,
            RelaxationFactors relaxationFactors = RelaxationFactors.Fluent,
            bool potentialFoamInit = false,
            bool renumberMesh = true
            )
        {
            this.iter = iter;
            this.writeInterval = writeInterval;
            this.keepTimeSteps = keepTimeSteps;
            this.Schemes = Schemes;
            this.CPUs = CPUs;
            this.simEngine = simEngine;
            this.ostype = ostype;
            this.turbModel = turbmodel;
            //this.totalGBRam = Convert.ToInt32((new ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 2))) + 0.5);
            this.relaxationFactors = relaxationFactors;
            this.Is64BitOS = Environment.Is64BitOperatingSystem;
            this.BlueCFDIsInstalled = CheckIfBlueCFDIsInstalled();
            this.WindowsGnuplotInstalled = CheckIfWinGnuplotISInstalled();
            this.IdenticalMPI = CheckForProperMPIVersions(BlueCFDIsInstalled, Is64BitOS);
            this.potentialFoamInit = potentialFoamInit;
            this.renumberMesh = renumberMesh;
        }

        public override string ToString()
        {
            return String.Format(@"
iter = {0}
writeInterval = {1}
keepTimeSteps = {2}
Scheme = {3}
turb = {4}
CPUs = {5}
Engine = {6}
OS = {7}
Turbulence Model = {8}
Relaxation Factors = {9}
potentialFoam initialization = {10}"
, iter.ToString(), writeInterval.ToString(), keepTimeSteps.ToString(), Schemes.ToString(), turbModel.ToString(), CPUs.ToString(), simEngine.ToString(), ostype.ToString(), turbModel.ToString(), relaxationFactors.ToString(), potentialFoamInit.ToString());
        }

        private bool CheckForProperMPIVersions(bool BlueCFDInstalled, bool Is64BitOS)
        {
            bool MPIIdentical = false;

            if (BlueCFDInstalled)
            {
                //string MPIWindows64 = @"C:\Windows\SysWOW64\msmpi.dll";
                string MPIWindows32 = @"C:\Windows\System32\msmpi.dll";
                //string MPIWindows = Is64BitOS == true ? MPIWindows64 : MPIWindows32;

                string MPIBlueCFD = @"C:\Program Files\blueCFD-Core-2017\ThirdParty-5.x\platforms\mingw_w64Gcc\MS-MPI-7.1\bin\msmpi.dll";

                FileInfo FileVol1 = new FileInfo(MPIWindows32);
                string fileLength1 = FileVol1.Length.ToString();
                string length1 = string.Empty;

                FileInfo FileVol2 = new FileInfo(MPIBlueCFD);
                string fileLength2 = FileVol2.Length.ToString();
                string length2 = string.Empty;

                if (length1 == length2)
                {
                    MPIIdentical = true;
                }
                // Size should be 1300688 bytes
            }

            return MPIIdentical;
        }

        private bool CheckIfBlueCFDIsInstalled()
        {
            bool IsBlueCFDInstalled = false;

            var pathGnuplotBlueCFD = @"C:\Program Files\blueCFD-Core-2017\msys64\mingw64\bin\gnuplot.exe";
            var pathParaviewBlueCFD = @"C:\Program Files\blueCFD-Core-2017\AddOns\ParaView\bin\paraview.exe";

            if (File.Exists(pathGnuplotBlueCFD) && File.Exists(pathParaviewBlueCFD))
            {
                IsBlueCFDInstalled = true;
            }
            return IsBlueCFDInstalled;
        }

        private bool CheckIfWinGnuplotISInstalled()
        {
            bool installed = false;

            string Wingnuplotpath = @"C:\Program Files\gnuplot\bin\gnuplot.exe";

            if (File.Exists(Wingnuplotpath))
            {
                installed = true;
            }

            return installed;
        }
    }
}