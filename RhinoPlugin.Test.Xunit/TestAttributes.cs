using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    public class NotWindowsServerFactAttribute : FactAttribute
    {
        public NotWindowsServerFactAttribute()
        {
            if (WindowsServerDetector.IsWindowsServer())
            {
                Skip = "Skipping test on Windows Server (CI/CD environment).";
            }
        }
    }
}
