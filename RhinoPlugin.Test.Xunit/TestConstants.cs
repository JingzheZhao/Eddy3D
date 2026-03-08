using System;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Shared test constants and default values.
    /// </summary>
    public static class TestConstants
    {
        // ─────────────────────────────────────────────────────────────────────
        // ABL / Simulation Defaults
        // ─────────────────────────────────────────────────────────────────────
        public const double DefaultTolerance = 1e-9;
        public const double DefaultUref = 10.0;
        public const double DefaultZref = 10.0;
        public const double DefaultZ0 = 1;
        public const int DefaultIterations = 1500;

        // ─────────────────────────────────────────────────────────────────────
        // Geometry Defaults
        // ─────────────────────────────────────────────────────────────────────
        public const double DefaultWeldAngleRadians = Math.PI;
        public const double DefaultCoplanarTolerance = 1e-6;

        // ─────────────────────────────────────────────────────────────────────
        // Solution / Project Structure
        // ─────────────────────────────────────────────────────────────────────
        public const string SolutionFolderName = "Eddy3D";

        // ─────────────────────────────────────────────────────────────────────
        // Rhino / Grasshopper Installation
        // ─────────────────────────────────────────────────────────────────────
        public const string GrasshopperDllName = "Grasshopper.dll";
        public const string GrasshopperPluginPath = @"Plug-ins\Grasshopper";

        /// <summary>
        /// Rhino version folder names to check for Grasshopper installation.
        /// </summary>
        public static readonly string[] RhinoVersions = { "Rhino WIP", "Rhino 8" };

        // ─────────────────────────────────────────────────────────────────────
        // Windows Registry (for server detection)
        // ─────────────────────────────────────────────────────────────────────
        public const string WindowsVersionRegistryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        public const string InstallationTypeValueName = "InstallationType";

        // ─────────────────────────────────────────────────────────────────────
        // Test Output Directories
        // ─────────────────────────────────────────────────────────────────────
        public static string TestingDirectory => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Eddy3D_Tests");

        public const int HoursPerYear = 8760;
    }
}
