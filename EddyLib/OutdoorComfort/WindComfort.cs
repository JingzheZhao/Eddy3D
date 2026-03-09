using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using static EddyLib.OutdoorComfort.WindComfortHelper;

[assembly: InternalsVisibleTo("RhinoPlugin.Tests.Xunit")]

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfort
    {
        public double[] ValsPedWindCmftCat { get; set; }

        public string[] ValsPedWindCmftClassStringified { get; set; }

        public string[] ValsPedWindCmftClassLetter { get; set; }

        public CmftThresholdInfo[] ThresholdInfo { get; set; }

        public WindComfort(WindFactorsTemporal wft, WindComfortHelper.PedCmftMetric pcidxx)
        {
            CalcPedestrianComfort(wft, pcidxx);
        }

        private void CalcPedestrianComfort(WindFactorsTemporal wft, WindComfortHelper.PedCmftMetric cmftidx)
        {
            int sensorCount = wft.ValuesTemporalAtProbingHeight.GetLength(1);

            this.ValsPedWindCmftCat = new double[sensorCount];
            this.ValsPedWindCmftClassStringified = new string[sensorCount];
            this.ValsPedWindCmftClassLetter = new string[sensorCount];
            this.ThresholdInfo = new CmftThresholdInfo[sensorCount];

            Dictionary<int, CmftThresholdInfo> TID = WindComfortMetricsWeibull.ThresholdInfo(cmftidx);

            Parallel.For(0, sensorCount, probe =>
            {
                // Move to 10m according to Blocken
                // (Optional scale step omitted for performance/design)

                var threshold = WindComfortMetricsCounting.CalcComfortCountBins(wft.ValuesTemporalAtProbingHeight, probe, TID);
                this.ThresholdInfo[probe] = threshold;
                this.ValsPedWindCmftCat[probe] = threshold.Cat;
                this.ValsPedWindCmftClassStringified[probe] = threshold.Class;
                this.ValsPedWindCmftClassLetter[probe] = threshold.ClassLetter;
            });
        }
    }
}
