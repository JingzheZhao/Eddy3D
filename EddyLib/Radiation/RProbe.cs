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

    public enum RProbeMetric {

        UTCI,
        MRT,
        dMRT,
        lwMRT,
        TotalRad,
        DirRad,
        WindSpeed


    }


    [ProtoContract]
    public class RProbe
    {

        public RProbe() { }
        public RProbe(Point3d pt , Vector3d vec) {
            Point = new EddyPoint(pt);
            Normal = new EddyVector(vec);
        }


        [ProtoMember(1)]
        public EddyPoint Point { get; set; }
        [ProtoMember(2)]
        public EddyVector Normal { get; set; }



        //View factor data
        [ProtoMember(100)]
        public double[] VFtoPolys { get; set; }
        [ProtoMember(101)]
        public Dictionary<string, double> VFtoMaterial { get; set; }


        //Radiation Data

        [ProtoMember(110)]
        public float[] TotalRad { get; set; }
        [ProtoMember(111)]
        public float[] DirRad { get; set; }
        [ProtoMember(112)]
        public float[] SolarGain_dMRT { get; set; }
        [ProtoMember(113)]
        public float[] LongWave_MRT { get; set; }


        [ProtoMember(130)]
        public float[] WindSpeed { get; set; }



        [ProtoMember(200)]
        public float[] UTCI { get; set; }

        [ProtoMember(201)]
        public int ComfortHours { get; set; }

        [ProtoMember(202)]
        public float ComfortAutonomy_Spring { get; set; }
        [ProtoMember(203)]
        public float ComfortAutonomy_Summer { get; set; }
        [ProtoMember(204)]
        public float ComfortAutonomy_Fall { get; set; }
        [ProtoMember(205)]
        public float ComfortAutonomy_Winter { get; set; }
      


    }
}
