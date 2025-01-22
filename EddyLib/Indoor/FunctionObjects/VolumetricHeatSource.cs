using Rhino.Geometry;

namespace EddyLib.Indoor.FunctionObjects
{
    public class VolumetricHeatSource : FunctionObject
    {
        public double Power { get; set; } = 0;

        public VolumetricHeatSource(Mesh Geometry, int volumeType, double Power, string Name)
        {
            this.volumeType = (VolumeType)volumeType;
            this.Name = Name;
            this.Geometry = Geometry;

            //if (String.IsNullOrWhiteSpace(Name))
            //{
            //    this.cellZone = "volumetricHeatSource";
            //}

            this.Power = Power;
        }

        public VolumetricHeatSource()
        {
        }
    }
}