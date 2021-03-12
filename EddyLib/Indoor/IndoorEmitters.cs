using EddyLib.Indoor.Dicts;
using Rhino.Geometry;

namespace EddyLib.Indoor
{
    public class VolumetricHeatSource : FunctionObject
    {
        public double Power { get; set; } = 0;

        public VolumetricHeatSource(Mesh Geometry, int volumeType, double Power, string Name)
        {
            this.volumeType = (VolumeType)volumeType;
            this.Name = "volumetricHeatSource";
            this.Geometry = Geometry;

            if (Name == "")
            {
                this.cellZone = "volumetricHeatSource";
            }

            this.Power = Power;
        }

        public VolumetricHeatSource()
        {
        }
    }

    public class CO2Source : FunctionObject
    {
        public double CO2 { get; set; } = 0;

        public CO2Source(Mesh Geometry, int volumeType, double CO2)
        {
            this.volumeType = (VolumeType)volumeType;
            this.Geometry = Geometry;
            this.cellZone = cellZone;
            this.CO2 = CO2;
            this.Name = "CO2Source";
        }

        public CO2Source()
        {
        }
    }
}