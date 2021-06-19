using Rhino.Geometry;

namespace EddyLib.Indoor
{
    public class FunctionObject
    {
        public string Name { get; set; }

        public string ID { get; set; }

        // public string FullDict { get; set; }

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

        public FunctionObject()
        {
        }

        public FunctionObject Duplicate()
        {
            return new FunctionObject();
        }
    }
}