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

        [Fact]
        public void PointsOutsideDomain_ReturnsFormattedString()
        {
            // Arrange
            int[] indices = { 1, 5, 10 };

            // Act
            string result = ReturnMsg.PointsOutsideDomain(indices);

            // Assert
            Assert.Contains("The probes with the indices:", result);
            Assert.Contains("1, 5, 10", result);
            Assert.Contains("can't be probed within the simulation domain and have been discarded.", result);
        }

        [Fact]
        public void PointsOutsideDomain_TruncatesLongLists()
        {
            // Arrange
            int[] indices = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };

            // Act
            string result = ReturnMsg.PointsOutsideDomain(indices);

            // Assert
            Assert.Contains("0, 1, 2, 3, 4, 5, 6, 7, 8, 9 ... and 3 more", result);
        }

        [Fact]
        public void PointsOutsideDomain_ReturnsEmptyForNullOrEmpty()
        {
            Assert.Equal(string.Empty, ReturnMsg.PointsOutsideDomain(null));
            Assert.Equal(string.Empty, ReturnMsg.PointsOutsideDomain(new int[0]));
        }
    }
}
