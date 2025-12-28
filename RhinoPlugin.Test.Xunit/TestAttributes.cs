using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    internal static class WindowsServerSkip
    {
        internal const string Reason = "Skipping test on Windows Server (CI/CD environment).";

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
}
