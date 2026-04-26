using EddyLib.Strings;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Unit")]
    public class ReturnMsgTests
    {
        [Fact]
        public void MeshDoesntExist_ReturnsFormattedStringWithCaseFolder()
        {
            // Arrange
            string caseFolder = "/path/to/dummy/folder";

            // Act
            string result = ReturnMsg.MeshDoesntExist(caseFolder);

            // Assert
            Assert.Contains(caseFolder, result);
            Assert.Contains("Could not find OpenFOAM mesh at", result);
        }
    }
}
