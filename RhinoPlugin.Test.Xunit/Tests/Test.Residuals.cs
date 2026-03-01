using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using EddyLib;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Unit")]
    public class Test_Residuals
    {
        [Fact]
        public void Parse_TypicalResidualsFile_ReturnsExpectedData()
        {
            // Arrange
            string fileContent = @"# Time        Ux        Uy        Uz
1.0         0.5       0.1       0.01
2.0         0.25      0.05      0.005
3.0         0.125     0.025     0.0025
";
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, fileContent);

            try
            {
                // Act
                var data = Residuals.Parse(tempFile);

                // Assert
                Assert.Equal(3, data.Iterations.Count);
                Assert.Equal(1.0, data.Iterations[0]);
                Assert.Equal(2.0, data.Iterations[1]);
                Assert.Equal(3.0, data.Iterations[2]);

                Assert.Equal(3, data.FieldNames.Count);
                Assert.Equal("Ux", data.FieldNames[0]);
                Assert.Equal("Uy", data.FieldNames[1]);
                Assert.Equal("Uz", data.FieldNames[2]);

                Assert.Equal(3, data.Values.Count); // 3 variables
                Assert.Equal(0.5, data.Values[0][0]);
                Assert.Equal(0.1, data.Values[1][0]);
                Assert.Equal(0.01, data.Values[2][0]);

                Assert.Equal(0.125, data.Values[0][2]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Parse_IncompleteLines_IgnoresInvalidLines()
        {
            // Arrange
            string fileContent = @"# Time        Ux        Uy        Uz
1.0         0.5       0.1       0.01
2.0
3.0         0.125     0.025     0.0025
";
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, fileContent);

            try
            {
                // Act
                var data = Residuals.Parse(tempFile);

                // Assert
                Assert.Equal(2, data.Iterations.Count); // Should skip line ""2.0"" which has < 2 tokens
                Assert.Equal(1.0, data.Iterations[0]);
                Assert.Equal(3.0, data.Iterations[1]);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void Parse_CorruptedData_ThrowsFormatException()
        {
            // Arrange
            string fileContent = @"# Time        Ux        Uy        Uz
1.0         0.5       invalid   0.01
";
            string tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, fileContent);

            try
            {
                // Act & Assert
                Assert.Throws<FormatException>(() => Residuals.Parse(tempFile));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }
    }
}
