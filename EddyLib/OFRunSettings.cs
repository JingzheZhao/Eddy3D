using Microsoft.VisualBasic.Devices;
using System;

namespace EddyLib
{
    public enum SimEngine
    {
        Docker,
        BlueCFD
    }
    public enum OSType
    {
        Windows10,
        Windows7,
        Linux,
        MaxOS
    }
    public enum TurbModel
    {
        kEpsilon,
        kOmegaSST,       
        RNGkEpsilon
    }

    public class OFRunSettings
    {
        public int iter = 1000;
        public int writeInterval = 10;
        public int keepTimeSteps = 2;
        public int Schemes = 0;
        public int turb = 0;
        public int CPUs = 1;
        public SimEngine simEngine = SimEngine.BlueCFD;
        public OSType ostype = OSType.Windows10;
        public TurbModel turbModel = TurbModel.kEpsilon;
        public int totalGBRam = Convert.ToInt32((new ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 2))) + 0.5);

        public override string ToString()
        {
            return String.Format(@"iter = {0}
writeInterval = {1}
keepTimeSteps = {2}
Scheme = {3}
turb = {4}
CPUs = {5}
Engine = {6}
OS = {7}
Turbulence Model = {8}", iter, writeInterval, keepTimeSteps, Schemes, turb, CPUs, simEngine.ToString(), ostype.ToString(), turbModel);

        }
    }
}