using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RhinoPlugin.Test.Xunit
{
    internal enum TestExecutionRequirement
    {
        RhinoInstalled,
        RhinoNativeHost,
        GrasshopperWithRhinoHost,
        BlueCfd,
        Radiance,
        EnergyPlus,
        Python,
        ExternalService,
        OpenFoamExecution
    }

    /// <summary>
    /// Centralized policy for deciding whether tests should run, skip, or fail-fast by environment.
    /// </summary>
    internal static class TestExecutionPolicy
    {
        internal const string WindowsServerReason = "Skipping test on Windows Server (CI/CD environment) due to special Rhino license necessary.";
        internal const string RhinoInstallMissingReason = "Rhino is not installed.";
        internal const string RhinoHostRequiresWindowsReason = "Rhino in-process hosting requires Windows (RhinoLibrary.dll P/Invoke).";
        internal const string GrasshopperMissingReason = "Skipping test because Grasshopper is not available.";
        internal const string UnsupportedPlatformReason = "Rhino tests currently support Windows and macOS only.";
        internal const string BlueCfdMissingReason = "Skipping test because blueCFD-Core 2024 / OpenFOAM 12 is not available.";
        internal const string RadianceMissingReason = "Skipping test because Radiance is not available.";
        internal const string EnergyPlusMissingReason = "Skipping test because EnergyPlus is not available.";
        internal const string PythonMissingReason = "Skipping test because Python is not available.";
        internal const string ExternalServiceDisabledReason = "Skipping external service integration test. Set EDDY3D_RUN_EXTERNAL_TESTS=1 to run.";
        internal const string OpenFoamExecutionDisabledReason = "Skipping OpenFOAM execution integration test. Set EDDY3D_RUN_OPENFOAM_TESTS=1 to run.";

        private static readonly Lazy<bool> RhinoInstalled = new Lazy<bool>(DetectRhinoInstalled);
        private static readonly Lazy<bool> GrasshopperInstalled = new Lazy<bool>(DetectGrasshopperInstalled);
        private static readonly Lazy<bool> BlueCfdInstalled = new Lazy<bool>(DetectBlueCfdInstalled);
        private static readonly Lazy<bool> RadianceInstalled = new Lazy<bool>(DetectRadianceInstalled);
        private static readonly Lazy<bool> EnergyPlusInstalled = new Lazy<bool>(DetectEnergyPlusInstalled);
        private static readonly Lazy<PythonCommand> PythonCommandValue = new Lazy<PythonCommand>(FindPythonCommand);
        private static readonly Lazy<bool> LocalEnvFileLoaded = new Lazy<bool>(LoadLocalEnvFile);

        internal static bool IsRhinoInstalled { get { EnsureLocalEnvFileLoaded(); return RhinoInstalled.Value; } }
        internal static bool IsGrasshopperInstalled { get { EnsureLocalEnvFileLoaded(); return GrasshopperInstalled.Value; } }
        internal static bool IsBlueCfdInstalled { get { EnsureLocalEnvFileLoaded(); return BlueCfdInstalled.Value; } }
        internal static bool IsRadianceInstalled { get { EnsureLocalEnvFileLoaded(); return RadianceInstalled.Value; } }
        internal static bool IsEnergyPlusInstalled { get { EnsureLocalEnvFileLoaded(); return EnergyPlusInstalled.Value; } }
        internal static bool IsPythonAvailable { get { EnsureLocalEnvFileLoaded(); return PythonCommandValue.Value != null; } }
        internal static string PythonExecutable { get { EnsureLocalEnvFileLoaded(); return PythonCommandValue.Value?.FileName; } }

        internal static ProcessStartInfo CreatePythonProcessStartInfo(string workingDirectory = null)
        {
            EnsureLocalEnvFileLoaded();

            var command = PythonCommandValue.Value;
            if (command == null)
            {
                throw new InvalidOperationException(PythonMissingReason);
            }

            var processStartInfo = new ProcessStartInfo(command.FileName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                processStartInfo.WorkingDirectory = workingDirectory;
            }

            foreach (string argument in command.Arguments)
            {
                processStartInfo.ArgumentList.Add(argument);
            }

            return processStartInfo;
        }

        internal static bool ShouldFailFastOnRhinoHostInitialization()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                   !WindowsServerDetector.IsWindowsServer();
        }

        internal static string GetSkipReason(TestExecutionRequirement requirement)
        {
            EnsureLocalEnvFileLoaded();

            switch (requirement)
            {
                case TestExecutionRequirement.RhinoInstalled:
                    return GetRhinoInstalledSkipReason();
                case TestExecutionRequirement.RhinoNativeHost:
                    return GetRhinoNativeHostSkipReason();
                case TestExecutionRequirement.GrasshopperWithRhinoHost:
                    return GetGrasshopperSkipReason();
                case TestExecutionRequirement.BlueCfd:
                    return IsBlueCfdInstalled ? null : BlueCfdMissingReason;
                case TestExecutionRequirement.Radiance:
                    return IsRadianceInstalled ? null : RadianceMissingReason;
                case TestExecutionRequirement.EnergyPlus:
                    return IsEnergyPlusInstalled ? null : EnergyPlusMissingReason;
                case TestExecutionRequirement.Python:
                    return IsPythonAvailable ? null : PythonMissingReason;
                case TestExecutionRequirement.ExternalService:
                    return ExternalServiceTestsEnabled() ? null : ExternalServiceDisabledReason;
                case TestExecutionRequirement.OpenFoamExecution:
                    return OpenFoamExecutionTestsEnabled() ? null : OpenFoamExecutionDisabledReason;
                default:
                    return null;
            }
        }

        private static string GetRhinoInstalledSkipReason()
        {
            if (WindowsServerDetector.IsWindowsServer())
            {
                return WindowsServerReason;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On desktop Windows we do not skip here; host initialization should fail-fast if broken.
                return null;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return IsRhinoInstalled ? null : RhinoInstallMissingReason;
            }

            return UnsupportedPlatformReason;
        }

        private static string GetRhinoNativeHostSkipReason()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return RhinoHostRequiresWindowsReason;
            }

            if (WindowsServerDetector.IsWindowsServer())
            {
                return WindowsServerReason;
            }

            return null;
        }

        private static string GetGrasshopperSkipReason()
        {
            var hostReason = GetRhinoNativeHostSkipReason();
            if (hostReason != null)
            {
                return hostReason;
            }

            if (IsGrasshopperInstalled)
            {
                return null;
            }

            try
            {
                Assembly.Load("Grasshopper");
                return null;
            }
            catch
            {
                return GrasshopperMissingReason;
            }
        }

        private static bool DetectRhinoInstalled()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var macCandidates = new[]
                    {
                        "/Applications/RhinoWIP.app",
                        "/Applications/Rhino 8.app",
                        "/Applications/Rhino.app"
                    };

                    foreach (var candidate in macCandidates)
                    {
                        if (Directory.Exists(candidate))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return false;
                }

                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var windowsCandidates = new[]
                {
                    Path.Combine(programFiles, "Rhino WIP", "System"),
                    Path.Combine(programFiles, "Rhino 8", "System"),
                    Path.Combine(programFiles, "Rhino 7", "System")
                };

                foreach (var candidate in windowsCandidates)
                {
                    if (Directory.Exists(candidate))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool DetectGrasshopperInstalled()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var macCandidates = new[]
                    {
                        "/Applications/RhinoWIP.app/Contents/Frameworks/RhCore.framework/Versions/A/Resources/ManagedPlugIns/GrasshopperPlugin.rhp/Grasshopper.dll",
                        "/Applications/Rhino 8.app/Contents/Frameworks/RhCore.framework/Versions/A/Resources/ManagedPlugIns/GrasshopperPlugin.rhp/Grasshopper.dll",
                        "/Applications/Rhino.app/Contents/Frameworks/RhCore.framework/Versions/A/Resources/ManagedPlugIns/GrasshopperPlugin.rhp/Grasshopper.dll"
                    };

                    foreach (var candidate in macCandidates)
                    {
                        if (File.Exists(candidate))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return false;
                }

                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                var windowsRhinoVersions = new[] { "Rhino WIP", "Rhino 8", "Rhino 7" };

                foreach (var rhinoVersion in windowsRhinoVersions)
                {
                    var dllPath = Path.Combine(
                        programFiles,
                        rhinoVersion,
                        TestConstants.GrasshopperPluginPath,
                        TestConstants.GrasshopperDllName);

                    if (File.Exists(dllPath))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool DetectBlueCfdInstalled()
        {
            try
            {
                EddyLib.DefaultDirectoriesAndPaths.CheckBlueCfd();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool DetectRadianceInstalled()
        {
            try
            {
                EddyLib.DefaultDirectoriesAndPaths.CheckRadiance();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool DetectEnergyPlusInstalled()
        {
            try
            {
                EddyLib.DefaultDirectoriesAndPaths.CheckEnergyPlus();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static PythonCommand FindPythonCommand()
        {
            var pythonOverride = Environment.GetEnvironmentVariable("EDDY3D_PYTHON_EXE");
            if (!string.IsNullOrWhiteSpace(pythonOverride) && CanStartProcess(pythonOverride, "--version"))
            {
                return new PythonCommand(pythonOverride);
            }

            string[] candidates = { "python", "python3" };
            foreach (string candidate in candidates)
            {
                if (CanStartProcess(candidate, "--version"))
                {
                    return new PythonCommand(candidate);
                }
            }

            var uvOverride = Environment.GetEnvironmentVariable("EDDY3D_UV_EXE");
            if (!string.IsNullOrWhiteSpace(uvOverride) && CanStartProcess(uvOverride, "run", "--no-project", "python", "--version"))
            {
                return new PythonCommand(uvOverride, "run", "--no-project", "python");
            }

            if (CanStartProcess("uv", "run", "--no-project", "python", "--version"))
            {
                return new PythonCommand("uv", "run", "--no-project", "python");
            }

            return null;
        }

        private static bool CanStartProcess(string fileName, params string[] arguments)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo(fileName)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                foreach (string argument in arguments)
                {
                    processStartInfo.ArgumentList.Add(argument);
                }

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        return false;
                    }

                    if (!process.WaitForExit(5000))
                    {
                        try { process.Kill(); } catch { }
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

        private sealed class PythonCommand
        {
            internal PythonCommand(string fileName, params string[] arguments)
            {
                FileName = fileName;
                Arguments = arguments ?? Array.Empty<string>();
            }

            internal string FileName { get; }
            internal string[] Arguments { get; }
        }

        private static bool ExternalServiceTestsEnabled()
        {
            EnsureLocalEnvFileLoaded();
            return IsTruthy(Environment.GetEnvironmentVariable("EDDY3D_RUN_EXTERNAL_TESTS"));
        }

        private static bool OpenFoamExecutionTestsEnabled()
        {
            EnsureLocalEnvFileLoaded();
            return IsTruthy(Environment.GetEnvironmentVariable("EDDY3D_RUN_OPENFOAM_TESTS"));
        }

        private static void EnsureLocalEnvFileLoaded()
        {
            _ = LocalEnvFileLoaded.Value;
        }

        private static bool LoadLocalEnvFile()
        {
            try
            {
                string envFile = FindRepositoryFile(".env");
                if (string.IsNullOrWhiteSpace(envFile) || !File.Exists(envFile))
                {
                    return false;
                }

                foreach (string rawLine in File.ReadLines(envFile))
                {
                    string line = rawLine.Trim();
                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                    {
                        line = line.Substring("export ".Length).Trim();
                    }

                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                    {
                        continue;
                    }

                    string name = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();
                    if (string.IsNullOrWhiteSpace(name) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
                    {
                        continue;
                    }

                    Environment.SetEnvironmentVariable(name, TrimWrappingQuotes(value));
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string FindRepositoryFile(string fileName)
        {
            string current = Directory.GetCurrentDirectory();
            while (!string.IsNullOrWhiteSpace(current))
            {
                string candidate = Path.Combine(current, fileName);
                if (File.Exists(candidate) && File.Exists(Path.Combine(current, "Eddy.sln")))
                {
                    return candidate;
                }

                string parent = Directory.GetParent(current)?.FullName;
                if (string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = parent;
            }

            return null;
        }

        private static string TrimWrappingQuotes(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }

        private static bool IsTruthy(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            return raw.Equals("1", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
