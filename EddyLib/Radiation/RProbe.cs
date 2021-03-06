using EddyLib.Geometry;
using ProtoBuf;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    [ProtoContract]
    [DataContract]

    public class RProbe
    {
        [DataMember]
        [ProtoMember(0)]
        public EddyPoint Point { get; set; }
        [DataMember]
        [ProtoMember(1)]
        public EddyVector Normal { get; set; }



        //View factor data

        [DataMember]
        [ProtoMember(100)]
        public double[] VFtoPolys { get; set; }
        [DataMember]
        [ProtoMember(101)]
        public Dictionary<string, double> VFtoMaterial { get; set; }


        //Radiation Data

        [DataMember]
        [ProtoMember(110)]
        public float[] TotalRad { get; set; }
        [DataMember]
        [ProtoMember(111)]
        public float[] DirRad { get; set; }
        [DataMember]
        [ProtoMember(112)]
        public float[] SolarGain_dMRT { get; set; }


    }
}
