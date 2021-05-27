using EddyLib.OutdoorComfort;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static EddyLib.OutdoorComfort.WindComfortHelper;

[assembly: InternalsVisibleTo("RhinoPlugin.Tests.Xunit")]

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfort
    {
        public double[] ValuesPedestrianWindComfortCat { get; set; }

        public string[] ValuesPedestrianWindComfortClass { get; set; }

        public string[] ValuesPedestrianWindComfortClassLetter { get; set; }

        public UThresholdInfo[] ThresholdInfo { get; set; }

        public WindComfort(WindFactorsSpatial ws, WindFactorsTemporal wa, WindComfortHelper.PCMetric pcidxx)
        {
            CalcPedestrianComfort(ws, wa, pcidxx);
        }

        private void CalcPedestrianComfort(WindFactorsSpatial ws, WindFactorsTemporal wa, WindComfortHelper.PCMetric cmftidx)
        {
            int sensorPointCount = wa.ValuesTemporalAtProbingHeight.GetLength(1);

            this.ValuesPedestrianWindComfortCat = new double[sensorPointCount];
            this.ValuesPedestrianWindComfortClass = new string[sensorPointCount];
            this.ValuesPedestrianWindComfortClassLetter = new string[sensorPointCount];
            this.ThresholdInfo = new UThresholdInfo[sensorPointCount];

            Dictionary<int, UThresholdInfo> LTI = WindComfortMetricsWeibull.ThresholdInfo(cmftidx);

            for (int probe = 0; probe < sensorPointCount; probe++)
            {
                // column is all hours of the year
                var column = ArrayHelper.CustomArray<double>.GetColumn(wa.ValuesTemporalAtProbingHeight, probe);

                // Move to 10m according to Blocken

                // column = column.Select(x => EddyLib.BCs.BoundaryCondition.ScaleABL(x, 1.75, ws.BCond.z0, 10)).ToArray();

                this.ThresholdInfo[probe] = WindComfortMetricsCounting.CalcComfortCountBins(column, LTI);

                this.ValuesPedestrianWindComfortCat[probe] = ThresholdInfo[probe].Cat;
                this.ValuesPedestrianWindComfortClass[probe] = ThresholdInfo[probe].Class;
                this.ValuesPedestrianWindComfortClassLetter[probe] = ThresholdInfo[probe].ClassLetter;
            }
        }
    }
}