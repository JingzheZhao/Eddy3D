using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace EddyLib.Docker
{
    /// <summary>
    /// Discovers the Docker executable and configures the process environment
    /// for cross-platform Docker execution.
    /// Ported from OutdoorPlus UMCFCase.GetDockerPath() and ConfigureDockerEnvironment().
    /// </summary>
    public static class DockerEnvironment
    {
        /// <summary>Returns true if the current platform is macOS.</summary>
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>Returns true if the current platform is Windows.</summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Finds the Docker executable path. On macOS, GUI apps (like Rhino)
        /// don't inherit the shell PATH, so common installation locations are probed.
        /// </summary>
        /// <returns>Full path to docker executable, or null if not found.</returns>
        public static string GetDockerPath()
        {
            if (IsMacOS)
            {
                var possiblePaths = new[]
                {
                    "/usr/local/bin/docker",
                    "/opt/homebrew/bin/docker",
                    "/Applications/Docker.app/Contents/Resources/bin/docker",
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".docker", "bin", "docker")
                };

                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                        return path;
                }

                return null;
            }
            else
            {
                // On Windows, check common locations then fall back to PATH
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var possiblePaths = new[]
                {
                    Path.Combine(programFiles, "Docker", "Docker", "resources", "bin", "docker.exe"),
                    Path.Combine(programFiles, "Docker", "Docker", "resources", "docker.exe"),
                    "docker" // Fall back to PATH
                };

                foreach (var path in possiblePaths)
                {
                    if (path == "docker" || File.Exists(path))
                        return path;
                }

                return "docker";
            }
        }

        /// <summary>
        /// Returns true if the Docker daemon is running and accessible.
        /// Runs <c>docker info</c> with a 10-second timeout.
        /// </summary>
        public static bool IsDockerAvailable()
        {
            try
            {
                var dockerExe = GetDockerPath();
                if (string.IsNullOrEmpty(dockerExe))
                    return false;

                var psi = new ProcessStartInfo
                {
                    FileName = dockerExe,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                };
                psi.ArgumentList.Add("info");
                psi.ArgumentList.Add("--format");
                psi.ArgumentList.Add("{{.ServerVersion}}");

                ConfigureDockerEnvironment(psi);

                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();
                    if (!process.WaitForExit(10000))
                    {
                        try { process.Kill(); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                        return false;
                    }
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the Docker version string, or null on failure.
        /// </summary>
        public static string GetDockerVersion()
        {
            try
            {
                var dockerExe = GetDockerPath();
                if (string.IsNullOrEmpty(dockerExe))
                    return null;

                var psi = new ProcessStartInfo
                {
                    FileName = dockerExe,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                };
                psi.ArgumentList.Add("--version");

                ConfigureDockerEnvironment(psi);

                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(10000);

                    if (process.ExitCode == 0)
                        return output.Trim();
                }
            }
            catch
            {
                // Ignore
            }

            return null;
        }

        /// <summary>
        /// Returns true when the given Docker image is available in the local image cache.
        /// Does not pull from a registry.
        /// </summary>
        public static bool IsImageAvailable(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
                return false;

            try
            {
                var dockerExe = GetDockerPath();
                if (string.IsNullOrEmpty(dockerExe))
                    return false;

                var psi = new ProcessStartInfo
                {
                    FileName = dockerExe,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                };
                psi.ArgumentList.Add("image");
                psi.ArgumentList.Add("inspect");
                psi.ArgumentList.Add(imageName);

                ConfigureDockerEnvironment(psi);

                using (var process = new Process { StartInfo = psi })
                {
                    process.Start();
                    if (!process.WaitForExit(10000))
                    {
                        try { process.Kill(); } catch (Exception ex) { Debug.WriteLine(ex.Message); }
                        return false;
                    }

                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Configures <see cref="ProcessStartInfo"/> environment variables for Docker on macOS.
        /// Adds Docker-related paths to PATH so credential helpers can be found.
        /// No-op on Windows.
        /// </summary>
        public static void ConfigureDockerEnvironment(ProcessStartInfo psi)
        {
            if (!IsMacOS)
                return;

            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "/usr/bin:/bin";
            var newPath = string.Join(":", DockerConfig.MacDockerPaths) + ":" + currentPath;
            psi.Environment["PATH"] = newPath;
        }
    }
}
