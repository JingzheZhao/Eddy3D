using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.OutdoorComfort.Metrics
{
    internal class WindComfortMetricsCounting
    {
        public static int CalcLawsonGeneralComfort(double[] annualVelocity)

        {
            // General Lawson

            //1 - A > 1.8 m / s < 2 % Sitting Long
            //2 - B > 3.6 m / s < 2 % Sitting Short
            //3 - C > 5.3 m / s < 2 % Walking Leisurely
            //4 - D > 7.6 m / s > 5 % Walking Fast
            //5 - E > 7.6 m / s >= 2 % Uncomfortable

            // Lets specify the categories from 1-5 which corresponds to A-E

            double twoPercent = 8760 * 0.01 * 0.02;

            int pedestrianComfort = 0;

            // start from the highest and start binning

            var E = annualVelocity.Where(num => num > 7.6).Count() >= twoPercent;
            var D = annualVelocity.Where(num => num > 7.6).Count() < twoPercent;
            var C = annualVelocity.Where(num => num > 5.3).Count() < twoPercent;
            var B = annualVelocity.Where(num => num > 3.6).Count() < twoPercent;
            var A = annualVelocity.Where(num => num > 1.8).Count() < twoPercent;

            if (E)
            {
                pedestrianComfort = 5;
                return pedestrianComfort;
            }
            else if (D && !E)
            {
                pedestrianComfort = 4;
                return pedestrianComfort;
            }
            else if (C && !D)
            {
                pedestrianComfort = 3;
                return pedestrianComfort;
            }
            else if (B && !C)
            {
                pedestrianComfort = 2;
                return pedestrianComfort;
            }
            else // (annualVelocity.Where(num => num > 3.6).Count() < onefive)
            {
                pedestrianComfort = 1;
                return pedestrianComfort;
            }
        }

        public static int CalcLawsonLDDCComfort(double[] annualVelocity)

        {
            // Lawson LDDC

            //1 - A > 2.5 m / s < 5 % Frequent sitting
            //2 - B > 4 m / s < 5 % Occasional sitting
            //3 - C > 6 m / s < 5 % Standing
            //4 - D > 8 m / s < 5 % Walking
            //5 - E > 8 m / s > 5 % Uncomfortable
            //6 - S > 15 m / s > 0.022 % Unsafe

            // Lets specify the categories from 1-5 which corresponds to A-E

            double fivePercent = 8760 * 0.01 * 0.05;
            double zerotwotwoPercent = 8760 * 0.01 * 0.022;

            int pedestrianComfort = 0;

            // start from the highest and start binning

            var S = annualVelocity.Where(num => num > 15).Count() > zerotwotwoPercent;
            var E = annualVelocity.Where(num => num > 8).Count() > fivePercent;
            var D = annualVelocity.Where(num => num > 8).Count() < fivePercent;
            var C = annualVelocity.Where(num => num > 6).Count() < fivePercent;
            var B = annualVelocity.Where(num => num > 4).Count() < fivePercent;
            var A = annualVelocity.Where(num => num > 2.5).Count() < fivePercent;

            if (S)
            {
                pedestrianComfort = 6;
                return pedestrianComfort;
            }
            else if (E && !S)
            {
                pedestrianComfort = 4;
                return pedestrianComfort;
            }
            else if (D && !E)
            {
                pedestrianComfort = 3;
                return pedestrianComfort;
            }
            else if (C && !D)
            {
                pedestrianComfort = 2;
                return pedestrianComfort;
            }
            else if (B && !C)
            {
                pedestrianComfort = 2;
                return pedestrianComfort;
            }
            else // (annualVelocity.Where(num => num > 3.6).Count() < onefive)
            {
                pedestrianComfort = 1;
                return pedestrianComfort;
            }
        }

        public static int CalcLawson2001Comfort(double[] annualVelocity)

        {
            //Lawson 2001

            //1 - A > 4 m / s < 5 % Sitting
            //2 - B > 6 m / s < 5 % Standing
            //3 - C > 8 m / s < 5 % Strolling
            //4 - D > 10 m / s < 5 % Business Walking
            //5 - E > 10 m / s > 5 % Uncomfortable
            //6 - S15 > 15 m / s > 0.023 % Unsafe frail
            //7 - S20 > 20 m / s > 0.023 % Unsafe all

            // Lets specify the categories from 1-7 which corresponds to A-S20

            double fivePercent = 8760 * 0.01 * 0.05;
            double zerotwothreePercent = 8760 * 0.01 * 0.023;

            int pedestrianComfort = 0;

            // start from the highest and start binning

            var S20 = annualVelocity.Where(num => num > 20).Count() > zerotwothreePercent;
            var S15 = annualVelocity.Where(num => num > 15).Count() > zerotwothreePercent;
            var E = annualVelocity.Where(num => num > 10).Count() > fivePercent;
            var D = annualVelocity.Where(num => num > 10).Count() < fivePercent;
            var C = annualVelocity.Where(num => num > 8).Count() < fivePercent;
            var B = annualVelocity.Where(num => num > 6).Count() < fivePercent;
            var A = annualVelocity.Where(num => num > 4).Count() < fivePercent;

            if (S20)
            {
                pedestrianComfort = 7;
                return pedestrianComfort;
            }
            else if (S15 && !S20)
            {
                pedestrianComfort = 6;
                return pedestrianComfort;
            }
            else if (E && !S15)
            {
                pedestrianComfort = 5;
                return pedestrianComfort;
            }
            else if (D && !E)
            {
                pedestrianComfort = 4;
                return pedestrianComfort;
            }
            else if (C && !D)
            {
                pedestrianComfort = 3;
                return pedestrianComfort;
            }
            else if (B && !C)
            {
                pedestrianComfort = 2;
                return pedestrianComfort;
            }
            else // (annualVelocity.Where(num => num > 3.6).Count() < onefive)
            {
                pedestrianComfort = 1;
                return pedestrianComfort;
            }
        }

        public static int CalcDavenportComfort(double[] annualVelocity)

        {
            // Lets specify the categories from 1-6 which corresponds to A-S
            // https://clqtg10snjb14i85u49wifbv-wpengine.netdna-ssl.com/wp-content/uploads/2019/11/Davenport.png

            double onefive = 8760 * 1.5 * 0.01;
            double one = 8760 * 0.01 * 0.01;

            int pedestrianComfort = 0;

            // start from the highest and start binning

            if (annualVelocity.Where(num => num > 15.1).Count() >= one)
            {
                pedestrianComfort = 6;
                return pedestrianComfort;
            }
            else if (annualVelocity.Where(num => num > 9.8).Count() >= onefive)
            {
                pedestrianComfort = 5;
                return pedestrianComfort;
            }
            else if (annualVelocity.Where(num => num > 9.8).Count() < onefive && annualVelocity.Where(num => num > 7.6).Count() > onefive)
            {
                pedestrianComfort = 4;
                return pedestrianComfort;
            }
            else if (annualVelocity.Where(num => num > 7.6).Count() < onefive && annualVelocity.Where(num => num > 5.3).Count() > onefive)
            {
                pedestrianComfort = 3;
                return pedestrianComfort;
            }
            else if (annualVelocity.Where(num => num > 5.3).Count() < onefive && annualVelocity.Where(num => num > 3.6).Count() > onefive)
            {
                pedestrianComfort = 2;
                return pedestrianComfort;
            }
            else // (annualVelocity.Where(num => num > 3.6).Count() < onefive)
            {
                pedestrianComfort = 1;
                return pedestrianComfort;
            }
        }

        public static int CalcNEN8100Comfort(double[] annualVelocity)

        {
            // Lets specify the categories from 1-6 which corresponds to A-S
            //  https://clqtg10snjb14i85u49wifbv-wpengine.netdna-ssl.com/wp-content/uploads/2019/11/textbox_NEN8100.png

            double year = 8760;

            int pedestrianComfort = 0;

            // start from the highest and start binning

            if (annualVelocity.Where(num => num > 15).Count() >= 0.05 * 0.01 * year)
            {
                pedestrianComfort = 6;
                return pedestrianComfort;
            }
            else if (annualVelocity.Where(num => num > 5).Count() >= 20 * 0.01 * year)
            {
                pedestrianComfort = 5;
                return pedestrianComfort;
            }
            else if (annualVelocity.Where(num => num > 5).Count() < 20 * 0.01 * year && annualVelocity.Where(num => num > 5).Count() > 10 * 0.01 * year)
            {
                pedestrianComfort = 4;
                return pedestrianComfort;
            }
            else if (annualVelocity.Where(num => num > 5).Count() < 10 * 0.01 * year && annualVelocity.Where(num => num > 5).Count() > 5 * 0.01 * year)
            {
                pedestrianComfort = 3;
                return pedestrianComfort;
            }
            else if (annualVelocity.Where(num => num > 5).Count() < 5 * 0.01 * year && annualVelocity.Where(num => num > 5).Count() > 2.5 * 0.01 * year)
            {
                pedestrianComfort = 2;
                return pedestrianComfort;
            }
            else // (annualVelocity.Where(num => num > 3.6).Count() < onefive)
            {
                pedestrianComfort = 1;
                return pedestrianComfort;
            }
        }
    }
}