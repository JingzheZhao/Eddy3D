using EddyLib;
using EddyLib.Radiation;
using System;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class SolarGainTests
    {
        private static void AssertRoundedEqual(double expected, double actual, int decimals = 2)
        {
            Assert.Equal(Math.Round(expected, decimals), Math.Round(actual, decimals));
        }

        //[RhinoRequiredFact]
        //public void AngleProjectionFactor_90_returns_0_282()
        //{
        //    // Arrange

        //    double alt = 90;

        //    double rad = Utilities.Deg2Rad(alt);

        //    //Act

        //    var fp = SolarGain.Get_fp_cylinder(rad);

        //    //Assert

        //    Assert.Equal(Math.Round(fp, 2), Math.Round(1 * 0.09 * 3.14, 2));
        //}

        //[RhinoRequiredFact]
        //public void AngleProjectionFactor_0_returns_0_525()
        //{
        //    // Arrange

        //    double alt = 0;

        //    double rad = Utilities.Deg2Rad(alt);

        //    //Act

        //    var fp = SolarGain.Get_fp_cylinder(rad);

        //    //Assert

        //    Assert.Equal(Math.Round(fp, 2), Math.Round(2 * 0.3 * 1.75, 2));
        //}

        //[RhinoRequiredFact]
        //public void AngleProjectionFactor_CBE()
        //{
        //    // Arrange

        //    double alt = 0;

        //    double az = 0;

        //    double rad = Utilities.Deg2Rad(alt);

        //    var posture = SolarGain.Posture.standing;

        //    //Act
        //    var fp = SolarGain.Get_fp(alt, az, posture);

        //    //Assert

        //    Assert.Equal(Math.Round(fp, 2), Math.Round(2 * 0.3 * 1.75, 2));
        //}

        [RhinoRequiredFact]
        public void ERFOriginal()
        {
            // Arrange
            // https://comfort.cbe.berkeley.edu/

            //  ERF function to estimate the impact of solar radiation on occupant comfort
            //  INPUTS:
            //  alt : altitude of sun in degrees [0, 90]
            //  az : azimuth of sun in degrees [0, 180]
            //  posture: posture of occupant ('seated', 'standing', or 'supine')
            //  Idir : direct beam intensity (normal)
            //  tsol: total solar transmittance (SC * 0.87)
            //  fsvv : sky vault view fraction : fraction of sky vault in occupant's view [0, 1]
            //  fbes : fraction body exposed to sun [0, 1]
            //  asa : avg shortwave abs : average shortwave absorptivity of body [0, 1]
            //  tsol_factor : (optional) correction to tsol based on angle of incidence

            double alt = 45;
            double az = 0;
            SolarGain.Posture posture = SolarGain.Posture.standing;
            double Idir = 700;
            double tsol = 1;
            double fsvv = 1;
            double fbes = 0.5;
            double asa = 0.7;

            //Act

            double ERF;
            double dMRT;

            SolarGain.ERF(alt, az, posture, Idir, tsol, fsvv, fbes, asa, out ERF, out dMRT);

            //Assert

            AssertRoundedEqual(188.16, ERF);
            AssertRoundedEqual(43.25, dMRT);
        }

        [RhinoRequiredFact]
        public void ERFModified()
        {
            // Arrange
            // https://comfort.cbe.berkeley.edu/

            //  ERF function to estimate the impact of solar radiation on occupant comfort
            //  INPUTS:
            //  alt : altitude of sun in degrees [0, 90]
            //  az : azimuth of sun in degrees [0, 180]
            //  posture: posture of occupant ('seated', 'standing', or 'supine')
            //  Idir : direct beam intensity (normal)
            //  tsol: total solar transmittance (SC * 0.87)
            //  fsvv : sky vault view fraction : fraction of sky vault in occupant's view [0, 1]
            //  fbes : fraction body exposed to sun [0, 1]
            //  asa : avg shortwave abs : average shortwave absorptivity of body [0, 1]
            //  tsol_factor : (optional) correction to tsol based on angle of incidence

            double alt = 45;

            //double az = 0;
            SolarGain.Posture posture = SolarGain.Posture.standing;
            double Idir = 700;

            //double tsol = 1;
            // double fsvv = 1;

            double Idiff = 150;

            //Act

            double dMRT = SolarGain.ERF_Modified(alt, posture, Idir, Idiff);

            //Assert

            // Assert.Equal(Math.Round(188.16, 2), Math.Round(ERF, 2));
            AssertRoundedEqual(47.34, dMRT);
        }

        [RhinoRequiredFact]
        public void Deg2Rad()
        {
            // Arrange

            double deg = 90;

            //Act

            var rad = Utilities.Deg2Rad(deg);

            //Assert

            AssertRoundedEqual(Math.PI / 2, rad);
        }

        // Regression coverage for the SolarGain hot path (PR #465 refactor).
        // Golden values were captured from the implementation that was first
        // proven equivalent to the pre-refactor code across 1M+ input combinations.
        // Tolerance pins the math to ~12 significant digits, far tighter than the
        // float result downstream consumers see.
        public static IEnumerable<object[]> ErfModifiedCases()
        {
            yield return new object[] {  0.0, SolarGain.Posture.standing,    0.0,    0.0, 0.70,  0.000000000000 };
            yield return new object[] { 15.0, SolarGain.Posture.standing,  200.0,   50.0, 0.70, 15.673801213466 };
            yield return new object[] { 30.0, SolarGain.Posture.standing,  500.0,  100.0, 0.70, 35.309869620400 };
            yield return new object[] { 45.0, SolarGain.Posture.standing,  700.0,  150.0, 0.70, 47.338486853867 };
            yield return new object[] { 60.0, SolarGain.Posture.standing,  800.0,  200.0, 0.70, 51.559527026745 };
            yield return new object[] { 75.0, SolarGain.Posture.standing, 1000.0,  300.0, 0.70, 60.726946863985 };
            yield return new object[] { 90.0, SolarGain.Posture.standing,  500.0,  100.0, 0.70, 18.477859914715 };
            yield return new object[] { 45.0, SolarGain.Posture.standing,  700.0,  150.0, 0.50, 33.813204895619 };
            yield return new object[] { 45.0, SolarGain.Posture.standing,  700.0,  150.0, 0.84, 56.806184224640 };
            yield return new object[] {  0.0, SolarGain.Posture.seating,     0.0,    0.0, 0.70,  0.000000000000 };
            yield return new object[] { 15.0, SolarGain.Posture.seating,   200.0,   50.0, 0.70, 16.071028310811 };
            yield return new object[] { 30.0, SolarGain.Posture.seating,   500.0,  100.0, 0.70, 36.269418281484 };
            yield return new object[] { 45.0, SolarGain.Posture.seating,   700.0,  150.0, 0.70, 48.543379946462 };
            yield return new object[] { 60.0, SolarGain.Posture.seating,   800.0,  200.0, 0.70, 52.684448839994 };
            yield return new object[] { 75.0, SolarGain.Posture.seating,  1000.0,  300.0, 0.70, 61.722148597353 };
            yield return new object[] { 90.0, SolarGain.Posture.seating,   500.0,  100.0, 0.70, 18.736074838062 };
            yield return new object[] { 45.0, SolarGain.Posture.seating,   700.0,  150.0, 0.50, 34.673842818902 };
            yield return new object[] { 45.0, SolarGain.Posture.seating,   700.0,  150.0, 0.84, 58.252055935755 };
            yield return new object[] {  0.0, SolarGain.Posture.supine,      0.0,    0.0, 0.70,  0.000000000000 };
            yield return new object[] { 15.0, SolarGain.Posture.supine,    200.0,   50.0, 0.70, 15.673801213466 };
            yield return new object[] { 30.0, SolarGain.Posture.supine,    500.0,  100.0, 0.70, 35.309869620400 };
            yield return new object[] { 45.0, SolarGain.Posture.supine,    700.0,  150.0, 0.70, 47.338486853867 };
            yield return new object[] { 60.0, SolarGain.Posture.supine,    800.0,  200.0, 0.70, 51.559527026745 };
            yield return new object[] { 75.0, SolarGain.Posture.supine,   1000.0,  300.0, 0.70, 60.726946863985 };
            yield return new object[] { 90.0, SolarGain.Posture.supine,    500.0,  100.0, 0.70, 18.477859914715 };
            yield return new object[] { 45.0, SolarGain.Posture.supine,    700.0,  150.0, 0.50, 33.813204895619 };
            yield return new object[] { 45.0, SolarGain.Posture.supine,    700.0,  150.0, 0.84, 56.806184224640 };
        }

        [Theory]
        [MemberData(nameof(ErfModifiedCases))]
        public void ERF_Modified_PinnedGoldenValues(double alt, SolarGain.Posture posture,
                                                    double Idir, double Idiff, double asa,
                                                    double expected)
        {
            double actual = SolarGain.ERF_Modified(alt, posture, Idir, Idiff, asa);
            Assert.Equal(expected, actual, 9);
        }

        [Fact]
        public void ComputeStanding_DiurnalSweep_MatchesGolden()
        {
            // Synthetic 24-hour diurnal: solar elevation peaks at 60 deg at noon.
            // Pinning per-hour outputs guards against algorithmic regressions in
            // both the ERF math and the parallel/serial loop structure.
            var weather = new Weather();
            var totalRad = new float[24];
            var directRad = new float[24];
            for (int h = 0; h < 24; h++)
            {
                double daylight = Math.Max(0, Math.Sin(Math.PI * (h - 6) / 12.0));
                weather.SolarElevation.Add(60.0 * daylight);
                directRad[h] = (float)(800.0 * daylight);
                totalRad[h]  = directRad[h] + (float)(150.0 * daylight);
            }

            float[] result = SolarGain.ComputeStanding(weather, totalRad, directRad);

            float[] expected = new float[]
            {
                0f, 0f, 0f, 0f, 0f, 0f, 0f,
                14.637166f, 27.633862f, 36.97499f, 42.365887f, 44.78298f,
                45.419178f,
                44.78298f, 42.365887f, 36.97499f, 27.633862f, 14.637166f,
                6.765344e-15f, 0f, 0f, 0f, 0f, 0f
            };
            Assert.Equal(expected.Length, result.Length);
            for (int h = 0; h < 24; h++)
                Assert.Equal(expected[h], result[h], 3);
        }

        [Fact]
        public void ComputeStanding_2D_MatchesScalarOverload()
        {
            // The float[][] (parallel) overload and the float[] (sequential) overload
            // must produce identical results for the same physical inputs.
            var weather = new Weather();
            int hours = 48;
            int sensors = 5;
            var rng = new Random(1234);
            var totalRad2D = new float[hours][];
            var directRad2D = new float[hours][];
            var totalRad1D = new float[hours];
            var directRad1D = new float[hours];

            for (int h = 0; h < hours; h++)
            {
                double daylight = Math.Max(0, Math.Sin(Math.PI * (h % 24 - 6) / 12.0));
                weather.SolarElevation.Add(60.0 * daylight);
                totalRad2D[h] = new float[sensors];
                directRad2D[h] = new float[sensors];
                directRad1D[h] = (float)(rng.NextDouble() * 800);
                totalRad1D[h]  = directRad1D[h] + (float)(rng.NextDouble() * 150);
                for (int p = 0; p < sensors; p++)
                {
                    // All sensors share the scalar inputs so the two overloads must agree.
                    directRad2D[h][p] = directRad1D[h];
                    totalRad2D[h][p]  = totalRad1D[h];
                }
            }

            float[][] result2D = SolarGain.ComputeStanding(weather, totalRad2D, directRad2D);
            float[]   result1D = SolarGain.ComputeStanding(weather, totalRad1D, directRad1D);

            for (int h = 0; h < hours; h++)
                for (int p = 0; p < sensors; p++)
                    Assert.Equal(result1D[h], result2D[h][p], 3);
        }
    }
}
