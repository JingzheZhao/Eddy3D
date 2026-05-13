using System.Runtime.InteropServices;

namespace EddyLib.Strings
{
    internal static class OpenFoamLibraryNames
    {
        internal static string Name(string baseName)
        {
            string normalized = (baseName ?? string.Empty)
                .Trim()
                .Trim('"');

            // OpenFOAM 12 (Foundation) prefers shorthand like "sampling" instead of "libsampling.so"
            if (normalized.Equals("libsampling", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("sampling", System.StringComparison.OrdinalIgnoreCase))
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "libsampling" : "sampling";
            }

            if (normalized.EndsWith(".so", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.EndsWith(".dll", System.StringComparison.OrdinalIgnoreCase))
            {
                normalized = System.IO.Path.GetFileNameWithoutExtension(normalized);
            }

            return normalized + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" : ".so");
        }
    }
}
