using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace EddyLib.Radiation
{
    [ProtoContract]
    public class MRT_Simulation_ResultProto    {

        public MRT_Simulation_ResultProto() { }

        public MRT_Simulation_ResultProto(string name , string dir , Weather weather ,List<RProbe> probes, List <RPolygon> polys)
        {
            this.ProjectName = name;
            this.BaseWorkingDir = dir;
            this.Weather = weather;
            this.Probes = probes;
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
        public List<RPolygon> Polys { get; set; } 


        public string buffMe()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, this);
                return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }
        public static MRT_Simulation_ResultProto unBuffMe(string txt)
        {
            byte[] arr = Convert.FromBase64String(txt);
            using (MemoryStream ms = new MemoryStream(arr))
                return ProtoBuf.Serializer.Deserialize<MRT_Simulation_ResultProto>(ms);
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

        public static MRT_Simulation_ResultProto ReadFromFile(string path)
        {

            MRT_Simulation_ResultProto result = null;
            using (var br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                // read body
                int size = br.ReadInt32();
                var bytes = br.ReadBytes(size);
                using (var ms = new MemoryStream(bytes))
                {
                    result = ProtoBuf.Serializer.Deserialize<MRT_Simulation_ResultProto>(ms);
                }
            }
            return result;
        }
    }
}
