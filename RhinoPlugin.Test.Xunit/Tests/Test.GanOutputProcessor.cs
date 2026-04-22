using EddyLib.GAN;
using Rhino.Geometry;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Trait("Category", "GAN")]
    public class Test_GanOutputProcessor
    {
        [Fact]
        public void CreatePixelQuad_PlacesPreviewAtTwoMeters()
        {
            Point3d[] quad = GanOutputProcessor.CreatePixelQuad(
                x0: 10.0,
                y0: 20.0,
                z: GanOutputProcessor.PreviewHeightMeters,
                pixelSize: 0.5,
                col: 0,
                row: 0);

            Assert.Equal(4, quad.Length);
            Assert.All(quad, vertex =>
                Assert.Equal(GanOutputProcessor.PreviewHeightMeters, vertex.Z, 6));
        }
    }
}
