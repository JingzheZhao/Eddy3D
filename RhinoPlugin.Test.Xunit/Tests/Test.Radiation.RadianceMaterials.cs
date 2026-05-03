using Xunit;
using EddyLib.Radiation;

namespace RhinoPlugin.Test.Xunit.Tests.Radiation
{
    public class TestRadianceMaterials
    {
        [Fact]
        public void GetID_ValidInput_ReturnsTrueAndExtractsID()
        {
            string description = "void plastic my_material\n0\n0\n5 0.5 0.5 0.5 0 0";
            bool result = RadianceMaterials.GetID(description, out string id);
            Assert.True(result);
            Assert.Equal("my_material", id);
        }

        [Fact]
        public void GetID_WithComments_IgnoresComments()
        {
            string description = "# This is a comment\nvoid glass my_glass # another comment\n0\n0\n3 0.9 0.9 0.9";
            bool result = RadianceMaterials.GetID(description, out string id);
            Assert.True(result);
            Assert.Equal("my_glass", id);
        }

        [Fact]
        public void GetID_MultipleMaterials_ReturnsLastID()
        {
            string description = "void plastic first_mat\n0\n0\n5 0.5 0.5 0.5 0 0\nvoid metal second_mat\n0\n0\n5 0.8 0.8 0.8 0 0";
            bool result = RadianceMaterials.GetID(description, out string id);
            Assert.True(result);
            Assert.Equal("second_mat", id);
        }

        [Fact]
        public void GetID_InvalidMaterialType_ReturnsFalse()
        {
            string description = "void unknown_type my_material\n0\n0\n5 0.5 0.5 0.5 0 0";
            bool result = RadianceMaterials.GetID(description, out string id);
            Assert.False(result);
            Assert.Equal(string.Empty, id);
        }

        [Fact]
        public void GetID_EmptyString_ReturnsFalse()
        {
            string description = "";
            bool result = RadianceMaterials.GetID(description, out string id);
            Assert.False(result);
            Assert.Equal(string.Empty, id);
        }

        [Fact]
        public void GetID_WhitespaceString_ReturnsFalse()
        {
            string description = "   \n\t  ";
            bool result = RadianceMaterials.GetID(description, out string id);
            Assert.False(result);
            Assert.Equal(string.Empty, id);
        }
    }
}
