using System.Globalization;

namespace EddyLib.FluidX3D
{
    public sealed class FluidX3DRunSettings
    {
        public int MemoryMb { get; set; } = 1000;

        public double SimSeconds { get; set; } = 360.0;

        public double ExportIntervalSeconds { get; set; } = 30.0;

        public double GroundZ { get; set; } = 0.0;

        public string SourceDirectory { get; set; } = string.Empty;

        public override string ToString()
        {
            return "FluidX3D Run Settings"
                + "\nMemoryMb = " + MemoryMb.ToString(CultureInfo.InvariantCulture)
                + "\nSimSeconds = " + SimSeconds.ToString(CultureInfo.InvariantCulture)
                + "\nExportIntervalSeconds = " + ExportIntervalSeconds.ToString(CultureInfo.InvariantCulture)
                + "\nGroundZ = " + GroundZ.ToString(CultureInfo.InvariantCulture)
                + "\nSourceDirectory = " + (SourceDirectory ?? string.Empty);
        }
    }
}
