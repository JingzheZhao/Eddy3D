using EddyLib;
using System;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Unit")]
    public class UtilitiesMathTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(180, Math.PI)]
        [InlineData(360, 2 * Math.PI)]
        [InlineData(90, Math.PI / 2)]
        [InlineData(-90, -Math.PI / 2)]
        [InlineData(45, Math.PI / 4)]
        public void Deg2Rad_ConvertsCorrectly(double degrees, double expectedRadians)
        {
            var result = Utilities.Deg2Rad(degrees);
            Assert.Equal(expectedRadians, result, 10); // 10 decimal places precision
        }
    }
}
