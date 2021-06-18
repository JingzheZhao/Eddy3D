using Newtonsoft.Json;
using ProtoBuf;
using Rhino.Geometry;

namespace EddyLib.Geometry
{
    [ProtoContract]
    public class EddyVector
    {
        [ProtoMember(1)]
        private string json;

        [ProtoMember(2)]
        private double[] coords;

        public Vector3d Value;

        public EddyVector()
        {
        }

        public EddyVector(Vector3d vec)
        {
            Value = vec;
        }

        [ProtoBeforeSerialization]
        public void BeforeWrite()
        {
            if (Value != null)
            {
                coords = new double[] { Value.X, Value.Y, Value.Z };
                json = null;
            }
        }

        [ProtoAfterDeserialization]
        public void AfterRead()
        {
            if (coords != null) Value = new Vector3d(coords[0], coords[1], coords[2]);
            else if (json != null) Value = JsonConvert.DeserializeObject<Vector3d>(json);
        }
    }
}