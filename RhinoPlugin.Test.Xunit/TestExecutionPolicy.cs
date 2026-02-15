using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RhinoPlugin.Test.Xunit
{
    internal enum TestExecutionRequirement
    {
        RhinoInstalled,
        RhinoNativeHost,
        GrasshopperWithRhinoHost
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

        private static readonly Lazy<bool> RhinoInstalled = new Lazy<bool>(DetectRhinoInstalled);
        private static readonly Lazy<bool> GrasshopperInstalled = new Lazy<bool>(DetectGrasshopperInstalled);

        internal static bool IsRhinoInstalled => RhinoInstalled.Value;
        internal static bool IsGrasshopperInstalled => GrasshopperInstalled.Value;

        internal static bool ShouldFailFastOnRhinoHostInitialization()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                   !WindowsServerDetector.IsWindowsServer();
        }

        internal static string GetSkipReason(TestExecutionRequirement requirement)
        {
            switch (requirement)
            {
                case TestExecutionRequirement.RhinoInstalled:
                    return GetRhinoInstalledSkipReason();
                case TestExecutionRequirement.RhinoNativeHost:
                    return GetRhinoNativeHostSkipReason();
                case TestExecutionRequirement.GrasshopperWithRhinoHost:
                    return GetGrasshopperSkipReason();
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
    }
}
