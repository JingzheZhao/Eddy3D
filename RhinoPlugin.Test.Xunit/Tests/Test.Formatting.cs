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

        [Fact]
        public void WritePV_FormatsPointCorrectly()
        {
            var p = new Rhino.Geometry.Point3d(1.1234, 2.5678, 3.9012);
            using var sw = new System.IO.StringWriter();

            Utilities.WritePV(sw, p);

            Assert.Equal("1.123 2.568 3.901", sw.ToString());
        }

        [Fact]
        public void WritePV_FormatsVectorCorrectly()
        {
            var v = new Rhino.Geometry.Vector3d(0.1234, -1.5678, 10.9012);
            using var sw = new System.IO.StringWriter();

            Utilities.WritePV(sw, v);

            Assert.Equal("0.123 -1.568 10.901", sw.ToString());
        }

        [Fact]
        public void WritePV_FormatsPointAndNormalCorrectly()
        {
            var p = new Rhino.Geometry.Point3d(1.0, 2.0, 3.0);
            var n = new Rhino.Geometry.Vector3d(0.0, 0.0, 1.0);
            using var sw = new System.IO.StringWriter();

            Utilities.WritePV(sw, p, n);

            Assert.Equal("1 2 3 0 0 1", sw.ToString());
        }
    }
}
