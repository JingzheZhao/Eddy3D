using EddyLib.OutdoorComfort.Metrics;
using EddyLib.OutdoorComfort;
using static EddyLib.OutdoorComfort.WindComfortHelper;
using System.Collections.Generic;

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfortWeibull
    {
        public double[] ValuesPedestrianWindComfortCat { get; set; }
        public string[] ValuesPedestrianWindComfortClass { get; set; }

        public string[] ValuesPedestrianWindComfortClassLetter { get; set; }

        public UThresholdInfo[] ThresholdInfo { get; set; }

        public WindComfortWeibull(WindFactorsAnnual wa, int[] SimulatedWindDirections, PCMetric cmftMetric)
        {
            CalcPedestrianComfort(wa.ValuesTemporal, SimulatedWindDirections, cmftMetric);
        }

        private void CalcPedestrianComfort(double[,] ValuesTemporal, int[] SimulatedWindDirections, PCMetric cmftMetric)
        {
            int sensorPointCount = ValuesTemporal.GetLength(1);

            this.ValuesPedestrianWindComfortCat = new double[sensorPointCount];
            this.ValuesPedestrianWindComfortClass = new string[sensorPointCount];
            this.ValuesPedestrianWindComfortClassLetter = new string[sensorPointCount];
            this.ThresholdInfo = new UThresholdInfo[sensorPointCount];

            // WindFactorsAnnual
            // [ProbingPoints , 8760]

            // WindFactorsSpatial
            // [SimulatedWindDirs , 8760]

            Dictionary<int, UThresholdInfo> LTI = WindComfortMetricsWeibull.ThresholdInfo(cmftMetric);

            for (int probe = 0; probe < sensorPointCount; probe++)
            {
                // column is all hours from one wind direction
                var column = ArrayHelper.CustomArray<double>.GetColumn(ValuesTemporal, probe);

                this.ThresholdInfo[probe] = WindComfortMetricsWeibull.CalcComfort(column, SimulatedWindDirections, LTI);
                this.ValuesPedestrianWindComfortCat[probe] = ThresholdInfo[probe].Cat;
                this.ValuesPedestrianWindComfortClass[probe] = ThresholdInfo[probe].Class;
                this.ValuesPedestrianWindComfortClassLetter[probe] = ThresholdInfo[probe].ClassLetter;
            }
        }
    }
}