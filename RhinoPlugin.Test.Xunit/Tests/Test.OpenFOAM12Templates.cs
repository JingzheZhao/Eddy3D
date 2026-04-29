using EddyLib;
using EddyLib.Strings;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "OpenFOAM")]
    public class Test_OpenFOAM12Templates
    {
        [Fact]
        public void PhysicalProperties_UsesOpenFoam12DictionaryName()
        {
            var text = OFExecDicts.PhysicalProperties();

            Assert.Contains("object      physicalProperties;", text);
            Assert.Contains("viscosityModel  constant;", text);
            Assert.DoesNotContain("object      transportProperties;", text);
            Assert.DoesNotContain("transportModel", text);
        }

        [Fact]
        public void MomentumTransport_UsesOpenFoam12DictionaryName()
        {
            var text = OFExecDicts.MomentumTransport(new OFRunSettings());

            Assert.Contains("object      momentumTransport;", text);
            Assert.Contains("model           RNGkEpsilon;", text);
            Assert.DoesNotContain("object      RASProperties;", text);
            Assert.DoesNotContain("RASModel", text);
        }
    }
}
