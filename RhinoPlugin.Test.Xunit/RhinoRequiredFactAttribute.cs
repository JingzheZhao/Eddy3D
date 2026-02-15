using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Fact attribute that skips the test if Rhino is not installed on the machine.
    /// </summary>
    public class RhinoRequiredFactAttribute : FactAttribute
    {
        private static readonly Lazy<bool> RhinoInstalled = new Lazy<bool>(CheckRhinoInstalled);
        public static bool IsRhinoInstalled => RhinoInstalled.Value;

        public RhinoRequiredFactAttribute()
        {
            if (!IsRhinoInstalled)
            {
                Skip = "Test requires Rhino to be installed";
            }
        }

        private static bool CheckRhinoInstalled()
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

                string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

                // Check for Rhino WIP first
                string systemDirWIP = Path.Combine(programFiles, "Rhino WIP", "System");
                if (Directory.Exists(systemDirWIP))
                {
                    return true;
                }

                // Check for Rhino 8
                string systemDir8 = Path.Combine(programFiles, "Rhino 8", "System");
                if (Directory.Exists(systemDir8))
                {
                    return true;
                }

                // Check for Rhino 7
                string systemDir7 = Path.Combine(programFiles, "Rhino 7", "System");
                if (Directory.Exists(systemDir7))
                {
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    public class RhinoRequiredTheoryAttribute : TheoryAttribute
    {
        public RhinoRequiredTheoryAttribute()
        {
            if (!RhinoRequiredFactAttribute.IsRhinoInstalled)
            {
                Skip = "Test requires Rhino to be installed";
            }
        }
    }
}
