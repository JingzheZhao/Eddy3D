namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Shared test constants and default values.
    /// </summary>
    public static class TestConstants
    {
        public const double DefaultTolerance = 1e-9;
        public const double DefaultUref = 10.0;
        public const double DefaultZref = 10.0;
        public const double DefaultZ0 = 0.05;
        public const int DefaultIterations = 3000;

        // Directory for temporary test output
        public static string TestingDirectory => System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Eddy3D_Tests");
    }
}