using EddyLib;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Unit")]
    public class UtilitiesStringsTests
    {
        [Theory]
        [InlineData("validName", true)]
        [InlineData("valid_Name", true)]
        [InlineData("valid-Name", true)]
        [InlineData("validName123", true)]
        [InlineData("123validName", true)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("invalid Name", false)]
        [InlineData("invalid/Name", false)]
        [InlineData("invalid\\Name", false)]
        [InlineData("invalid&Name", false)]
        [InlineData("invalid|Name", false)]
        [InlineData("invalid;Name", false)]
        [InlineData("..", false)]
        [InlineData("dot.name", false)]
        public void IsValidProbeName_ReturnsExpectedResult(string input, bool expected)
        {
            var result = Utilities.IsValidProbeName(input);
            Assert.Equal(expected, result);
        }
    }
}
