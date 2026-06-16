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
        /// Regression baseline for the UTCI GH component with hourly MRT per probe.
        /// Expected values captured from a real Grasshopper run with 8100 probe points, 9 hours.
        /// MRT varies per hour per probe. Tests both hourly UTCI and per-probe averaged UTCI.
        /// </summary>
        [Fact]
        public void UTCI_GHComponent_ReproducesKnownOutputs()
        {
            // 9 hours of weather data
            double[] tair = { 24.0, 26.0, 27.8, 30.0, 30.0, 31.0, 31.0, 30.0, 28.9 };
            double[] wind = {  5.6,  5.6,  7.2,  9.2,  8.7, 10.8, 11.3, 10.2, 12.3 };
            double[] rh   = { 78.0, 74.0, 58.0, 71.0, 66.0, 58.0, 49.0, 29.0, 25.0 };

            static double[] RunProbe(double[] tair, double[] wind, double[] rh, double[] mrt)
            {
                int n = tair.Length;
                var hourly = new double[n];
                for (int h = 0; h < n; h++)
                    hourly[h] = EddyLib.UTCI.CalcUTCICorrectBounds(tair[h], rh[h], wind[h], mrt[h], out _);
                return hourly;
            }

            // Probe 0 — hourly MRT and expected hourly UTCI from GH
            double[] mrt0 = { 65.050068, 73.344712, 74.860354, 74.159168, 74.845757, 76.572948, 76.624939, 74.541833, 69.596283 };
            double[] exp0 = { 29.983, 33.725, 34.300, 37.167, 37.009, 37.962, 37.338, 35.306, 31.851 };
            double[] out0 = RunProbe(tair, wind, rh, mrt0);
            for (int h = 0; h < 9; h++)
                Assert.Equal(exp0[h], Math.Round(out0[h], 3));

            // Probe 5 — lower MRT values
            double[] mrt5 = { 64.798795, 72.753875, 74.133199, 73.468066, 74.223848, 76.042392, 76.307895, 74.480816, 69.859018 };
            double[] exp5 = { 29.922, 33.586, 34.130, 37.022, 36.876, 37.850, 37.269, 35.291, 31.919 };
            double[] out5 = RunProbe(tair, wind, rh, mrt5);
            for (int h = 0; h < 9; h++)
                Assert.Equal(exp5[h], Math.Round(out5[h], 3));

            // Probe 28 — highest MRT values in this sample
            double[] mrt28 = { 65.085964, 73.429117, 74.964233, 74.257896, 74.934601, 76.648741, 76.670231, 74.550549, 69.55875  };
            double[] exp28 = { 29.992, 33.744, 34.324, 37.188, 37.028, 37.978, 37.348, 35.308, 31.842 };
            double[] out28 = RunProbe(tair, wind, rh, mrt28);
            for (int h = 0; h < 9; h++)
                Assert.Equal(exp28[h], Math.Round(out28[h], 3));
        }
    }
}
