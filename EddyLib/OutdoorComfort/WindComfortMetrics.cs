using System.Collections.Generic;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort
{
    internal class WindComfortMetricsCounting
    {
        private const int HoursPerYear = 8760;

        private static bool CheckExceedance(double[] annualVelocity, CmftThresholdInfo THI)
        {
            double threshold = THI.TimeThres * HoursPerYear;
            int exceedanceCount = 0;

            if (THI.Operator == CompOperator.G)
            {
                for (int i = 0; i < annualVelocity.Length; i++)
                {
                    if (annualVelocity[i] > THI.UThres)
                    {
                        exceedanceCount++;
                        if (exceedanceCount > threshold)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            else if (THI.Operator == CompOperator.GOE)
            {
                for (int i = 0; i < annualVelocity.Length; i++)
                {
                    if (annualVelocity[i] > THI.UThres)
                    {
                        exceedanceCount++;
                        if (exceedanceCount >= threshold)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            else
            {
                for (int i = 0; i < annualVelocity.Length; i++)
                {
                    if (annualVelocity[i] > THI.UThres)
                    {
                        exceedanceCount++;
                        if (exceedanceCount >= threshold)
                        {
                            return false;
                        }
                    }
                }

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
