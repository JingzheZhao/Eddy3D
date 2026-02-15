using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Tests requiring Rhino installation, with environment behavior controlled by TestExecutionPolicy.
    /// </summary>
    public class RhinoRequiredFactAttribute : FactAttribute
    {
        public static bool IsRhinoInstalled => TestExecutionPolicy.IsRhinoInstalled;

        public RhinoRequiredFactAttribute()
        {
            Skip = TestExecutionPolicy.GetSkipReason(TestExecutionRequirement.RhinoInstalled);
        }
    }

    public class RhinoRequiredTheoryAttribute : TheoryAttribute
    {
        public RhinoRequiredTheoryAttribute()
        {
            Skip = TestExecutionPolicy.GetSkipReason(TestExecutionRequirement.RhinoInstalled);
        }
    }
}
