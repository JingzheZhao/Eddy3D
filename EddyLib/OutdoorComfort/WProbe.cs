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
    public class WProbe
    {
        public WProbe()
        {
        }

        public WProbe(Point3d pt, int NumberOfWindDirections)
        {
            Point = new EddyPoint(pt);

            WindDirections = new int[NumberOfWindDirections];
            U = new EddyVector[NumberOfWindDirections];
        }

        [ProtoMember(1)]
        public EddyPoint Point { get; set; }

        // OpenFOAM Data

        // BCs
        [ProtoMember(100)]
        public int[] WindDirections { get; set; }

        [ProtoMember(101)]
        public float Z0 { get; set; }

        [ProtoMember(102)]
        public float Zref { get; set; }

        [ProtoMember(103)]
        public float Uref { get; set; }

        // FieldData

        [ProtoMember(110)]
        public EddyVector[] U { get; set; }

        [ProtoMember(111)]
        public float[] Cp_coeff { get; set; }

        [ProtoMember(112)]
        public float[] P { get; set; }

        [ProtoMember(113)]
        public float[] Epsilon { get; set; }

        [ProtoMember(114)]
        public float[] Omega { get; set; }

        [ProtoMember(115)]
        public float[] K { get; set; }

        [ProtoMember(116)]
        public float[] Nut { get; set; }

        [ProtoMember(117)]
        public EddyVector[] Phi { get; set; }

        [ProtoMember(118)]
        public float[] Aoa { get; set; }

        // Comfort Data
        [ProtoMember(200)]
        public float[] WindFactorsSpatial { get; set; }

        [ProtoMember(201)]
        public float[] WindFactorsTemporal { get; set; }

        [ProtoMember(210)]
        public float[] LawsonGeneral { get; set; }

        [ProtoMember(211)]
        public float[] LawsonLDDC { get; set; }

        [ProtoMember(212)]
        public float[] Lawson2001 { get; set; }

        [ProtoMember(213)]
        public float[] Davenport { get; set; }

        [ProtoMember(14)]
        public float[] NEN8100Comfort { get; set; }

        [ProtoMember(215)]
        public float[] NEN8100Safety { get; set; }
    }
}