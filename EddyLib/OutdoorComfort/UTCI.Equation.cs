using EddyLib.OutdoorComfort;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EddyLib
{
    public partial class UTCI
    {
        public static double CalcUTCI(double TaC, double RH, double Wsp, double mrt)
        {
            double v = Wsp;//wind speed

            double DMRT = mrt - TaC;
            double Pa = CalcPa(TaC, RH);

            /*Function value is the UTCI in degree Celsius
             !~computed by a 6th order approximating polynomial from the 4 Input paramters
             !~
             !~Input parameters(all of type DOUBLE PRECISION)
             !~-TaC       : air temperature, degree Celsius
             !~-Pa    : water vapour presure, kPa
             !~-Tmrt   : mean radiant temperature, degree Celsius
             DMRT = Tmrt - TaC
             !~-va10m  : wind speed 10 m above ground level in m / s
             !~
               !~UTCI_approx, Version a 0.002, October 2009
             !~Copyright(C) 2009  Peter Broede
             For more information, please refer to utci.org

             Additions made by Timur Dogan, Cornell AAP, ESL.

             */
            double utci_temp = 0;

            #region utci_equation

            utci_temp = TaC + (6.07562052 * Math.Pow(10, -1)) +
              (-2.27712343 * Math.Pow(10, -2)) * TaC + (8.06470249 * Math.Pow(10, -4)) * TaC * TaC + (-1.54271372 * Math.Pow(10, -4)) * TaC * TaC * TaC + (-3.24651735 * Math.Pow(10, -6)) * TaC * TaC * TaC * TaC +
              (7.32602852 * Math.Pow(10, -8)) * TaC * TaC * TaC * TaC * TaC + (1.35959073 * Math.Pow(10, -9)) * TaC * TaC * TaC * TaC * TaC * TaC + (-2.2583652) * v + (8.80326035 * Math.Pow(10, -2)) * TaC * v + (2.16844454 * Math.Pow(10, -3)) * TaC * TaC * v +
              (-1.53347087 * Math.Pow(10, -5)) * TaC * TaC * TaC * v + (-5.72983704 * Math.Pow(10, -7)) * TaC * TaC * TaC * TaC * v + (-2.55090145 * Math.Pow(10, -9)) * TaC * TaC * TaC * TaC * TaC * v + (-7.51269505 * Math.Pow(10, -1)) * v * v +
              (-4.08350271 * Math.Pow(10, -3)) * TaC * v * v + (-5.21670675 * Math.Pow(10, -5)) * TaC * TaC * v * v + (1.94544667 * Math.Pow(10, -6)) * TaC * TaC * TaC * v * v + (1.14099531 * Math.Pow(10, -8)) * TaC * TaC * TaC * TaC * v * v +
              (1.58137256 * Math.Pow(10, -1)) * v * v * v + (-6.57263143 * Math.Pow(10, -5)) * TaC * v * v * v + (2.22697524 * Math.Pow(10, -7)) * TaC * TaC * v * v * v + (-4.16117031 * Math.Pow(10, -8)) * TaC * TaC * TaC * v * v * v +
              (-1.27762753 * Math.Pow(10, -2)) * v * v * v * v + (9.66891875 * Math.Pow(10, -6)) * TaC * v * v * v * v + (2.52785852 * Math.Pow(10, -9)) * TaC * TaC * v * v * v * v + (4.56306672 * Math.Pow(10, -4)) * v * v * v * v * v +
              (-1.74202546 * Math.Pow(10, -7)) * TaC * v * v * v * v * v + (-5.91491269 * Math.Pow(10, -6)) * v * v * v * v * v * v + (3.98374029 * Math.Pow(10, -1)) * DMRT + (1.83945314 * Math.Pow(10, -4)) * TaC * DMRT +
              (-1.7375451 * Math.Pow(10, -4)) * TaC * TaC * DMRT + (-7.60781159 * Math.Pow(10, -7)) * TaC * TaC * TaC * DMRT + (3.77830287 * Math.Pow(10, -8)) * TaC * TaC * TaC * TaC * DMRT + (5.43079673 * Math.Pow(10, -10)) * TaC * TaC * TaC * TaC * TaC * DMRT +
              (-2.00518269 * Math.Pow(10, -2)) * v * DMRT + (8.92859837 * Math.Pow(10, -4)) * TaC * v * DMRT + (3.45433048 * Math.Pow(10, -6)) * TaC * TaC * v * DMRT + (-3.77925774 * Math.Pow(10, -7)) * TaC * TaC * TaC * v * DMRT +
              (-1.69699377 * Math.Pow(10, -9)) * TaC * TaC * TaC * TaC * v * DMRT + (1.69992415 * Math.Pow(10, -4)) * v * v * DMRT + (-4.99204314 * Math.Pow(10, -5)) * TaC * v * v * DMRT + (2.47417178 * Math.Pow(10, -7)) * TaC * TaC * v * v * DMRT +
              (1.07596466 * Math.Pow(10, -8)) * TaC * TaC * TaC * v * v * DMRT + (8.49242932 * Math.Pow(10, -5)) * v * v * v * DMRT + (1.35191328 * Math.Pow(10, -6)) * TaC * v * v * v * DMRT + (-6.21531254 * Math.Pow(10, -9)) * TaC * TaC * v * v * v * DMRT +
              (-4.99410301 * Math.Pow(10, -6)) * v * v * v * v * DMRT + (-1.89489258 * Math.Pow(10, -8)) * TaC * v * v * v * v * DMRT + (8.15300114 * Math.Pow(10, -8)) * v * v * v * v * v * DMRT + (7.5504309 * Math.Pow(10, -4)) * DMRT * DMRT +
              (-5.65095215 * Math.Pow(10, -5)) * TaC * DMRT * DMRT + (-4.52166564 * Math.Pow(10, -7)) * TaC * TaC * DMRT * DMRT + (2.46688878 * Math.Pow(10, -8)) * TaC * TaC * TaC * DMRT * DMRT + (2.42674348 * Math.Pow(10, -10)) * TaC * TaC * TaC * TaC * DMRT * DMRT +
              (1.5454725 * Math.Pow(10, -4)) * v * DMRT * DMRT + (5.2411097 * Math.Pow(10, -6)) * TaC * v * DMRT * DMRT + (-8.75874982 * Math.Pow(10, -8)) * TaC * TaC * v * DMRT * DMRT + (-1.50743064 * Math.Pow(10, -9)) * TaC * TaC * TaC * v * DMRT * DMRT +
              (-1.56236307 * Math.Pow(10, -5)) * v * v * DMRT * DMRT + (-1.33895614 * Math.Pow(10, -7)) * TaC * v * v * DMRT * DMRT + (2.49709824 * Math.Pow(10, -9)) * TaC * TaC * v * v * DMRT * DMRT + (6.51711721 * Math.Pow(10, -7)) * v * v * v * DMRT * DMRT +
              (1.94960053 * Math.Pow(10, -9)) * TaC * v * v * v * DMRT * DMRT + (-1.00361113 * Math.Pow(10, -8)) * v * v * v * v * DMRT * DMRT + (-1.21206673 * Math.Pow(10, -5)) * DMRT * DMRT * DMRT + (-2.1820366 * Math.Pow(10, -7)) * TaC * DMRT * DMRT * DMRT +
              (7.51269482 * Math.Pow(10, -9)) * TaC * TaC * DMRT * DMRT * DMRT + (9.79063848 * Math.Pow(10, -11)) * TaC * TaC * TaC * DMRT * DMRT * DMRT + (1.25006734 * Math.Pow(10, -6)) * v * DMRT * DMRT * DMRT + (-1.81584736 * Math.Pow(10, -9)) * TaC * v * DMRT * DMRT * DMRT +
              (-3.52197671 * Math.Pow(10, -10)) * TaC * TaC * v * DMRT * DMRT * DMRT + (-3.3651463 * Math.Pow(10, -8)) * v * v * DMRT * DMRT * DMRT + (1.35908359 * Math.Pow(10, -10)) * TaC * v * v * DMRT * DMRT * DMRT + (4.1703262 * Math.Pow(10, -10)) * v * v * v * DMRT * DMRT * DMRT +
              (-1.30369025 * Math.Pow(10, -9)) * DMRT * DMRT * DMRT * DMRT + (4.13908461 * Math.Pow(10, -10)) * TaC * DMRT * DMRT * DMRT * DMRT + (9.22652254 * Math.Pow(10, -12)) * TaC * TaC * DMRT * DMRT * DMRT * DMRT + (-5.08220384 * Math.Pow(10, -9)) * v * DMRT * DMRT * DMRT * DMRT +
              (-2.24730961 * Math.Pow(10, -11)) * TaC * v * DMRT * DMRT * DMRT * DMRT + (1.17139133 * Math.Pow(10, -10)) * v * v * DMRT * DMRT * DMRT * DMRT + (6.62154879 * Math.Pow(10, -10)) * DMRT * DMRT * DMRT * DMRT * DMRT + (4.0386326 * Math.Pow(10, -13)) * TaC * DMRT * DMRT * DMRT * DMRT * DMRT +
              (1.95087203 * Math.Pow(10, -12)) * v * DMRT * DMRT * DMRT * DMRT * DMRT + (-4.73602469 * Math.Pow(10, -12)) * DMRT * DMRT * DMRT * DMRT * DMRT * DMRT + (5.12733497) * Pa + (-3.12788561 * Math.Pow(10, -1)) * TaC * Pa + (-1.96701861 * Math.Pow(10, -2)) * TaC * TaC * Pa +
              (9.9969087 * Math.Pow(10, -4)) * TaC * TaC * TaC * Pa + (9.51738512 * Math.Pow(10, -6)) * TaC * TaC * TaC * TaC * Pa + (-4.66426341 * Math.Pow(10, -7)) * TaC * TaC * TaC * TaC * TaC * Pa + (5.48050612 * Math.Pow(10, -1)) * v * Pa +
              (-3.30552823 * Math.Pow(10, -3)) * TaC * v * Pa + (-1.6411944 * Math.Pow(10, -3)) * TaC * TaC * v * Pa + (-5.16670694 * Math.Pow(10, -6)) * TaC * TaC * TaC * v * Pa + (9.52692432 * Math.Pow(10, -7)) * TaC * TaC * TaC * TaC * v * Pa +
              (-4.29223622 * Math.Pow(10, -2)) * v * v * Pa + (5.00845667 * Math.Pow(10, -3)) * TaC * v * v * Pa + (1.00601257 * Math.Pow(10, -6)) * TaC * TaC * v * v * Pa + (-1.81748644 * Math.Pow(10, -6)) * TaC * TaC * TaC * v * v * Pa +
              (-1.25813502 * Math.Pow(10, -3)) * v * v * v * Pa + (-1.79330391 * Math.Pow(10, -4)) * TaC * v * v * v * Pa + (2.34994441 * Math.Pow(10, -6)) * TaC * TaC * v * v * v * Pa + (1.29735808 * Math.Pow(10, -4)) * v * v * v * v * Pa +
              (1.2906487 * Math.Pow(10, -6)) * TaC * v * v * v * v * Pa + (-2.28558686 * Math.Pow(10, -6)) * v * v * v * v * v * Pa + (-3.69476348 * Math.Pow(10, -2)) * DMRT * Pa + (1.62325322 * Math.Pow(10, -3)) * TaC * DMRT * Pa +
              (-3.1427968 * Math.Pow(10, -5)) * TaC * TaC * DMRT * Pa + (2.59835559 * Math.Pow(10, -6)) * TaC * TaC * TaC * DMRT * Pa + (-4.77136523 * Math.Pow(10, -8)) * TaC * TaC * TaC * TaC * DMRT * Pa + (8.6420339 * Math.Pow(10, -3)) * v * DMRT * Pa +
              (-6.87405181 * Math.Pow(10, -4)) * TaC * v * DMRT * Pa + (-9.13863872 * Math.Pow(10, -6)) * TaC * TaC * v * DMRT * Pa + (5.15916806 * Math.Pow(10, -7)) * TaC * TaC * TaC * v * DMRT * Pa + (-3.59217476 * Math.Pow(10, -5)) * v * v * DMRT * Pa +
              (3.28696511 * Math.Pow(10, -5)) * TaC * v * v * DMRT * Pa + (-7.10542454 * Math.Pow(10, -7)) * TaC * TaC * v * v * DMRT * Pa + (-1.243823 * Math.Pow(10, -5)) * v * v * v * DMRT * Pa + (-7.385844 * Math.Pow(10, -9)) * TaC * v * v * v * DMRT * Pa +
              (2.20609296 * Math.Pow(10, -7)) * v * v * v * v * DMRT * Pa + (-7.3246918 * Math.Pow(10, -4)) * DMRT * DMRT * Pa + (-1.87381964 * Math.Pow(10, -5)) * TaC * DMRT * DMRT * Pa + (4.80925239 * Math.Pow(10, -6)) * TaC * TaC * DMRT * DMRT * Pa +
              (-8.7549204 * Math.Pow(10, -8)) * TaC * TaC * TaC * DMRT * DMRT * Pa + (2.7786293 * Math.Pow(10, -5)) * v * DMRT * DMRT * Pa + (-5.06004592 * Math.Pow(10, -6)) * TaC * v * DMRT * DMRT * Pa + (1.14325367 * Math.Pow(10, -7)) * TaC * TaC * v * DMRT * DMRT * Pa +
              (2.53016723 * Math.Pow(10, -6)) * v * v * DMRT * DMRT * Pa + (-1.72857035 * Math.Pow(10, -8)) * TaC * v * v * DMRT * DMRT * Pa + (-3.95079398 * Math.Pow(10, -8)) * v * v * v * DMRT * DMRT * Pa + (-3.59413173 * Math.Pow(10, -7)) * DMRT * DMRT * DMRT * Pa +
              (7.04388046 * Math.Pow(10, -7)) * TaC * DMRT * DMRT * DMRT * Pa + (-1.89309167 * Math.Pow(10, -8)) * TaC * TaC * DMRT * DMRT * DMRT * Pa + (-4.79768731 * Math.Pow(10, -7)) * v * DMRT * DMRT * DMRT * Pa + (7.96079978 * Math.Pow(10, -9)) * TaC * v * DMRT * DMRT * DMRT * Pa +
              (1.62897058 * Math.Pow(10, -9)) * v * v * DMRT * DMRT * DMRT * Pa + (3.94367674 * Math.Pow(10, -8)) * DMRT * DMRT * DMRT * DMRT * Pa + (-1.18566247 * Math.Pow(10, -9)) * TaC * DMRT * DMRT * DMRT * DMRT * Pa + (3.34678041 * Math.Pow(10, -10)) * v * DMRT * DMRT * DMRT * DMRT * Pa +
              (-1.15606447 * Math.Pow(10, -10)) * DMRT * DMRT * DMRT * DMRT * DMRT * Pa + (-2.80626406) * Pa * Pa + (5.48712484 * Math.Pow(10, -1)) * TaC * Pa * Pa + (-3.9942841 * Math.Pow(10, -3)) * TaC * TaC * Pa * Pa +
              (-9.54009191 * Math.Pow(10, -4)) * TaC * TaC * TaC * Pa * Pa + (1.93090978 * Math.Pow(10, -5)) * TaC * TaC * TaC * TaC * Pa * Pa + (-3.08806365 * Math.Pow(10, -1)) * v * Pa * Pa + (1.16952364 * Math.Pow(10, -2)) * TaC * v * Pa * Pa +
              (4.95271903 * Math.Pow(10, -4)) * TaC * TaC * v * Pa * Pa + (-1.90710882 * Math.Pow(10, -5)) * TaC * TaC * TaC * v * Pa * Pa + (2.10787756 * Math.Pow(10, -3)) * v * v * Pa * Pa + (-6.98445738 * Math.Pow(10, -4)) * TaC * v * v * Pa * Pa +
              (2.30109073 * Math.Pow(10, -5)) * TaC * TaC * v * v * Pa * Pa + (4.1785659 * Math.Pow(10, -4)) * v * v * v * Pa * Pa + (-1.27043871 * Math.Pow(10, -5)) * TaC * v * v * v * Pa * Pa + (-3.04620472 * Math.Pow(10, -6)) * v * v * v * v * Pa * Pa +
              (5.14507424 * Math.Pow(10, -2)) * DMRT * Pa * Pa + (-4.32510997 * Math.Pow(10, -3)) * TaC * DMRT * Pa * Pa + (8.99281156 * Math.Pow(10, -5)) * TaC * TaC * DMRT * Pa * Pa + (-7.14663943 * Math.Pow(10, -7)) * TaC * TaC * TaC * DMRT * Pa * Pa +
              (-2.66016305 * Math.Pow(10, -4)) * v * DMRT * Pa * Pa + (2.63789586 * Math.Pow(10, -4)) * TaC * v * DMRT * Pa * Pa + (-7.01199003 * Math.Pow(10, -6)) * TaC * TaC * v * DMRT * Pa * Pa + (-1.06823306 * Math.Pow(10, -4)) * v * v * DMRT * Pa * Pa +
              (3.61341136 * Math.Pow(10, -6)) * TaC * v * v * DMRT * Pa * Pa + (2.29748967 * Math.Pow(10, -7)) * v * v * v * DMRT * Pa * Pa + (3.04788893 * Math.Pow(10, -4)) * DMRT * DMRT * Pa * Pa + (-6.42070836 * Math.Pow(10, -5)) * TaC * DMRT * DMRT * Pa * Pa +
              (1.16257971 * Math.Pow(10, -6)) * TaC * TaC * DMRT * DMRT * Pa * Pa + (7.68023384 * Math.Pow(10, -6)) * v * DMRT * DMRT * Pa * Pa + (-5.47446896 * Math.Pow(10, -7)) * TaC * v * DMRT * DMRT * Pa * Pa + (-3.5993791 * Math.Pow(10, -8)) * v * v * DMRT * DMRT * Pa * Pa +
              (-4.36497725 * Math.Pow(10, -6)) * DMRT * DMRT * DMRT * Pa * Pa + (1.68737969 * Math.Pow(10, -7)) * TaC * DMRT * DMRT * DMRT * Pa * Pa + (2.67489271 * Math.Pow(10, -8)) * v * DMRT * DMRT * DMRT * Pa * Pa + (3.23926897 * Math.Pow(10, -9)) * DMRT * DMRT * DMRT * DMRT * Pa * Pa +
              (-3.53874123 * Math.Pow(10, -2)) * Pa * Pa * Pa + (-2.2120119 * Math.Pow(10, -1)) * TaC * Pa * Pa * Pa + (1.55126038 * Math.Pow(10, -2)) * TaC * TaC * Pa * Pa * Pa + (-2.63917279 * Math.Pow(10, -4)) * TaC * TaC * TaC * Pa * Pa * Pa +
              (4.53433455 * Math.Pow(10, -2)) * v * Pa * Pa * Pa + (-4.32943862 * Math.Pow(10, -3)) * TaC * v * Pa * Pa * Pa + (1.45389826 * Math.Pow(10, -4)) * TaC * TaC * v * Pa * Pa * Pa + (2.1750861 * Math.Pow(10, -4)) * v * v * Pa * Pa * Pa +
              (-6.66724702 * Math.Pow(10, -5)) * TaC * v * v * Pa * Pa * Pa + (3.3321714 * Math.Pow(10, -5)) * v * v * v * Pa * Pa * Pa + (-2.26921615 * Math.Pow(10, -3)) * DMRT * Pa * Pa * Pa + (3.80261982 * Math.Pow(10, -4)) * TaC * DMRT * Pa * Pa * Pa +
              (-5.45314314 * Math.Pow(10, -9)) * TaC * TaC * DMRT * Pa * Pa * Pa + (-7.96355448 * Math.Pow(10, -4)) * v * DMRT * Pa * Pa * Pa + (2.53458034 * Math.Pow(10, -5)) * TaC * v * DMRT * Pa * Pa * Pa + (-6.31223658 * Math.Pow(10, -6)) * v * v * DMRT * Pa * Pa * Pa +
              (3.02122035 * Math.Pow(10, -4)) * DMRT * DMRT * Pa * Pa * Pa + (-4.77403547 * Math.Pow(10, -6)) * TaC * DMRT * DMRT * Pa * Pa * Pa + (1.73825715 * Math.Pow(10, -6)) * v * DMRT * DMRT * Pa * Pa * Pa + (-4.09087898 * Math.Pow(10, -7)) * DMRT * DMRT * DMRT * Pa * Pa * Pa +
              (6.14155345 * Math.Pow(10, -1)) * Pa * Pa * Pa * Pa + (-6.16755931 * Math.Pow(10, -2)) * TaC * Pa * Pa * Pa * Pa + (1.33374846 * Math.Pow(10, -3)) * TaC * TaC * Pa * Pa * Pa * Pa + (3.55375387 * Math.Pow(10, -3)) * v * Pa * Pa * Pa * Pa +
              (-5.13027851 * Math.Pow(10, -4)) * TaC * v * Pa * Pa * Pa * Pa + (1.02449757 * Math.Pow(10, -4)) * v * v * Pa * Pa * Pa * Pa + (-1.48526421 * Math.Pow(10, -3)) * DMRT * Pa * Pa * Pa * Pa + (-4.11469183 * Math.Pow(10, -5)) * TaC * DMRT * Pa * Pa * Pa * Pa +
              (-6.80434415 * Math.Pow(10, -6)) * v * DMRT * Pa * Pa * Pa * Pa + (-9.77675906 * Math.Pow(10, -6)) * DMRT * DMRT * Pa * Pa * Pa * Pa + (8.82773108 * Math.Pow(10, -2)) * Pa * Pa * Pa * Pa * Pa + (-3.01859306 * Math.Pow(10, -3)) * TaC * Pa * Pa * Pa * Pa * Pa +
              (1.04452989 * Math.Pow(10, -3)) * v * Pa * Pa * Pa * Pa * Pa + (2.47090539 * Math.Pow(10, -4)) * DMRT * Pa * Pa * Pa * Pa * Pa + (1.48348065 * Math.Pow(10, -3)) * Pa * Pa * Pa * Pa * Pa * Pa
              ;

            #endregion utci_equation

            return utci_temp;

            // eval conditions

            /*
                Color cl = new Color();

                if(conditionOfPerson == 3) cl = (System.Drawing.Color.Red);
                else if (conditionOfPerson == 2) cl = (System.Drawing.Color.DarkOrange);
                else if (conditionOfPerson == 1) cl = (System.Drawing.Color.Yellow);
                else if (conditionOfPerson == 0) cl = (System.Drawing.Color.Green);
                else if (conditionOfPerson == -1) cl = (System.Drawing.Color.Cyan);
                else if (conditionOfPerson == -2) cl = (System.Drawing.Color.Blue);
                else if (conditionOfPerson == -3) cl = (System.Drawing.Color.BlueViolet);
                else RhinoApp.WriteLine("Wrong UTCI value");

                Col = cl;
            */
        }

        private static int[,] CalcConditionOfPerson(double[,] UTCI)
        {
            int numberOfProbes = UTCI.GetLength(1);

            var Condition = new int[8760, numberOfProbes];

            Parallel.For(0, numberOfProbes, probe =>
            {
                for (int hour = 0; hour < 8760; hour++)
                {
                    Condition[hour, probe] = CalcConditionOfPerson(UTCI[hour, probe]);
                }
            });
            return Condition;
        }

        public static int CalcConditionOfPerson(double UTCI)
        {
            int cOfPerson;
            if (UTCI < -40)
            {
                cOfPerson = -5;
            }
            else if ((-40 <= UTCI) && (UTCI < -27))
            {
                cOfPerson = -4;
            }
            else if ((-27 <= UTCI) && (UTCI < -13))
            {
                cOfPerson = -3;
            }
            else if ((-13 <= UTCI) && (UTCI < 0))
            {
                cOfPerson = -2;
            }
            else if ((0 <= UTCI) && (UTCI < 9))
            {
                cOfPerson = -1;
            }
            else if ((9 <= UTCI) && (UTCI < 26))
            {
                cOfPerson = 0;
            }
            else if ((26 <= UTCI) && (UTCI < 28))
            {
                cOfPerson = 1;
            }
            else if ((28 <= UTCI) && (UTCI < 32))
            {
                cOfPerson = 2;
            }
            else if ((32 <= UTCI) && (UTCI < 38))
            {
                cOfPerson = 3;
            }
            else if ((38 <= UTCI) && (UTCI < 46))
            {
                cOfPerson = 4;
            }
            else
            {
                cOfPerson = 5;
            }

            return cOfPerson;
        }

        private static double[] CalcAnnualComfortableHours(int[,] ValuesCondition)
        {
            int numberOfProbes = ValuesCondition.GetLength(1);
            var ValuesAnnualPercentageTemp = new double[8760, numberOfProbes];
            var ValuesAnnualPercentage = new double[numberOfProbes];

            Parallel.For(0, numberOfProbes, probe =>
            {
                for (int hour = 0; hour < 8760; hour++)
                {
                    if (ValuesCondition[hour, probe] == 0)
                    {
                        ValuesAnnualPercentageTemp[hour, probe] += 1;
                    }
                }
            });

            for (int probe = 0; probe < numberOfProbes; probe++)
            {
                // Returns column of matrix aka all annual values per point
                var column = ArrayHelper.CustomArray<double>.GetColumn(ValuesAnnualPercentageTemp, probe);

                ValuesAnnualPercentage[probe] = column.Sum() / 8760;
            }

            return ValuesAnnualPercentage;
        }

    }
}
