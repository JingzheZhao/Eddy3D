using EddyLib.Radiation;
using EddyLib;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace RhinoPlugin.Tests.Xunit
{
    [Collection("Rhino Collection")]
    public class SolarGainTests
    {
        [Fact]
        public void AngleProjectionFactor_90_returns_0_282()
        {
            // Arrange

            double alt = 90;

            double rad = Utilities.Deg2Rad(alt);

            //Act

            var fp = SolarGain.Get_fp_cylinder(rad);

            //Assert

            Assert.Equal(Math.Round(fp, 2), Math.Round(1 * 0.09 * 3.14, 2));
        }

        [Fact]
        public void AngleProjectionFactor_0_returns_0_525()
        {
            // Arrange

            double alt = 0;

            double rad = Utilities.Deg2Rad(alt);

            //Act

            var fp = SolarGain.Get_fp_cylinder(rad);

            //Assert

            Assert.Equal(Math.Round(fp, 2), Math.Round(2 * 0.3 * 1.75, 2));
        }
    }
}