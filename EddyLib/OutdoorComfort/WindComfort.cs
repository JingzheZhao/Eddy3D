using EddyLib.OutdoorComfort.Metrics;

namespace EddyLib.OutdoorComfort
{
    // This is a post-processing class
    public class WindComfort
    {
        public double[] ValuesPedestrianWindComfort;

        public WindComfort(WindFactorsAnnual wf, PCIdx pcidxx)
        {
            this.ValuesPedestrianWindComfort = CalcPedestrianComfort(wf.ValuesTemporal, pcidxx);
        }

        public enum PCIdx
        {
            LawsonGeneral,

            LawsonLDDC,

            Lawson2001,

            Davenport,

            NEN8100,
        };

        private double[] CalcPedestrianComfort(double[,] ValuesWindFactors, WindComfort.PCIdx cmftidx)
        {
            int sensorPointCount = ValuesWindFactors.GetLength(1);

            var PedestrianWindComfort = new double[sensorPointCount];

            for (int probe = 0; probe < sensorPointCount; probe++)
            {
                // column is all hours of the year
                var column = ArrayHelper.CustomArray<double>.GetColumn(ValuesWindFactors, probe);

                if (cmftidx == EddyLib.OutdoorComfort.WindComfort.PCIdx.Davenport)
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcDavenportComfort(column);
                }
                else if (cmftidx == EddyLib.OutdoorComfort.WindComfort.PCIdx.LawsonGeneral)
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcLawsonGeneralComfort(column);
                }
                else if (cmftidx == EddyLib.OutdoorComfort.WindComfort.PCIdx.LawsonLDDC)
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcLawsonLDDCComfort(column);
                }
                else if (cmftidx == EddyLib.OutdoorComfort.WindComfort.PCIdx.Lawson2001)
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcLawson2001Comfort(column);
                }
                else
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcNEN8100Comfort(column);
                }
            }

            return PedestrianWindComfort;
        }
    }
}