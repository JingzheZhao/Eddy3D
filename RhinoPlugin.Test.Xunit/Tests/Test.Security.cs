using System;
using Xunit;
using EddyLib;
using EddyLib.Docker;

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

        [Theory]
        [InlineData("path;rm -rf /")]
        [InlineData("path&whoami")]
        [InlineData("path\"quote")]
        public void DockerRunner_Methods_ThrowOnMaliciousPaths(string maliciousPath)
        {
            // Use a safe dockerExe and imageName for instantiation
            var runner = new DockerRunner("docker", "busybox");

            // Assert that all methods taking hostCasePath throw ArgumentException
            Assert.Throws<ArgumentException>(() => runner.RunHeadless("ls", maliciousPath));
            Assert.Throws<ArgumentException>(() => runner.RunInteractive("ls", maliciousPath));
            Assert.Throws<ArgumentException>(() => runner.WriteDockerRunScript("safe.sh", "ls", maliciousPath));
            Assert.Throws<ArgumentException>(() => DockerRunner.BuildCommandFileContent(new[] { "ls" }, maliciousPath, "Title"));

            // Assert that WriteDockerRunScript also throws if scriptPath is malicious
            Assert.Throws<ArgumentException>(() => runner.WriteDockerRunScript(maliciousPath, "ls", "safe_path"));
        }

        [Fact]
        public void DockerRunner_Constructor_ThrowsOnMaliciousParameters()
        {
            Assert.Throws<ArgumentException>(() => new DockerRunner("docker;malicious", "busybox"));
            Assert.Throws<ArgumentException>(() => new DockerRunner("docker", "busybox&malicious"));
        }

        [Fact]
        public void DockerRunner_DefaultConstructor_DoesNotThrow()
        {
            // Verifies that null/default parameters don't cause ArgumentException
            var runner = new DockerRunner();
            Assert.NotNull(runner);
        }

        [Fact]
        public void DockerRunner_BuildCommandFileContent_ThrowsOnMaliciousPath()
        {
            Assert.Throws<ArgumentException>(() => DockerRunner.BuildCommandFileContent(new[] { "ls" }, "path;malicious", "Title"));
        }

        [Fact]
        public void TwoPhaseDDS_CommandLineArgsNew_ThrowsOnMaliciousPaths()
        {
            // We don't need a full instance if we only want to test the validation logic
            // but it's an instance method.
            var dds = (EddyLib.Radiation.TwoPhaseDDS)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(EddyLib.Radiation.TwoPhaseDDS));

            Assert.Throws<ArgumentException>(() => dds.CommandLineArgsNew("safe", "safe", 1, "safe", "malicious&path", 1, 1, 0, 0, 1));
            Assert.Throws<ArgumentException>(() => dds.CommandLineArgsNew("malicious&path", "safe", 1, "safe", "safe", 1, 1, 0, 0, 1));
        }

        [Fact]
        public void TwoPhaseDDS_Constructor_ThrowsOnMaliciousWorkingDirectory()
        {
            Assert.Throws<ArgumentException>(() => new EddyLib.Radiation.TwoPhaseDDS("malicious&path", null, null, null, false));
        }

        [Theory]
        [InlineData("command;malicious")]
        [InlineData("command&malicious")]
        [InlineData("command|malicious")]
        public void ProcessRunner_Methods_ThrowOnMaliciousCommands(string maliciousCommand)
        {
            Assert.Throws<ArgumentException>(() => EddyLib.Helpers.ProcessRunner.RunCommand(maliciousCommand, true));
            Assert.Throws<ArgumentException>(() => EddyLib.Helpers.ProcessRunner.RunCommandAsync(maliciousCommand, true));
        }

        [Fact]
        public void ProcessRunner_RunGnuplot_ThrowsOnMaliciousPath()
        {
            Assert.Throws<ArgumentException>(() => EddyLib.Helpers.ProcessRunner.RunGnuplot("script.plt;malicious", true));
        }

        [Theory]
        [InlineData("arg;malicious")]
        [InlineData("arg&malicious")]
        public void StartProcess_StartProcessCMDNT_ThrowsOnMaliciousArguments(string maliciousArg)
        {
            Assert.Throws<ArgumentException>(() => Utilities.StartProcess.StartProcessCMDNT(maliciousArg, true));
        }

        [Theory]
        [InlineData("templates/file.ghx")]
        [InlineData("file.gh")]
        public void ValidateRelativePath_SafePaths_DoesNotThrow(string path)
        {
            Utilities.ValidateRelativePath(path);
        }

        [Theory]
        [InlineData("/absolute/path")]
        [InlineData("C:\\absolute\\path")]
        [InlineData("../traversal")]
        [InlineData("path/../traversal")]
        [InlineData("path;malicious")]
        public void ValidateRelativePath_UnsafePaths_ThrowsArgumentException(string path)
        {
            Assert.Throws<ArgumentException>(() => Utilities.ValidateRelativePath(path));
        }
    }
}
