using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;
using ProtoBuf;
using Newtonsoft.Json;
using System.IO;
using System.Runtime.Serialization;

namespace EddyLib.Geometry
{ 
    [ProtoContract]

    public class EddyPoint
    {
        [ProtoMember(1)]
        private string json; // obsolete
        [ProtoMember(2)]
        private double[] coords;

        public Point3d Value;

        public EddyPoint()
        {
        }

        public EddyPoint(Point3d pt)
        {
            Value = pt;
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
            if (coords != null) Value = new Point3d(coords[0], coords[1], coords[2]);
            else if (json != null) Value = JsonConvert.DeserializeObject<Point3d>(json);
        }
    }
}
