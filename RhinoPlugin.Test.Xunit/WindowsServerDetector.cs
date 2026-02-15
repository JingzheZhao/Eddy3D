using System.Runtime.InteropServices;

namespace RhinoPlugin.Test.Xunit
{
    public static class WindowsServerDetector
    {
        public static bool IsWindowsServer()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

#if HOST_NONWINDOWS
            // When compiled on non-Windows with net8.0, Microsoft.Win32.Registry is not available.
            return false;
#else
            try
            {
                // Prefer 64-bit view to avoid WOW64 redirection; fallback to Default if needed.
                using (var baseKey = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64))
                using (var key = baseKey.OpenSubKey(TestConstants.WindowsVersionRegistryPath))
                {
                    if (key != null)
                    {
                        var installationType = key.GetValue(TestConstants.InstallationTypeValueName) as string;
                        if (!string.IsNullOrEmpty(installationType) &&
                            installationType.IndexOf("Server", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Registry not available — assume not a server.
            }

            return false;
#endif
        }
    }
}