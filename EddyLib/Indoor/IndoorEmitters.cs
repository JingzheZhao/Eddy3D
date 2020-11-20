using EddyLib.Indoor.Dicts;
using Rhino.Geometry;

namespace EddyLib.Indoor
{
    public class OFIndoorFunctionObject : FunctionObject
    {
        //public enum EmitterType
        //{
        //    CO2,
        //    VolumetricHeatSource
        //}

        //public EmitterType Type { get; set; }

        //  public string Id { get; set; }

        public enum VolumeType
        {
            specific,
            absolute
        }

        public VolumeType volumeType { get; set; }

        public string cellZone { get; set; }

        public Mesh Geometry { get; set; }

        //public string Name { get; set; }
    }

    public class VolumetricHeatSource : OFIndoorFunctionObject
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

        public VolumetricHeatSource Duplicate()
        {
            VolumetricHeatSource dup = new VolumetricHeatSource(Geometry, (int)volumeType, Power, Name);
            return dup;
        }
    }

    public class CO2Source : OFIndoorFunctionObject
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

        public CO2Source Duplicate()
        {
            CO2Source dup = new CO2Source(Geometry, (int)volumeType, CO2);
            return dup;
        }
    }
}