using EddyLib.OutdoorComfort.Metrics;
using EddyLib.OutdoorComfort;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfort
    {
        public double[] ValuesPedestrianWindComfort;

        public WindComfort(WindFactorsAnnual wf, WindComfortHelper.PCIdx pcidxx)
        {
            this.ValuesPedestrianWindComfort = CalcPedestrianComfort(wf.ValuesTemporal, pcidxx);
        }

        private double[] CalcPedestrianComfort(double[,] ValuesWindFactors, WindComfortHelper.PCIdx cmftidx)
        {
            int sensorPointCount = ValuesWindFactors.GetLength(1);

            var PedestrianWindComfort = new double[sensorPointCount];

            for (int probe = 0; probe < sensorPointCount; probe++)
            {
                // column is all hours of the year
                var column = ArrayHelper.CustomArray<double>.GetColumn(ValuesWindFactors, probe);

                if (cmftidx == PCIdx.Davenport)
                {
                    PedestrianWindComfort[probe] = WindComfortMetricsCounting.CalcDavenportComfort(column);
                }
                else if (cmftidx == PCIdx.LawsonGeneral)
                {
                    PedestrianWindComfort[probe] = WindComfortMetricsCounting.CalcLawsonGeneralComfort(column);
                }
                else if (cmftidx == PCIdx.LawsonLDDC)
                {
                    PedestrianWindComfort[probe] = WindComfortMetricsCounting.CalcLawsonLDDCComfort(column);
                }
                else if (cmftidx == PCIdx.Lawson2001)
                {
                    PedestrianWindComfort[probe] = WindComfortMetricsCounting.CalcLawson2001Comfort(column);
                }
                else
                {
                    PedestrianWindComfort[probe] = WindComfortMetricsCounting.CalcNEN8100Comfort(column);
                }
            }

            return PedestrianWindComfort;
        }
    }
}