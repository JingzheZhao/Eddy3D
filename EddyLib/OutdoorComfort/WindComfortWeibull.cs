using EddyLib.OutdoorComfort.Metrics;
using EddyLib.OutdoorComfort;
using static EddyLib.OutdoorComfort.WindComfortHelper;
using System.Collections.Generic;

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfortWeibull
    {
        public double[] ValuesPedestrianWindComfort;

        public UThresholdInfo[] ThresholdInfo;

        public WindComfortWeibull(WindFactorsAnnual wa, int[] SimulatedWindDirections, Weather w, PCIdx cmftidx)
        {
            var EPWWindDirs = w.WindDirection;
            var EPWWsp = w.WindSpeed;

            CalcPedestrianComfort(wa.ValuesTemporal, SimulatedWindDirections, cmftidx);
        }

        private void CalcPedestrianComfort(double[,] ValuesTemporal, int[] SimulatedWindDirections, PCIdx cmftidx)
        {
            int sensorPointCount = ValuesTemporal.GetLength(1);

            var PedestrianWindComfort = new double[sensorPointCount];

            // WindFactorsAnnual
            // [ProbingPoints , 8760]

            // WindFactorsSpatial
            // [SimulatedWindDirs , 8760]

            Dictionary<int, UThresholdInfo> LTI = WindComfortMetricsWeibull.ThresholdInfo(cmftidx);

            for (int probe = 0; probe < sensorPointCount; probe++)
            {
                // column is all hours from one wind direction
                var column = ArrayHelper.CustomArray<double>.GetColumn(ValuesTemporal, probe);

                this.ThresholdInfo[probe] = WindComfortMetricsWeibull.CalcComfort(column, SimulatedWindDirections, LTI);
                this.ValuesPedestrianWindComfort[probe] = ThresholdInfo[probe].Cat;
            }
        }
    }
}