using Xunit;
using EddyLib.Strings;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Unit")]
    public class TestReturnMsg
    {
        [Fact]
        public void ParsingFailed_ReturnsExpectedString()
        {
            var expected = $"Failed to parse residuals. They might be corrupted or in an incompatible format.";
            var actual = ReturnMsg.ParsingFailed();

            Assert.Equal(expected, actual);
        }
    }
}
