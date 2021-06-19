using Newtonsoft.Json;
using ProtoBuf;
using Rhino.Geometry;

namespace EddyLib.Geometry
{
    [ProtoContract]
    public class EddyMesh
    {
        [ProtoMember(1)]
        private string json;

        public Mesh Value;

        public EddyMesh()
        {
        }

        public EddyMesh(Mesh m)
        {
            Value = m;
        }

        [ProtoBeforeSerialization]
        public void BeforeWrite()
        {
            if (Value != null) json = JsonConvert.SerializeObject(Value);
        }

        [ProtoAfterDeserialization]
        public void AfterRead()
        {
            if (json != null) Value = JsonConvert.DeserializeObject<Mesh>(json);
        }
    }
}