using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

            for (int probe = 0; probe < sensorCount; probe++)
            {
                // column is all hours of the year
                double[] column = ArrayHelper.CustomArray<double>.GetColumn(wft.ValuesTemporalAtProbingHeight, probe);

                // Move to 10m according to Blocken

                // column = column.Select(x => EddyLib.BCs.BoundaryCondition.ScaleABL(x, 1.75, ws.BCond.z0, 10)).ToArray();

                this.ThresholdInfo[probe] = WindComfortMetricsCounting.CalcComfortCountBins(column, TID);
                this.ValsPedWindCmftCat[probe] = ThresholdInfo[probe].Cat;
                this.ValsPedWindCmftClassStringified[probe] = ThresholdInfo[probe].Class;
                this.ValsPedWindCmftClassLetter[probe] = ThresholdInfo[probe].ClassLetter;
            }
        }
    }
}