using System.Collections.Generic;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfortWeibull
    {
        public double[] ValuesPedestrianWindComfortCat { get; set; }
        public string[] ValuesPedestrianWindComfortClass { get; set; }

        public string[] ValuesPedestrianWindComfortClassLetter { get; set; }

        public UThresholdInfo[] ThresholdInfo { get; set; }

        public WindComfortWeibull(WindFactorsSpatial ws, WindFactorsTemporal wa, PCMetric cmftMetric)
        {
            CalcPedestrianComfort(ws, wa, cmftMetric);
        }

        private void CalcPedestrianComfort(WindFactorsSpatial ws, WindFactorsTemporal wa, PCMetric cmftMetric)
        {
            int sensorPointCount = wa.ValuesTemporalAtProbingHeight.GetLength(1);

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
                var column = ArrayHelper.CustomArray<double>.GetColumn(wa.ValuesTemporalAtProbingHeight, probe);

                // Move to 10m according to Blocken

                //  column = column.Select(x => EddyLib.BCs.BoundaryCondition.ScaleABL(x, 1.75, ws.BCond.z0, 10)).ToArray();

                this.ThresholdInfo[probe] = WindComfortMetricsWeibull.CalcExceedance(column, LTI);
                this.ValuesPedestrianWindComfortCat[probe] = ThresholdInfo[probe].Cat;
                this.ValuesPedestrianWindComfortClass[probe] = ThresholdInfo[probe].Class;
                this.ValuesPedestrianWindComfortClassLetter[probe] = ThresholdInfo[probe].ClassLetter;
            }
        }
    }
}