using Newtonsoft.Json;
using ProtoBuf;
using Rhino.Geometry;

namespace EddyLib.Geometry
{
    [ProtoContract]
    public class EddyBrep
    {
        [ProtoMember(1)]
        private string json;

        public Brep Value;

        public EddyBrep()
        {
        }

        public EddyBrep(Brep b)
        {
            Value = b;
        }

        [ProtoBeforeSerialization]
        public void BeforeWrite()
        {
            if (Value != null) json = JsonConvert.SerializeObject(Value);
        }

        [ProtoAfterDeserialization]
        public void AfterRead()
        {
            if (json != null) Value = JsonConvert.DeserializeObject<Brep>(json);
        }
    }
}