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
        private const int HoursPerYear = 8760;

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

        /// <summary>
        /// Creates a UTCI calculation instance. Loads cached results if available, or calculates fresh values.
        /// </summary>
        /// <param name="probes">Sensor probe locations</param>
        /// <param name="wf">Wind factors temporal data</param>
        /// <param name="weather">Weather data</param>
        /// <param name="mrt">Mean radiant temperature data</param>
        /// <param name="baseWorkingDir">Directory for cached results</param>
        /// <param name="recalc">Force recalculation even if cache exists</param>
        /// <param name="truncateBy">Decimal places for rounding (reserved; constructor uses legacy 1-decimal output)</param>
        public UTCI(Point3d[] probes, WindFactorsTemporal wf, Weather weather, MRT mrt, string baseWorkingDir, bool recalc, int truncateBy = 1)
        {
            var binPath = Path.Combine(baseWorkingDir, "UTCI.bin");
            this.probes = probes;

            // Try to load cached results
            if (!recalc && File.Exists(binPath))
            {
                var cached = RadianceFiles.loadBinD(binPath);

                if (cached.GetLength(1) == probes.Length)
                {
                    // Cache valid - use it
                    ValuesUTCI = cached;
                    ValuesCondition = CalcConditionOfPerson(cached);
                    ValuesAnnualPercentage = CalcAnnualComfortableHours(ValuesCondition);
                    wrongNumberOfProbes = false;
                    resultPrecalculated = true;
                    return;
                }

                // Cache has wrong probe count
                wrongNumberOfProbes = true;
                resultPrecalculated = false;
                return;
            }

            // Calculate fresh results
            if (File.Exists(binPath))
            {
                File.Delete(binPath);
            }

            // Preserve legacy rounding behavior for UTCI time series outputs.
            var result = CalcUTCI(probes, weather, wf, mrt, 1);

            ValuesUTCI = result.Item1;
            ValuesCondition = result.Item2;
            ValuesAnnualPercentage = result.Item3;
            uncertaintyMRTArray = result.Item4;
            uncertaintyWindArray = result.Item5;

            RadianceFiles.writeBin(binPath, ValuesUTCI);
            resultPrecalculated = false;
        }

        //Tuple items: utci, humcondition, valuesAnnualPercentage, uncertaintyMRTArray, uncertaintyWindArray
        public static Tuple<double[,], int[,], double[], bool[,], bool[,]> CalcUTCI(Point3d[] Probes, Weather weather, WindFactorsTemporal wf, MRT mrt, int truncateBy)
        {
            int numberOfHours = HoursPerYear;
            int numberOfProbes = Probes.Length;

            var sw = new Stopwatch();
            sw.Start();
            int processedProbes = 0;

            var uncertaintyMRTArray = new bool[numberOfHours, numberOfProbes];
            var uncertaintyWindArray = new bool[numberOfHours, numberOfProbes];
            var utci = new double[numberOfHours, numberOfProbes];

            var humcondition = new int[numberOfHours, numberOfProbes];
            var valuesAnnualPercentage = new double[numberOfProbes];

            using (var progress = new ASCIIProgressBar())
            {
                Parallel.For(0, numberOfProbes, probe =>
                {
                    var processed = System.Threading.Interlocked.Increment(ref processedProbes);
                    progress.Report((double)processed / numberOfProbes);

                    for (int hour = 0; hour < numberOfHours; hour++)
                    {
                        uncertaintyWindArray[hour, probe] = false;
                        uncertaintyMRTArray[hour, probe] = false;

                        // Check for extreme MRTs

                        double resultingMRT = mrt.Values[hour, probe];

                        if (resultingMRT < weather.DryBulbTemp[hour] - 30) { resultingMRT = weather.DryBulbTemp[hour] - 30; uncertaintyMRTArray[hour, probe] = true; }
                        if (resultingMRT > weather.DryBulbTemp[hour] + 70) { resultingMRT = weather.DryBulbTemp[hour] + 70; uncertaintyMRTArray[hour, probe] = true; }

                        // Check for extreme Windspeeds

                        double resultingWindSpeedforUTCI = wf.ValuesTemporalAtProbingHeight[hour, probe];

                        if (resultingWindSpeedforUTCI > 17) { resultingWindSpeedforUTCI = 17; uncertaintyWindArray[hour, probe] = true; }
                        if (resultingWindSpeedforUTCI < 0.5) { resultingWindSpeedforUTCI = 0.5; uncertaintyWindArray[hour, probe] = true; }

                        // lift to 10 m height as required

                        var resultingWindSpeedforUTCI_At10 = At10Meters(resultingWindSpeedforUTCI, Probes[probe].Z);

                        utci[hour, probe] = CalcUTCI(weather.DryBulbTemp[hour], weather.RelativeHumidity[hour], resultingWindSpeedforUTCI_At10, resultingMRT);
                    }
                });

                // Apply rounding in a separate pass to avoid Math.Round in the hot loop
                if (truncateBy >= 0)
                {
                    Parallel.For(0, numberOfProbes, probe =>
                    {
                        for (int hour = 0; hour < numberOfHours; hour++)
                        {
                            utci[hour, probe] = Math.Round(utci[hour, probe], truncateBy);
                        }
                    });
                }

                humcondition = CalcConditionOfPerson(utci);
                valuesAnnualPercentage = CalcAnnualComfortableHours(humcondition);
            }//end using prog bar

            Console.WriteLine(Utilities.ConvertComputeTimes(sw.ElapsedMilliseconds));
            var elapsedTime = sw.ElapsedMilliseconds;

            return new Tuple<double[,], int[,], double[], bool[,], bool[,]>(utci, humcondition, valuesAnnualPercentage, uncertaintyMRTArray, uncertaintyWindArray);
        }

        public static double CalcUTCICorrectBounds(double TaC_IN, double RH_IN, double Wsp_IN, double MRT_IN, out bool outOfBounds)
        {
            // Br�de, P., Fiala, D., Blazejczyk, K., Holm�r, I., Jendritzky, G., Kampmann, B., Tinz, B., & Havenith, G. (2012). Deriving the operational procedure for the Universal Thermal Climate Index (UTCI). International Journal of Biometeorology, 56(3), 481�494. https://doi.org/10.1007/s00484-011-0454-1

            var TaC_New = TaC_IN;
            var RH_New = RH_IN;
            var MRT_New = MRT_IN;
            var Wsp_New = Wsp_IN;
            outOfBounds = false;

            // Check for extreme ambient temperatures

            if (TaC_IN < (-50)) { TaC_New = -50; outOfBounds = true; }
            else if (TaC_IN > (50)) { TaC_New = 50; outOfBounds = true; }

            // Check for extreme MRTs

            if (MRT_IN - TaC_New < -30) { MRT_New = 30; outOfBounds = true; }
            else if (MRT_IN - TaC_New > 70) { MRT_New = 70; outOfBounds = true; }

            // Check for extreme RH
            if (RH_IN < (5)) { RH_New = 5; outOfBounds = true; }
            else if (RH_IN > (100)) { RH_New = 100; outOfBounds = true; }

            // Check for extreme Windspeeds
            // Tested: Wsp needs to be clamped at 17, otherwise polynomial goes crazy!

            if (Wsp_IN > 17) { Wsp_New = 17; outOfBounds = true; }
            else if (Wsp_IN < 0.5) { Wsp_New = 0.5; outOfBounds = true; }

            double utci = CalcUTCI(TaC_New, RH_New, Wsp_New, MRT_New);

            return utci;
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

        /// <summary>
        /// Bins UTCI stress category values and calculates percentages.
        /// Categories: -5 (extreme cold) to +5 (extreme heat), 0 = no stress
        /// </summary>
        public static void Binning(List<double> Vals,
            ref double ExtrCold,
            ref double VryStrngCold,
            ref double StrngCold,
            ref double MdrtCold,
            ref double SlgtCold,
            ref double NoStress,
            ref double SlgtHeat,
            ref double MdrtHeat,
            ref double StrngHeat,
            ref double VryStrngHeat,
            ref double ExtrHeat)
        {
            if (Vals == null || Vals.Count == 0) return;

            // Group values by category and count occurrences
            var counts = Vals
                .Select(v => (int)Math.Round(v))
                .GroupBy(v => v)
                .ToDictionary(g => g.Key, g => g.Count());

            int total = Vals.Count;
            double GetPct(int category) =>
                counts.TryGetValue(category, out int count)
                    ? Math.Round((double)count / total, 3)
                    : 0.0;

            ExtrCold = GetPct(-5);
            VryStrngCold = GetPct(-4);
            StrngCold = GetPct(-3);
            MdrtCold = GetPct(-2);
            SlgtCold = GetPct(-1);
            NoStress = GetPct(0);
            SlgtHeat = GetPct(1);
            MdrtHeat = GetPct(2);
            StrngHeat = GetPct(3);
            VryStrngHeat = GetPct(4);
            ExtrHeat = GetPct(5);
        }

        /// <summary>
        ///Return the velocity va_h at 10 m from a measured height h for the UTCI calculation as described in
        ///Br�de, P., Fiala, D., Blazejczyk, K., Holm�r, I., Jendritzky, G., Kampmann, B., Tinz, B., & Havenith, G. (2012). Deriving the operational procedure for the Universal Thermal Climate Index(UTCI). International Journal of Biometeorology, 56(3), 481�494. https://doi.org/10.1007/s00484-011-0454-1
        ///</summary>
        public static double At10Meters(double va_h, double h)
        {
            return va_h * Math.Log(10 / 0.01) / Math.Log(h / 0.01);
        }

        /// <summary>
        /// Maps UTCI stress category values to colors for visualization.
        /// Categories: -3 (cold) to +3 (heat)
        /// </summary>
        public static void Colors(List<double> Vals, ref object Clrs)
        {
            var colorMap = new Dictionary<int, Color>
            {
                {  3, Color.Red },        // Strong heat stress
                {  2, Color.DarkOrange }, // Moderate heat stress
                {  1, Color.Yellow },     // Slight heat stress
                {  0, Color.Green },      // No thermal stress
                { -1, Color.Cyan },       // Slight cold stress
                { -2, Color.Blue },       // Moderate cold stress
                { -3, Color.BlueViolet }  // Strong cold stress
            };

            var colors = new List<Color>();
            foreach (int v in Vals)
            {
                if (colorMap.TryGetValue(v, out Color color))
                {
                    colors.Add(color);
                }
            }
            Clrs = colors;
        }
    }
}
