using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EddyLib.Docker;

namespace EddyLib.Helpers
{
    /// <summary>
    /// Utilities for running external processes and commands.
    /// </summary>
    public static class ProcessRunner
    {
        /// <summary>
        /// Runs an OpenFOAM command inside Docker and returns the result.
        /// </summary>
        /// <param name="bashCommand">Bash command(s) to execute inside the container.</param>
        /// <param name="hostCasePath">Host filesystem path to mount as the case directory.</param>
        /// <param name="timeoutMs">Timeout in milliseconds (0 = no timeout).</param>
        public static DockerCommandResult RunDockerCommand(
            string bashCommand,
            string hostCasePath,
            int timeoutMs = 0)
        {
            var runner = new DockerRunner();
            return runner.RunHeadless(bashCommand, hostCasePath, timeoutMs);
        }

        private const string DefaultCmdPath = @"C:\Windows\System32\cmd.exe";

        /// <summary>
        /// Runs a command in cmd.exe.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="hideWindow">If true, hides the console window.</param>
        /// <param name="waitForExit">If true, waits for the process to complete.</param>
        /// <param name="closeAfter">If true, closes the process after completion.</param>
        /// <param name="executable">Path to the executable (defaults to cmd.exe).</param>
        public static void RunCommand(
            string command,
            bool hideWindow,
            bool waitForExit = false,
            bool closeAfter = false,
            string executable = DefaultCmdPath)
        {
            Utilities.ValidatePathForShell(executable);
            Utilities.ValidatePathForShell(command);

            var psi = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = hideWindow
            };

            // ✅ GOOD: Pass parameters as discrete tokens in ArgumentList instead of piping to StandardInput.
            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(command);

            using (var process = Process.Start(psi))
            {
                if (waitForExit)
                {
                    process?.WaitForExit();
                }

                if (closeAfter)
                {
                    process?.Close();
                }
            }
        }

        /// <summary>
        /// Runs a Gnuplot script.
        /// </summary>
        /// <param name="scriptPath">Path to the Gnuplot script.</param>
        /// <param name="hideWindow">If true, hides the console window.</param>
        /// <param name="waitForExit">If true, waits for completion.</param>
        public static void RunGnuplot(string scriptPath, bool hideWindow, bool waitForExit = false)
        {
            Utilities.ValidatePathForShell(scriptPath);
            RunCommand(scriptPath, hideWindow, waitForExit, executable: DefaultCmdPath);
        }

        /// <summary>
        /// Runs a command asynchronously in a new thread.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="hideWindow">If true, hides the console window.</param>
        /// <param name="closeAfter">If true, closes the process after completion.</param>
        /// <param name="executable">Path to the executable.</param>
        /// <param name="onCompleted">Optional callback when process completes.</param>
        public static void RunCommandAsync(
            string command,
            bool hideWindow,
            bool closeAfter = false,
            string executable = DefaultCmdPath,
            EventHandler onCompleted = null)
        {
            Utilities.ValidatePathForShell(executable);
            Utilities.ValidatePathForShell(command);

            if (!File.Exists(executable))
            {
                return;
            }

            var thread = new Thread(() =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    CreateNoWindow = hideWindow
                };

                // ✅ GOOD: Avoid StandardInput and string concatenation. Use ArgumentList for shell parameters.
                psi.ArgumentList.Add("/c");
                psi.ArgumentList.Add(command);

                using (var process = Process.Start(psi))
                {
                    process?.WaitForExit();

                    if (closeAfter)
                    {
                        process?.Close();
                    }

                    onCompleted?.Invoke(process, EventArgs.Empty);
                }
            });

            thread.Start();
        }
    }
}
