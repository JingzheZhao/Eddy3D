using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    public class RSurface
    {

        public string Name;
        public Brep Surface;
        public Mesh LowPoly;
        public Mesh HighPoly;

        public List<RPolygon> Polys;

        public RadiationSurfaceType Type;
        public double PatchSize;

        public string MaterialID;
        public string Material;
        public RSurface() { }
        public RSurface(string name, Brep b, RadiationSurfaceType type, string material, double patchSize = 3)
        {
            Name = name;
            Surface = b;
            PatchSize = patchSize > 0 ? patchSize : 2;
            Material = material;


            MaterialID = "";
            RadianceMaterial.GetID(Material, out MaterialID);

            //Material = refl > 1 ? 1 : refl;
            Type = type;


            // simple mesh for rad sim and obstruction calculation
            MeshingParameters mp_low = new MeshingParameters();
            LowPoly = new Mesh();
            foreach (var m in Mesh.CreateFromBrep(b, mp_low))
            {
                LowPoly.Append(m);
            }


            // fine subdivisions for viewfactor analysis
            MeshingParameters mp_high = new MeshingParameters();
            mp_high.MinimumEdgeLength = patchSize;
            mp_high.MaximumEdgeLength = patchSize;

            HighPoly = new Mesh();
            foreach (var m in Mesh.CreateFromBrep(b, mp_high))
            {
                HighPoly.Append(m);
            }


            Polys = MakePolys(HighPoly, 0, 0.5, Name + "_" + Type.ToString(), this);
        }
        private static List<RPolygon> MakePolys(Mesh _ms, double rad, double refl, string matName, RSurface parent)
        {
            var polys = new List<RPolygon>();

            if (_ms == null) return polys;

            _ms.FaceNormals.ComputeFaceNormals();

            for (int i = 0; i < _ms.Faces.Count; ++i)
            {
                RPolygon pg = new RPolygon();
                polys.Add(pg);

                pg.cen = _ms.Faces.GetFaceCenter(i);
                pg.n = _ms.FaceNormals[i];
                pg.n.Unitize();

                pg.rin = rad;
                pg.rout = 0.0;
                pg.refl = refl;
                pg.m = _ms;
                pg.matName = matName;
                pg.parent = parent;


                if (_ms.Faces[i].IsQuad)
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);
                    Point3d v3 = new Point3d(_ms.Vertices[_ms.Faces[i].D]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);
                    Vector3d n2 = Vector3d.CrossProduct(v2 - v0, v3 - v0);

                    pg.area = n1.Length * 0.5 + n2.Length * 0.5;
                }
                else
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);

                    pg.area = n1.Length * 0.5;
                }
            }
            return polys;
        }

        public enum RadiationSurfaceType
        {
            Building = 0,
            Ground = 1,
            Vegetation = 2,
            Tree = 3,
            Sky = 4
        }
    }
}
