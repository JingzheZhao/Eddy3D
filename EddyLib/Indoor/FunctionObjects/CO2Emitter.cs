using Rhino.Geometry;

namespace EddyLib.Indoor
{
    public class CO2Emitter : FunctionObject
    {
        public double CO2 { get; set; } = 0;

        public CO2Emitter(Mesh Geometry, int volumeType, double InjectionRate, string Name)
        {
            this.volumeType = (VolumeType)volumeType;
            this.Geometry = Geometry;
            this.cellZone = cellZone;
            this.CO2 = InjectionRate;
            this.Name = Name;
        }

        public CO2Emitter()
        {
        }
    }
}