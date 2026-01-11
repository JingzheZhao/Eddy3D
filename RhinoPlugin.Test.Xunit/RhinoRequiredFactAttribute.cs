using System;
using System.IO;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Fact attribute that skips the test if Rhino is not installed on the machine.
    /// </summary>
    public class RhinoRequiredFactAttribute : FactAttribute
    {
        private static readonly Lazy<bool> _rhinoInstalled = new Lazy<bool>(CheckRhinoInstalled);

        public RhinoRequiredFactAttribute()
        {
            if (!_rhinoInstalled.Value)
            {
                Skip = "Test requires Rhino to be installed";
            }
        }

        private static bool CheckRhinoInstalled()
        {
            try
            {
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
}
