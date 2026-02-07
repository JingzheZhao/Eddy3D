using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace EddyLib.Docker
{
    /// <summary>
    /// Result of a Docker command execution.
    /// Ported from OutdoorPlus CampusCaseOpenFoamHarness.CommandResult.
    /// </summary>
    public struct DockerCommandResult
    {
        public int ExitCode { get; }
        public string StdOut { get; }
        public string StdErr { get; }
        public bool Success { get { return ExitCode == 0; } }

        public DockerCommandResult(int exitCode, string stdout, string stderr)
        {
            ExitCode = exitCode;
            StdOut = stdout ?? string.Empty;
            StdErr = stderr ?? string.Empty;
        }

        public override string ToString()
        {
            return string.Format("ExitCode={0}, StdOut.Length={1}, StdErr.Length={2}",
                ExitCode, StdOut.Length, StdErr.Length);
        }
    }

    /// <summary>
    /// Executes OpenFOAM commands inside a Docker container.
    /// Supports both headless (background) and interactive (terminal) modes.
    /// Ported from OutdoorPlus UMCFCase.RunDockerCommand() and RunDockerCommandInteractive().
    /// </summary>
    public class DockerRunner
    {
        private readonly string _dockerExe;
        private readonly string _imageName;

        /// <summary>
        /// Creates a new DockerRunner instance.
        /// </summary>
        /// <param name="dockerExe">Path to the docker executable. If null, auto-discovers.</param>
        /// <param name="imageName">Docker image name. If null, uses <see cref="DockerConfig.ImageName"/>.</param>
        public DockerRunner(string dockerExe = null, string imageName = null)
        {
            _dockerExe = dockerExe ?? DockerEnvironment.GetDockerPath();
            _imageName = imageName ?? DockerConfig.ImageName;
        }

        /// <summary>
        /// Runs a bash command headlessly inside a Docker container.
        /// Captures stdout/stderr and returns a <see cref="DockerCommandResult"/>.
        /// </summary>
        /// <param name="bashCmd">The bash command(s) to execute (chained with &amp;&amp;).</param>
        /// <param name="hostCasePath">Host path to mount as /case.</param>
        /// <param name="timeoutMs">Timeout in ms (0 = no timeout).</param>
        public DockerCommandResult RunHeadless(string bashCmd, string hostCasePath, int timeoutMs = 0)
        {
            if (string.IsNullOrEmpty(_dockerExe))
            {
                return new DockerCommandResult(-1, string.Empty,
                    "Docker not found. Please install Docker Desktop.");
            }

            var args = string.Format(
                "run --rm --platform {0} --entrypoint /bin/bash -v \"{1}:{2}\" -w {2} {3} -c \"{4}\"",
                DockerConfig.Platform,
                hostCasePath,
                DockerConfig.CaseMountPoint,
                _imageName,
                bashCmd.Replace("\"", "\\\""));

            var psi = new ProcessStartInfo
            {
                FileName = _dockerExe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            DockerEnvironment.ConfigureDockerEnvironment(psi);

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            using (var process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null) stdout.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null) stderr.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (timeoutMs > 0)
                {
                    if (!process.WaitForExit(timeoutMs))
                    {
                        try { process.Kill(); } catch { }
                        return new DockerCommandResult(-1, stdout.ToString(),
                            "Docker command timed out after " + timeoutMs + "ms");
                    }
                }
                else
                {
                    process.WaitForExit();
                }

                return new DockerCommandResult(process.ExitCode, stdout.ToString(), stderr.ToString());
            }
        }

        /// <summary>
        /// Runs a bash command in an interactive terminal window.
        /// macOS: writes a .sh script and opens Terminal.app.
        /// Windows: launches wt.exe or cmd.exe.
        /// </summary>
        /// <param name="bashCmd">The bash command(s) to execute.</param>
        /// <param name="hostCasePath">Host path to mount as /case.</param>
        /// <returns>Log of the launch process.</returns>
        public string RunInteractive(string bashCmd, string hostCasePath)
        {
            var log = new StringBuilder();

            if (string.IsNullOrEmpty(_dockerExe))
            {
                log.AppendLine("ERROR: Docker not found. Please install Docker Desktop.");
                return log.ToString();
            }

            try
            {
                log.AppendLine(string.Format("{0} Preparing interactive Docker command...",
                    DateTime.Now.ToString("HH:mm:ss")));
                log.AppendLine(string.Format("{0} Using Docker: {1}",
                    DateTime.Now.ToString("HH:mm:ss"), _dockerExe));

                var escapedBashCmd = bashCmd.Replace("'", "'\"'\"'");

                if (DockerEnvironment.IsMacOS)
                {
                    LaunchInteractiveMacOS(escapedBashCmd, hostCasePath, log);
                }
                else
                {
                    LaunchInteractiveWindows(escapedBashCmd, hostCasePath, log);
                }
            }
            catch (Exception ex)
            {
                log.AppendLine(string.Format("Error launching interactive Docker: {0}", ex.Message));
            }

            return log.ToString();
        }

        /// <summary>
        /// Writes a reusable .sh script for manual Docker execution.
        /// </summary>
        /// <param name="scriptPath">Full path for the output script file.</param>
        /// <param name="bashCmd">The bash command(s) to execute inside the container.</param>
        /// <param name="hostCasePath">Host path to mount as /case.</param>
        public void WriteDockerRunScript(string scriptPath, string bashCmd, string hostCasePath)
        {
            var dockerExe = _dockerExe ?? "/usr/local/bin/docker";
            var escapedCmd = bashCmd.Replace("'", "'\"'\"'");
            var preamble = BuildPreamble();
            var fullCmd = preamble + " && " + escapedCmd;

            var scriptContent = string.Format(
@"#!/bin/bash
# Auto-generated script for Eddy3D Docker execution
# Generated: {0}

export PATH=""/usr/local/bin:/opt/homebrew/bin:/Applications/Docker.app/Contents/Resources/bin:$HOME/.docker/bin:$PATH""

echo ""Eddy3D Docker Runner""
echo ""Image: {1}""
echo ""Case: {2}""
echo ""----------------------------------------""

{3} run --rm -it --platform {4} --entrypoint /bin/bash \
  -v ""{2}:{5}"" \
  -w {5} {1} \
  -c '{6}'

echo """"
echo ""----------------------------------------""
echo ""Docker execution complete.""",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                _imageName,
                hostCasePath,
                dockerExe,
                DockerConfig.Platform,
                DockerConfig.CaseMountPoint,
                fullCmd);

            File.WriteAllText(scriptPath, scriptContent);

            // Make executable on Unix
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var chmodPsi = new ProcessStartInfo("/bin/chmod", string.Format("+x \"{0}\"", scriptPath))
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using (var p = Process.Start(chmodPsi))
                    {
                        p?.WaitForExit();
                    }
                }
                catch
                {
                    // Ignore chmod errors
                }
            }
        }

        /// <summary>
        /// Pulls the Docker image. Returns result with pull output.
        /// </summary>
        public DockerCommandResult PullImage()
        {
            if (string.IsNullOrEmpty(_dockerExe))
            {
                return new DockerCommandResult(-1, string.Empty,
                    "Docker not found. Please install Docker Desktop.");
            }

            var args = string.Format("pull --platform {0} {1}", DockerConfig.Platform, _imageName);

            var psi = new ProcessStartInfo
            {
                FileName = _dockerExe,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            DockerEnvironment.ConfigureDockerEnvironment(psi);

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            using (var process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null) stdout.AppendLine(e.Data);
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null) stderr.AppendLine(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                return new DockerCommandResult(process.ExitCode, stdout.ToString(), stderr.ToString());
            }
        }

        /// <summary>
        /// Builds the bash preamble: sources bashrc, sets PATH for MPI.
        /// </summary>
        public static string BuildPreamble()
        {
            return string.Format("source {0} && export PATH={1}:$PATH",
                DockerConfig.OpenFoamBashrc, DockerConfig.MpiPath);
        }

        /// <summary>
        /// Joins a list of commands with preamble into a single &amp;&amp; chain.
        /// </summary>
        public static string BuildCommandChain(IReadOnlyList<string> commands)
        {
            var all = new List<string>
            {
                "cd " + DockerConfig.CaseMountPoint,
                BuildPreamble()
            };
            all.AddRange(commands);
            return string.Join(" && ", all);
        }

        private void LaunchInteractiveMacOS(string escapedBashCmd, string hostCasePath, StringBuilder log)
        {
            // Write a shell script and open it with Terminal.app
            var scriptPath = Path.Combine(hostCasePath, "run_docker.sh");

            var scriptContent = string.Format(
@"#!/bin/bash
export PATH=""/usr/local/bin:/opt/homebrew/bin:/Applications/Docker.app/Contents/Resources/bin:$HOME/.docker/bin:$PATH""

echo ""Starting Docker container...""
echo ""Image: {0}""
echo ""Case: {1}""
echo ""----------------------------------------""

{2} run --rm -it --platform {3} --entrypoint /bin/bash \
  -v ""{1}:{4}"" \
  -w {4} {0} \
  -c '{5}'

echo """"
echo ""----------------------------------------""
echo ""Docker execution complete.""",
                _imageName,
                hostCasePath,
                _dockerExe,
                DockerConfig.Platform,
                DockerConfig.CaseMountPoint,
                escapedBashCmd);

            File.WriteAllText(scriptPath, scriptContent);
            log.AppendLine(string.Format("{0} Created script: {1}",
                DateTime.Now.ToString("HH:mm:ss"), scriptPath));

            // Make script executable
            var chmodPsi = new ProcessStartInfo
            {
                FileName = "/bin/chmod",
                Arguments = string.Format("+x \"{0}\"", scriptPath),
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var chmodProcess = Process.Start(chmodPsi))
            {
                chmodProcess?.WaitForExit();
            }

            // Open Terminal.app with the script
            var psi = new ProcessStartInfo
            {
                FileName = "/usr/bin/open",
                Arguments = string.Format("-a Terminal \"{0}\"", scriptPath),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            log.AppendLine(string.Format("{0} Opening Terminal.app...", DateTime.Now.ToString("HH:mm:ss")));

            using (var process = Process.Start(psi))
            {
                var stderr = process?.StandardError.ReadToEnd();
                process?.WaitForExit();

                if (process?.ExitCode != 0)
                {
                    log.AppendLine(string.Format("Warning: Terminal open returned exit code {0}",
                        process?.ExitCode));
                    if (!string.IsNullOrEmpty(stderr))
                        log.AppendLine(string.Format("Error: {0}", stderr));
                }
                else
                {
                    log.AppendLine(string.Format("{0} Terminal window opened for Docker command.",
                        DateTime.Now.ToString("HH:mm:ss")));
                }
            }
        }

        private void LaunchInteractiveWindows(string escapedBashCmd, string hostCasePath, StringBuilder log)
        {
            var dockerArgs = string.Format(
                "run --rm -it -v \"{0}:{1}\" -w {1} {2} /bin/bash -c '{3}'",
                hostCasePath,
                DockerConfig.CaseMountPoint,
                _imageName,
                escapedBashCmd);

            var fullDockerCmd = string.Format("{0} {1}", _dockerExe, dockerArgs);
            log.AppendLine(string.Format("{0} Launching terminal with: {1}",
                DateTime.Now.ToString("HH:mm:ss"), fullDockerCmd));

            try
            {
                Process.Start("wt.exe", fullDockerCmd);
            }
            catch
            {
                Process.Start("cmd.exe", string.Format("/k {0}", fullDockerCmd));
            }

            log.AppendLine(string.Format("{0} Terminal window opened for Docker command.",
                DateTime.Now.ToString("HH:mm:ss")));
        }
    }
}
