using EddyLib.Geometry;
using ProtoBuf;
using Rhino.Geometry;

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
            UMag = new float[NumberOfWindDirections];

            Cp_coeff = new float[NumberOfWindDirections];

            P = new float[NumberOfWindDirections];

            Epsilon = new float[NumberOfWindDirections];

            Omega = new float[NumberOfWindDirections];

            K = new float[NumberOfWindDirections];

            Nut = new float[NumberOfWindDirections];

            Phi = new float[NumberOfWindDirections];

            Aoa = new float[NumberOfWindDirections];
        }

        [ProtoMember(1)]
        public EddyPoint Point { get; set; }

        //[ProtoMember(3)]
        //public EddyMesh PreviewGeo { get; set; }

        //[ProtoMember(4)]
        //public float Area { get; set; } = 1;

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
        public float[] Phi { get; set; }

        [ProtoMember(118)]
        public float[] Aoa { get; set; }

        [ProtoMember(119)]
        public float[] UMag { get; set; }

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

        public void SetOFFields(field Field, Vector3d Value, int WindDirection)
        {
            switch (Field)
            {
                case field.U:

                    this.U[WindDirection] = new EddyVector(Value);
                    this.UMag[WindDirection] = (float)Value.Length;
                    break;
            }
        }

        public void SetOFFields(field Field, float Value, int WindDirection)
        {
            switch (Field)
            {
                case field.p:
                    this.P[WindDirection] = Value;
                    break;

                case field.cp_coeff:
                    this.Cp_coeff[WindDirection] = Value;
                    break;

                case field.epsilon:
                    this.Epsilon[WindDirection] = Value;
                    break;

                case field.omega:
                    this.Omega[WindDirection] = Value;
                    break;

                case field.k:
                    this.K[WindDirection] = Value;
                    break;

                case field.nut:
                    this.Nut[WindDirection] = Value;
                    break;

                case field.phi:
                    this.Phi[WindDirection] = Value;
                    break;

                case field.AoA:
                    this.Aoa[WindDirection] = Value;
                    break;
            }
        }
    }
}