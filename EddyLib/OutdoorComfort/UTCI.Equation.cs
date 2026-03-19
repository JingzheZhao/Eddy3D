using System;
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

            utci_temp = TaC + (6.07562052E-1) +
              (-2.27712343E-2) * TaC + (8.06470249E-4) * TaC * TaC + (-1.54271372E-4) * TaC * TaC * TaC + (-3.24651735E-6) * TaC * TaC * TaC * TaC +
              (7.32602852E-8) * TaC * TaC * TaC * TaC * TaC + (1.35959073E-9) * TaC * TaC * TaC * TaC * TaC * TaC + (-2.2583652) * v + (8.80326035E-2) * TaC * v + (2.16844454E-3) * TaC * TaC * v +
              (-1.53347087E-5) * TaC * TaC * TaC * v + (-5.72983704E-7) * TaC * TaC * TaC * TaC * v + (-2.55090145E-9) * TaC * TaC * TaC * TaC * TaC * v + (-7.51269505E-1) * v * v +
              (-4.08350271E-3) * TaC * v * v + (-5.21670675E-5) * TaC * TaC * v * v + (1.94544667E-6) * TaC * TaC * TaC * v * v + (1.14099531E-8) * TaC * TaC * TaC * TaC * v * v +
              (1.58137256E-1) * v * v * v + (-6.57263143E-5) * TaC * v * v * v + (2.22697524E-7) * TaC * TaC * v * v * v + (-4.16117031E-8) * TaC * TaC * TaC * v * v * v +
              (-1.27762753E-2) * v * v * v * v + (9.66891875E-6) * TaC * v * v * v * v + (2.52785852E-9) * TaC * TaC * v * v * v * v + (4.56306672E-4) * v * v * v * v * v +
              (-1.74202546E-7) * TaC * v * v * v * v * v + (-5.91491269E-6) * v * v * v * v * v * v + (3.98374029E-1) * DMRT + (1.83945314E-4) * TaC * DMRT +
              (-1.7375451E-4) * TaC * TaC * DMRT + (-7.60781159E-7) * TaC * TaC * TaC * DMRT + (3.77830287E-8) * TaC * TaC * TaC * TaC * DMRT + (5.43079673E-10) * TaC * TaC * TaC * TaC * TaC * DMRT +
              (-2.00518269E-2) * v * DMRT + (8.92859837E-4) * TaC * v * DMRT + (3.45433048E-6) * TaC * TaC * v * DMRT + (-3.77925774E-7) * TaC * TaC * TaC * v * DMRT +
              (-1.69699377E-9) * TaC * TaC * TaC * TaC * v * DMRT + (1.69992415E-4) * v * v * DMRT + (-4.99204314E-5) * TaC * v * v * DMRT + (2.47417178E-7) * TaC * TaC * v * v * DMRT +
              (1.07596466E-8) * TaC * TaC * TaC * v * v * DMRT + (8.49242932E-5) * v * v * v * DMRT + (1.35191328E-6) * TaC * v * v * v * DMRT + (-6.21531254E-9) * TaC * TaC * v * v * v * DMRT +
              (-4.99410301E-6) * v * v * v * v * DMRT + (-1.89489258E-8) * TaC * v * v * v * v * DMRT + (8.15300114E-8) * v * v * v * v * v * DMRT + (7.5504309E-4) * DMRT * DMRT +
              (-5.65095215E-5) * TaC * DMRT * DMRT + (-4.52166564E-7) * TaC * TaC * DMRT * DMRT + (2.46688878E-8) * TaC * TaC * TaC * DMRT * DMRT + (2.42674348E-10) * TaC * TaC * TaC * TaC * DMRT * DMRT +
              (1.5454725E-4) * v * DMRT * DMRT + (5.2411097E-6) * TaC * v * DMRT * DMRT + (-8.75874982E-8) * TaC * TaC * v * DMRT * DMRT + (-1.50743064E-9) * TaC * TaC * TaC * v * DMRT * DMRT +
              (-1.56236307E-5) * v * v * DMRT * DMRT + (-1.33895614E-7) * TaC * v * v * DMRT * DMRT + (2.49709824E-9) * TaC * TaC * v * v * DMRT * DMRT + (6.51711721E-7) * v * v * v * DMRT * DMRT +
              (1.94960053E-9) * TaC * v * v * v * DMRT * DMRT + (-1.00361113E-8) * v * v * v * v * DMRT * DMRT + (-1.21206673E-5) * DMRT * DMRT * DMRT + (-2.1820366E-7) * TaC * DMRT * DMRT * DMRT +
              (7.51269482E-9) * TaC * TaC * DMRT * DMRT * DMRT + (9.79063848E-11) * TaC * TaC * TaC * DMRT * DMRT * DMRT + (1.25006734E-6) * v * DMRT * DMRT * DMRT + (-1.81584736E-9) * TaC * v * DMRT * DMRT * DMRT +
              (-3.52197671E-10) * TaC * TaC * v * DMRT * DMRT * DMRT + (-3.3651463E-8) * v * v * DMRT * DMRT * DMRT + (1.35908359E-10) * TaC * v * v * DMRT * DMRT * DMRT + (4.1703262E-10) * v * v * v * DMRT * DMRT * DMRT +
              (-1.30369025E-9) * DMRT * DMRT * DMRT * DMRT + (4.13908461E-10) * TaC * DMRT * DMRT * DMRT * DMRT + (9.22652254E-12) * TaC * TaC * DMRT * DMRT * DMRT * DMRT + (-5.08220384E-9) * v * DMRT * DMRT * DMRT * DMRT +
              (-2.24730961E-11) * TaC * v * DMRT * DMRT * DMRT * DMRT + (1.17139133E-10) * v * v * DMRT * DMRT * DMRT * DMRT + (6.62154879E-10) * DMRT * DMRT * DMRT * DMRT * DMRT + (4.0386326E-13) * TaC * DMRT * DMRT * DMRT * DMRT * DMRT +
              (1.95087203E-12) * v * DMRT * DMRT * DMRT * DMRT * DMRT + (-4.73602469E-12) * DMRT * DMRT * DMRT * DMRT * DMRT * DMRT + (5.12733497) * Pa + (-3.12788561E-1) * TaC * Pa + (-1.96701861E-2) * TaC * TaC * Pa +
              (9.9969087E-4) * TaC * TaC * TaC * Pa + (9.51738512E-6) * TaC * TaC * TaC * TaC * Pa + (-4.66426341E-7) * TaC * TaC * TaC * TaC * TaC * Pa + (5.48050612E-1) * v * Pa +
              (-3.30552823E-3) * TaC * v * Pa + (-1.6411944E-3) * TaC * TaC * v * Pa + (-5.16670694E-6) * TaC * TaC * TaC * v * Pa + (9.52692432E-7) * TaC * TaC * TaC * TaC * v * Pa +
              (-4.29223622E-2) * v * v * Pa + (5.00845667E-3) * TaC * v * v * Pa + (1.00601257E-6) * TaC * TaC * v * v * Pa + (-1.81748644E-6) * TaC * TaC * TaC * v * v * Pa +
              (-1.25813502E-3) * v * v * v * Pa + (-1.79330391E-4) * TaC * v * v * v * Pa + (2.34994441E-6) * TaC * TaC * v * v * v * Pa + (1.29735808E-4) * v * v * v * v * Pa +
              (1.2906487E-6) * TaC * v * v * v * v * Pa + (-2.28558686E-6) * v * v * v * v * v * Pa + (-3.69476348E-2) * DMRT * Pa + (1.62325322E-3) * TaC * DMRT * Pa +
              (-3.1427968E-5) * TaC * TaC * DMRT * Pa + (2.59835559E-6) * TaC * TaC * TaC * DMRT * Pa + (-4.77136523E-8) * TaC * TaC * TaC * TaC * DMRT * Pa + (8.6420339E-3) * v * DMRT * Pa +
              (-6.87405181E-4) * TaC * v * DMRT * Pa + (-9.13863872E-6) * TaC * TaC * v * DMRT * Pa + (5.15916806E-7) * TaC * TaC * TaC * v * DMRT * Pa + (-3.59217476E-5) * v * v * DMRT * Pa +
              (3.28696511E-5) * TaC * v * v * DMRT * Pa + (-7.10542454E-7) * TaC * TaC * v * v * DMRT * Pa + (-1.243823E-5) * v * v * v * DMRT * Pa + (-7.385844E-9) * TaC * v * v * v * DMRT * Pa +
              (2.20609296E-7) * v * v * v * v * DMRT * Pa + (-7.3246918E-4) * DMRT * DMRT * Pa + (-1.87381964E-5) * TaC * DMRT * DMRT * Pa + (4.80925239E-6) * TaC * TaC * DMRT * DMRT * Pa +
              (-8.7549204E-8) * TaC * TaC * TaC * DMRT * DMRT * Pa + (2.7786293E-5) * v * DMRT * DMRT * Pa + (-5.06004592E-6) * TaC * v * DMRT * DMRT * Pa + (1.14325367E-7) * TaC * TaC * v * DMRT * DMRT * Pa +
              (2.53016723E-6) * v * v * DMRT * DMRT * Pa + (-1.72857035E-8) * TaC * v * v * DMRT * DMRT * Pa + (-3.95079398E-8) * v * v * v * DMRT * DMRT * Pa + (-3.59413173E-7) * DMRT * DMRT * DMRT * Pa +
              (7.04388046E-7) * TaC * DMRT * DMRT * DMRT * Pa + (-1.89309167E-8) * TaC * TaC * DMRT * DMRT * DMRT * Pa + (-4.79768731E-7) * v * DMRT * DMRT * DMRT * Pa + (7.96079978E-9) * TaC * v * DMRT * DMRT * DMRT * Pa +
              (1.62897058E-9) * v * v * DMRT * DMRT * DMRT * Pa + (3.94367674E-8) * DMRT * DMRT * DMRT * DMRT * Pa + (-1.18566247E-9) * TaC * DMRT * DMRT * DMRT * DMRT * Pa + (3.34678041E-10) * v * DMRT * DMRT * DMRT * DMRT * Pa +
              (-1.15606447E-10) * DMRT * DMRT * DMRT * DMRT * DMRT * Pa + (-2.80626406) * Pa * Pa + (5.48712484E-1) * TaC * Pa * Pa + (-3.9942841E-3) * TaC * TaC * Pa * Pa +
              (-9.54009191E-4) * TaC * TaC * TaC * Pa * Pa + (1.93090978E-5) * TaC * TaC * TaC * TaC * Pa * Pa + (-3.08806365E-1) * v * Pa * Pa + (1.16952364E-2) * TaC * v * Pa * Pa +
              (4.95271903E-4) * TaC * TaC * v * Pa * Pa + (-1.90710882E-5) * TaC * TaC * TaC * v * Pa * Pa + (2.10787756E-3) * v * v * Pa * Pa + (-6.98445738E-4) * TaC * v * v * Pa * Pa +
              (2.30109073E-5) * TaC * TaC * v * v * Pa * Pa + (4.1785659E-4) * v * v * v * Pa * Pa + (-1.27043871E-5) * TaC * v * v * v * Pa * Pa + (-3.04620472E-6) * v * v * v * v * Pa * Pa +
              (5.14507424E-2) * DMRT * Pa * Pa + (-4.32510997E-3) * TaC * DMRT * Pa * Pa + (8.99281156E-5) * TaC * TaC * DMRT * Pa * Pa + (-7.14663943E-7) * TaC * TaC * TaC * DMRT * Pa * Pa +
              (-2.66016305E-4) * v * DMRT * Pa * Pa + (2.63789586E-4) * TaC * v * DMRT * Pa * Pa + (-7.01199003E-6) * TaC * TaC * v * DMRT * Pa * Pa + (-1.06823306E-4) * v * v * DMRT * Pa * Pa +
              (3.61341136E-6) * TaC * v * v * DMRT * Pa * Pa + (2.29748967E-7) * v * v * v * DMRT * Pa * Pa + (3.04788893E-4) * DMRT * DMRT * Pa * Pa + (-6.42070836E-5) * TaC * DMRT * DMRT * Pa * Pa +
              (1.16257971E-6) * TaC * TaC * DMRT * DMRT * Pa * Pa + (7.68023384E-6) * v * DMRT * DMRT * Pa * Pa + (-5.47446896E-7) * TaC * v * DMRT * DMRT * Pa * Pa + (-3.5993791E-8) * v * v * DMRT * DMRT * Pa * Pa +
              (-4.36497725E-6) * DMRT * DMRT * DMRT * Pa * Pa + (1.68737969E-7) * TaC * DMRT * DMRT * DMRT * Pa * Pa + (2.67489271E-8) * v * DMRT * DMRT * DMRT * Pa * Pa + (3.23926897E-9) * DMRT * DMRT * DMRT * DMRT * Pa * Pa +
              (-3.53874123E-2) * Pa * Pa * Pa + (-2.2120119E-1) * TaC * Pa * Pa * Pa + (1.55126038E-2) * TaC * TaC * Pa * Pa * Pa + (-2.63917279E-4) * TaC * TaC * TaC * Pa * Pa * Pa +
              (4.53433455E-2) * v * Pa * Pa * Pa + (-4.32943862E-3) * TaC * v * Pa * Pa * Pa + (1.45389826E-4) * TaC * TaC * v * Pa * Pa * Pa + (2.1750861E-4) * v * v * Pa * Pa * Pa +
              (-6.66724702E-5) * TaC * v * v * Pa * Pa * Pa + (3.3321714E-5) * v * v * v * Pa * Pa * Pa + (-2.26921615E-3) * DMRT * Pa * Pa * Pa + (3.80261982E-4) * TaC * DMRT * Pa * Pa * Pa +
              (-5.45314314E-9) * TaC * TaC * DMRT * Pa * Pa * Pa + (-7.96355448E-4) * v * DMRT * Pa * Pa * Pa + (2.53458034E-5) * TaC * v * DMRT * Pa * Pa * Pa + (-6.31223658E-6) * v * v * DMRT * Pa * Pa * Pa +
              (3.02122035E-4) * DMRT * DMRT * Pa * Pa * Pa + (-4.77403547E-6) * TaC * DMRT * DMRT * Pa * Pa * Pa + (1.73825715E-6) * v * DMRT * DMRT * Pa * Pa * Pa + (-4.09087898E-7) * DMRT * DMRT * DMRT * Pa * Pa * Pa +
              (6.14155345E-1) * Pa * Pa * Pa * Pa + (-6.16755931E-2) * TaC * Pa * Pa * Pa * Pa + (1.33374846E-3) * TaC * TaC * Pa * Pa * Pa * Pa + (3.55375387E-3) * v * Pa * Pa * Pa * Pa +
              (-5.13027851E-4) * TaC * v * Pa * Pa * Pa * Pa + (1.02449757E-4) * v * v * Pa * Pa * Pa * Pa + (-1.48526421E-3) * DMRT * Pa * Pa * Pa * Pa + (-4.11469183E-5) * TaC * DMRT * Pa * Pa * Pa * Pa +
              (-6.80434415E-6) * v * DMRT * Pa * Pa * Pa * Pa + (-9.77675906E-6) * DMRT * DMRT * Pa * Pa * Pa * Pa + (8.82773108E-2) * Pa * Pa * Pa * Pa * Pa + (-3.01859306E-3) * TaC * Pa * Pa * Pa * Pa * Pa +
              (1.04452989E-3) * v * Pa * Pa * Pa * Pa * Pa + (2.47090539E-4) * DMRT * Pa * Pa * Pa * Pa * Pa + (1.48348065E-3) * Pa * Pa * Pa * Pa * Pa * Pa
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

            var Condition = new int[HoursPerYear, numberOfProbes];

            Parallel.For(0, numberOfProbes, probe =>
            {
                for (int hour = 0; hour < HoursPerYear; hour++)
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
            // Bolt: Optimized memory allocation by removing the temporary O(N*M) 2D array and
            // calculating the sum inline for each probe, which eliminates the O(N) column extraction
            // and array summing overhead.
            int numberOfProbes = ValuesCondition.GetLength(1);
            var ValuesAnnualPercentage = new double[numberOfProbes];

            Parallel.For(0, numberOfProbes, probe =>
            {
                int comfortableHours = 0;
                for (int hour = 0; hour < HoursPerYear; hour++)
                {
                    if (ValuesCondition[hour, probe] == 0)
                    {
                        comfortableHours++;
                    }
                }
                ValuesAnnualPercentage[probe] = (double)comfortableHours / HoursPerYear;
            });

            return ValuesAnnualPercentage;
        }

    }
}
