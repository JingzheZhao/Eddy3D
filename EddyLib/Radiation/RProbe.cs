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

    public enum RProbeMetric
    {

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

        [ProtoMember(1)]
        public EddyPoint Point { get; set; }
        [ProtoMember(2)]
        public EddyVector Normal { get; set; }
        [ProtoMember(3)]
        public EddyMesh PreviewGeo { get; set; }
        [ProtoMember(4)]
        public float Area { get; set; } = 1;

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


        public static List<RProbe> Mesh2Probes(Mesh _ms)
        {
            List<RProbe> probes = new List<RProbe>();
            if (_ms == null) return probes;

            _ms.FaceNormals.ComputeFaceNormals();



            for (int i = 0; i < _ms.Faces.Count; ++i)
            {
                RProbe pg = new RProbe();
                pg.Point = new EddyPoint( _ms.Faces.GetFaceCenter(i) );
                pg.Normal = new EddyVector( _ms.FaceNormals[i] );
                pg.Normal.Value.Unitize();

                probes.Add(pg);


                if (_ms.Faces[i].IsQuad)
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);
                    Point3d v3 = new Point3d(_ms.Vertices[_ms.Faces[i].D]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);
                    Vector3d n2 = Vector3d.CrossProduct(v2 - v0, v3 - v0);

                    pg.Area = (float)(n1.Length * 0.5 + n2.Length * 0.5);

                    pg.PreviewGeo = new EddyMesh( new Mesh() );
                    pg.PreviewGeo.Value.Vertices.Add(v0);
                    pg.PreviewGeo.Value.Vertices.Add(v1);
                    pg.PreviewGeo.Value.Vertices.Add(v2);
                    pg.PreviewGeo.Value.Vertices.Add(v3);
                    pg.PreviewGeo.Value.Faces.AddFace(0, 1, 2, 3);
                }
                else
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);

                    pg.Area = (float)(n1.Length * 0.5);

                    pg.PreviewGeo = new EddyMesh(new Mesh());
                    pg.PreviewGeo.Value.Vertices.Add(v0);
                    pg.PreviewGeo.Value.Vertices.Add(v1);
                    pg.PreviewGeo.Value.Vertices.Add(v2);
                    pg.PreviewGeo.Value.Faces.AddFace(0, 1, 2);
                }
            }
            return probes;
        }

    }
}
