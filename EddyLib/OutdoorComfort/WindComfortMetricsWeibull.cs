using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static EddyLib.OutdoorComfort.WindComfortHelper;

[assembly: InternalsVisibleTo("RhinoPlugin.Test.Xunit")]

namespace EddyLib.OutdoorComfort
{
    internal class WindComfortMetricsWeibull
    {
        public static Dictionary<int, CmftThresholdInfo> ThresholdInfo(PedCmftMetric cmftidx)
        {
            Dictionary<int, CmftThresholdInfo> UTI = new Dictionary<int, CmftThresholdInfo>();

            if (cmftidx == PedCmftMetric.LawsonGeneral)
            {
                // General Lawson

                //1 - A > 1.8 m / s < 2 % Sitting Long
                //2 - B > 3.6 m / s < 2 % Sitting Short
                //3 - C > 5.3 m / s < 2 % Walking Leisurely
                //4 - D > 7.6 m / s > 5 % Walking Fast
                //5 - E > 7.6 m / s >= 2 % Uncomfortable

                UTI.Add(1, new CmftThresholdInfo { Cat = 1, UThres = 1.8, TimeThres = Perc2Dec(2), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                UTI.Add(2, new CmftThresholdInfo { Cat = 2, UThres = 3.6, TimeThres = Perc2Dec(2), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.S });
                UTI.Add(3, new CmftThresholdInfo { Cat = 3, UThres = 5.3, TimeThres = Perc2Dec(2), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.S });
                UTI.Add(4, new CmftThresholdInfo { Cat = 4, UThres = 7.6, TimeThres = Perc2Dec(5), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.GOE });
                UTI.Add(5, new CmftThresholdInfo { Cat = 5, UThres = 7.6, TimeThres = Perc2Dec(2), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.GOE });
            }
            else if (cmftidx == PedCmftMetric.LawsonLDDC)
            {
                // Lawson LDDC

                //1 - A > 2.5 m / s < 5 % Frequent sitting
                //2 - B > 4 m / s < 5 % Occasional sitting
                //3 - C > 6 m / s < 5 % Standing
                //4 - D > 8 m / s < 5 % Walking
                //5 - E > 8 m / s > 5 % Uncomfortable
                //6 - S > 15 m / s > 0.022 % Unsafe

                UTI.Add(1, new CmftThresholdInfo { Cat = 1, UThres = 2.5, TimeThres = Perc2Dec(5), Class = "Frequent sitting", ClassLetter = "A", Operator = CompOperator.S });
                UTI.Add(2, new CmftThresholdInfo { Cat = 2, UThres = 4, TimeThres = Perc2Dec(5), Class = "Occasional sitting", ClassLetter = "B", Operator = CompOperator.S });
                UTI.Add(3, new CmftThresholdInfo { Cat = 3, UThres = 6, TimeThres = Perc2Dec(5), Class = "Standing", ClassLetter = "C", Operator = CompOperator.S });
                UTI.Add(4, new CmftThresholdInfo { Cat = 4, UThres = 8, TimeThres = Perc2Dec(5), Class = "Walking", ClassLetter = "D", Operator = CompOperator.S });
                UTI.Add(5, new CmftThresholdInfo { Cat = 5, UThres = 8, TimeThres = Perc2Dec(5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });
                UTI.Add(6, new CmftThresholdInfo { Cat = 6, UThres = 15, TimeThres = Perc2Dec(0.022), Class = "Unsafe", ClassLetter = "S", Operator = CompOperator.G });
            }
            else if (cmftidx == PedCmftMetric.Lawson2001)
            {
                //Lawson 2001

                //1 - A > 4 m / s < 5 % Sitting
                //2 - B > 6 m / s < 5 % Standing
                //3 - C > 8 m / s < 5 % Strolling
                //4 - D > 10 m / s < 5 % Business Walking
                //5 - E > 10 m / s > 5 % Uncomfortable
                //6 - S15 > 15 m / s > 0.023 % Unsafe frail
                //7 - S20 > 20 m / s > 0.023 % Unsafe all

                UTI.Add(1, new CmftThresholdInfo { Cat = 1, UThres = 4, TimeThres = Perc2Dec(5), Class = "Frequent sitting", ClassLetter = "A", Operator = CompOperator.S });
                UTI.Add(2, new CmftThresholdInfo { Cat = 2, UThres = 6, TimeThres = Perc2Dec(5), Class = "Occasional sitting", ClassLetter = "B", Operator = CompOperator.S });
                UTI.Add(3, new CmftThresholdInfo { Cat = 3, UThres = 8, TimeThres = Perc2Dec(5), Class = "Standing", ClassLetter = "C", Operator = CompOperator.S });
                UTI.Add(4, new CmftThresholdInfo { Cat = 4, UThres = 10, TimeThres = Perc2Dec(5), Class = "Walking", ClassLetter = "D", Operator = CompOperator.S });
                UTI.Add(5, new CmftThresholdInfo { Cat = 5, UThres = 10, TimeThres = Perc2Dec(5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });
                UTI.Add(6, new CmftThresholdInfo { Cat = 6, UThres = 15, TimeThres = Perc2Dec(0.023), Class = "Unsafe", ClassLetter = "S15", Operator = CompOperator.G });
                UTI.Add(7, new CmftThresholdInfo { Cat = 7, UThres = 15, TimeThres = Perc2Dec(0.023), Class = "Unsafe", ClassLetter = "S20", Operator = CompOperator.G });
            }
            else if (cmftidx == PedCmftMetric.Davenport)
            {
                UTI.Add(1, new CmftThresholdInfo { Cat = 1, UThres = 3.6, TimeThres = Perc2Dec(1.5), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                UTI.Add(2, new CmftThresholdInfo { Cat = 2, UThres = 5.3, TimeThres = Perc2Dec(1.5), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.S });
                UTI.Add(3, new CmftThresholdInfo { Cat = 3, UThres = 7.6, TimeThres = Perc2Dec(1.5), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.S });
                UTI.Add(4, new CmftThresholdInfo { Cat = 4, UThres = 9.8, TimeThres = Perc2Dec(1.5), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.S });
                UTI.Add(5, new CmftThresholdInfo { Cat = 5, UThres = 9.8, TimeThres = Perc2Dec(1.5), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.GOE });
                UTI.Add(6, new CmftThresholdInfo { Cat = 6, UThres = 15.1, TimeThres = Perc2Dec(0.01), Class = "Dangerous", ClassLetter = "S", Operator = CompOperator.GOE });
            }
            else if (cmftidx == PedCmftMetric.NEN8100Comfort)
            {
                UTI.Add(1, new CmftThresholdInfo { Cat = 1, UThres = 5, TimeThres = Perc2Dec(2.5), Class = "Sitting Long", ClassLetter = "A", Operator = CompOperator.S });
                UTI.Add(2, new CmftThresholdInfo { Cat = 2, UThres = 5, TimeThres = Perc2Dec(5), Class = "Sitting Short", ClassLetter = "B", Operator = CompOperator.S });
                UTI.Add(3, new CmftThresholdInfo { Cat = 3, UThres = 5, TimeThres = Perc2Dec(10), Class = "Walking Leisurely", ClassLetter = "C", Operator = CompOperator.S });
                UTI.Add(4, new CmftThresholdInfo { Cat = 4, UThres = 5, TimeThres = Perc2Dec(20), Class = "Walking Fast", ClassLetter = "D", Operator = CompOperator.S });
                UTI.Add(5, new CmftThresholdInfo { Cat = 5, UThres = 5, TimeThres = Perc2Dec(20), Class = "Uncomfortable", ClassLetter = "E", Operator = CompOperator.G });
            }
            else if (cmftidx == PedCmftMetric.NEN8100Safety)
            {
                UTI.Add(1, new CmftThresholdInfo { Cat = 1, UThres = 15, TimeThres = Perc2Dec(0.05), Class = "No Risk", ClassLetter = "A", Operator = CompOperator.S });
                UTI.Add(2, new CmftThresholdInfo { Cat = 2, UThres = 15, TimeThres = Perc2Dec(0.3), Class = "Limited Risk", ClassLetter = "B", Operator = CompOperator.S });
                UTI.Add(3, new CmftThresholdInfo { Cat = 3, UThres = 15, TimeThres = Perc2Dec(0.3), Class = "Dangerous", ClassLetter = "C", Operator = CompOperator.G });
            }

            return UTI;
        }

        public static CmftThresholdInfo CalcExceedance(
       double[,] temporalVelocityMatrix, int probeIndex,
       Dictionary<int, CmftThresholdInfo> CTID)
        {
            if (CTID == null || CTID.Count == 0)
                throw new ArgumentException("CTID must contain at least one threshold.", nameof(CTID));

            // Best-case scenario: "no wind, sitting is possible"
            // Heuristic: sitting has the lowest threshold (smallest UThres).
            var bestCase = CTID.Values.OrderBy(t => t.UThres).First();

            if (temporalVelocityMatrix == null || temporalVelocityMatrix.GetLength(0) == 0)
                return bestCase;

            int rowCount = temporalVelocityMatrix.GetLength(0);

            // Filter invalid values; Weibull needs strictly positive samples.
            int validCount = 0;
            for (int i = 0; i < rowCount; i++)
            {
                double value = temporalVelocityMatrix[i, probeIndex];
                if (!double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0)
                {
                    validCount++;
                }
            }

            if (validCount == 0)
                return bestCase;

            var arrToProcess = new double[validCount];
            int validIndex = 0;
            for (int i = 0; i < rowCount; i++)
            {
                double value = temporalVelocityMatrix[i, probeIndex];
                if (!double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0)
                {
                    arrToProcess[validIndex] = value;
                    validIndex++;
                }
            }

            double kappa, lambda;
            try
            {
                var estimate = Weibull.Estimate(arrToProcess);
                kappa = estimate.Shape;
                lambda = estimate.Scale;

                if (!(kappa > 0.0) || !(lambda > 0.0) || double.IsNaN(kappa) || double.IsNaN(lambda))
                    return bestCase;
            }
            catch
            {
                // Estimator failed: return best-case
                return bestCase;
            }

            // Start from highest threshold and bin down
            foreach (var TI in CTID.Values.OrderByDescending(t => t.UThres))
            {
                // Treat all wind directions with equal weight (per your ref).
                double exceedanceProbability = Prob_Exceedance(1.0, TI.UThres, kappa, lambda); // P( U > U_thres )
                bool exceedance = CheckExceedance(exceedanceProbability, TI);

                if (exceedance)
                    return TI;
            }

            // Nothing exceeded → best comfort class (sitting)
            return bestCase;
        }

        private static bool CheckExceedance(double ExceedanceProbability, CmftThresholdInfo TI)
        {
            if (TI.Operator == CompOperator.G)
            {
                return ExceedanceProbability > TI.TimeThres;
            }
            else if (TI.Operator == CompOperator.GOE)
            {
                return ExceedanceProbability >= TI.TimeThres;
            }
            else if (TI.Operator == CompOperator.S)
            {
                return ExceedanceProbability < TI.TimeThres;
            }

            return false;
        }

        //private static bool Match(int input, int compare_to)
        //{
        //    return input == compare_to;
        //}

        private static double Perc2Dec(double input)
        {
            return input / 100;
        }

        private static double Prob_Exceedance(double a_theta, double u, double k, double c)
        {
            // not in percentage
            // c = lambda
            return a_theta * Math.Exp(-Math.Pow((u / c), k));
        }
    }
}
