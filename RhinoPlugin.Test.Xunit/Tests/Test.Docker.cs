using System;
using System.Collections.Generic;
using System.IO;
using EddyLib.Docker;
using Xunit;
using Xunit.Abstractions;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Custom Fact attribute that skips tests when Docker is not available.
    /// </summary>
    public sealed class DockerAvailableFactAttribute : FactAttribute
    {
        public DockerAvailableFactAttribute()
        {
            if (!DockerEnvironment.IsDockerAvailable())
                Skip = "Docker is not available on this machine.";
        }
    }

    /// <summary>
    /// Unit and integration tests for the Docker runtime infrastructure.
    /// Unit tests run without Docker; integration tests require Docker Desktop.
    /// </summary>
    [Trait("Category", "Docker")]
    public class Test_Docker
    {
        private readonly ITestOutputHelper _output;

        public Test_Docker(ITestOutputHelper output)
        {
            _output = output;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Unit Tests (no Docker required)
        // ─────────────────────────────────────────────────────────────────────

        [Fact]
        public void DockerConfig_HasExpectedImageName()
        {
            Assert.Equal("pkastner/openfoam:8-umcf-4856041", DockerConfig.ImageName);
        }

        [Fact]
        public void DockerConfig_HasExpectedBashrcPath()
        {
            Assert.Equal("/home/openfoam/OpenFOAM-8/etc/bashrc", DockerConfig.OpenFoamBashrc);
        }

        [Fact]
        public void DockerConfig_HasExpectedMpiPath()
        {
            Assert.Equal("/opt/amazon/openmpi/bin", DockerConfig.MpiPath);
        }

        [Fact]
        public void DockerConfig_HasExpectedCaseMountPoint()
        {
            Assert.Equal("/case", DockerConfig.CaseMountPoint);
        }

        [Fact]
        public void DockerConfig_HasExpectedPlatform()
        {
            Assert.Equal("linux/amd64", DockerConfig.Platform);
        }

        [Fact]
        public void BuildPreamble_ContainsBashrcAndMpiPath()
        {
            var preamble = DockerRunner.BuildPreamble();

            Assert.Contains(DockerConfig.OpenFoamBashrc, preamble);
            Assert.Contains(DockerConfig.MpiPath, preamble);
            Assert.StartsWith("export PATH=", preamble);
            Assert.Contains("&& source ", preamble);
            Assert.Contains("export PATH=", preamble);
        }

        [Fact]
        public void BuildCommandChain_JoinsCommandsCorrectly()
        {
            var commands = new List<string> { "blockMesh", "snappyHexMesh -overwrite" };
            var chain = DockerRunner.BuildCommandChain(commands);

            // Should start with cd /case
            Assert.StartsWith("cd " + DockerConfig.CaseMountPoint, chain);

            // Should contain the preamble
            Assert.Contains(DockerConfig.OpenFoamBashrc, chain);

            // Should contain both commands
            Assert.Contains("blockMesh", chain);
            Assert.Contains("snappyHexMesh -overwrite", chain);

            // Should be joined with &&
            Assert.Contains(" && ", chain);

            // Count the number of && separators.
            // BuildPreamble expands to "export PATH=... && source ...",
            // so chain parts are: cd, export PATH, source, blockMesh, snappyHexMesh.
            var parts = chain.Split(new[] { " && " }, StringSplitOptions.None);
            Assert.Equal(5, parts.Length);
        }

        [Fact]
        public void BuildCommandChain_EmptyCommands_HasPreambleOnly()
        {
            var commands = new List<string>();
            var chain = DockerRunner.BuildCommandChain(commands);

            Assert.StartsWith("cd " + DockerConfig.CaseMountPoint, chain);
            Assert.Contains(DockerConfig.OpenFoamBashrc, chain);

            var parts = chain.Split(new[] { " && " }, StringSplitOptions.None);
            Assert.Equal(3, parts.Length); // cd + export PATH + source
        }

        [Fact]
        public void DockerBatchScriptBuilder_ShellScript_ContainsExpectedContent()
        {
            var commands = new List<string> { "blockMesh", "simpleFoam" };
            var script = DockerBatchScriptBuilder.BuildDockerShellScript(commands, "/tmp/test-case");

            Assert.StartsWith("#!/bin/bash", script);
            Assert.Contains(DockerConfig.ImageName, script);
            Assert.Contains("/tmp/test-case", script);
            Assert.Contains(DockerConfig.CaseMountPoint, script);
            Assert.Contains(DockerConfig.Platform, script);
            Assert.Contains("Docker execution complete.", script);
        }

        [Fact]
        public void DockerRunner_CommandFileContent_AutoClosesMacTerminalOnSuccess()
        {
            var commands = new List<string> { "blockMesh" };
            var script = DockerRunner.BuildCommandFileContent(commands, "/tmp/test-case", "Mesh");

            Assert.Contains("TERM_PROGRAM", script);
            Assert.Contains("Apple_Terminal", script);
            Assert.Contains("_eddy_terminal_target", script);
            Assert.Contains("osascript", script);
            Assert.Contains("mktemp", script);
            Assert.Contains("/usr/bin/nohup /usr/bin/osascript", script);
            Assert.Contains("disown $!", script);
            Assert.Contains("delay 1", script);
            Assert.Contains("close w saving no", script);
            Assert.Contains("System Events", script);
            Assert.Contains("Eddy3D Terminal window could not be found", script);
            Assert.Contains("Closing Terminal window in %s seconds", script);
            Assert.Contains("Docker execution failed with exit code", script);
            Assert.Contains("read -n 1", script);
        }

        [Fact]
        public void DockerBatchScriptBuilder_BatWrapper_ContainsExpectedContent()
        {
            var commands = new List<string> { "blockMesh" };
            var script = DockerBatchScriptBuilder.BuildDockerBatWrapper(commands, @"C:\Users\test\case");

            Assert.StartsWith("@echo off", script);
            Assert.Contains(DockerConfig.ImageName, script);
            Assert.Contains(@"C:\Users\test\case", script);
            Assert.Contains("Docker execution complete.", script);
            Assert.Contains("Closing this window in 60 seconds", script);
            Assert.Contains("for /l %%i in (60,-1,1)", script);
            Assert.Contains("timeout /t 1 /nobreak", script);
        }

        [Fact]
        public void DockerCommandResult_Success_WhenExitCodeZero()
        {
            var result = new DockerCommandResult(0, "output", "");
            Assert.True(result.Success);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("output", result.StdOut);
            Assert.Equal("", result.StdErr);
        }

        [Fact]
        public void DockerCommandResult_Failure_WhenExitCodeNonZero()
        {
            var result = new DockerCommandResult(1, "", "error");
            Assert.False(result.Success);
            Assert.Equal(1, result.ExitCode);
        }

        [Fact]
        public void DockerCommandResult_HandlesNullStrings()
        {
            var result = new DockerCommandResult(0, null, null);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Equal(string.Empty, result.StdErr);
        }

        [Fact]
        public void DockerRunner_Constructor_WithCustomImage()
        {
            var runner = new DockerRunner(imageName: "custom/image:latest");
            // Should not throw
            Assert.NotNull(runner);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Integration Tests (require Docker)
        // ─────────────────────────────────────────────────────────────────────

        [DockerAvailableFact]
        public void DockerPath_ResolvesOnCurrentPlatform()
        {
            var path = DockerEnvironment.GetDockerPath();

            _output.WriteLine("Docker path: {0}", path ?? "(null)");
            Assert.NotNull(path);
            Assert.NotEmpty(path);
        }

        [DockerAvailableFact]
        public void DockerAvailability_ReturnsTrue()
        {
            Assert.True(DockerEnvironment.IsDockerAvailable());
        }

        [DockerAvailableFact]
        public void DockerVersion_ReturnsNonEmpty()
        {
            var version = DockerEnvironment.GetDockerVersion();

            _output.WriteLine("Docker version: {0}", version ?? "(null)");
            Assert.NotNull(version);
            Assert.Contains("Docker", version);
        }

        [DockerAvailableFact]
        public void RunHeadless_EchoCommand_ReturnsOutput()
        {
            var runner = new DockerRunner();
            var tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D_Tests", "Docker_Echo");
            Directory.CreateDirectory(tempDir);

            try
            {
                var result = runner.RunHeadless("echo hello-from-docker", tempDir, timeoutMs: 60000);

                _output.WriteLine("Exit code: {0}", result.ExitCode);
                _output.WriteLine("StdOut: {0}", result.StdOut);
                _output.WriteLine("StdErr: {0}", result.StdErr);

                Assert.True(result.Success, "Docker echo command should succeed. StdErr: " + result.StdErr);
                Assert.Contains("hello-from-docker", result.StdOut);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [DockerAvailableFact]
        public void RunHeadless_OpenFoamVersion_Succeeds()
        {
            var runner = new DockerRunner();
            var tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D_Tests", "Docker_OF");
            Directory.CreateDirectory(tempDir);

            try
            {
                var cmd = string.Format("source {0} && simpleFoam -help", DockerConfig.OpenFoamBashrc);
                var result = runner.RunHeadless(cmd, tempDir, timeoutMs: 120000);

                _output.WriteLine("Exit code: {0}", result.ExitCode);
                _output.WriteLine("StdOut: {0}", result.StdOut);

                // simpleFoam -help returns exit code 0 or 1 depending on OpenFOAM version,
                // but it should produce output mentioning "simpleFoam" or "Usage"
                Assert.True(
                    result.StdOut.Contains("simpleFoam") || result.StdOut.Contains("Usage") || result.StdErr.Contains("simpleFoam"),
                    "Expected OpenFOAM simpleFoam help output");
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [DockerAvailableFact]
        public void WriteDockerRunScript_CreatesExecutableFile()
        {
            var runner = new DockerRunner();
            var tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D_Tests", "Docker_Script");
            Directory.CreateDirectory(tempDir);

            try
            {
                var scriptPath = Path.Combine(tempDir, "test_run.sh");
                runner.WriteDockerRunScript(scriptPath, "blockMesh && simpleFoam", tempDir);

                Assert.True(File.Exists(scriptPath), "Script file should be created");

                var content = File.ReadAllText(scriptPath);
                Assert.StartsWith("#!/bin/bash", content);
                Assert.Contains(DockerConfig.ImageName, content);
                Assert.Contains(DockerConfig.OpenFoamBashrc, content);
                Assert.Contains("blockMesh", content);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [DockerAvailableFact]
        [Trait("Category", "Slow")]
        public void PullImage_Succeeds()
        {
            var runner = new DockerRunner();
            var result = runner.PullImage();

            _output.WriteLine("Exit code: {0}", result.ExitCode);
            _output.WriteLine("StdOut: {0}", result.StdOut);
            _output.WriteLine("StdErr: {0}", result.StdErr);

            Assert.True(result.Success,
                string.Format("Docker pull should succeed. StdErr: {0}", result.StdErr));
        }

        [DockerAvailableFact]
        public void CheckDocker_DoesNotThrow_WhenDockerAvailable()
        {
            // Should not throw when Docker is available
            EddyLib.DefaultDirectoriesAndPaths.CheckDocker();
        }
    }
}
