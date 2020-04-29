using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace EddyLib
{
    public class UTCI

    {
        // public static object Options { get; private set; }

        //Outputs

        // [x][] time [][x] points

        public double[,] ValuesUTCI;

        public int[,] ValuesCondition;

        public double[] ValuesAnnualPercentage;

        public bool[,] uncertaintyMRTArray;

        public bool[,] uncertaintyWindArray;

        public long elapsedTime;

        public Point3d[] probes;

        public bool wrongNumberOfProbes;

        public bool resultPrecalculated;

        public UTCI(Point3d[] probes, WindFactors wf, Weather weather, MRT mrt, BoundaryConditions bcond, string baseWorkingDir, bool recalc, int truncateBy = 1)
        {
            var csvUTCI = baseWorkingDir + @"UTCI.csv";
            var binUTCI = baseWorkingDir + @"UTCI.bin";

            this.probes = probes;

            if (recalc == false && File.Exists(binUTCI))
            {
                var temp = RadianceFiles.loadBinD(binUTCI);

                if (temp.GetLength(1) == probes.Length)
                {
                    this.ValuesUTCI = temp;
                    this.ValuesCondition = CalcConditionOfPerson(temp);
                    this.ValuesAnnualPercentage = CalcAnnualComfortableHours(ValuesCondition);
                    this.wrongNumberOfProbes = false;
                    this.resultPrecalculated = true;
                    return;
                }
                else
                {
                    this.wrongNumberOfProbes = true;
                    this.resultPrecalculated = false;
                    return;
                }
            }
            if (recalc == true)
            {
                if (File.Exists(csvUTCI))
                {
                    File.Delete(csvUTCI);
                }
                if (File.Exists(binUTCI))
                {
                    File.Delete(binUTCI);
                }

                var res = CalcUTCI(probes, weather, wf, mrt, 1);

                this.ValuesUTCI = res.Item1;
                this.ValuesCondition = res.Item2;
                this.ValuesAnnualPercentage = res.Item3;
                this.uncertaintyMRTArray = res.Item4;
                this.uncertaintyWindArray = res.Item5;

                RadianceFiles.writeBin(binUTCI, this.ValuesUTCI);
                ArrayHelper._2DArray2CSV(this.ValuesUTCI, csvUTCI, true);
            }
        }

        //Tuple items: utci, humcondition, valuesAnnualPercentage, uncertaintyMRTArray, uncertaintyWindArray
        public static Tuple<double[,], int[,], double[], bool[,], bool[,]> CalcUTCI(Point3d[] Probes, Weather weather, WindFactors wf, MRT mrt, int truncateBy)
        {
            int numberOfHours = 8760;
            int numberOfProbes = Probes.Length;

            var sw = new Stopwatch();
            sw.Start();
            int cnt = 0;

            var uncertaintyMRTArray = new bool[numberOfHours, numberOfProbes];
            var uncertaintyWindArray = new bool[numberOfHours, numberOfProbes];
            var utci = new double[numberOfHours, numberOfProbes];

            var humcondition = new int[numberOfHours, numberOfProbes];
            var valuesAnnualPercentage = new double[numberOfProbes];

            using (var progress = new ASCIIProgressBar())
            {
                Parallel.For(0, numberOfProbes, probe =>
                {
                    cnt++;
                    progress.Report((double)cnt / numberOfProbes);

                    for (int hour = 0; hour < numberOfHours; hour++)
                    {
                        uncertaintyWindArray[hour, probe] = false;
                        uncertaintyMRTArray[hour, probe] = false;

                        // Check for extreme MRTs

                        double resultingMRT = mrt.Values[hour, probe];

                        if (resultingMRT < weather.DryBulbTemp[hour] - 30) { resultingMRT = weather.DryBulbTemp[hour] - 30; uncertaintyMRTArray[hour, probe] = true; }
                        if (resultingMRT > weather.DryBulbTemp[hour] + 70) { resultingMRT = weather.DryBulbTemp[hour] + 70; uncertaintyMRTArray[hour, probe] = true; }

                        // Check for extreme Windspeeds

                        double resultingWindSpeedforUTCI = wf.ValuesWindFactors[hour, probe];

                        if (resultingWindSpeedforUTCI > 17) { resultingWindSpeedforUTCI = 17; uncertaintyWindArray[hour, probe] = true; }
                        if (resultingWindSpeedforUTCI < 0.5) { resultingWindSpeedforUTCI = 0.5; uncertaintyWindArray[hour, probe] = true; }

                        // lift to 10 m height as required

                        resultingWindSpeedforUTCI = At10Meters(resultingWindSpeedforUTCI, Probes[probe].Z);

                        utci[hour, probe] = Math.Round(CalcUTCI(weather.DryBulbTemp[hour], weather.RelativeHumidity[hour], resultingWindSpeedforUTCI, resultingMRT), truncateBy);
                    }
                });

                humcondition = CalcConditionOfPerson(utci);
                valuesAnnualPercentage = CalcAnnualComfortableHours(humcondition);
            }//end using prog bar

            Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));
            var elapsedTime = sw.ElapsedMilliseconds;

            return new Tuple<double[,], int[,], double[], bool[,], bool[,]>(utci, humcondition, valuesAnnualPercentage, uncertaintyMRTArray, uncertaintyWindArray);
        }

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

        private static double CalcPa(double TaC, double RH)
        {
            double pa_temp = 0;
            double TaK = TaC + 273;

            pa_temp = Math.Exp(2.7150305 * Math.Log(TaK) - 2.8365744 * 1000 * Math.Pow(TaK, -2) - 6.028076559 * 1000 * Math.Pow(TaK, -1)
              + 1.954263612 * 10 - 2.737830188 / 100 * Math.Pow(TaK, 1) + 1.6261698 / 100000 * Math.Pow(TaK, 2) + 7.0229056 * Math.Pow(10, -10) * Math.Pow(TaK, 3)
              - 1.8680009 * Math.Pow(10, -13) * Math.Pow(TaK, 4)) * 0.01 * RH / 1000;

            return pa_temp;
        }

        public static void Binning(List<double> Vals, ref object StrngCold, ref object MdrtCold, ref object SlgtCold, ref object NoStress, ref object SlgtHeat, ref object MdrtHeat, ref object StrngHeat)
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

        //public static void ConditionOfPerson(List<double> UTCI, ref object conditionOfPerson)
        //{
        //    List<double> rtl = new List<double>();
        //    double condition = 0;

        // for (int i = 0; i < UTCI.Count; i++) { condition = UTCI[i]; if (UTCI[i] < -13) { condition
        // = -3; } else if ((-13 <= UTCI[i]) && (UTCI[i] < 0)) { condition = -2; } else if ((0 <=
        // UTCI[i]) && (UTCI[i] < 9)) { condition = -1; } else if ((9 <= UTCI[i]) && (UTCI[i] < 26))
        // { condition = 0; } else if ((26 <= UTCI[i]) && (UTCI[i] < 28)) { condition = 1; } else if
        // ((28 <= UTCI[i]) && (UTCI[i] < 32)) { condition = 2; } else { condition = 3; }

        // rtl.Add(condition); }

        //    conditionOfPerson = rtl;
        //}

        ///<summary>
        ///Return the velocity va_h at 10 m from a measured height h for the UTCI calculation as described in
        ///Bröde, P., Fiala, D., Błażejczyk, K., Holmér, I., Jendritzky, G., Kampmann, B., Tinz, B., & Havenith, G. (2012). Deriving the operational procedure for the Universal Thermal Climate Index(UTCI). International Journal of Biometeorology, 56(3), 481–494. https://doi.org/10.1007/s00484-011-0454-1
        ///</summary>
        public static double At10Meters(double va_h, double h)
        {
            return va_h * Math.Log(10 / 0.01) / Math.Log(h / 0.01);
        }

        public static void Colors(List<double> Vals, ref object Clrs)
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

        // // es = saturation vapour pressure in Pa // T is temperature in K // g is list of
        // coefficients for curve fit

        // double T_kelvin; //int I; double[] g = {
        // -2.8365744E3,
        // -6.028076559E3, 1.954263612E1,
        // -2.737830188E-2, 1.6261698E-5, 7.0229056E-10,
        // -1.8680009E-13, 2.7150305 };

        // T_kelvin = T_celcius + 273.15; //! air temp in K double es = g[7] * Math.Log(T_kelvin);
        // //do i=0,6 for (int i = 0; i < 6; i++) { es = es + g[i] * Math.Pow(T_kelvin, (i - 2)); }
        // // end do es = Math.Exp(es) * 0.01; //! *0.01: convert Pa to hPa

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

        // // implicit none //!~type of input of the argument list // DOUBLE PRECISION Ta,va,Tmrt,ehPa,

        // // Pa,D_Tmrt; double D_Tmrt = Tmrt - Ta; double Pa = ehPa / 10.0; //!~use vapour pressure
        // in kPa // !~calculate 6th order polynomial as approximation double UTCI_approx = Ta +
        // (6.07562052E-01) + (-2.27712343E-02) * Ta + (8.06470249E-04) * Ta * Ta + (-1.54271372E-04)
        // * Ta * Ta * Ta + (-3.24651735E-06) * Ta * Ta * Ta * Ta + (7.32602852E-08) * Ta * Ta * Ta *
        // Ta * Ta + (1.35959073E-09) * Ta * Ta * Ta * Ta * Ta * Ta + (-2.25836520D + 00) * va +
        // (8.80326035E-02) * Ta * va + (2.16844454E-03) * Ta * Ta * va + (-1.53347087E-05) * Ta * Ta
        // * Ta * va + (-5.72983704E-07) * Ta * Ta * Ta * Ta * va + (-2.55090145E-09) * Ta * Ta * Ta
        // * Ta * Ta * va + (-7.51269505E-01) * va * va + (-4.08350271E-03) * Ta * va * va +
        // (-5.21670675E-05) * Ta * Ta * va * va + (1.94544667E-06) * Ta * Ta * Ta * va * va +
        // (1.14099531E-08) * Ta * Ta * Ta * Ta * va * va + (1.58137256E-01) * va * va * va +
        // (-6.57263143E-05) * Ta * va * va * va + (2.22697524E-07) * Ta * Ta * va * va * va +
        // (-4.16117031E-08) * Ta * Ta * Ta * va * va * va + (-1.27762753E-02) * va * va * va * va +
        // (9.66891875E-06) * Ta * va * va * va * va + (2.52785852E-09) * Ta * Ta * va * va * va * va
        // + (4.56306672E-04) * va * va * va * va * va + (-1.74202546E-07) * Ta * va * va * va * va *
        // va + (-5.91491269E-06) * va * va * va * va * va * va + (3.98374029E-01) * D_Tmrt +
        // (1.83945314E-04) * Ta * D_Tmrt + (-1.73754510E-04) * Ta * Ta * D_Tmrt + (-7.60781159E-07)
        // * Ta * Ta * Ta * D_Tmrt + (3.77830287E-08) * Ta * Ta * Ta * Ta * D_Tmrt + (5.43079673E-10)
        // * Ta * Ta * Ta * Ta * Ta * D_Tmrt + (-2.00518269E-02) * va * D_Tmrt + (8.92859837E-04) *
        // Ta * va * D_Tmrt + (3.45433048E-06) * Ta * Ta * va * D_Tmrt + (-3.77925774E-07) * Ta * Ta
        // * Ta * va * D_Tmrt + (-1.69699377E-09) * Ta * Ta * Ta * Ta * va * D_Tmrt +
        // (1.69992415E-04) * va * va * D_Tmrt + (-4.99204314E-05) * Ta * va * va * D_Tmrt +
        // (2.47417178E-07) * Ta * Ta * va * va * D_Tmrt + (1.07596466E-08) * Ta * Ta * Ta * va * va
        // * D_Tmrt + (8.49242932E-05) * va * va * va * D_Tmrt + (1.35191328E-06) * Ta * va * va * va
        // * D_Tmrt + (-6.21531254E-09) * Ta * Ta * va * va * va * D_Tmrt + (-4.99410301E-06) * va *
        // va * va * va * D_Tmrt + (-1.89489258E-08) * Ta * va * va * va * va * D_Tmrt +
        // (8.15300114E-08) * va * va * va * va * va * D_Tmrt + (7.55043090E-04) * D_Tmrt * D_Tmrt +
        // (-5.65095215E-05) * Ta * D_Tmrt * D_Tmrt + (-4.52166564E-07) * Ta * Ta * D_Tmrt * D_Tmrt +
        // (2.46688878E-08) * Ta * Ta * Ta * D_Tmrt * D_Tmrt + (2.42674348E-10) * Ta * Ta * Ta * Ta *
        // D_Tmrt * D_Tmrt + (1.54547250E-04) * va * D_Tmrt * D_Tmrt + (5.24110970E-06) * Ta * va *
        // D_Tmrt * D_Tmrt + (-8.75874982E-08) * Ta * Ta * va * D_Tmrt * D_Tmrt + (-1.50743064E-09) *
        // Ta * Ta * Ta * va * D_Tmrt * D_Tmrt + (-1.56236307E-05) * va * va * D_Tmrt * D_Tmrt +
        // (-1.33895614E-07) * Ta * va * va * D_Tmrt * D_Tmrt + (2.49709824E-09) * Ta * Ta * va * va
        // * D_Tmrt * D_Tmrt + (6.51711721E-07) * va * va * va * D_Tmrt * D_Tmrt + (1.94960053E-09) *
        // Ta * va * va * va * D_Tmrt * D_Tmrt + (-1.00361113E-08) * va * va * va * va * D_Tmrt *
        // D_Tmrt + (-1.21206673E-05) * D_Tmrt * D_Tmrt * D_Tmrt + (-2.18203660E-07) * Ta * D_Tmrt *
        // D_Tmrt * D_Tmrt + (7.51269482E-09) * Ta * Ta * D_Tmrt * D_Tmrt * D_Tmrt + (9.79063848E-11)
        // * Ta * Ta * Ta * D_Tmrt * D_Tmrt * D_Tmrt + (1.25006734E-06) * va * D_Tmrt * D_Tmrt *
        // D_Tmrt + (-1.81584736E-09) * Ta * va * D_Tmrt * D_Tmrt * D_Tmrt + (-3.52197671E-10) * Ta *
        // Ta * va * D_Tmrt * D_Tmrt * D_Tmrt + (-3.36514630E-08) * va * va * D_Tmrt * D_Tmrt *
        // D_Tmrt + (1.35908359E-10) * Ta * va * va * D_Tmrt * D_Tmrt * D_Tmrt + (4.17032620E-10) *
        // va * va * va * D_Tmrt * D_Tmrt * D_Tmrt + (-1.30369025E-09) * D_Tmrt * D_Tmrt * D_Tmrt *
        // D_Tmrt + (4.13908461E-10) * Ta * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt + (9.22652254E-12) * Ta
        // * Ta * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt + (-5.08220384E-09) * va * D_Tmrt * D_Tmrt *
        // D_Tmrt * D_Tmrt + (-2.24730961E-11) * Ta * va * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        // (1.17139133E-10) * va * va * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt + (6.62154879E-10) * D_Tmrt
        // * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt + (4.03863260E-13) * Ta * D_Tmrt * D_Tmrt * D_Tmrt *
        // D_Tmrt * D_Tmrt + (1.95087203E-12) * va * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt +
        // (-4.73602469E-12) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt + (5.12733497D +
        // 00) * Pa + (-3.12788561E-01) * Ta * Pa + (-1.96701861E-02) * Ta * Ta * Pa +
        // (9.99690870E-04) * Ta * Ta * Ta * Pa + (9.51738512E-06) * Ta * Ta * Ta * Ta * Pa +
        // (-4.66426341E-07) * Ta * Ta * Ta * Ta * Ta * Pa + (5.48050612E-01) * va * Pa +
        // (-3.30552823E-03) * Ta * va * Pa + (-1.64119440E-03) * Ta * Ta * va * Pa +
        // (-5.16670694E-06) * Ta * Ta * Ta * va * Pa + (9.52692432E-07) * Ta * Ta * Ta * Ta * va *
        // Pa + (-4.29223622E-02) * va * va * Pa + (5.00845667E-03) * Ta * va * va * Pa +
        // (1.00601257E-06) * Ta * Ta * va * va * Pa + (-1.81748644E-06) * Ta * Ta * Ta * va * va *
        // Pa + (-1.25813502E-03) * va * va * va * Pa + (-1.79330391E-04) * Ta * va * va * va * Pa +
        // (2.34994441E-06) * Ta * Ta * va * va * va * Pa + (1.29735808E-04) * va * va * va * va * Pa
        // + (1.29064870E-06) * Ta * va * va * va * va * Pa + (-2.28558686E-06) * va * va * va * va *
        // va * Pa + (-3.69476348E-02) * D_Tmrt * Pa + (1.62325322E-03) * Ta * D_Tmrt * Pa +
        // (-3.14279680E-05) * Ta * Ta * D_Tmrt * Pa + (2.59835559E-06) * Ta * Ta * Ta * D_Tmrt * Pa
        // + (-4.77136523E-08) * Ta * Ta * Ta * Ta * D_Tmrt * Pa + (8.64203390E-03) * va * D_Tmrt *
        // Pa + (-6.87405181E-04) * Ta * va * D_Tmrt * Pa + (-9.13863872E-06) * Ta * Ta * va * D_Tmrt
        // * Pa + (5.15916806E-07) * Ta * Ta * Ta * va * D_Tmrt * Pa + (-3.59217476E-05) * va * va *
        // D_Tmrt * Pa + (3.28696511E-05) * Ta * va * va * D_Tmrt * Pa + (-7.10542454E-07) * Ta * Ta
        // * va * va * D_Tmrt * Pa + (-1.24382300E-05) * va * va * va * D_Tmrt * Pa +
        // (-7.38584400E-09) * Ta * va * va * va * D_Tmrt * Pa + (2.20609296E-07) * va * va * va * va
        // * D_Tmrt * Pa + (-7.32469180E-04) * D_Tmrt * D_Tmrt * Pa + (-1.87381964E-05) * Ta * D_Tmrt
        // * D_Tmrt * Pa + (4.80925239E-06) * Ta * Ta * D_Tmrt * D_Tmrt * Pa + (-8.75492040E-08) * Ta
        // * Ta * Ta * D_Tmrt * D_Tmrt * Pa + (2.77862930E-05) * va * D_Tmrt * D_Tmrt * Pa +
        // (-5.06004592E-06) * Ta * va * D_Tmrt * D_Tmrt * Pa + (1.14325367E-07) * Ta * Ta * va *
        // D_Tmrt * D_Tmrt * Pa + (2.53016723E-06) * va * va * D_Tmrt * D_Tmrt * Pa +
        // (-1.72857035E-08) * Ta * va * va * D_Tmrt * D_Tmrt * Pa + (-3.95079398E-08) * va * va * va
        // * D_Tmrt * D_Tmrt * Pa + (-3.59413173E-07) * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        // (7.04388046E-07) * Ta * D_Tmrt * D_Tmrt * D_Tmrt * Pa + (-1.89309167E-08) * Ta * Ta *
        // D_Tmrt * D_Tmrt * D_Tmrt * Pa + (-4.79768731E-07) * va * D_Tmrt * D_Tmrt * D_Tmrt * Pa +
        // (7.96079978E-09) * Ta * va * D_Tmrt * D_Tmrt * D_Tmrt * Pa + (1.62897058E-09) * va * va *
        // D_Tmrt * D_Tmrt * D_Tmrt * Pa + (3.94367674E-08) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa
        // + (-1.18566247E-09) * Ta * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa + (3.34678041E-10) * va
        // * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa + (-1.15606447E-10) * D_Tmrt * D_Tmrt * D_Tmrt *
        // D_Tmrt * D_Tmrt * Pa + (-2.80626406D + 00) * Pa * Pa + (5.48712484E-01) * Ta * Pa * Pa +
        // (-3.99428410E-03) * Ta * Ta * Pa * Pa + (-9.54009191E-04) * Ta * Ta * Ta * Pa * Pa +
        // (1.93090978E-05) * Ta * Ta * Ta * Ta * Pa * Pa + (-3.08806365E-01) * va * Pa * Pa +
        // (1.16952364E-02) * Ta * va * Pa * Pa + (4.95271903E-04) * Ta * Ta * va * Pa * Pa +
        // (-1.90710882E-05) * Ta * Ta * Ta * va * Pa * Pa + (2.10787756E-03) * va * va * Pa * Pa +
        // (-6.98445738E-04) * Ta * va * va * Pa * Pa + (2.30109073E-05) * Ta * Ta * va * va * Pa *
        // Pa + (4.17856590E-04) * va * va * va * Pa * Pa + (-1.27043871E-05) * Ta * va * va * va *
        // Pa * Pa + (-3.04620472E-06) * va * va * va * va * Pa * Pa + (5.14507424E-02) * D_Tmrt * Pa
        // * Pa + (-4.32510997E-03) * Ta * D_Tmrt * Pa * Pa + (8.99281156E-05) * Ta * Ta * D_Tmrt *
        // Pa * Pa + (-7.14663943E-07) * Ta * Ta * Ta * D_Tmrt * Pa * Pa + (-2.66016305E-04) * va *
        // D_Tmrt * Pa * Pa + (2.63789586E-04) * Ta * va * D_Tmrt * Pa * Pa + (-7.01199003E-06) * Ta
        // * Ta * va * D_Tmrt * Pa * Pa + (-1.06823306E-04) * va * va * D_Tmrt * Pa * Pa +
        // (3.61341136E-06) * Ta * va * va * D_Tmrt * Pa * Pa + (2.29748967E-07) * va * va * va *
        // D_Tmrt * Pa * Pa + (3.04788893E-04) * D_Tmrt * D_Tmrt * Pa * Pa + (-6.42070836E-05) * Ta *
        // D_Tmrt * D_Tmrt * Pa * Pa + (1.16257971E-06) * Ta * Ta * D_Tmrt * D_Tmrt * Pa * Pa +
        // (7.68023384E-06) * va * D_Tmrt * D_Tmrt * Pa * Pa + (-5.47446896E-07) * Ta * va * D_Tmrt *
        // D_Tmrt * Pa * Pa + (-3.59937910E-08) * va * va * D_Tmrt * D_Tmrt * Pa * Pa +
        // (-4.36497725E-06) * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa + (1.68737969E-07) * Ta * D_Tmrt *
        // D_Tmrt * D_Tmrt * Pa * Pa + (2.67489271E-08) * va * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa +
        // (3.23926897E-09) * D_Tmrt * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa + (-3.53874123E-02) * Pa *
        // Pa * Pa + (-2.21201190E-01) * Ta * Pa * Pa * Pa + (1.55126038E-02) * Ta * Ta * Pa * Pa *
        // Pa + (-2.63917279E-04) * Ta * Ta * Ta * Pa * Pa * Pa + (4.53433455E-02) * va * Pa * Pa *
        // Pa + (-4.32943862E-03) * Ta * va * Pa * Pa * Pa + (1.45389826E-04) * Ta * Ta * va * Pa *
        // Pa * Pa + (2.17508610E-04) * va * va * Pa * Pa * Pa + (-6.66724702E-05) * Ta * va * va *
        // Pa * Pa * Pa + (3.33217140E-05) * va * va * va * Pa * Pa * Pa + (-2.26921615E-03) * D_Tmrt
        // * Pa * Pa * Pa + (3.80261982E-04) * Ta * D_Tmrt * Pa * Pa * Pa + (-5.45314314E-09) * Ta *
        // Ta * D_Tmrt * Pa * Pa * Pa + (-7.96355448E-04) * va * D_Tmrt * Pa * Pa * Pa +
        // (2.53458034E-05) * Ta * va * D_Tmrt * Pa * Pa * Pa + (-6.31223658E-06) * va * va * D_Tmrt
        // * Pa * Pa * Pa + (3.02122035E-04) * D_Tmrt * D_Tmrt * Pa * Pa * Pa + (-4.77403547E-06) *
        // Ta * D_Tmrt * D_Tmrt * Pa * Pa * Pa + (1.73825715E-06) * va * D_Tmrt * D_Tmrt * Pa * Pa *
        // Pa + (-4.09087898E-07) * D_Tmrt * D_Tmrt * D_Tmrt * Pa * Pa * Pa + (6.14155345E-01) * Pa *
        // Pa * Pa * Pa + (-6.16755931E-02) * Ta * Pa * Pa * Pa * Pa + (1.33374846E-03) * Ta * Ta *
        // Pa * Pa * Pa * Pa + (3.55375387E-03) * va * Pa * Pa * Pa * Pa + (-5.13027851E-04) * Ta *
        // va * Pa * Pa * Pa * Pa + (1.02449757E-04) * va * va * Pa * Pa * Pa * Pa +
        // (-1.48526421E-03) * D_Tmrt * Pa * Pa * Pa * Pa + (-4.11469183E-05) * Ta * D_Tmrt * Pa * Pa
        // * Pa * Pa + (-6.80434415E-06) * va * D_Tmrt * Pa * Pa * Pa * Pa + (-9.77675906E-06) *
        // D_Tmrt * D_Tmrt * Pa * Pa * Pa * Pa + (8.82773108E-02) * Pa * Pa * Pa * Pa * Pa +
        // (-3.01859306E-03) * Ta * Pa * Pa * Pa * Pa * Pa + (1.04452989E-03) * va * Pa * Pa * Pa *
        // Pa * Pa + (2.47090539E-04) * D_Tmrt * Pa * Pa * Pa * Pa * Pa + (1.48348065E-03) * Pa * Pa
        // * Pa * Pa * Pa * Pa;

        //    return UTCI_approx;
        //}

        //private static double es(double T_celcius)
        //{
        //    //!~ **********************************************
        //    //!~calculates saturation vapour pressure over water in hPa for input air temperature(ta) in celsius according to:
        //    //!~Hardy, R.; ITS-90 Formulations for Vapor Pressure, Frostpoint Temperature, Dewpoint Temperature and Enhancement Factors in the Range -100 to 100 °C;
        //    //!~Proceedings of Third International Symposium on Humidity and Moisture; edited by National Physical Laboratory(NPL), London, 1998, pp. 214-221
        //    //!~http://www.thunderscientific.com/tech_info/reflibrary/its90formulas.pdf (retrieved 2008-10-01)

        // // es = saturation vapour pressure in Pa // T is temperature in K // g is list of
        // coefficients for curve fit

        // double T_kelvin; //int I; double[] g = {
        // -2.8365744E3,
        // -6.028076559E3, 1.954263612E1,
        // -2.737830188E-2, 1.6261698E-5, 7.0229056E-10,
        // -1.8680009E-13, 2.7150305 };

        // T_kelvin = T_celcius + 273.15; //! air temp in K double es = g[7] * Math.Log(T_kelvin);
        // //do i=0,6 for (int i = 0; i < 6; i++) { es = es + g[i] * Math.Pow(T_kelvin, (i - 2)); }
        // // end do es = Math.Exp(es) * 0.01; //! *0.01: convert Pa to hPa

        //    return es;
        //}
    }
}