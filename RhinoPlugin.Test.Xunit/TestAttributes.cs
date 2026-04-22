using System.Runtime.InteropServices;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    public class WindowsOnlyFactAttribute : FactAttribute
    {
        public WindowsOnlyFactAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "Windows-only test.";
            }
        }
    }

    public class WindowsOnlyTheoryAttribute : TheoryAttribute
    {
        public WindowsOnlyTheoryAttribute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Skip = "Windows-only test.";
            }
        }
    }

    /// <summary>
    /// Backward-compatible attribute name for tests that require Rhino native host support.
    /// </summary>
    public class NotWindowsServerFactAttribute : FactAttribute
    {
        public NotWindowsServerFactAttribute()
        {
            Skip = TestExecutionPolicy.GetSkipReason(TestExecutionRequirement.RhinoNativeHost);
        }
    }

    /// <summary>
    /// Backward-compatible attribute name for tests that require Rhino native host support.
    /// </summary>
    public class NotWindowsServerTheoryAttribute : TheoryAttribute
    {
        public NotWindowsServerTheoryAttribute()
        {
            Skip = TestExecutionPolicy.GetSkipReason(TestExecutionRequirement.RhinoNativeHost);
        }
    }

    public class RequiresGrasshopperFactAttribute : FactAttribute
    {
        public RequiresGrasshopperFactAttribute()
        {
            Skip = TestExecutionPolicy.GetSkipReason(TestExecutionRequirement.GrasshopperWithRhinoHost);
        }
    }

    public class RequiresGrasshopperTheoryAttribute : TheoryAttribute
    {
        public RequiresGrasshopperTheoryAttribute()
        {
            Skip = TestExecutionPolicy.GetSkipReason(TestExecutionRequirement.GrasshopperWithRhinoHost);
        }
    }
}
