using EddyLib.Geometry;
using EddyLib.UI;
using Medallion.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ProtoBuf;
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
    [ProtoContract]
    public class MRTSimulationResultProto    {

        public MRTSimulationResultProto() { }

        public MRTSimulationResultProto(string name , string dir , Weather weather ,List<RProbe> probes, List<Mesh> meshes, List <RPolygon> polys)
        {
            this.ProjectName = name;
            this.BaseWorkingDir = dir;
            this.Weather = weather;
            this.Probes = probes;
            this.Meshes = new List<EddyMesh>();
            foreach (var  m in meshes)
            {
                Meshes.Add(new EddyMesh(m));
            }
            this.Polys = polys;
        }


        [ProtoMember(1)]
        public string ProjectName = "";
        [ProtoMember(2)]
        public string BaseWorkingDir = "";
        [ProtoMember(3)]
        public Weather Weather;

        [ProtoMember(4)]
        public List<RProbe> Probes { get; set; }

        [ProtoMember(5)]
        public List<EddyMesh> Meshes { get; set; }

        [ProtoMember(6)]
        public List<RPolygon> Polys { get; set; } 



        public string buffMe()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, this);
                return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }
        public static MRTSimulationResultProto unBuffMe(string txt)
        {
            byte[] arr = Convert.FromBase64String(txt);
            using (MemoryStream ms = new MemoryStream(arr))
                return ProtoBuf.Serializer.Deserialize<MRTSimulationResultProto>(ms);
        }

        public bool WriteToFile(string path)
        {

            using (var bw = new BinaryWriter(File.Create(path)))
            {
                // write body
                using (var ms = new MemoryStream())
                {
                    ProtoBuf.Serializer.Serialize(ms, this);
                    var bytes = ms.ToArray();
                    if (bytes != null)
                    {
                        bw.Write(bytes.Length); // byte length
                        bw.Write(bytes); // content
                    }
                    else bw.Write(0);
                }
            }
            return true;
        }

        public static MRTSimulationResultProto ReadFromFile(string path)
        {

            MRTSimulationResultProto result = null;
            using (var br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                // read body
                int size = br.ReadInt32();
                var bytes = br.ReadBytes(size);
                using (var ms = new MemoryStream(bytes))
                {
                    result = ProtoBuf.Serializer.Deserialize<MRTSimulationResultProto>(ms);
                }
            }
            return result;
        }
    }
}
