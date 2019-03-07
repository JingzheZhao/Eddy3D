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
        Linux
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
        public int mode = 0;
        public int turb = 0;
        public int CPUs = 1;
        public SimEngine simEngine = SimEngine.Docker;
        public OSType ostype = OSType.Windows10;
        public TurbModel turbModel = TurbModel.kEpsilon;

        public override string ToString()
        {
            return String.Format(@"iter = {0}
writeInterval = {1}
keepTimeSteps = {2}
mode = {3}
turb = {4}
CPUs = {5}
Engine = {6}
OS = {7}
Turbulence Model = {8}", iter, writeInterval, keepTimeSteps, mode, turb, CPUs, simEngine.ToString(), ostype.ToString(), turbModel);

        }
    }
}