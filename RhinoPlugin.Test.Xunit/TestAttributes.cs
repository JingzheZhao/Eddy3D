using Xunit;
using System.IO;
using System.Reflection;
using System;
using System.Runtime.InteropServices;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Unified skip-reason logic for tests that require the Rhino in-process host.
    /// On non-Windows this host cannot run (requires RhinoLibrary.dll P/Invoke).
    /// On Windows Server the special Rhino CI license may be absent.
    /// Otherwise Rhino must be installed.
    /// </summary>
    internal static class RhinoHostSkip
    {
        internal const string Reason = "Rhino in-process hosting requires Windows with Rhino installed.";

        internal static string GetReason()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Reason;
            if (WindowsServerDetector.IsWindowsServer())
                return WindowsServerSkip.Reason;
            if (!RhinoRequiredFactAttribute.IsRhinoInstalled)
                return "Rhino is not installed.";
            return null;
        }
    }

    internal static class WindowsServerSkip
    {
        internal const string Reason = "Skipping test on Windows Server (CI/CD environment) due to special Rhino license necessary.";

        internal static string GetReason()
        {
            return WindowsServerDetector.IsWindowsServer() ? Reason : null;
        }
    }

    /// <summary>
    /// Skips the test on Windows Server CI and when the Rhino native host is unavailable.
    /// These tests use RhinoCommon geometry operations that P/Invoke into native opennurbs,
    /// so they need the Rhino host to be initialized. On macOS the host cannot start
    /// (requires Windows P/Invoke), so these tests skip gracefully.
    /// </summary>
    public class NotWindowsServerFactAttribute : FactAttribute
    {
        public NotWindowsServerFactAttribute()
        {
            var reason = WindowsServerSkip.GetReason();
            if (reason == null && !XunitTestInitFixture.RhinoAvailable)
                reason = XunitTestInitFixture.RhinoSkipReason ?? "Rhino native host is not available.";
            Skip = reason;
        }
    }

    /// <summary>
    /// Skips the test on Windows Server CI and when the Rhino native host is unavailable.
    /// These tests use RhinoCommon geometry operations that P/Invoke into native opennurbs,
    /// so they need the Rhino host to be initialized. On macOS the host cannot start
    /// (requires Windows P/Invoke), so these tests skip gracefully.
    /// </summary>
    public class NotWindowsServerTheoryAttribute : TheoryAttribute
    {
        public NotWindowsServerTheoryAttribute()
        {
            var reason = WindowsServerSkip.GetReason();
            if (reason == null && !XunitTestInitFixture.RhinoAvailable)
                reason = XunitTestInitFixture.RhinoSkipReason ?? "Rhino native host is not available.";
            Skip = reason;
        }
    }

    internal static class GrasshopperSkip
    {
        internal const string Reason = "Skipping test because Grasshopper is not available.";

        internal static string GetReason()
        {
            // If the Rhino host is not available, skip for that reason first
            var hostReason = RhinoHostSkip.GetReason();
            if (hostReason != null) return hostReason;

            if (HasGrasshopperInstall())
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
                return Reason;
            }
        }

        private static bool HasGrasshopperInstall()
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

            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            foreach (var rhinoVersion in TestConstants.RhinoVersions)
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
    }

    public class RequiresGrasshopperFactAttribute : FactAttribute
    {
        public RequiresGrasshopperFactAttribute()
        {
            Skip = GrasshopperSkip.GetReason();
        }
    }

    public class RequiresGrasshopperTheoryAttribute : TheoryAttribute
    {
        public RequiresGrasshopperTheoryAttribute()
        {
            Skip = GrasshopperSkip.GetReason();
        }
    }
}
