using EddyLib;
using EddyLib.Radiation;
using System;
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

        //[Fact]
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

        //[Fact]
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

        //[Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public void Deg2Rad()
        {
            // Arrange

            double deg = 90;

            //Act

            var rad = Utilities.Deg2Rad(deg);

            //Assert

            AssertRoundedEqual(Math.PI / 2, rad);
        }
    }
}
