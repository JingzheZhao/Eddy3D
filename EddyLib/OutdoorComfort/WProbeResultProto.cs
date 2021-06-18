using EddyLib.Radiation;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace EddyLib
{
    [ProtoContract]
    public class WProbeResultProto
    {
        public WProbeResultProto()
        {
        }

        public WProbeResultProto(string name, string dir, List<WProbe> probes)
        {
            this.ProjectName = name;
            this.BaseWorkingDir = dir;

            this.Probes = probes;
        }

        [ProtoMember(1)]
        public string ProjectName = "";

        [ProtoMember(2)]
        public string BaseWorkingDir = "";

        [ProtoMember(3)]
        public List<WProbe> Probes { get; set; }

        public string buffMe()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, this);
                return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }

        public static WProbeResultProto unBuffMe(string txt)
        {
            byte[] arr = Convert.FromBase64String(txt);
            using (MemoryStream ms = new MemoryStream(arr))
                return ProtoBuf.Serializer.Deserialize<WProbeResultProto>(ms);
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

        public static WProbeResultProto ReadFromFile(string path)
        {
            WProbeResultProto result = null;
            using (var br = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                // read body
                int size = br.ReadInt32();
                var bytes = br.ReadBytes(size);
                using (var ms = new MemoryStream(bytes))
                {
                    result = ProtoBuf.Serializer.Deserialize<WProbeResultProto>(ms);
                }
            }
            return result;
        }
    }
}