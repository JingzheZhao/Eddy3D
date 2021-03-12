using EddyLib.Indoor.Dicts;
using Rhino.Geometry;

namespace EddyLib.Indoor
{
   

    public class CO2Source : FunctionObject
    {
        public double CO2 { get; set; } = 0;

        public CO2Source(Mesh Geometry, int volumeType, double InjectionRate, string Name)
        {
            this.volumeType = (VolumeType)volumeType;
            this.Geometry = Geometry;
            this.cellZone = cellZone;
            this.CO2 = InjectionRate;
            this.Name = Name;
        }

        public CO2Source()
        {
        }
    }
}