using EddyLib;
using EddyLib.BCs;
using EddyLib.Strings;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Unit")]
    public class FormattingTests
    {
        [Fact]
        public void FormatDouble_PreservesSmallNonZeroValues()
        {
            var formatted = Utilities.FormatDouble(0.00015);

            Assert.Equal("0.00015", formatted);
        }

        [Fact]
        public void AblDictionary_WritesSmallZ0WithoutRoundingToZero()
        {
            var abl = new ABL(0, 5.0, 10.0, 0.00015, 0.0);

            var dict = BCDicts.ABL(abl, 0);

            Assert.Contains("z0 uniform 0.00015;", dict);
        }

        [Fact]
        public void BuildTable_PreservesSmallZ0WithoutRoundingToZero()
        {
            var summary = BCHelpers.BuildTable(
                new List<int> { 0 },
                new List<double> { 5.0 },
                new List<double> { 0.00015 },
                string.Empty);

            Assert.Contains("0.00015", summary);
        }
    }
}
