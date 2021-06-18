using System.Collections.Generic;
using System.Linq;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort
{
    internal class WindComfortMetricsCounting
    {
        private static bool CheckExceedance(double[] annualVelocity, UThresholdInfo THI)
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

        public static UThresholdInfo CalcComfortCountBins(double[] annualVelocity, Dictionary<int, UThresholdInfo> THI)

        {
            UThresholdInfo pedestrianComfort = THI[1];

            foreach (var Entry in THI.Values)
            {
                bool Exceedance = CheckExceedance(annualVelocity, Entry);
                if (Exceedance)
                {
                    pedestrianComfort = Entry;
                }
            }
            return pedestrianComfort;
        }
    }
}