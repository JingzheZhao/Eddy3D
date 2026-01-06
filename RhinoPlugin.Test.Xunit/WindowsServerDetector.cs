using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace RhinoPlugin.Test.Xunit
{
    public static class WindowsServerDetector
    {
        public static bool IsWindowsServer()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return false;

            // Prefer 64-bit view to avoid WOW64 redirection; fallback to Default if needed.
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
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

            return false;
        }
    }
}