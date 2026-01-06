using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace EddyLib.Helpers
{
    /// <summary>
    /// Utilities for running external processes and commands.
    /// </summary>
    public static class ProcessRunner
    {
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
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    CreateNoWindow = hideWindow
                }
            };

            process.Start();

            using (var writer = process.StandardInput)
            {
                writer.WriteLine(command);
                writer.Flush();
            }

            if (waitForExit)
            {
                process.WaitForExit();
            }

            if (closeAfter)
            {
                process.Close();
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
            if (!File.Exists(executable))
            {
                return;
            }

            var thread = new Thread(() =>
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executable,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        CreateNoWindow = hideWindow
                    }
                };

                string fullCommand = closeAfter ? command + "\r\nexit\r\n" : command;

                process.Start();

                using (var writer = process.StandardInput)
                {
                    writer.WriteLine(fullCommand);
                }

                process.WaitForExit();

                if (closeAfter)
                {
                    process.Close();
                }

                onCompleted?.Invoke(process, EventArgs.Empty);
            });

            thread.Start();
        }
    }
}
