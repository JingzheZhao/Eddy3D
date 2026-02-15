using EddyLib;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    [Trait("Category", "Unit")]
    public class SimpleConsistentTests
    {
        [RhinoRequiredFact]
        public void RunSettings_DefaultSimpleConsistent_IsFalse()
        {
            var runSettings = new OFRunSettings();
            Assert.False(runSettings.simpleConsistent);
        }

    }
}
