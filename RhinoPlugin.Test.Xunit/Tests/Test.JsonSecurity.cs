using EddyLib.Helpers;
using Xunit;
using Newtonsoft.Json;
using System;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "Security")]
    public class JsonSecurityTests
    {
        public class MaliciousPayload
        {
            public static bool WasInstantiated = false;
            public MaliciousPayload()
            {
                WasInstantiated = true;
            }
        }

        [Fact]
        public void JsonHelper_Deserialize_ShouldNotInstantiateArbitraryTypes()
        {
            // Reset the flag
            MaliciousPayload.WasInstantiated = false;

            // This payload attempts to use $type to instantiate MaliciousPayload
            string typeName = typeof(MaliciousPayload).AssemblyQualifiedName;
            string json = "{ \"$type\": \"" + typeName + "\" }";

            try
            {
                // We attempt to deserialize into a completely different type
                JsonHelper.Deserialize<object>(json);
            }
            catch
            {
                // Ignore errors during deserialization, we only care if the type was instantiated
            }

            // Assert that the malicious type was NOT instantiated
            Assert.False(MaliciousPayload.WasInstantiated, "Security Vulnerability: Arbitrary type instantiated via JSON $type property!");
        }

        [Fact]
        public void Tree_Settings_Serialization_ShouldStillWork()
        {
            var settings = EddyLib.Radiation.Tree_Settings.GenerateTree();
            settings.Name = "TestTree";

            string json = settings.toJSON();
            var deserialized = EddyLib.Radiation.Tree_Settings.fromJSON(json);

            Assert.NotNull(deserialized);
            Assert.Equal("TestTree", deserialized.Name);
            Assert.Equal(settings.RadianceMaterial, deserialized.RadianceMaterial);
        }

        [Fact]
        public void MRT_Simulation_Settings_Serialization_ShouldStillWork()
        {
            var settings = new EddyLib.Radiation.MRT_Simulation_Settings
            {
                ComputeReflectionsAndDiffuseRadiation = false,
                CumulativeViewFactorCutoffPercentile = 50
            };

            string json = settings.toJSON();
            var deserialized = EddyLib.Radiation.MRT_Simulation_Settings.fromJSON(json);

            Assert.NotNull(deserialized);
            Assert.False(deserialized.ComputeReflectionsAndDiffuseRadiation);
            Assert.Equal(50, deserialized.CumulativeViewFactorCutoffPercentile);
        }
    }
}
