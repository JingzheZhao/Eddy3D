using Rhino.Geometry;

namespace EddyLib.Indoor
{
    internal class OFIndoorEmitters
    {
        private Mesh Geometry { get; set; }

        private double HeatOutput { get; set; } = 0;

        private double ParticleOutput { get; set; } = 0;
    }
}