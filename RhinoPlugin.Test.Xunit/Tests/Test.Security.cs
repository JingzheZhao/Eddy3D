using System;
using Xunit;
using EddyLib;

namespace RhinoPlugin.Test.Xunit
{
    public class SecurityTests
    {
        [Theory]
        [InlineData("safe_path/file.txt")]
        [InlineData("/home/user/eddy3d/case")]
        [InlineData("relative/path_with-underscores")]
        public void ValidatePathForShell_SafePaths_DoesNotThrow(string path)
        {
            // Act & Assert
            Utilities.ValidatePathForShell(path);
        }

        [Theory]
        [InlineData("path&with&ampersand")]
        [InlineData("path|with|pipe")]
        [InlineData("path;with;semicolon")]
        [InlineData("path$with$dollar")]
        [InlineData("path`with`backtick")]
        [InlineData("path<with<less")]
        [InlineData("path>with>greater")]
        [InlineData("path(with)parens")]
        [InlineData("path[with]brackets")]
        [InlineData("path{with}braces")]
        [InlineData("path*with*star")]
        [InlineData("path?with?question")]
        [InlineData("path!with!bang")]
        [InlineData("path\nwith\nnewline")]
        [InlineData("path'with'singlequote")]
        [InlineData("path\"with\"doublequote")]
        public void ValidatePathForShell_MaliciousPaths_ThrowsArgumentException(string path)
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => Utilities.ValidatePathForShell(path));
        }

        [Fact]
        public void ValidatePathForShell_WindowsPath_BehaviorByPlatform()
        {
            string path = "C:\\Users\\Public\\Documents\\Case";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                // On Windows, backslash is a valid path separator and should not throw
                Utilities.ValidatePathForShell(path);
            }
            else
            {
                // On Unix, backslash is a metacharacter and should throw
                Assert.Throws<ArgumentException>(() => Utilities.ValidatePathForShell(path));
            }
        }
    }
}
