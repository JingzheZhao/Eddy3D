using EddyLib.UI;
using Medallion.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace EddyLib.Radiation
{
    [DataContract]
    public class RadiationSimulationDDSResult
    { 
        public RadiationSimulationDDSResult(List<Mesh> meshes, float[][] totalRad, float[][] DirRad, float[][] dMRT)
        {
            this.AnalysisMeshes = meshes;
            this.TotalRad = totalRad;
            //this.DiffRad = DiffRad;
            this.DirRad = DirRad;
            this.SolarGain_dMRT = dMRT;
        }



        [DataMember]
        public List<Mesh> AnalysisMeshes { get; set; }
        [DataMember]
        public float[][] TotalRad { get; set; }
        //[DataMember]
        //public float[][] DiffRad { get; set; }
        [DataMember]
        public float[][] DirRad { get; set; }
        [DataMember]
        public float[][] SolarGain_dMRT { get; set; }

        public string ToBson()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BsonDataWriter datawriter = new BsonDataWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(datawriter, this);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
        public static RadiationSimulationDDSResult FromBson(string base64data)
        {
            byte[] data = Convert.FromBase64String(base64data);

            using (MemoryStream ms = new MemoryStream(data))
            using (BsonDataReader reader = new BsonDataReader(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<RadiationSimulationDDSResult>(reader);
            }
        }

        //public static string ToBson<T>(T value)
        //{
        //    using (MemoryStream ms = new MemoryStream())
        //    using (BsonDataWriter datawriter = new BsonDataWriter(ms))
        //    {
        //        JsonSerializer serializer = new JsonSerializer();
        //        serializer.Serialize(datawriter, value);
        //        return Convert.ToBase64String(ms.ToArray());
        //    }

        //}
        //public static T FromBson<T>(string base64data)
        //{
        //    byte[] data = Convert.FromBase64String(base64data);

        //    using (MemoryStream ms = new MemoryStream(data))
        //    using (BsonDataReader reader = new BsonDataReader(ms))
        //    {
        //        JsonSerializer serializer = new JsonSerializer();
        //        return serializer.Deserialize<T>(reader);
        //    }
        //}
    }
}
