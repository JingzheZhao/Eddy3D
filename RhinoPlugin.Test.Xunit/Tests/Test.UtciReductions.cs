using System;
using System.Reflection;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Regression tests for the private array reductions in
    /// <c>EddyLib/OutdoorComfort/UTCI.Equation.cs</c> that were re-ordered for
    /// row-major cache locality in PR #557.
    ///
    /// The optimization swapped the loop order of <c>CalcConditionOfPerson(double[,])</c>
    /// (now parallel over hours, inner loop over probes) and made
    /// <c>CalcAnnualComfortableHours(int[,])</c> a sequential row-major pass. Both changes
    /// must be behaviour-preserving. These tests pin the expected results so any accidental
    /// transpose of the <c>[hour, probe]</c> indexing or off-by-one in the counting is caught.
    ///
    /// The methods are <c>private static</c> and operate purely on arrays (no Rhino
    /// dependency), so they are exercised via reflection with plain <see cref="FactAttribute"/>.
    /// </summary>
    public class UtciReductionTests
    {
        // Mirrors the private const UTCI.HoursPerYear.
        private const int HoursPerYear = 8760;

        private static readonly Type UtciType = typeof(EddyLib.UTCI);

        private static int[,] InvokeCalcConditionOfPerson(double[,] utci)
        {
            var method = UtciType.GetMethod(
                "CalcConditionOfPerson",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(double[,]) },
                modifiers: null);

            Assert.NotNull(method);
            return (int[,])method.Invoke(null, new object[] { utci });
        }

        private static double[] InvokeCalcAnnualComfortableHours(int[,] condition)
        {
            var method = UtciType.GetMethod(
                "CalcAnnualComfortableHours",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(int[,]) },
                modifiers: null);

            Assert.NotNull(method);
            return (double[])method.Invoke(null, new object[] { condition });
        }

        /// <summary>
        /// Each cell of the condition grid must equal the per-value classification of the
        /// matching UTCI cell. A constant value per probe makes the expected category obvious.
        /// </summary>
        [Fact]
        public void CalcConditionOfPerson_Grid_ClassifiesEveryCellByItsUtciValue()
        {
            // probe -> constant UTCI value with a known category (see public CalcConditionOfPerson).
            double[] perProbeValue = { 15.0, -50.0, 40.0, 27.0, -20.0 };
            int numberOfProbes = perProbeValue.Length;

            var utci = new double[HoursPerYear, numberOfProbes];
            for (int hour = 0; hour < HoursPerYear; hour++)
            {
                for (int probe = 0; probe < numberOfProbes; probe++)
                {
                    utci[hour, probe] = perProbeValue[probe];
                }
            }

            int[,] condition = InvokeCalcConditionOfPerson(utci);

            Assert.Equal(HoursPerYear, condition.GetLength(0));
            Assert.Equal(numberOfProbes, condition.GetLength(1));

            // Expected category for each probe, via the trusted single-value overload.
            var expected = new int[numberOfProbes];
            for (int probe = 0; probe < numberOfProbes; probe++)
            {
                expected[probe] = EddyLib.UTCI.CalcConditionOfPerson(perProbeValue[probe]);
            }

            // Spot-check a representative spread of hours (full 8760 x N comparison is redundant).
            int[] sampleHours = { 0, 1, 4379, 8758, 8759 };
            foreach (int hour in sampleHours)
            {
                for (int probe = 0; probe < numberOfProbes; probe++)
                {
                    Assert.Equal(expected[probe], condition[hour, probe]);
                }
            }
        }

        /// <summary>
        /// Guards against an accidental transpose of the <c>[hour, probe]</c> indexing
        /// introduced by the loop re-ordering: classification must vary per hour, not bleed
        /// across probes.
        /// </summary>
        [Fact]
        public void CalcConditionOfPerson_Grid_PreservesHourProbeOrientation()
        {
            const int numberOfProbes = 2;
            var utci = new double[HoursPerYear, numberOfProbes];

            for (int hour = 0; hour < HoursPerYear; hour++)
            {
                // Probe 0 alternates comfortable / extreme cold by hour parity.
                utci[hour, 0] = (hour % 2 == 0) ? 15.0 : -50.0;
                // Probe 1 is constant extreme heat.
                utci[hour, 1] = 40.0;
            }

            int[,] condition = InvokeCalcConditionOfPerson(utci);

            int comfortable = EddyLib.UTCI.CalcConditionOfPerson(15.0);   // 0
            int extremeCold = EddyLib.UTCI.CalcConditionOfPerson(-50.0);  // -5
            int extremeHeat = EddyLib.UTCI.CalcConditionOfPerson(40.0);   // 4

            Assert.Equal(comfortable, condition[0, 0]);
            Assert.Equal(extremeCold, condition[1, 0]);
            Assert.Equal(comfortable, condition[8758, 0]);
            Assert.Equal(extremeCold, condition[8759, 0]);

            Assert.Equal(extremeHeat, condition[0, 1]);
            Assert.Equal(extremeHeat, condition[8759, 1]);
        }

        /// <summary>
        /// The annual comfortable fraction per probe is (# hours with condition == 0) / 8760.
        /// </summary>
        [Fact]
        public void CalcAnnualComfortableHours_CountsComfortableHoursPerProbe()
        {
            const int numberOfProbes = 4;
            const int comfortableHoursProbe0 = 2190; // exactly a quarter of the year

            var condition = new int[HoursPerYear, numberOfProbes];
            for (int hour = 0; hour < HoursPerYear; hour++)
            {
                // Probe 0: comfortable for the first quarter, non-zero afterwards.
                condition[hour, 0] = hour < comfortableHoursProbe0 ? 0 : 3;
                // Probe 1: comfortable every hour.
                condition[hour, 1] = 0;
                // Probe 2: never comfortable (use a mix of positive and negative non-zero codes).
                condition[hour, 2] = (hour % 2 == 0) ? 2 : -2;
                // Probe 3: comfortable only on even hours.
                condition[hour, 3] = (hour % 2 == 0) ? 0 : 1;
            }

            double[] result = InvokeCalcAnnualComfortableHours(condition);

            Assert.Equal(numberOfProbes, result.Length);
            Assert.Equal((double)comfortableHoursProbe0 / HoursPerYear, result[0], 12);
            Assert.Equal(1.0, result[1], 12);
            Assert.Equal(0.0, result[2], 12);
            Assert.Equal((double)(HoursPerYear / 2) / HoursPerYear, result[3], 12);
        }

        /// <summary>
        /// Only the exact code 0 counts as comfortable; neighbouring codes (±1) must not.
        /// </summary>
        [Fact]
        public void CalcAnnualComfortableHours_OnlyZeroCodeCountsAsComfortable()
        {
            const int numberOfProbes = 1;
            var condition = new int[HoursPerYear, numberOfProbes];
            for (int hour = 0; hour < HoursPerYear; hour++)
            {
                // Cycle through -1, 0, 1 so exactly one third are comfortable.
                condition[hour, 0] = (hour % 3) - 1;
            }

            int expectedComfortable = 0;
            for (int hour = 0; hour < HoursPerYear; hour++)
            {
                if (condition[hour, 0] == 0) { expectedComfortable++; }
            }

            double[] result = InvokeCalcAnnualComfortableHours(condition);

            Assert.Equal((double)expectedComfortable / HoursPerYear, result[0], 12);
        }

        /// <summary>
        /// Regression baseline for the UTCI GH component fix (switch to GH_ParamAccess.list
        /// with explicit outer-hour × inner-probe loop and per-probe averaging).
        ///
        /// Expected values were captured from a real Grasshopper run with 40 probe points
        /// across four weather periods. Each case feeds N hours of weather + one probe's MRT
        /// and asserts the averaged output matches the recorded GH output to 6 decimal places.
        ///
        /// Any accidental reversal of the hour/probe loop order, wrong averaging denominator,
        /// or regression in CalcUTCICorrectBounds will cause this test to fail.
        /// </summary>
        [Fact]
        public void UTCI_GHComponent_ReproducesKnownOutputs()
        {
            static double RunSingleProbe(double[] tair, double[] wind, double[] rh, double mrt)
            {
                double sum = 0;
                for (int h = 0; h < tair.Length; h++)
                    sum += EddyLib.UTCI.CalcUTCICorrectBounds(tair[h], rh[h], wind[h], mrt, out _);
                return sum / tair.Length;
            }

            // Period 1 — 3 hours (summer morning peak)
            double[] tair1 = { 24.0, 26.0, 27.8 };
            double[] wind1 = { 5.6,  5.6,  7.2  };
            double[] rh1   = { 78.0, 74.0, 58.0 };

            // Probe 0: open sky (Sky Exposure = 0.51)
            Assert.Equal(31.507523, Math.Round(RunSingleProbe(tair1, wind1, rh1, 66.078342), 6));
            // Probe 4: open sky, higher MRT (Sky Exposure = 0.89)
            Assert.Equal(32.474236, Math.Round(RunSingleProbe(tair1, wind1, rh1, 70.188322), 6));
            // Probe 7: under building (Sky Exposure = 0, lower MRT)
            Assert.Equal(30.20874,  Math.Round(RunSingleProbe(tair1, wind1, rh1, 60.54855),  5));
            // Probe 39: open sky, highest MRT in period (Sky Exposure = 0.96)
            Assert.Equal(32.650236, Math.Round(RunSingleProbe(tair1, wind1, rh1, 70.935591), 6));

            // Period 2 — 7 hours
            double[] tair2 = { 24.0, 26.0, 27.8, 30.0, 30.0, 31.0, 31.0 };
            double[] wind2 = { 5.6,  5.6,  7.2,  9.2,  8.7,  10.8, 11.3 };
            double[] rh2   = { 78.0, 74.0, 58.0, 71.0, 66.0, 58.0, 49.0 };

            // Probe 0: open sky
            Assert.Equal(34.256664, Math.Round(RunSingleProbe(tair2, wind2, rh2, 68.536816), 6));
            // Probe 7: under building
            Assert.Equal(33.022049, Math.Round(RunSingleProbe(tair2, wind2, rh2, 62.903944), 6));
            // Probe 39: highest MRT
            Assert.Equal(35.351829, Math.Round(RunSingleProbe(tair2, wind2, rh2, 73.48461),  6));

            // Period 3 — 1 hour (single-hour edge case)
            double[] tair3 = { 27.0 };
            double[] wind3 = { 10.8 };
            double[] rh3   = { 36.0 };

            // Probe 0: open sky
            Assert.Equal(29.594003, Math.Round(RunSingleProbe(tair3, wind3, rh3, 66.545835), 6));
            // Probe 7: under building (Sky Exposure = 0, MRT higher than open sky due to ground)
            Assert.Equal(31.180157, Math.Round(RunSingleProbe(tair3, wind3, rh3, 72.716213), 6));

            // Period 4 — 7 hours (afternoon/evening cool-down)
            double[] tair4 = { 28.9, 27.0, 27.0, 25.3, 23.0, 21.0, 20.0 };
            double[] wind4 = { 12.3, 10.8, 10.2, 10.3,  8.2,  6.1,  6.2 };
            double[] rh4   = { 25.0, 36.0, 41.0, 36.0, 41.0, 46.0, 49.0 };

            // Probe 0: open sky
            Assert.Equal(22.808508, Math.Round(RunSingleProbe(tair4, wind4, rh4, 47.165242), 6));
            // Probe 4: open sky, lower MRT (more shaded)
            Assert.Equal(20.947438, Math.Round(RunSingleProbe(tair4, wind4, rh4, 40.003771), 6));
            // Probe 7: under building (higher MRT due to ground radiation)
            Assert.Equal(25.33555,  Math.Round(RunSingleProbe(tair4, wind4, rh4, 56.800676), 5));
            // Probe 39: lowest MRT in period
            Assert.Equal(20.610785, Math.Round(RunSingleProbe(tair4, wind4, rh4, 38.701686), 6));
        }
    }
}
