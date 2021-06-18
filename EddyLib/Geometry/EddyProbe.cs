using Rhino.Geometry;
using System.Collections.Generic;

namespace EddyLib.Radiation
{
    public class EddyProbe
    {
        public EddyProbe()
        {
        }

        public EddyProbe(Point3d pt, Vector3d vec)
        {
            Point = pt;
            Normal = vec;
        }

        public EddyProbe(Point3d pt, Vector3d vec, Mesh geo)
        {
            Point = pt;
            Normal = vec;
            PreviewGeo = geo;
        }

        public Point3d Point { get; set; }

        public Vector3d Normal { get; set; }

        public double Area { get; set; }
        public Mesh PreviewGeo { get; set; }

        public static List<EddyProbe> Mesh2Probes(Mesh _ms)
        {
            List<EddyProbe> probes = new List<EddyProbe>();
            if (_ms == null) return probes;

            _ms.FaceNormals.ComputeFaceNormals();

            for (int i = 0; i < _ms.Faces.Count; ++i)
            {
                EddyProbe pg = new EddyProbe();
                pg.Point = (_ms.Faces.GetFaceCenter(i));
                pg.Normal = (_ms.FaceNormals[i]);
                pg.Normal.Unitize();

                probes.Add(pg);

                if (_ms.Faces[i].IsQuad)
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);
                    Point3d v3 = new Point3d(_ms.Vertices[_ms.Faces[i].D]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);
                    Vector3d n2 = Vector3d.CrossProduct(v2 - v0, v3 - v0);

                    pg.Area = (n1.Length * 0.5 + n2.Length * 0.5);

                    pg.PreviewGeo = (new Mesh());
                    pg.PreviewGeo.Vertices.Add(v0);
                    pg.PreviewGeo.Vertices.Add(v1);
                    pg.PreviewGeo.Vertices.Add(v2);
                    pg.PreviewGeo.Vertices.Add(v3);
                    pg.PreviewGeo.Faces.AddFace(0, 1, 2, 3);
                }
                else
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);

                    pg.Area = (n1.Length * 0.5);

                    pg.PreviewGeo = (new Mesh());
                    pg.PreviewGeo.Vertices.Add(v0);
                    pg.PreviewGeo.Vertices.Add(v1);
                    pg.PreviewGeo.Vertices.Add(v2);
                    pg.PreviewGeo.Faces.AddFace(0, 1, 2);
                }
            }
            return probes;
        }
    }
}