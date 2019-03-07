using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib
{
    public static class UTCI
    {
        public static object Options { get; private set; }

        public static double CalcUTCIForPoint(double TaC, double RH, double Wsp, double mrt)
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

        public static void CalcUTCIArray(double[][] probes, int numberOfHours, Weather weather, double[][] DirRad, double[][] DiffRad, double[,] windReduction, double z0, double zref, double Uref, out bool[,] uncertaintyMRTArray, out bool[,] uncertaintyWindArray, out Stopwatch sw, out double[,] Utci)
        {

            int sensorPointCount = probes.Length;


            sw = new Stopwatch();
            sw.Start();

            int cnt = 0;

            uncertaintyMRTArray = new bool[numberOfHours, sensorPointCount];
            uncertaintyWindArray = new bool[numberOfHours, sensorPointCount];
            Utci = new double[numberOfHours, sensorPointCount];

            var tempUtci = Utci;
            var tempuncertaintyMRTArray = uncertaintyMRTArray;
            var tempuncertaintyWindArray = uncertaintyWindArray;


            using (var progress = new ASCIIProgressBar())
            {

                //for (int j = 0; j < sensorPointCount; j++)
                //{

                Parallel.For(0, sensorPointCount,
              j =>
              {
                  cnt++;
                  progress.Report((double)cnt / sensorPointCount);

                  var currentProbingPoint = new Point3d(probes[j][0], probes[j][1], probes[j][2]);
                  var probingHeight = currentProbingPoint.Z;

                  for (int i = 0; i < numberOfHours; i++)
                  {

                      tempuncertaintyWindArray[i, j] = false;
                      tempuncertaintyMRTArray[i, j] = false;

                      // Check for extreme mrts

                      double mrt = UTCI.GetMRT(weather.DryBulbTemp[i], weather.RelativeHumidity[i], DiffRad[i][j], DirRad[i][j], weather.SolarElevation[i], weather.DryBulbTemp[i], weather.Wst, weather.Hst, weather.BodyA, weather.GrRef, 0.95)[0];

                      if (mrt < weather.DryBulbTemp[i] - 30)
                      {
                          mrt = 30;
                          tempuncertaintyMRTArray[i, j] = true;
                      }
                      if (mrt > weather.DryBulbTemp[i] + 70)
                      {
                          mrt = 70;
                          tempuncertaintyMRTArray[i, j] = true;
                      }

                      // Check for extreme windspeeds

                      double resultingWindSpeedforUTCI = windReduction[i, j] * UTCI.GetVelocityAtProbingHeightFromEPW(weather.WindSpeed[i], z0, zref, probingHeight);

                      if (windReduction[i, j] * UTCI.GetVelocityAtProbingHeightFromEPW(weather.WindSpeed[i], z0, zref, probingHeight) > 17)
                      {
                          resultingWindSpeedforUTCI = 17;
                          tempUtci[i, j] = UTCI.CalcUTCIForPoint(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                          tempuncertaintyWindArray[i, j] = true;
                      }
                      else if (resultingWindSpeedforUTCI < 0.5)
                      {
                          resultingWindSpeedforUTCI = 0.5;
                          tempUtci[i, j] = UTCI.CalcUTCIForPoint(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                          tempuncertaintyWindArray[i, j] = true;
                      }
                      else
                      {
                          tempUtci[i, j] = UTCI.CalcUTCIForPoint(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                      }

                      //double cOfPerson = 0;

                      //if (Utci[i, j] < -40) cOfPerson = -5;
                      //else if ((-40 <= Utci[i, j]) && (Utci[i, j] < -27)) cOfPerson = -4;
                      //else if ((-27 <= Utci[i, j]) && (Utci[i, j] < -13)) cOfPerson = -3;
                      //else if ((-13 <= Utci[i, j]) && (Utci[i, j] < 0)) cOfPerson = -2;
                      //else if ((0 <= Utci[i, j]) && (Utci[i, j] < 9)) cOfPerson = -1;
                      //else if ((9 <= Utci[i, j]) && (Utci[i, j] < 26)) cOfPerson = 0;
                      //else if ((26 <= Utci[i, j]) && (Utci[i, j] < 28)) cOfPerson = 1;
                      //else if ((28 <= Utci[i, j]) && (Utci[i, j] < 32)) cOfPerson = 2;
                      //else if ((32 <= Utci[i, j]) && (Utci[i, j] < 38)) cOfPerson = 3;
                      //else if ((38 <= Utci[i, j]) && (Utci[i, j] < 46)) cOfPerson = 4;
                      //else cOfPerson = 5;

                      //conditionOfPerson[i, j] = cOfPerson;

                  }
                  // Console.WriteLine("Sensor " + j + " done.");
                  //  }
              });

                Utci = tempUtci;
                uncertaintyMRTArray = tempuncertaintyMRTArray;
                uncertaintyWindArray = tempuncertaintyWindArray;

            }//end using prog bar

            Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));
        }
                       
        public static double[] GetMRT(double Tair, double RelHum, double DiffRad, double DirRad, double SolarElev, double T_celsius,
double Wst, double Hst, double BodyA, double GrRef, double Eb)
        {
            //Standard call
            //UTCI.GetMRT(weather.DryBulbTemp[i], weather.RelativeHumidity[i], DiffRad[i][j], DirRad[i][j], weather.SolarElevation[i], weather.DryBulbTemp[i], weather.Wst, weather.Hst, weather.BodyA, weather.GrRef, 0.95)[0];

            // Why do we assume Eb = 0.95 when calling function? Is Eb the same as Es/Ec? What is Eb?
            // What is Hst and Wst?


            double[] MRT = new double[2];

            MRT[0] = Tair;
            MRT[1] = Tair;

            //Reference: [1] http://www.academia.edu/13838171/The_Human_Bio-Meteorological_Chart_A_design_tool_for_outdoor_thermal_comfort
            //Reference: [2] The calculation of the mean radiant temperature of a subject exposed to the solar radiation—a generalised algorithm
            //Reference: [3] The Computation of Equivalent Potential Temperature - David Bolton

                    

            double SBConst = 5.67E-8;


            double es = Math.Log(RelHum / 100) + 17.67 * Tair / (243.5 + Tair); // [3] for -30 -- 35°C
            double T_dewP = 243.5 * es / (17.67 - es); // [3]
            double e = 0.7122 + 0.0056 * T_dewP + 0.000073 * Math.Pow(T_dewP, 2) + 0.00884; // polinomial for curve fit [1]
            double TSkyKelvin = (Tair + 273) * Math.Pow(e, 0.25);  // [1]
            double T_celsius_kelvin = T_celsius + 273;

            double Fs = (Math.Atan(0.5 * Wst / (Hst - 1))) * 180 / Math.PI * 0.0056; // where does this come from?
            // where FiS is the angle factor between the ith internal surface of the envelope and the subject, ei is its emissivity, Ai is the area of the interested surface, Ti the temperature, ri the reflection coefficient of the ith surface and Gi the radiation reaching the ith internal surface.
            double Fc = 1 - Fs;  // remaining angle factor

            double Es = 0.95;  // Emissivities? Why 0.95?
            double Ec = 0.95;  // Emissivities? 

            double Fd = 0.50;  // Does this account for 50 % sky and 50 % ground? if yes then this should be an input that changes with respect to the canyon
            double f = 0.00000043 * Math.Pow(SolarElev, 3) - 0.000068 * Math.Pow(SolarElev, 2) + 0.0003 * SolarElev + 0.3081; // Where does this come from?

            double IR = Math.Pow((1 / Eb * (Fs * Math.Pow(TSkyKelvin, 4) * Es + Fc * Math.Pow(T_celsius_kelvin, 4) * Ec)), 0.25);
            double DF = Math.Pow(((DiffRad * Fd + (DiffRad + DirRad * Math.Sin(SolarElev * Math.PI / 180)) * GrRef) * BodyA * 0.725 / (Eb * SBConst)), 0.25);
            double DR = Math.Pow((DirRad * f * BodyA * 0.725 / (Eb * SBConst)), 0.25);

            double MRTKelvin = Math.Pow(Math.Pow(IR, 4) + Math.Pow(DF, 4) + Math.Pow(DR, 4), 0.25);
            double MRTCelsius = MRTKelvin - 273;

            MRT[0] = MRTCelsius;
            MRT[1] = IR - 273;
            return MRT;
        }


        public static double[] ReadComfortHoursFromCSV(string baseWorkingDir, List<int> hoursToEvaluate)
        {

            //// Fill datatrees from CSV

            var path = baseWorkingDir + @"\UTCI.csv";
            List<double> ComfortHoursList = new List<double>();

            var allLines = File.ReadAllLines(path);
            var numberOfProbes = allLines.Count();


            // Fill array once; fastest method so far

            double[,] HourlyUTCI = new double[numberOfProbes, 8760];

            //double[,] HourlyHumanConditions = new double[numberOfProbes, 8760];
            double[] ComfortHours = new double[numberOfProbes];

            System.Threading.Tasks.Parallel.For(0, numberOfProbes,
                    i =>
                    {

                        //              if (GH_Document.IsEscapeKeyDown())
                        //              {
                        //                  private GH_Document GHDocument = OnPingDocument();
                        //GHDocument.RequestAbortSolution();
                        //              }

                        for (int h = 0; h < 8760; h++)
                        {
                            HourlyUTCI[i, h] = double.Parse(allLines[i].Split(',')[h]);
                        }

                    });

            //var hoursToEvaluate = Utilities.GetEvalHoursFromLB(ladybugAnalysisPeriod);



            //system.threading.tasks.parallel.for (0, numberofprobes,
            //  i =>
            //  {

            for (int probes = 0; probes < numberOfProbes; probes++)
            {


                int comfortCnt = 0;
                foreach (int hour in hoursToEvaluate)
                {
                    if (UTCI.GetConditionOfPerson(HourlyUTCI[probes, hour]) == 0)
                    {
                        //HourlyHumanConditions[i,hour]=UTCI.GetConditionOfPerson(HourlyUTCI[i, hour]);
                        comfortCnt++;

                    }

                }
                ComfortHours[probes] = Math.Round((double)comfortCnt * 100 / hoursToEvaluate.Count, 1);
                //});
            }

            return ComfortHours;

        }

        public static double GetVelocityAtProbingHeightFromEPW(double URef, double z0, double zref, double probingHeight)
        {
            var UAtProbingHeightFromEPW = ((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((probingHeight + z0) / z0);
            return UAtProbingHeightFromEPW;
        }

        public static int GetConditionOfPerson(double UTCI)
        {

            int cOfPerson = 0;

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

        public static string[] LoadWindReductionArrayFromCSV(string filePath)
        {
            var ReductionData = File.ReadAllLines(filePath).Skip(1).ToArray();
            int sensorPointCount = ReductionData.Length;

            var numberOfWindDirsSimulated = ReductionData[0].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Count();

            for (int i = 0; i < sensorPointCount; i++)
            {
                var l = ReductionData[i];
                if (l.Contains("∞"))
                {
                    ReductionData[i] = l.Replace("∞", "0");
                }
            }
            return ReductionData;
        }

        public static double[,] GetWindReduction(string[] ReductionData, int numberOfHours, List<int> simulatedWindDirList, Weather weather)
        {

            int sensorPointCount = ReductionData.Length;
            int numberOfWindDirs = simulatedWindDirList.Count;

            // Array of Reduction data

            double[,] windReduction = new double[numberOfHours, sensorPointCount];

            int cntReduction = 0;
            using (var progress = new ASCIIProgressBar())
            {


                var ReductionArray = new double[numberOfWindDirs][];

                for (int d = 0; d < numberOfWindDirs; d++)
                {

                    ReductionArray[d] = new double[sensorPointCount];
                    for (int p = 0; p < sensorPointCount; p++)
                    {
                        ReductionArray[d][p] = double.Parse(ReductionData[p].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[d]);
                    }
                }

                Console.WriteLine("Calculating: Wind reduction factors");

                for (int j = 0; j < sensorPointCount; j++)
                {
                    cntReduction++;
                    progress.Report((double)cntReduction / sensorPointCount);
                    for (int i = 0; i < numberOfHours; i++)
                    {

                        // hours of weather file in iterator missing
                        windReduction[i, j] = UTCI.GetWindReductionFactor(j, ReductionArray, sensorPointCount, simulatedWindDirList, weather.WindSpeed[i], weather.WindDirection[i]);
                    }
                }

            }
            return windReduction;
        }

        public static void WriteWindReductionArrayToCSV(string WindDirs, string WorkingDir, int Mode, double URef, double zref, double z0, string probesFilePath, bool Verbose, out StringBuilder errorLog)
        {
            errorLog = new StringBuilder();
            errorLog.AppendLine("test");

            var simulatedWindDirList = WindDirs.Split(',');
            int numberOfWindDirs = simulatedWindDirList.Length;

            // Read all variables from one file path. Variables are usually identical for all wind directions so this should be robust.
            var ABLfilePath = WorkingDir + "\\" + WindDirs.Split(',')[0] + @"\0.org\ABLConditions";

            try
            {

                // Error checking

                if (!Directory.Exists(WorkingDir)) { errorLog.AppendLine(WorkingDir + " not found. Exiting"); Console.WriteLine(WorkingDir + " not found. Exiting"); }

                if (Utilities.IsDirectoryEmpty(WorkingDir + @"\mesh\constant\polyMesh"))
                {
                    errorLog.AppendLine("The mesh folder is empty. Can't pull probes from a mesh that does not exist.");
                    //throw new System.ArgumentException("The mesh folder is empty. Can't pull probes from a mesh that does not exist.");
                }


                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    var fp = WorkingDir + @"\" + simulatedWindDirList[i] + @"\system\U_Probes";
                    if (!File.Exists(fp))
                    {
                        errorLog.AppendLine(@"The wind direction """ + simulatedWindDirList[i] + @""" misses the probing dictionary. Please connect the ""writeProbes"" component and recompute the solution.");
                        throw new System.ArgumentException("The wind direction " + simulatedWindDirList[i] + @" misses the probing dictionary. Please connect the component ""writeProbes"" and recompute the solution.");
                    }
                }

                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    var fp = WorkingDir + @"\" + simulatedWindDirList[i] + @"\constant\polyMesh";
                    if (!Directory.Exists(fp))
                    {
                        errorLog.AppendLine(@"The wind direction """ + simulatedWindDirList[i] + @""" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
                        throw new System.ArgumentException("The wind direction " + simulatedWindDirList[i] + @" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
                    }
                }

                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    ABLfilePath = WorkingDir + "\\" + simulatedWindDirList[i] + @"\0.org\ABLConditions";
                    if (!File.Exists(ABLfilePath)) { Console.WriteLine(ABLfilePath + " not found. Exiting"); errorLog.AppendLine(ABLfilePath + " not found. Exiting"); }
                }


                // Check if U file is in last iteration
                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    string iter = Utilities.GetLastIterationFromDirectory(WorkingDir + @"\" + simulatedWindDirList[i]).ToString();
                    string fp = WorkingDir + @"\" + simulatedWindDirList[i] + @"\" + iter + @"\U";


                    if (!File.Exists(fp))
                    {
                        errorLog.AppendLine(@"The simulation folder of the wind direction """ + simulatedWindDirList[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                        throw new System.ArgumentException(@"The simulation folder of the wind direction """ + simulatedWindDirList[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                    }
                }





                // Delete files in subfolders
                var listOfDirsInfo = new List<string>();

                for (int i = 0; i < numberOfWindDirs; i++)
                {
                    listOfDirsInfo.Add((@"C:\Temp\" + simulatedWindDirList[i] + @"\postProcessing\"));

                }



                //for (int i = 0; i < numberOfWindDirs; i++)
                //{


                //    foreach (var subDir in new DirectoryInfo(listOfDirsInfo[i]).GetDirectories())
                //    {

                //        if (subDir.ToString().ToLower() == "residuals")
                //        {
                //            continue;
                //        }
                //        subDir.Delete(true);
                //    }
                //}



                Utilities.ParseABLConditionsFromCaseFolder(ABLfilePath, out URef, out z0, out zref);

                double[][] probes = EddyLib.RadianceFiles.readPTS(probesFilePath);
                var numberOfProbes = probes.GetLength(0);

                List<Point3d> pointList = new List<Point3d>();

                for (int i = 0; i < probes.GetLength(0); i++)
                {
                    pointList.Add(new Point3d(probes[i][0], probes[i][1], probes[i][2]));
                }



                if (Mode == 0) // cp
                {

                    //try
                    //{


                    //    StringBuilder command = new StringBuilder();

                    //    string pointName = "cp_Probes";
                    //    string OFfield = "total(p)_coeff";

                    //    for (int i = 0; i < numberOfWindDirs; i++)
                    //    {

                    //        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + "controlDict", EddyLib.StringTemplatescontrolDict(DOM, null, i));
                    //        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, EddyLib.StringTemplatessampleProbes(listOfPoints, pointName, options.mode));
                    //        command.Append(@"postProcess -case " + windDirs[i] + " -func " + pointName + @" -newTimes | tee  " + windDirs[i] + @"/log_probes;");



                    //    }

                    //    ProcessStartInfo psi = new ProcessStartInfo(EddyLib.Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + options.workingDir);
                    //    Process p = new Process();
                    //    p.StartInfo = psi;
                    //    p.Start();
                    //    p.WaitForExit();
                    //    //p.Close();

                    //    Thread.Sleep(2 * probes.GetLength(0) * numberOfWindDirs);


                    //    for (int i = 0; i < numberOfWindDirs; i++)
                    //    {
                    //        //Thread.Sleep(2 * probes.GetLength(0));
                    //        ParsingProbes cp = new ParsingProbes(pointList, pointName, options.workingDir + "\\" + windDirs[i], OFfield);
                    //        //cpTree.AddRange(cp.cpValues, new Grasshopper.Kernel.Data.GH_Path(i));
                    //    }

                    //}
                    //catch (Exception e) { Console.WriteLine(e.Message); return; }


                }

                if (Mode == 1) // U
                {

                    Console.WriteLine("Probing the simulation results.");

                    Stopwatch sw = new Stopwatch(); sw.Start();



                    StringBuilder command = new StringBuilder();

                    string pointName = "U_Probes";
                    string OFfield = "U";

                    for (int i = 0; i < numberOfWindDirs; i++)
                    {
                        
                        // Write the dicts

                        //File.WriteAllText(options.workingDir + dirs[i] + @"\system\" + pointName, EddyLib.StringTemplatessampleProbes(listOfPoints, pointName, options.mode));
                        command.Append(@"postProcess -case " + simulatedWindDirList[i] + " -func " + pointName + @" -latestTime | tee -a  " + simulatedWindDirList[i] + @"/log_probes;");


                    }
                    

                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", @" -e """ + command + @""" -f " + "\"" + WorkingDir);
                    Process p = new Process
                    {
                        StartInfo = psi
                    };
                    p.Start();
                    p.WaitForExit();
                    p.Close();

                    // Issue
                    // Could not find a part of the path 'C:\temp\0\PostProcessing\U_Probes'.
                    // This happens if OF process closes immideately after calling

                    //Thread.Sleep(2 * 30* Math.Sqrt(probes.GetLength(0)) * numberOfWindDirs);

                    Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));



                    Console.WriteLine("Parsing the velocity vectors for the probes of every wind direction and writing result files.");
                    Stopwatch sw2 = new Stopwatch(); sw2.Start();


                    for (int i = 0; i < numberOfWindDirs; i++)
                    {

                        // Parse values
                        //Thread.Sleep(2 * probes.GetLength(0));
                        int fieldtype = 1; //vectors
                        var U = new Probes(pointList, pointName, WorkingDir + "\\" + simulatedWindDirList[i], OFfield, fieldtype);

                        // Create datatree

                        // uTree.AddRange(U.uValues, new Grasshopper.Kernel.Data.GH_Path(i));

                    }


                    List<string> fullProbeFilePath = new List<String>();

                    //var numberOfProbes = File.ReadAllLines(fullProbeFilePath[0]).Count(); //defined above                    
                    //string[] abc = replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);


                    //Build list of paths



                    for (int i = 0; i < numberOfWindDirs; i++)
                    {
                        var path = WorkingDir + "\\" + simulatedWindDirList[i] + @"\postProcessing\U_Probes.csv";
                        if (!File.Exists(path)) { Console.WriteLine(path + " not found. Exiting"); errorLog.AppendLine(path + " not found. Exiting"); return; }
                        fullProbeFilePath.Add(path);
                    }


                    Console.WriteLine(Utilities.ConvertComputeTimes(sw2.ElapsedMilliseconds));




                    // Array for output data

                    Console.WriteLine("Re-collecting output data from every wind direction.");
                    Stopwatch sw3 = new Stopwatch(); sw3.Start();


                    Vector3d[,] AnnualData = new Vector3d[numberOfWindDirs, numberOfProbes];

                    var UData = new string[numberOfWindDirs][];

                    for (int i = 0; i < numberOfWindDirs; i++)
                    {
                        UData[i] = File.ReadAllLines(fullProbeFilePath[i]);
                    }


                    //UData[0] = File.ReadAllLines(fullProbeFilePath[0]);
                    //UData[1] = File.ReadAllLines(fullProbeFilePath[1]);
                    //UData[2] = File.ReadAllLines(fullProbeFilePath[2]);
                    //UData[3] = File.ReadAllLines(fullProbeFilePath[3]);
                    //UData[4] = File.ReadAllLines(fullProbeFilePath[4]);
                    //UData[5] = File.ReadAllLines(fullProbeFilePath[5]);
                    //UData[6] = File.ReadAllLines(fullProbeFilePath[6]);
                    //UData[7] = File.ReadAllLines(fullProbeFilePath[7]);

                    using (var progress = new ASCIIProgressBar())
                    {
                        int cnt = 0;
                        Parallel.For(0, numberOfWindDirs,
                        r =>

                        {

                                    //for (int r = 0; r < numberOfWindDirs; r++)
                                    //{
                                    //listOfAnnualData[r] = new Vector3d[numberOfProbes];
                                    for (int c = 0; c < numberOfProbes; c++)
                            {
                                AnnualData[r, c] = new Vector3d(double.Parse(UData[r][c].Split(',')[0]), double.Parse(UData[r][c].Split(',')[1]), double.Parse(UData[r][c].Split(',')[2]));
                                progress.Report((double)cnt / numberOfProbes * numberOfWindDirs);
                                cnt++;
                            }
                                    //}
                                });
                    }

                    Console.WriteLine(Utilities.ConvertComputeTimes(sw3.ElapsedMilliseconds));


                    //Write U Array to file
                    Console.WriteLine("Writing U Array");
                    Stopwatch sw4 = new Stopwatch(); sw4.Start();


                    System.Text.StringBuilder UFile = new System.Text.StringBuilder();

                    using (var progress = new ASCIIProgressBar())
                    {
                        int cnt = 0;

                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            UFile.Append(simulatedWindDirList[i] + " , , ,");

                        }
                        UFile.AppendLine("");
                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            UFile.Append("x, y, z,");
                        }
                        UFile.AppendLine("");

                        for (int r = 0; r < numberOfProbes; r++)
                        {
                            for (int c = 0; c < numberOfWindDirs; c++)
                            {

                                UFile.Append(String.Format("{0:0.##}", AnnualData[c, r].X) + "," + String.Format("{0:0.##}", AnnualData[c, r].Y) + "," + String.Format("{0:0.##}", AnnualData[c, r].Z) + ",");
                                progress.Report((double)cnt / numberOfProbes * numberOfWindDirs);
                                cnt++;
                            }
                            UFile.AppendLine("");
                        }

                    }

                    File.WriteAllText(WorkingDir + @"\U.csv", UFile.ToString());

                    //Write Reduction Array to file


                    Console.WriteLine(Utilities.ConvertComputeTimes(sw4.ElapsedMilliseconds));


                    Console.WriteLine("Write Reduction Array");
                    Stopwatch sw5 = new Stopwatch(); sw5.Start();


                    // Calculate the undisturbed velocity at probing height !!!This only makes sense for horizontal slices!!!

                    using (var progress = new ASCIIProgressBar())
                    {
                        int cnt = 0;



                        var probingHeight = pointList[0].Z;
                        var UProbingHeight = ((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((probingHeight + z0) / z0);


                        System.Text.StringBuilder ReductionFile = new System.Text.StringBuilder();

                        for (int i = 0; i < numberOfWindDirs; i++)
                        {
                            ReductionFile.Append(simulatedWindDirList[i] + ",");
                        }

                        ReductionFile.AppendLine("");
                        for (int r = 0; r < numberOfProbes; r++)
                        {
                            for (int c = 0; c < numberOfWindDirs; c++)
                            {
                                ReductionFile.Append(String.Format("{0:0.#}", Math.Round(Math.Sqrt(Math.Pow(AnnualData[c, r].X, 2) + Math.Pow(AnnualData[c, r].Y, 2) + Math.Pow(AnnualData[c, r].Z, 2)) / UProbingHeight, 3)) + ",");
                                progress.Report((double)cnt / numberOfProbes * numberOfWindDirs);
                                cnt++;
                            }
                            ReductionFile.AppendLine("");
                        }
                        File.WriteAllText(WorkingDir + @"\

", ReductionFile.ToString());


                        if (Verbose)
                        {
                            File.WriteAllText(WorkingDir + @"\Probes.err", errorLog.ToString());
                        }

                        Console.WriteLine(Utilities.ConvertComputeTimes(sw5.ElapsedMilliseconds));

                        Console.WriteLine("Done");


                    }

                }
            }


            catch (Exception e) { Console.WriteLine(e.Message); File.WriteAllText(WorkingDir + @"\Probes.err", errorLog.ToString()); return; }
        }


        private static double GetWindReductionFactor(int probeIndex, double[][] ReductionArray, int numberOfProbes, List<int> windDirsSimulated, double windVelWeatherFile, double windDirFromWeatherFile)
        {

            int numberOfWindDirs = windDirsSimulated.Count();

            // 0, 45, 90, 135, 180, 225, 270, 315, 360 


            var nextLowIndex = ReturnNextLowerIndex(windDirsSimulated, windDirFromWeatherFile);
            var nextUpIndex = ReturnNextUpperIndex(windDirsSimulated, windDirFromWeatherFile);

            var nextLowDir = windDirsSimulated[nextLowIndex];
            var nextUpDir = windDirsSimulated[nextUpIndex];


            double distanceToLower = windDirFromWeatherFile - windDirsSimulated[nextLowIndex];
            double distanceToUpper;

            if (nextUpIndex == 0)
            {
                distanceToUpper = Math.Abs((windDirFromWeatherFile - windDirsSimulated[nextUpIndex]) - 360);
            }
            else
            {
                distanceToUpper = windDirFromWeatherFile - windDirsSimulated[nextUpIndex];
            }




            //var y1_y0 = distanceToUpper;
            //var x0 = ReductionArray[nextUpIndex][probeIndex];
            //var x1_x0 = ReductionArray[nextLowIndex][probeIndex] - ReductionArray[nextUpIndex][probeIndex];
            //var y_y0 = distanceToLower + distanceToUpper;
            var weightingLow = 1 - (distanceToLower / (distanceToLower + distanceToUpper));
            var weightingUp = 1 - (distanceToUpper / (distanceToLower + distanceToUpper));
            var nextLowerReduction = ReductionArray[nextLowIndex][probeIndex];
            var nextUpperReduction = ReductionArray[nextUpIndex][probeIndex];

            var windRedFactorInterpolated = ((nextLowerReduction * weightingLow) + (nextUpperReduction * weightingUp));


            //  (ReductionArray[nextLow][probeIndex] + distanceToLower * (ReductionArray[nextUp][probeIndex] / (distanceToLower + distanceToUpper)));


            return windRedFactorInterpolated;
        }


        private static int ReturnNextLowerIndex(List<int> windDirs, double UTCIWindDir)
        {


            int lowerIndex = 0;
            int NextLower = windDirs[0];

            for (int i = 0; i < windDirs.Count(); i++)
            {
                if (windDirs[i] < UTCIWindDir)
                {
                    NextLower = windDirs[i];
                    lowerIndex = i;
                }
            }

            return lowerIndex;
        }
        private static int ReturnNextUpperIndex(List<int> windDirs, double UTCIWindDir)
        {
            // Make sure that 360 input is equal to 0
            if (UTCIWindDir == 360)
            {
                UTCIWindDir = 0;
            }


            int upperIndex = windDirs.Count - 1;
            int NextUpper = windDirs[0];

            //Add 360 to enable comparison with "0" degrees
            for (int i = 0; i < windDirs.Count; i++)
            {
                if (windDirs[i] == 0)
                {
                    windDirs.Add(360);
                }
            }

            for (int i = windDirs.Count - 1; i > 0; i--)
            {
                if (windDirs[i] > UTCIWindDir)
                {
                    NextUpper = windDirs[i];
                    upperIndex = i;
                }
            }

            return upperIndex;
        }


        public static void CalculateUTCIArray(double[][] probes, int numberOfHours, Weather weather, double[][] DirRad, double[][] DiffRad, double[,] windReduction, double z0, double zref, double Uref, out bool[,] uncertaintyMRTArray, out bool[,] uncertaintyWindArray, out Stopwatch sw, out double[,] Utci)
        {

            int sensorPointCount = probes.Length;


            sw = new Stopwatch();
            sw.Start();

            int cnt = 0;

            uncertaintyMRTArray = new bool[numberOfHours, sensorPointCount];
            uncertaintyWindArray = new bool[numberOfHours, sensorPointCount];
            Utci = new double[numberOfHours, sensorPointCount];

            var tempUtci = Utci;
            var tempuncertaintyMRTArray = uncertaintyMRTArray;
            var tempuncertaintyWindArray = uncertaintyWindArray;


            using (var progress = new ASCIIProgressBar())
            {

                //for (int j = 0; j < sensorPointCount; j++)
                //{

                Parallel.For(0, sensorPointCount,
              j =>
              {
                  cnt++;
                  progress.Report((double)cnt / sensorPointCount);

                  var currentProbingPoint = new Point3d(probes[j][0], probes[j][1], probes[j][2]);
                  var probingHeight = currentProbingPoint.Z;

                  for (int i = 0; i < numberOfHours; i++)
                  {

                      tempuncertaintyWindArray[i, j] = false;
                      tempuncertaintyMRTArray[i, j] = false;

              // Check for extreme mrts

              double mrt = UTCI.GetMRT(weather.DryBulbTemp[i], weather.RelativeHumidity[i], DiffRad[i][j], DirRad[i][j], weather.SolarElevation[i], weather.DryBulbTemp[i], weather.Wst, weather.Hst, weather.BodyA, weather.GrRef, 0.95)[0];

                      if (mrt < weather.DryBulbTemp[i] - 30)
                      {
                          mrt = 30;
                          tempuncertaintyMRTArray[i, j] = true;
                      }
                      if (mrt > weather.DryBulbTemp[i] + 70)
                      {
                          mrt = 70;
                          tempuncertaintyMRTArray[i, j] = true;
                      }

              // Check for extreme windspeeds

              double resultingWindSpeedforUTCI = windReduction[i, j] * UTCI.GetVelocityAtProbingHeightFromEPW(weather.WindSpeed[i], z0, zref, probingHeight);

                      if (windReduction[i, j] * UTCI.GetVelocityAtProbingHeightFromEPW(weather.WindSpeed[i], z0, zref, probingHeight) > 17)
                      {
                          resultingWindSpeedforUTCI = 17;
                          tempUtci[i, j] = UTCI.CalcUTCIForPoint(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                          tempuncertaintyWindArray[i, j] = true;
                      }
                      else if (resultingWindSpeedforUTCI < 0.5)
                      {
                          resultingWindSpeedforUTCI = 0.5;
                          tempUtci[i, j] = UTCI.CalcUTCIForPoint(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                          tempuncertaintyWindArray[i, j] = true;
                      }
                      else
                      {
                          tempUtci[i, j] = UTCI.CalcUTCIForPoint(weather.DryBulbTemp[i], weather.RelativeHumidity[i], resultingWindSpeedforUTCI, mrt);
                      }

              //double cOfPerson = 0;

              //if (Utci[i, j] < -40) cOfPerson = -5;
              //else if ((-40 <= Utci[i, j]) && (Utci[i, j] < -27)) cOfPerson = -4;
              //else if ((-27 <= Utci[i, j]) && (Utci[i, j] < -13)) cOfPerson = -3;
              //else if ((-13 <= Utci[i, j]) && (Utci[i, j] < 0)) cOfPerson = -2;
              //else if ((0 <= Utci[i, j]) && (Utci[i, j] < 9)) cOfPerson = -1;
              //else if ((9 <= Utci[i, j]) && (Utci[i, j] < 26)) cOfPerson = 0;
              //else if ((26 <= Utci[i, j]) && (Utci[i, j] < 28)) cOfPerson = 1;
              //else if ((28 <= Utci[i, j]) && (Utci[i, j] < 32)) cOfPerson = 2;
              //else if ((32 <= Utci[i, j]) && (Utci[i, j] < 38)) cOfPerson = 3;
              //else if ((38 <= Utci[i, j]) && (Utci[i, j] < 46)) cOfPerson = 4;
              //else cOfPerson = 5;

              //conditionOfPerson[i, j] = cOfPerson;

          }
          // Console.WriteLine("Sensor " + j + " done.");
          //  }
      });

                Utci = tempUtci;
                uncertaintyMRTArray = tempuncertaintyMRTArray;
                uncertaintyWindArray = tempuncertaintyWindArray;

            }//end using prog bar

            Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));
        }


        public static void WriteUTCIToCSV(string workingDir, double[][] probes,
            int numberOfHours, bool verboseMode, bool[,] uncertaintyMRTArray, bool[,] uncertaintyWindArray, double[,] UTCIArray, int[] debug, Weather weather, StringBuilder errorLog, double[][] DiffRad, double[][] DirRad, double[,] windReduction,
        double URef = 5,
        double zref = 10,
        double z0 = 1)
        {


            int sensorPointCount = probes.Length;


            //Write Array to file
            StringBuilder sbUtci = new StringBuilder();
            for (int j = 0; j < sensorPointCount; j++)
            {
                for (int i = 0; i < numberOfHours; i++)
                {
                    sbUtci.Append(String.Format("{0:0.0}", UTCIArray[i, j]) + ",");
                }
                sbUtci.AppendLine("");
            }
            File.WriteAllText(workingDir + @"\UTCI.csv", sbUtci.ToString());

            // Uncertainty output for UTCI calculations

            StringBuilder sbUtciUncertainty = new StringBuilder();
            sbUtciUncertainty.AppendLine("The calculated UTCI values lie outside of uncertainty (U) bounds for the following sensor points and hours either because of low/high wind velocities or MRT values:");
            int counter = 0;
            for (int j = 0; j < sensorPointCount; j++)
            {

                //Percentage for each sensorpoint
                int cntSensorPercent = 0;
                sbUtciUncertainty.Append("SP: " + j + ",");
                for (int i = 0; i < numberOfHours; i++)
                {

                    if (uncertaintyMRTArray[i, j] == true || uncertaintyWindArray[i, j] == true)
                    {
                        cntSensorPercent++;
                    }
                }

                sbUtciUncertainty.Append("\t" + (int)Math.Round((double)(100 * cntSensorPercent) / numberOfHours) + " % U,\tHours: ");
                cntSensorPercent = 0;
                //Hours for each sensorpoint
                for (int i = 0; i < numberOfHours; i++)
                {
                    if (uncertaintyMRTArray[i, j] == true || uncertaintyWindArray[i, j] == true)
                    {


                        sbUtciUncertainty.Append(i + ",");
                        counter++;
                    }
                }
                sbUtciUncertainty.AppendLine("");
            }
            sbUtciUncertainty.AppendLine("Total incidents of uncertainty: " + counter + " or " + Math.Round((double)counter * 100 / (numberOfHours * sensorPointCount), 0) + " % overall annual uncertainty");
            File.WriteAllText(workingDir + @"\UTCI.uncertainty", sbUtciUncertainty.ToString());


            //Write Debug info to file
#if DEBUG
            StringBuilder sbUtciDEBUG = new StringBuilder();

            sbUtciDEBUG.AppendLine(@"UTCI for sensor point " + debug[1] + " over all hours of the year:");


            var currentProbingPoint = new Point3d(probes[debug[1]][0], probes[debug[1]][1], probes[debug[1]][2]);
            var probingHeight = currentProbingPoint.Z;

            for (int i = 0; i < numberOfHours; i++)
            {

                sbUtciDEBUG.Append(String.Format("{0:0.0}", UTCIArray[i, debug[1]]) + ",");
            }
            sbUtciDEBUG.Append(Environment.NewLine); sbUtciDEBUG.Append(Environment.NewLine);
            sbUtciDEBUG.AppendLine("Detailed Values for sensor point " + debug[1] + " at hour " + debug[0] + ":");
            sbUtciDEBUG.AppendLine("Air temperature: " + weather.DryBulbTemp[debug[0]]);
            sbUtciDEBUG.AppendLine("MRT: " + String.Format("{0:0.0}", UTCI.GetMRT(weather.DryBulbTemp[debug[0]], weather.RelativeHumidity[debug[0]], DiffRad[debug[0]][debug[1]], DirRad[debug[0]][debug[1]], weather.SolarElevation[debug[0]], weather.DryBulbTemp[debug[0]], weather.Wst, weather.Hst, weather.BodyA, weather.GrRef, 0.95)[0]));
            sbUtciDEBUG.AppendLine("Vapour pressure: " + weather.Pressure[debug[0]]);
            sbUtciDEBUG.AppendLine("Relative humidity: " + weather.RelativeHumidity[debug[0]]);


            sbUtciDEBUG.AppendLine("Wind speed from .epw: " + String.Format("{0:0.0}", weather.WindSpeed[debug[0]]));
            //sbUtciDEBUG.AppendLine("probingHeight from CFD: " + String.Format("{0:0.0}", probingHeight));
            sbUtciDEBUG.AppendLine("Scaled-down wind velocity from .epw: " + String.Format("{0:0.0}", UTCI.GetVelocityAtProbingHeightFromEPW(weather.WindSpeed[debug[0]], z0, zref, probingHeight)));
            sbUtciDEBUG.AppendLine("Wind reduction from CFD: " + String.Format("{0:0.0}", windReduction[debug[0], debug[1]]));
            sbUtciDEBUG.AppendLine("Resulting wind velocity for UTCI calculation: " + String.Format("{0:0.0}", windReduction[debug[0], debug[1]] * UTCI.GetVelocityAtProbingHeightFromEPW(weather.WindSpeed[debug[0]], z0, zref, probingHeight)));

            sbUtciDEBUG.AppendLine("UTCI: " + String.Format("{0:0.0}", UTCIArray[debug[0], debug[1]]));
            sbUtciDEBUG.AppendLine("");
            File.WriteAllText(workingDir + @"\UTCI_debug_hour_" + debug[0] + "_probe_" + debug[1] + ".csv", sbUtciDEBUG.ToString());
#endif



            if (verboseMode)
            {
                File.WriteAllText(workingDir + @"\UTCI.err", errorLog.ToString());
            }




        }

        private static double CalcPa(double TaC, double RH)
        {
            double pa_temp = 0;
            double TaK = TaC + 273;

            pa_temp = Math.Exp(2.7150305 * Math.Log(TaK) - 2.8365744 * 1000 * Math.Pow(TaK, -2) - 6.028076559 * 1000 * Math.Pow(TaK, -1)
              + 1.954263612 * 10 - 2.737830188 / 100 * Math.Pow(TaK, 1) + 1.6261698 / 100000 * Math.Pow(TaK, 2) + 7.0229056 * Math.Pow(10, -10) * Math.Pow(TaK, 3)
              - 1.8680009 * Math.Pow(10, -13) * Math.Pow(TaK, 4)) * 0.01 * RH / 1000;

            return pa_temp;
        }

        public static void UTCI_Binning(List<double> Vals, ref object StrngCold, ref object MdrtCold, ref object SlgtCold, ref object NoStress, ref object SlgtHeat, ref object MdrtHeat, ref object StrngHeat)
        {


            int sC = 0;
            int mC = 0;
            int lC = 0;
            int nS = 0;
            int lH = 0;
            int mH = 0;
            int sH = 0;

            foreach (int v in Vals)
            {

                if (v == 3)
                {
                    sH += 1;
                }
                else if (v == 2)
                {
                    mH += 1;
                }
                else if (v == 1)
                {
                    lH += 1;
                }
                else if (v == 0)
                {
                    nS += 1;
                }
                else if (v == -1)
                {
                    lC += 1;
                }
                else if (v == -2)
                {
                    mC += 1;
                }
                else if (v == -3)
                {
                    sC += 1;
                }
                // else RhinoApp.WriteLine("Wrong UTCI value");
            }

            StrngCold = Math.Round((double)sC / Vals.Count, 3);
            MdrtCold = Math.Round((double)mC / Vals.Count, 3);
            SlgtCold = Math.Round((double)lC / Vals.Count, 3);
            NoStress = Math.Round((double)nS / Vals.Count, 3);
            SlgtHeat = Math.Round((double)lH / Vals.Count, 3);
            MdrtHeat = Math.Round((double)mH / Vals.Count, 3);
            StrngHeat = Math.Round((double)sH / Vals.Count, 3);

        }

        public static void UTCI_ConditionOfPerson(List<double> UTCI, ref object conditionOfPerson)
        {
            List<double> rtl = new List<double>();
            double condition = 0;

            for (int i = 0; i < UTCI.Count; i++)
            {


                condition = UTCI[i];
                if (UTCI[i] < -13)
                {
                    condition = -3;
                }
                else if ((-13 <= UTCI[i]) && (UTCI[i] < 0))
                {
                    condition = -2;
                }
                else if ((0 <= UTCI[i]) && (UTCI[i] < 9))
                {
                    condition = -1;
                }
                else if ((9 <= UTCI[i]) && (UTCI[i] < 26))
                {
                    condition = 0;
                }
                else if ((26 <= UTCI[i]) && (UTCI[i] < 28))
                {
                    condition = 1;
                }
                else if ((28 <= UTCI[i]) && (UTCI[i] < 32))
                {
                    condition = 2;
                }
                else
                {
                    condition = 3;
                }

                rtl.Add(condition);
            }

            conditionOfPerson = rtl;
        }
        public static void UTCI_Colors(List<double> Vals, ref object Clrs)
        {
            List<Color> cl = new List<Color>();

            foreach (int v in Vals)
            {
                if (v == 3)
                {
                    cl.Add(System.Drawing.Color.Red);
                }
                else if (v == 2)
                {
                    cl.Add(System.Drawing.Color.DarkOrange);
                }
                else if (v == 1)
                {
                    cl.Add(System.Drawing.Color.Yellow);
                }
                else if (v == 0)
                {
                    cl.Add(System.Drawing.Color.Green);
                }
                else if (v == -1)
                {
                    cl.Add(System.Drawing.Color.Cyan);
                }
                else if (v == -2)
                {
                    cl.Add(System.Drawing.Color.Blue);
                }
                else if (v == -3)
                {
                    cl.Add(System.Drawing.Color.BlueViolet);
                }
                // else RhinoApp.WriteLine("Wrong UTCI value");
            }

            Clrs = cl;

        }

        // Not being used

        //private static double CalcVapourPressure(double T_celcius)
        //{
        //    //!~ **********************************************
        //    //!~calculates saturation vapour pressure over water in hPa for input air temperature(ta) in celsius according to:
        //    //!~Hardy, R.; ITS-90 Formulations for Vapor Pressure, Frostpoint Temperature, Dewpoint Temperature and Enhancement Factors in the Range -100 to 100 °C; 
        //    //!~Proceedings of Third International Symposium on Humidity and Moisture; edited by National Physical Laboratory(NPL), London, 1998, pp. 214-221
        //    //!~http://www.thunderscientific.com/tech_info/reflibrary/its90formulas.pdf (retrieved 2008-10-01)

        //    // es = saturation vapour pressure in Pa
        //    // T is temperature in K
        //    // g is list of coefficients for curve fit


        //    double T_kelvin;
        //    //int I;
        //    double[] g = {
        //        -2.8365744E3,
        //        -6.028076559E3,
        //        1.954263612E1,
        //        -2.737830188E-2,
        //        1.6261698E-5,
        //        7.0229056E-10,
        //        -1.8680009E-13,
        //        2.7150305 };

        //    T_kelvin = T_celcius + 273.15;       //! air temp in K
        //    double es = g[7] * Math.Log(T_kelvin);
        //    //do i=0,6
        //    for (int i = 0; i < 6; i++)
        //    {
        //        es = es + g[i] * Math.Pow(T_kelvin, (i - 2));
        //    }
        //    // end do
        //    es = Math.Exp(es) * 0.01;   //! *0.01: convert Pa to hPa

        //    return es;
        //}


        //private static double UTCI_approx(double Ta, double ehPa, double Tmrt, double va)
        //{

        //    //!~DOUBLE PRECISION Function value is the UTCI in degree Celsius
        //    //!~computed by a 6th order approximating polynomial from the 4 Input paramters 
        //    //!~
        //    //!~Input parameters(all of type DOUBLE PRECISION)
        //    //!~ - Ta       : air temperature, degree Celsius
        //    //!~ - ehPa    : water vapour presure, hPa=hecto Pascal
        //    //!~ - Tmrt   : mean radiant temperature, degree Celsius
        //    //!~ - va10m  : wind speed 10 m above ground level in m/s
        //    //!~
        //    //!~UTCI_approx, Version a 0.002, October 2009
        //    //!~Copyright(C) 2009  Peter Broede

        //    // implicit none
        //    //!~type of input of the argument list
        //    // DOUBLE PRECISION Ta,va,Tmrt,ehPa,

        //    // Pa,D_Tmrt;
        //    double D_Tmrt = Tmrt - Ta;
        //    double Pa = ehPa / 10.0; //!~use vapour pressure in kPa
        //                             // !~calculate 6th order polynomial as approximation
        //    double UTCI_approx = Ta +
        //      (6.07562052E-01) +
        //      (-2.27712343E-02) * Ta +
        //      (8.06470249E-04) * Ta * Ta +
        //      (-1.54271372E-04) * Ta * Ta * Ta +
        //      (-3.24651735E-06) * Ta * Ta * Ta * Ta +
        //      (7.32602852E-08) * Ta * Ta * Ta * Ta * Ta +
        //      (1.35959073E-09) * Ta * Ta * Ta * Ta * Ta * Ta +
        //      (-2.25836520D + 00) * va +
        //      (8.80326035E-02) * Ta * va +
        //      (2.16844454E-03) * Ta * Ta * va +
        //      (-1.53347087E-05) * Ta * Ta * Ta * va +
        //      (-5.72983704E-07) * Ta * Ta * Ta * Ta * va +
        //      (-2.55090145E-09) * Ta * Ta * Ta * Ta * Ta * va +
        //      (-7.51269505E-01) * va * va +
        //      (-4.08350271E-03) * Ta * va * va +
        //      (-5.21670675E-05) * Ta * Ta * va * va +
        //      (1.94544667E-06) * Ta * Ta * Ta * va * va +
        //      (1.14099531E-08) * Ta * Ta * Ta * Ta * va * va +
        //      (1.58137256E-01) * va * va * va +
        //      (-6.57263143E-05) * Ta * va * va * va +
        //      (2.22697524E-07) * Ta * Ta * va * va * va +
        //      (-4.16117031E-08) * Ta * Ta * Ta * va * va * va +
        //      (-1.27762753E-02) * va * va * va * va +
        //      (9.66891875E-06) * Ta * va * va * va * va +
        //      (2.52785852E-09) * Ta * Ta * va * va * va * va +
        //      (4.56306672E-04) * va * va * va * va * va +
        //      (-1.74202546E-07) * Ta * va * va * va * va * va +
        //      (-5.91491269E-06) * va * va * va * va * va * va +
        //      (3.98374029E-01) * D_Tmrt +
        //      (1.83945314E-04) * Ta * D_Tmrt +
        //      (-1.73754510E-04) * Ta * Ta * D_Tmrt +
        //      (-7.60781159E-07) * Ta * Ta * Ta * D_Tmrt +
        //      (3.77830287E-08) * Ta * Ta * Ta * Ta * D_Tmrt +
        //      (5.43079673E-10) * Ta * Ta * Ta * Ta * Ta * D_Tmrt +
        //      (-2.00518269E-02) * va * D_Tmrt +
        //      (8.92859837E-04) * Ta * va * D_Tmrt +
        //      (3.45433048E-06) * Ta * Ta * va * D_Tmrt +
        //      (-3.77925774E-07) * Ta * Ta * Ta * va * D_Tmrt +
        //      (-1.69699377E-09) * Ta * Ta * Ta * Ta * va * D_Tmrt +
        //      (1.69992415E-04) * va * va * D_Tmrt +
        //      (-4.99204314E-05) * Ta * va * va * D_Tmrt +
        //      (2.47417178E-07) * Ta * Ta * va * va * D_Tmrt +
        //      (1.07596466E-08) * Ta * Ta * Ta * va * va * D_Tmrt +
        //      (8.49242932E-05) * va * va * va * D_Tmrt +
        //      (1.35191328E-06) * Ta * va * va * va * D_Tmrt +
        //      (-6.21531254E-09) * Ta * Ta * va * va * va * D_Tmrt +
        //      (-4.99410301E-06) * va * va * va * va * D_Tmrt +
        //      (-1.89489258E-08) * Ta * va * va * va * va * D_Tmrt +
        //      (8.15300114E-08) * va * va * va * va * va * D_Tmrt +
        //      (7.55043090E-04) * D_Tmrt * D_Tmrt +
        //      (-5.65095215E-05) * Ta * D_Tmrt * D_Tmrt +
        //      (-4.52166564E-07) * Ta * Ta * D_Tmrt * D_Tmrt +
        //      (2.46688878E-08) * Ta * Ta * Ta * D_Tmrt * D_Tmrt +
        //      (2.42674348E-10) * Ta * Ta * Ta * Ta * D_Tmrt * D_Tmrt +
        //      (1.54547250E-04) * va * D_Tmrt * D_Tmrt +
        //      (5.24110970E-06) * Ta * va * D_Tmrt * D_Tmrt +
        //      (-8.75874982E-08) * Ta * Ta * va * D_Tmrt * D_Tmrt +
        //      (-1.50743064E-09) * Ta * Ta * Ta * va * D_Tmrt * D_Tmrt +
        //      (-1.56236307E-05) * va * va * D_Tmrt * D_Tmrt +
        //      (-1.33895614E-07) * Ta * va * va * D_Tmrt * D_Tmrt +
        //      (2.49709824E-09) * Ta * Ta * va * va * D_Tmrt * D_Tmrt +
        //      (6.51711721E-07) * va * va * va * D_Tmrt * D_Tmrt +
        //      (1.94960053E-09) * Ta * va * va * va * D_Tmrt * D_Tmrt +
        //      (-1.00361113E-08) * va * va * va * va * D_Tmrt * D_Tmrt +
        //      (-1.21206673E-05) * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (-2.18203660E-07) * Ta * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (7.51269482E-09) * Ta * Ta * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (9.79063848E-11) * Ta * Ta * Ta * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (1.25006734E-06) * va * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (-1.81584736E-09) * Ta * va * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (-3.52197671E-10) * Ta * Ta * va * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (-3.36514630E-08) * va * va * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (1.35908359E-10) * Ta * va * va * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (4.17032620E-10) * va * va * va * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (-1.30369025E-09) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (4.13908461E-10) * Ta * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (9.22652254E-12) * Ta * Ta * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (-5.08220384E-09) * va * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (-2.24730961E-11) * Ta * va * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (1.17139133E-10) * va * va * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (6.62154879E-10) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (4.03863260E-13) * Ta * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (1.95087203E-12) * va * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (-4.73602469E-12) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        //      (5.12733497D + 00) * Pa +
        //      (-3.12788561E-01) * Ta * Pa +
        //      (-1.96701861E-02) * Ta * Ta * Pa +
        //      (9.99690870E-04) * Ta * Ta * Ta * Pa +
        //      (9.51738512E-06) * Ta * Ta * Ta * Ta * Pa +
        //      (-4.66426341E-07) * Ta * Ta * Ta * Ta * Ta * Pa +
        //      (5.48050612E-01) * va * Pa +
        //      (-3.30552823E-03) * Ta * va * Pa +
        //      (-1.64119440E-03) * Ta * Ta * va * Pa +
        //      (-5.16670694E-06) * Ta * Ta * Ta * va * Pa +
        //      (9.52692432E-07) * Ta * Ta * Ta * Ta * va * Pa +
        //      (-4.29223622E-02) * va * va * Pa +
        //      (5.00845667E-03) * Ta * va * va * Pa +
        //      (1.00601257E-06) * Ta * Ta * va * va * Pa +
        //      (-1.81748644E-06) * Ta * Ta * Ta * va * va * Pa +
        //      (-1.25813502E-03) * va * va * va * Pa +
        //      (-1.79330391E-04) * Ta * va * va * va * Pa +
        //      (2.34994441E-06) * Ta * Ta * va * va * va * Pa +
        //      (1.29735808E-04) * va * va * va * va * Pa +
        //      (1.29064870E-06) * Ta * va * va * va * va * Pa +
        //      (-2.28558686E-06) * va * va * va * va * va * Pa +
        //      (-3.69476348E-02) * D_Tmrt * Pa +
        //      (1.62325322E-03) * Ta * D_Tmrt * Pa +
        //      (-3.14279680E-05) * Ta * Ta * D_Tmrt * Pa +
        //      (2.59835559E-06) * Ta * Ta * Ta * D_Tmrt * Pa +
        //      (-4.77136523E-08) * Ta * Ta * Ta * Ta * D_Tmrt * Pa +
        //      (8.64203390E-03) * va * D_Tmrt * Pa +
        //      (-6.87405181E-04) * Ta * va * D_Tmrt * Pa +
        //      (-9.13863872E-06) * Ta * Ta * va * D_Tmrt * Pa +
        //      (5.15916806E-07) * Ta * Ta * Ta * va * D_Tmrt * Pa +
        //      (-3.59217476E-05) * va * va * D_Tmrt * Pa +
        //      (3.28696511E-05) * Ta * va * va * D_Tmrt * Pa +
        //      (-7.10542454E-07) * Ta * Ta * va * va * D_Tmrt * Pa +
        //      (-1.24382300E-05) * va * va * va * D_Tmrt * Pa +
        //      (-7.38584400E-09) * Ta * va * va * va * D_Tmrt * Pa +
        //      (2.20609296E-07) * va * va * va * va * D_Tmrt * Pa +
        //      (-7.32469180E-04) * D_Tmrt * D_Tmrt * Pa +
        //      (-1.87381964E-05) * Ta * D_Tmrt * D_Tmrt * Pa +
        //      (4.80925239E-06) * Ta * Ta * D_Tmrt * D_Tmrt * Pa +
        //      (-8.75492040E-08) * Ta * Ta * Ta * D_Tmrt * D_Tmrt * Pa +
        //      (2.77862930E-05) * va * D_Tmrt * D_Tmrt * Pa +
        //      (-5.06004592E-06) * Ta * va * D_Tmrt * D_Tmrt * Pa +
        //      (1.14325367E-07) * Ta * Ta * va * D_Tmrt * D_Tmrt * Pa +
        //      (2.53016723E-06) * va * va * D_Tmrt * D_Tmrt * Pa +
        //      (-1.72857035E-08) * Ta * va * va * D_Tmrt * D_Tmrt * Pa +
        //      (-3.95079398E-08) * va * va * va * D_Tmrt * D_Tmrt * Pa +
        //      (-3.59413173E-07) * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (7.04388046E-07) * Ta * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (-1.89309167E-08) * Ta * Ta * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (-4.79768731E-07) * va * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (7.96079978E-09) * Ta * va * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (1.62897058E-09) * va * va * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (3.94367674E-08) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (-1.18566247E-09) * Ta * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (3.34678041E-10) * va * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (-1.15606447E-10) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        //      (-2.80626406D + 00) * Pa * Pa +
        //      (5.48712484E-01) * Ta * Pa * Pa +
        //      (-3.99428410E-03) * Ta * Ta * Pa * Pa +
        //      (-9.54009191E-04) * Ta * Ta * Ta * Pa * Pa +
        //      (1.93090978E-05) * Ta * Ta * Ta * Ta * Pa * Pa +
        //      (-3.08806365E-01) * va * Pa * Pa +
        //      (1.16952364E-02) * Ta * va * Pa * Pa +
        //      (4.95271903E-04) * Ta * Ta * va * Pa * Pa +
        //      (-1.90710882E-05) * Ta * Ta * Ta * va * Pa * Pa +
        //      (2.10787756E-03) * va * va * Pa * Pa +
        //      (-6.98445738E-04) * Ta * va * va * Pa * Pa +
        //      (2.30109073E-05) * Ta * Ta * va * va * Pa * Pa +
        //      (4.17856590E-04) * va * va * va * Pa * Pa +
        //      (-1.27043871E-05) * Ta * va * va * va * Pa * Pa +
        //      (-3.04620472E-06) * va * va * va * va * Pa * Pa +
        //      (5.14507424E-02) * D_Tmrt * Pa * Pa +
        //      (-4.32510997E-03) * Ta * D_Tmrt * Pa * Pa +
        //      (8.99281156E-05) * Ta * Ta * D_Tmrt * Pa * Pa +
        //      (-7.14663943E-07) * Ta * Ta * Ta * D_Tmrt * Pa * Pa +
        //      (-2.66016305E-04) * va * D_Tmrt * Pa * Pa +
        //      (2.63789586E-04) * Ta * va * D_Tmrt * Pa * Pa +
        //      (-7.01199003E-06) * Ta * Ta * va * D_Tmrt * Pa * Pa +
        //      (-1.06823306E-04) * va * va * D_Tmrt * Pa * Pa +
        //      (3.61341136E-06) * Ta * va * va * D_Tmrt * Pa * Pa +
        //      (2.29748967E-07) * va * va * va * D_Tmrt * Pa * Pa +
        //      (3.04788893E-04) * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (-6.42070836E-05) * Ta * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (1.16257971E-06) * Ta * Ta * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (7.68023384E-06) * va * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (-5.47446896E-07) * Ta * va * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (-3.59937910E-08) * va * va * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (-4.36497725E-06) * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (1.68737969E-07) * Ta * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (2.67489271E-08) * va * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (3.23926897E-09) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa +
        //      (-3.53874123E-02) * Pa * Pa * Pa +
        //      (-2.21201190E-01) * Ta * Pa * Pa * Pa +
        //      (1.55126038E-02) * Ta * Ta * Pa * Pa * Pa +
        //      (-2.63917279E-04) * Ta * Ta * Ta * Pa * Pa * Pa +
        //      (4.53433455E-02) * va * Pa * Pa * Pa +
        //      (-4.32943862E-03) * Ta * va * Pa * Pa * Pa +
        //      (1.45389826E-04) * Ta * Ta * va * Pa * Pa * Pa +
        //      (2.17508610E-04) * va * va * Pa * Pa * Pa +
        //      (-6.66724702E-05) * Ta * va * va * Pa * Pa * Pa +
        //      (3.33217140E-05) * va * va * va * Pa * Pa * Pa +
        //      (-2.26921615E-03) * D_Tmrt * Pa * Pa * Pa +
        //      (3.80261982E-04) * Ta * D_Tmrt * Pa * Pa * Pa +
        //      (-5.45314314E-09) * Ta * Ta * D_Tmrt * Pa * Pa * Pa +
        //      (-7.96355448E-04) * va * D_Tmrt * Pa * Pa * Pa +
        //      (2.53458034E-05) * Ta * va * D_Tmrt * Pa * Pa * Pa +
        //      (-6.31223658E-06) * va * va * D_Tmrt * Pa * Pa * Pa +
        //      (3.02122035E-04) * D_Tmrt * D_Tmrt * Pa * Pa * Pa +
        //      (-4.77403547E-06) * Ta * D_Tmrt * D_Tmrt * Pa * Pa * Pa +
        //      (1.73825715E-06) * va * D_Tmrt * D_Tmrt * Pa * Pa * Pa +
        //      (-4.09087898E-07) * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa * Pa +
        //      (6.14155345E-01) * Pa * Pa * Pa * Pa +
        //      (-6.16755931E-02) * Ta * Pa * Pa * Pa * Pa +
        //      (1.33374846E-03) * Ta * Ta * Pa * Pa * Pa * Pa +
        //      (3.55375387E-03) * va * Pa * Pa * Pa * Pa +
        //      (-5.13027851E-04) * Ta * va * Pa * Pa * Pa * Pa +
        //      (1.02449757E-04) * va * va * Pa * Pa * Pa * Pa +
        //      (-1.48526421E-03) * D_Tmrt * Pa * Pa * Pa * Pa +
        //      (-4.11469183E-05) * Ta * D_Tmrt * Pa * Pa * Pa * Pa +
        //      (-6.80434415E-06) * va * D_Tmrt * Pa * Pa * Pa * Pa +
        //      (-9.77675906E-06) * D_Tmrt * D_Tmrt * Pa * Pa * Pa * Pa +
        //      (8.82773108E-02) * Pa * Pa * Pa * Pa * Pa +
        //      (-3.01859306E-03) * Ta * Pa * Pa * Pa * Pa * Pa +
        //      (1.04452989E-03) * va * Pa * Pa * Pa * Pa * Pa +
        //      (2.47090539E-04) * D_Tmrt * Pa * Pa * Pa * Pa * Pa +
        //      (1.48348065E-03) * Pa * Pa * Pa * Pa * Pa * Pa;

        //    return UTCI_approx;
        //}

        //private static double es(double T_celcius)
        //{
        //    //!~ **********************************************
        //    //!~calculates saturation vapour pressure over water in hPa for input air temperature(ta) in celsius according to:
        //    //!~Hardy, R.; ITS-90 Formulations for Vapor Pressure, Frostpoint Temperature, Dewpoint Temperature and Enhancement Factors in the Range -100 to 100 °C; 
        //    //!~Proceedings of Third International Symposium on Humidity and Moisture; edited by National Physical Laboratory(NPL), London, 1998, pp. 214-221
        //    //!~http://www.thunderscientific.com/tech_info/reflibrary/its90formulas.pdf (retrieved 2008-10-01)

        //    // es = saturation vapour pressure in Pa
        //    // T is temperature in K
        //    // g is list of coefficients for curve fit


        //    double T_kelvin;
        //    //int I;
        //    double[] g = {
        //        -2.8365744E3,
        //        -6.028076559E3,
        //        1.954263612E1,
        //        -2.737830188E-2,
        //        1.6261698E-5,
        //        7.0229056E-10,
        //        -1.8680009E-13,
        //        2.7150305 };

        //    T_kelvin = T_celcius + 273.15;       //! air temp in K
        //    double es = g[7] * Math.Log(T_kelvin);
        //    //do i=0,6
        //    for (int i = 0; i < 6; i++)
        //    {
        //        es = es + g[i] * Math.Pow(T_kelvin, (i - 2));
        //    }
        //    // end do
        //    es = Math.Exp(es) * 0.01;   //! *0.01: convert Pa to hPa

        //    return es;
        //}

       
        

    }
}
