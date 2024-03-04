using System.Collections.Generic;
using System.Linq;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort
{
    internal class WindComfortMetricsCounting
    {
        private static bool CheckExceedance(double[] annualVelocity, CmftThresholdInfo THI)
        {
            if (THI.Operator == CompOperator.G)
            {
                return annualVelocity.Where(num => num > THI.UThres).Count() > THI.TimeThres * 8760;
            }
            else if (THI.Operator == CompOperator.GOE)
            {
                return annualVelocity.Where(num => num > THI.UThres).Count() >= THI.TimeThres * 8760;
            }
            else
            {
                return annualVelocity.Where(num => num > THI.UThres).Count() < THI.TimeThres * 8760;
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