using Xunit;
using System.IO;
using System.Reflection;
using System;

namespace RhinoPlugin.Test.Xunit
{
    internal static class WindowsServerSkip
    {
        internal const string Reason = "Skipping test on Windows Server (CI/CD environment) due to special Rhino license necessary.";

        internal static string GetReason()
        {
            return WindowsServerDetector.IsWindowsServer() ? Reason : null;
        }
    }

    public class NotWindowsServerFactAttribute : FactAttribute
    {
        public NotWindowsServerFactAttribute()
        {
            Skip = WindowsServerSkip.GetReason();
        }
    }

    public class NotWindowsServerTheoryAttribute : TheoryAttribute
    {
        public NotWindowsServerTheoryAttribute()
        {
            Skip = WindowsServerSkip.GetReason();
        }
    }

    internal static class GrasshopperSkip
    {
        internal const string Reason = "Skipping test because Grasshopper is not available.";

        internal static string GetReason()
        {
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