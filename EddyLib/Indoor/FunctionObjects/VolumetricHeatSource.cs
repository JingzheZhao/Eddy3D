using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.FunctionObjects
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
}
