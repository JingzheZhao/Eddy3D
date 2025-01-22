using System.Collections.Generic;
using System.Linq;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfortWeibull
    {
        public double[] ValsPedWindCmftCat { get; set; }
        public string[] ValsPedWindCmftClassStringified { get; set; }
        public string[] ValsPedWindCmftClassLetter { get; set; }

        public CmftThresholdInfo[] ThresholdInfo { get; set; }

        public WindComfortWeibull(WindFactorsTemporal wft, PedCmftMetric cmftMetric)
        {
            CalcPedestrianComfort(wft, cmftMetric);
        }

        private void CalcPedestrianComfort(WindFactorsTemporal wft, PedCmftMetric cmftMetric)
        {
            int sensorCount = wft.ValuesTemporalAtProbingHeight.GetLength(1);

            this.ValsPedWindCmftCat = new double[sensorCount];
            this.ValsPedWindCmftClassStringified = new string[sensorCount];
            this.ValsPedWindCmftClassLetter = new string[sensorCount];
            this.ThresholdInfo = new CmftThresholdInfo[sensorCount];

            // WindFactorsAnnual
            // [ProbingPoints , 8760]

            // WindFactorsSpatial
            // [SimulatedWindDirs , 8760]

            Dictionary<int, CmftThresholdInfo> TID = WindComfortMetricsWeibull.ThresholdInfo(cmftMetric);

            for (int probe = 0; probe < sensorCount; probe++)
            {
                // column is all hours from one wind direction
                double[] ColumnWFA = ArrayHelper.CustomArray<double>.GetColumn(wft.ValuesTemporalAtProbingHeight, probe);

                // Move to 10m according to Blocken

                //  column = column.Select(x => EddyLib.BCs.BoundaryCondition.ScaleABL(x, 1.75, ws.BCond.z0, 10)).ToArray();

                // Make sure Weibull estimator doesn't run forever.
                // Use LINQ to check if all values are not identical
                if (ColumnWFA.Distinct().Count() == 0)
                {
                    continue;
                };

                this.ThresholdInfo[probe] = WindComfortMetricsWeibull.CalcExceedance(ColumnWFA, TID);
                this.ValsPedWindCmftCat[probe] = ThresholdInfo[probe].Cat;
                this.ValsPedWindCmftClassStringified[probe] = ThresholdInfo[probe].Class;
                this.ValsPedWindCmftClassLetter[probe] = ThresholdInfo[probe].ClassLetter;
            }
        }
    }
}