using System.Collections.Generic;
using System.Linq;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort
{
    internal class WindComfortMetricsCounting
    {
        private const int HoursPerYear = 8760;

        private static bool CheckExceedance(double[] annualVelocity, CmftThresholdInfo THI)
        {
            int exceedanceCount = annualVelocity.Count(num => num > THI.UThres);
            double threshold = THI.TimeThres * HoursPerYear;

            if (THI.Operator == CompOperator.G)
            {
                return exceedanceCount > threshold;
            }
            else if (THI.Operator == CompOperator.GOE)
            {
                return exceedanceCount >= threshold;
            }
            else
            {
                return exceedanceCount < threshold;
            }
        }

        public static CmftThresholdInfo CalcComfortCountBins(double[] annualVelocity, Dictionary<int, CmftThresholdInfo> CTID)
        {
            // If we can't make an estimate, let's return the best case scenario --> no wind, sitting is possible
            CmftThresholdInfo pedestrianComfort = CTID[1];

            foreach (CmftThresholdInfo TH in CTID.Values)
            {
                bool Exceedance = CheckExceedance(annualVelocity, TH);
                if (Exceedance)
                {
                    pedestrianComfort = TH;
                }
            }
            return pedestrianComfort;
        }
    }
}
