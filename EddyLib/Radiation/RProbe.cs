using EddyLib.Geometry;
using ProtoBuf;
using Rhino.Geometry;
using System.Collections.Generic;

namespace EddyLib.Radiation
{
    /// <summary>
    /// Metrics available for radiation probes.
    /// </summary>
    public enum RProbeMetric
    {
        UTCI,
        MRT,
        dMRT,
        lwMRT,
        TotalRad,
        DirRad,
        WindVelMag
    }

    /// <summary>
    /// Radiation probe for MRT and thermal comfort calculations.
    /// </summary>
    [ProtoContract]
    public class RProbe
    {
        #region Constructors

        public RProbe() { }

        public RProbe(Point3d pt, Vector3d vec)
        {
            Point = new EddyPoint(pt);
            Normal = new EddyVector(vec);
        }

        public RProbe(Point3d pt, Vector3d vec, Mesh geo)
        {
            Point = new EddyPoint(pt);
            Normal = new EddyVector(vec);
            PreviewGeo = new EddyMesh(geo);
        }

        #endregion

        #region Core Properties

        [ProtoMember(1)]
        public EddyPoint Point { get; set; }

        [ProtoMember(2)]
        public EddyVector Normal { get; set; }

        [ProtoMember(3)]
        public EddyMesh PreviewGeo { get; set; }

        [ProtoMember(4)]
        public float Area { get; set; } = 1;

        #endregion

        #region View Factor Data

        [ProtoMember(100)]
        public double[] VFtoPolys { get; set; }

        [ProtoMember(101)]
        public Dictionary<string, double> VFtoMaterial { get; set; }

        #endregion

        #region Radiation Data

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

        #endregion

        #region Comfort Results

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

        #endregion

        #region Static Methods

        /// <summary>
        /// Converts mesh faces to radiation probes at face centers.
        /// </summary>
        public static List<RProbe> Mesh2Probes(Mesh mesh)
        {
            var probes = new List<RProbe>();
            if (mesh == null) return probes;

            mesh.FaceNormals.ComputeFaceNormals();

            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                var face = mesh.Faces[i];
                var probe = new RProbe
                {
                    Point = new EddyPoint(mesh.Faces.GetFaceCenter(i)),
                    Normal = new EddyVector(mesh.FaceNormals[i])
                };
                probe.Normal.Value.Unitize();

                // Get face vertices
                Point3d v0 = mesh.Vertices[face.A];
                Point3d v1 = mesh.Vertices[face.B];
                Point3d v2 = mesh.Vertices[face.C];

                // Calculate area using cross product
                Vector3d cross1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);
                double area = cross1.Length * 0.5;

                // Create preview mesh
                var previewMesh = new Mesh();
                previewMesh.Vertices.Add(v0);
                previewMesh.Vertices.Add(v1);
                previewMesh.Vertices.Add(v2);

                if (face.IsQuad)
                {
                    Point3d v3 = mesh.Vertices[face.D];
                    Vector3d cross2 = Vector3d.CrossProduct(v2 - v0, v3 - v0);
                    area += cross2.Length * 0.5;

                    previewMesh.Vertices.Add(v3);
                    previewMesh.Faces.AddFace(0, 1, 2, 3);
                }
                else
                {
                    previewMesh.Faces.AddFace(0, 1, 2);
                }

                probe.Area = (float)area;
                probe.PreviewGeo = new EddyMesh(previewMesh);
                probes.Add(probe);
            }

            return probes;
        }

        #endregion
    }
}