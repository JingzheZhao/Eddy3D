using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static EddyLib.OutdoorComfort.WindComfortHelper;

namespace EddyLib.OutdoorComfort.Metrics
{
    internal class WindComfortMetricsWeibull
    {
        public static Dictionary<int, UThresholdInfo> ThresholdInfo(PCIdx cmftidx)
        {
            Dictionary<int, UThresholdInfo> UTC = new Dictionary<int, UThresholdInfo>();

            if (cmftidx == PCIdx.LawsonGeneral)
            {
                // General Lawson

                //1 - A > 1.8 m / s < 2 % Sitting Long
                //2 - B > 3.6 m / s < 2 % Sitting Short
                //3 - C > 5.3 m / s < 2 % Walking Leisurely
                //4 - D > 7.6 m / s > 5 % Walking Fast
                //5 - E > 7.6 m / s >= 2 % Uncomfortable

                UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 1.8, TimeThres = Percent(2), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 3.6, TimeThres = Percent(2), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.S });
                UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 5.3, TimeThres = Percent(2), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.S });
                UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 7.6, TimeThres = Percent(5), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.GOE });
                UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 7.6, TimeThres = Percent(2), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.GOE });

                //UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 1.8, TimeThres = Percent(2), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                //UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 3.6, TimeThres = Percent(2), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.G });
                //UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 5.3, TimeThres = Percent(2), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.G });
                //UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 7.6, TimeThres = Percent(5), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.GOE });
                //UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 7.6, TimeThres = Percent(2), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.GOE });
            }
            else if (cmftidx == PCIdx.LawsonLDDC)
            {
                // Lawson LDDC

                //1 - A > 2.5 m / s < 5 % Frequent sitting
                //2 - B > 4 m / s < 5 % Occasional sitting
                //3 - C > 6 m / s < 5 % Standing
                //4 - D > 8 m / s < 5 % Walking
                //5 - E > 8 m / s > 5 % Uncomfortable
                //6 - S > 15 m / s > 0.022 % Unsafe

                UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 2.5, TimeThres = Percent(5), Class = "Frequent sitting", ClassLetter = "A", Operator = CompOperator.S });
                UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 4, TimeThres = Percent(5), Class = "Occasional sitting", ClassLetter = "B", Operator = CompOperator.S });
                UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 6, TimeThres = Percent(5), Class = "Standing", ClassLetter = "C", Operator = CompOperator.S });
                UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 8, TimeThres = Percent(5), Class = "Walking", ClassLetter = "D", Operator = CompOperator.S });
                UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 8, TimeThres = Percent(5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });
                UTC.Add(6, new UThresholdInfo { Cat = 6, UThres = 15, TimeThres = Percent(0.022), Class = "Unsafe", ClassLetter = "S", Operator = CompOperator.G });

                //UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 2.5, TimeThres = Percent(5), Class = "Frequent sitting", ClassLetter = "A", Operator = CompOperator.S });
                //UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 4, TimeThres = Percent(5), Class = "Occasional sitting", ClassLetter = "B", Operator = CompOperator.G });
                //UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 6, TimeThres = Percent(5), Class = "Standing", ClassLetter = "C", Operator = CompOperator.G });
                //UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 8, TimeThres = Percent(5), Class = "Walking", ClassLetter = "D", Operator = CompOperator.G });
                //UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 8, TimeThres = Percent(5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });
                //UTC.Add(6, new UThresholdInfo { Cat = 6, UThres = 15, TimeThres = Percent(0.022), Class = "Unsafe", ClassLetter = "S", Operator = CompOperator.G });
            }
            else if (cmftidx == PCIdx.Lawson2001)
            {
                //Lawson 2001

                //1 - A > 4 m / s < 5 % Sitting
                //2 - B > 6 m / s < 5 % Standing
                //3 - C > 8 m / s < 5 % Strolling
                //4 - D > 10 m / s < 5 % Business Walking
                //5 - E > 10 m / s > 5 % Uncomfortable
                //6 - S15 > 15 m / s > 0.023 % Unsafe frail
                //7 - S20 > 20 m / s > 0.023 % Unsafe all

                UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 4, TimeThres = Percent(5), Class = "Frequent sitting", ClassLetter = "A", Operator = CompOperator.S });
                UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 6, TimeThres = Percent(5), Class = "Occasional sitting", ClassLetter = "B", Operator = CompOperator.S });
                UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 8, TimeThres = Percent(5), Class = "Standing", ClassLetter = "C", Operator = CompOperator.S });
                UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 10, TimeThres = Percent(5), Class = "Walking", ClassLetter = "D", Operator = CompOperator.S });
                UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 10, TimeThres = Percent(5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });
                UTC.Add(6, new UThresholdInfo { Cat = 6, UThres = 15, TimeThres = Percent(0.023), Class = "Unsafe", ClassLetter = "S15", Operator = CompOperator.G });
                UTC.Add(7, new UThresholdInfo { Cat = 7, UThres = 15, TimeThres = Percent(0.023), Class = "Unsafe", ClassLetter = "S20", Operator = CompOperator.G });

                //UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 4, TimeThres = Percent(5), Class = "Frequent sitting", ClassLetter = "A", Operator = CompOperator.S });
                //UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 6, TimeThres = Percent(5), Class = "Occasional sitting", ClassLetter = "B", Operator = CompOperator.G });
                //UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 8, TimeThres = Percent(5), Class = "Standing", ClassLetter = "C", Operator = CompOperator.G });
                //UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 10, TimeThres = Percent(5), Class = "Walking", ClassLetter = "D", Operator = CompOperator.G });
                //UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 10, TimeThres = Percent(5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });
                //UTC.Add(6, new UThresholdInfo { Cat = 6, UThres = 15, TimeThres = Percent(0.023), Class = "Unsafe", ClassLetter = "S15", Operator = CompOperator.G });
                //UTC.Add(7, new UThresholdInfo { Cat = 7, UThres = 15, TimeThres = Percent(0.023), Class = "Unsafe", ClassLetter = "S20", Operator = CompOperator.G });
            }
            else if (cmftidx == PCIdx.Davenport)
            {
                UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 3.6, TimeThres = Percent(1.5), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 5.3, TimeThres = Percent(1.5), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.S });
                UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 7.6, TimeThres = Percent(1.5), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.S });
                UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 9.8, TimeThres = Percent(1.5), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.S });
                UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 9.8, TimeThres = Percent(1.5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.GOE });
                UTC.Add(6, new UThresholdInfo { Cat = 6, UThres = 15.1, TimeThres = Percent(0.01), Class = "Dangerous", ClassLetter = "S", Operator = CompOperator.GOE });

                //UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 3.6, TimeThres = Percent(1.5), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                //UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 5.3, TimeThres = Percent(1.5), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.G });
                //UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 7.6, TimeThres = Percent(1.5), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.G });
                //UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 9.8, TimeThres = Percent(1.5), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.G });
                //UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 9.8, TimeThres = Percent(1.5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.GOE });
                //UTC.Add(6, new UThresholdInfo { Cat = 6, UThres = 15.1, TimeThres = Percent(0.01), Class = "Dangerous", ClassLetter = "S", Operator = CompOperator.GOE });
            }
            else if (cmftidx == PCIdx.NEN8100Comfort)
            {
                UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 5, TimeThres = Percent(2.5), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 5, TimeThres = Percent(5), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.S });
                UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 5, TimeThres = Percent(10), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.S });
                UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 5, TimeThres = Percent(20), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.S });
                UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 5, TimeThres = Percent(20), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });

                //UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 5, TimeThres = Percent(2.5), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                //UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 5, TimeThres = Percent(5), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.G });
                //UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 5, TimeThres = Percent(10), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.G });
                //UTC.Add(4, new UThresholdInfo { Cat = 4, UThres = 5, TimeThres = Percent(20), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.G });
                //UTC.Add(5, new UThresholdInfo { Cat = 5, UThres = 5, TimeThres = Percent(20), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });
            }
            else if (cmftidx == PCIdx.NEN8100Safety)
            {
                UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 15, TimeThres = Percent(0.05), Class = "No Risk", ClassLetter = "A", Operator = CompOperator.S });
                UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 15, TimeThres = Percent(0.3), Class = "Limited Risk", ClassLetter = "B", Operator = CompOperator.S });
                UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 15, TimeThres = Percent(0.3), Class = "Dangerous", ClassLetter = "C", Operator = CompOperator.G });

                //UTC.Add(1, new UThresholdInfo { Cat = 1, UThres = 15, TimeThres = Percent(0.05), Class = "No Risk", ClassLetter = "A", Operator = CompOperator.S });
                //UTC.Add(2, new UThresholdInfo { Cat = 2, UThres = 15, TimeThres = Percent(0.3), Class = "Limited Risk", ClassLetter = "B", Operator = CompOperator.G });
                //UTC.Add(3, new UThresholdInfo { Cat = 3, UThres = 15, TimeThres = Percent(0.3), Class = "Dangerous", ClassLetter = "C", Operator = CompOperator.G });
            }

            return UTC;
        }

        public static UThresholdInfo CalcComfort(double[] TemporalVelocityArray, int[] simulatedWindDirections, Dictionary<int, UThresholdInfo> LTI)

        {
            double ExceedanceProbability = 0.0;

            var pedestrianComfort = new UThresholdInfo { Cat = 0, Class = "Not Calculated", ClassLetter = "N/A" };

            // start from the highest and start binning
            //        Parallel.ForEach(LTI.Values.Reverse(),
            //Entry =>
            //{
            foreach (var Entry in LTI.Values.Reverse())
            {
                // Weibull estimate

                var Estimate = Weibull.Estimate(TemporalVelocityArray);

                var Kappa = Estimate.Shape;
                var Lambda = Estimate.Scale;

                // Treat all wind directions with equal weight, like suggested in https://www.sciencedirect.com/science/article/pii/S0360132316300415#fig22, eq. 14

                double PercentagePerYear = (double)1 / simulatedWindDirections.Count();
                double P_Upot_Utr10m = Prob_Exceedance(PercentagePerYear, Entry.UThres, Kappa, Lambda);

                ExceedanceProbability = P_Upot_Utr10m;

                bool Exceedance = CheckExceedance(ExceedanceProbability, Entry);

                if (Exceedance)
                {
                    pedestrianComfort = Entry;
                    return pedestrianComfort;
                }
            }

            //});
            return pedestrianComfort;
        }

        private static double Percent(double input)
        {
            return input / 100;
        }

        private static bool CheckExceedance(double ExceedanceProbability, UThresholdInfo TI)
        {
            if (TI.Operator == CompOperator.G)
            {
                return ExceedanceProbability > TI.TimeThres;
            }
            else if (TI.Operator == CompOperator.GOE)
            {
                return ExceedanceProbability >= TI.TimeThres;
            }
            else
            {
                return ExceedanceProbability < TI.TimeThres;
            }
        }

        private static bool Match(int input, int compare_to)
        {
            return input == compare_to;
        }

        private static double Prob_Exceedance(double a_theta, double u, double k, double c)
        {
            // not in percentage
            // c = lambda
            return a_theta * Math.Exp(-Math.Pow((u / c), k));
        }
    }
}