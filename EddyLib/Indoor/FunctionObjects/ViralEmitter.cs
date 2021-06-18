using Rhino.Geometry;

namespace EddyLib.Indoor
{
    public class ViralEmitter : FunctionObject
    {
        public double Virus { get; set; } = 0;

        public ViralEmitter(Mesh Geometry, int volumeType, double InjectionRate, string Name)
        {
            this.volumeType = (VolumeType)volumeType;
            this.Geometry = Geometry;
            this.cellZone = cellZone;
            this.Virus = InjectionRate;
            this.Name = Name;
        }

        public ViralEmitter()
        {
        }
    }
}