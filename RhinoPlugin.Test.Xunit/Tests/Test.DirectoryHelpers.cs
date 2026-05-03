using System;
using System.IO;
using EddyLib.Helpers;
using Xunit;

namespace RhinoPlugin.Test.Xunit.Tests
{
    [Trait("Category", "Unit")]
    public class Test_DirectoryHelpers
    {
        [Fact]
        public void RecursiveDelete_HandlesNonExistentDirectory()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var dirInfo = new DirectoryInfo(nonExistentPath);

            // Act & Assert (Should not throw)
            var exception = Record.Exception(() => DirectoryHelpers.RecursiveDelete(dirInfo));
            Assert.Null(exception);
            Assert.False(Directory.Exists(nonExistentPath));
        }

        [Fact]
        public void RecursiveDelete_DeletesEmptyDirectory()
        {
            // Arrange
            var emptyDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(emptyDirPath);
            var dirInfo = new DirectoryInfo(emptyDirPath);

            // Act
            DirectoryHelpers.RecursiveDelete(dirInfo);

            // Assert
            Assert.False(Directory.Exists(emptyDirPath));
        }

        [Fact]
        public void RecursiveDelete_DeletesDirectoryStructureWithNestedFoldersAndFiles()
        {
            // Arrange
            var baseDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(baseDirPath);

            var nestedDirPath1 = Path.Combine(baseDirPath, "nested1");
            var nestedDirPath2 = Path.Combine(baseDirPath, "nested2");
            var doubleNestedDirPath = Path.Combine(nestedDirPath1, "nested1_1");

            Directory.CreateDirectory(nestedDirPath1);
            Directory.CreateDirectory(nestedDirPath2);
            Directory.CreateDirectory(doubleNestedDirPath);

            File.WriteAllText(Path.Combine(baseDirPath, "file1.txt"), "content");
            File.WriteAllText(Path.Combine(nestedDirPath1, "file2.txt"), "content");
            File.WriteAllText(Path.Combine(nestedDirPath2, "file3.txt"), "content");
            File.WriteAllText(Path.Combine(doubleNestedDirPath, "file4.txt"), "content");

            var dirInfo = new DirectoryInfo(baseDirPath);

            // Act
            DirectoryHelpers.RecursiveDelete(dirInfo);

            // Assert
            Assert.False(Directory.Exists(baseDirPath));
        }

        [Fact]
        public void DeleteDirectory_HandlesNonExistentDirectory()
        {
            // Arrange
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            // Act & Assert (Should not throw)
            var exception = Record.Exception(() => DirectoryHelpers.DeleteDirectory(nonExistentPath));
            Assert.Null(exception);
            Assert.False(Directory.Exists(nonExistentPath));
        }

        [Fact]
        public void DeleteDirectory_DeletesEmptyDirectory()
        {
            // Arrange
            var emptyDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(emptyDirPath);

            // Act
            DirectoryHelpers.DeleteDirectory(emptyDirPath);

            // Assert
            Assert.False(Directory.Exists(emptyDirPath));
        }

        [Fact]
        public void DeleteDirectory_DeletesDirectoryStructureWithNestedFoldersAndFiles()
        {
            // Arrange
            var baseDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(baseDirPath);

            var nestedDirPath1 = Path.Combine(baseDirPath, "nested1");
            var nestedDirPath2 = Path.Combine(baseDirPath, "nested2");
            var doubleNestedDirPath = Path.Combine(nestedDirPath1, "nested1_1");

            Directory.CreateDirectory(nestedDirPath1);
            Directory.CreateDirectory(nestedDirPath2);
            Directory.CreateDirectory(doubleNestedDirPath);

            File.WriteAllText(Path.Combine(baseDirPath, "file1.txt"), "content");
            File.WriteAllText(Path.Combine(nestedDirPath1, "file2.txt"), "content");
            File.WriteAllText(Path.Combine(nestedDirPath2, "file3.txt"), "content");
            File.WriteAllText(Path.Combine(doubleNestedDirPath, "file4.txt"), "content");

            // Act
            DirectoryHelpers.DeleteDirectory(baseDirPath);

            // Assert
            Assert.False(Directory.Exists(baseDirPath));
        }

        [Fact]
        public void RecursiveDelete_DeletesDirectoryContainingReadOnlyFiles()
        {
            // Arrange
            var baseDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(baseDirPath);

            var filePath = Path.Combine(baseDirPath, "readonly.txt");
            File.WriteAllText(filePath, "content");

            // Make file read-only
            var fileInfo = new FileInfo(filePath);
            fileInfo.IsReadOnly = true;

            var dirInfo = new DirectoryInfo(baseDirPath);

            // Act
            DirectoryHelpers.RecursiveDelete(dirInfo);

            // Assert
            Assert.False(Directory.Exists(baseDirPath));
        }

        [Fact]
        public void EscapeBackslashes_NullInput_ThrowsNullReferenceException()
        {
            // Act & Assert
            Assert.Throws<NullReferenceException>(() => DirectoryHelpers.EscapeBackslashes(null));
        }

        [Fact]
        public void EscapeBackslashes_EmptyString_ReturnsEmptyString()
        {
            // Act
            var result = DirectoryHelpers.EscapeBackslashes(string.Empty);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void EscapeBackslashes_NoBackslashes_ReturnsSameString()
        {
            // Arrange
            var input = "folder/subfolder/file.txt";

            // Act
            var result = DirectoryHelpers.EscapeBackslashes(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void EscapeBackslashes_SingleBackslash_ReturnsDoubleBackslashes()
        {
            // Arrange
            var input = @"a\b";

            // Act
            var result = DirectoryHelpers.EscapeBackslashes(input);

            // Assert
            Assert.Equal(@"a\\b", result);
        }

        [Fact]
        public void EscapeBackslashes_MultipleSingleBackslashes_EscapesAll()
        {
            // Arrange
            var input = @"a\b\c\d";

            // Act
            var result = DirectoryHelpers.EscapeBackslashes(input);

            // Assert
            Assert.Equal(@"a\\b\\c\\d", result);
        }

        [Fact]
        public void EscapeBackslashes_ConsecutiveBackslashes_DoubleBackslashes_ReturnsTripleBackslashes()
        {
            // Arrange
            var input = @"a\\b";

            // Act
            var result = DirectoryHelpers.EscapeBackslashes(input);

            // Assert
            Assert.Equal(@"a\\\b", result);
        }

        [Fact]
        public void EscapeBackslashes_ConsecutiveBackslashes_TripleBackslashes_ReturnsDoubleBackslashes()
        {
            // Arrange
            var input = @"a\\\b";

            // Act
            var result = DirectoryHelpers.EscapeBackslashes(input);

            // Assert
            Assert.Equal(@"a\\b", result);
        }

        [Fact]
        public void EscapeBackslashes_ConsecutiveBackslashes_QuadrupleBackslashes_ReturnsQuadrupleBackslashes()
        {
            // Arrange
            var input = @"a\\\\b";

            // Act
            var result = DirectoryHelpers.EscapeBackslashes(input);

            // Assert
            Assert.Equal(@"a\\\\b", result);
        }

        [Theory]
        [InlineData(@"\", @"\\")]
        [InlineData(@"\\", @"\\\")]
        [InlineData(@"\\\", @"\\")]
        [InlineData(@"\\\\", @"\\\\")]
        [InlineData(@"\\a", @"\\\a")]
        [InlineData(@"a\\", @"a\\\")]
        [InlineData(@"a\b\\c", @"a\\b\\\c")]
        [InlineData(@"\\a\b\", @"\\\a\\b\\")]
        [InlineData(@"\\\\\", @"\\\\\")]
        [InlineData(@"\\\\\\", @"\\\\")]
        [InlineData(@"\\\\\\\", @"\\\\\\")]
        [InlineData(@"a\b\\\c\\\\d\\\\\e", @"a\\b\\c\\\\d\\\\\e")]
        public void EscapeBackslashes_VariousCombinations_ReturnsExpectedResult(string input, string expected)
        {
            // Act
            var result = DirectoryHelpers.EscapeBackslashes(input);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
