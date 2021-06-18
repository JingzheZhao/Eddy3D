using EddyLib.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace EddyLib.Radiation
{
    [Obsolete]
    internal class RunRSystem
    {
        private void RunScript(List<GeometryBase> geo, List<Point3d> probes, int selF, int selP, ref object Geo, ref object GeoSee, ref object VFSee, ref object AllGeo, ref object Probe, ref object PGeoSee, ref object PVFSee, ref object ProbeVFLabels, ref object ProbeVFs, ref object ProbeVF2Surfs)
        {
            Mesh obstr = new Mesh();
            foreach (Mesh m in geo)
            {
                m.Vertices.CullUnused();
                obstr.Append(m);
            }

            RadiositySystem radio = new RadiositySystem();
            foreach (GeometryBase g in geo)
            {
                Mesh m = (Mesh)g;
                string matName = (string)g.UserDictionary["EddyConstruction"];
                radio.AddMesh(m, 1.0, 1.0, matName);
            }

            // only for Face - Face view factors
            radio.BuildFFMatrix(obstr);

            radio.AddProbes(probes);

            radio.BuildVFToProbes(obstr);

            radio.BuildVFToProbesByMaterial();

            for (int i = 0; i < geo.Count; i++)
            {
                geo[i].UserDictionary.Set("EddyViewFactorArray", radio.F[i]);
            }
            AllGeo = geo;

            // --------------------------------
            // getting the material view factors for the probes
            // --------------------------------

            List<string> probevflabels = radio.UniqueMaterialNames;
            DataTree<double> probevfs = new DataTree<double>();

            for (int i = 0; i < radio.RProbes.Count; i++)
            {
                for (int j = 0; j < probevflabels.Count; j++)
                {
                    probevfs.Add(radio.RProbes[i].VFtoMaterial[probevflabels[j]], new GH_Path(j));
                }
            }

            ProbeVFLabels = probevflabels;
            ProbeVFs = probevfs;

            // --------------------------------
            // getting the surface view factors for the probes
            // --------------------------------

            DataTree<double> probevfs2surfaces = new DataTree<double>();

            for (int i = 0; i < radio.RProbes.Count; i++)
            {
                probevfs2surfaces.AddRange(radio.RProbes[i].VFtoPolys, new GH_Path(i));
            }
            ProbeVF2Surfs = probevfs2surfaces;

            // --------------------------------
            // select one surface for debugging
            // --------------------------------
            Geo = geo[selF];

            List<double> vfIcanSee = new List<double>();
            List<Mesh> geoIcanSee = new List<Mesh>();

            for (int i = 0; i < radio.F[selF].Length; i++)
            {
                if (radio.F[selF][i] > 0)
                {
                    vfIcanSee.Add(radio.F[selF][i]);
                    geoIcanSee.Add((Mesh)geo[i]);
                }
            }

            VFSee = vfIcanSee;
            GeoSee = geoIcanSee;

            // --------------------------------
            // select one probe for debugging
            // --------------------------------

            Probe = probes[selP];

            List<double> probe_vfIcanSee = new List<double>();
            List<Mesh> probe_geoIcanSee = new List<Mesh>();

            for (int i = 0; i < radio.RProbes[selP].VFtoPolys.Length; i++)
            {
                double pvf = radio.RProbes[selP].VFtoPolys[i];
                if (pvf > 0)
                {
                    probe_vfIcanSee.Add(pvf);
                    probe_geoIcanSee.Add((Mesh)radio.polys[i].Mesh.Value);
                }
            }

            PVFSee = probe_vfIcanSee;
            PGeoSee = probe_geoIcanSee;
        }
    }

    [Obsolete]
    public class RadiositySystem
    {
        public RadiositySystem()
        {
        }

        public double maxv = 0.0;

        public List<RProbe> RProbes = new List<RProbe>();
        public List<RPolygon> polys = new List<RPolygon>();

        public List<string> UniqueMaterialNames = new List<string>();

        public double[][] F;
        public double[] xk0;
        public double[] xk1;
        public double[] b;

        //Sum up view factors to the different materials in the model
        public void BuildVFToProbesByMaterial()
        {
            UniqueMaterialNames = polys.Select(s => s.Name).ToHashSet().ToList();

            // set up dictionary
            for (int i = 0; i < RProbes.Count; i++)
            {
                RProbes[i].VFtoMaterial = new Dictionary<string, double>();
                for (int j = 0; j < UniqueMaterialNames.Count; j++)
                {
                    RProbes[i].VFtoMaterial.Add(UniqueMaterialNames[j], 0);
                }
            }

            for (int i = 0; i < RProbes.Count; i++)
            {
                for (int j = 0; j < polys.Count; j++)
                {
                    RProbes[i].VFtoMaterial[polys[j].Name] += RProbes[i].VFtoPolys[j];
                }
            }

            // normalize results
            for (int i = 0; i < RProbes.Count; i++)
            {
                double total = 0;
                for (int j = 0; j < UniqueMaterialNames.Count; j++)
                {
                    total += RProbes[i].VFtoMaterial[UniqueMaterialNames[j]];
                }
                double scale = 1 / total;
                for (int j = 0; j < UniqueMaterialNames.Count; j++)
                {
                    RProbes[i].VFtoMaterial[UniqueMaterialNames[j]] *= scale;
                }
            }
        }

        //Compute Form factors taking into account occlusions from a list of meshes
        public void BuildVFToProbes(Mesh Obst)
        {
            System.Threading.Tasks.Parallel.For(0, RProbes.Count, i =>
            {
                for (int j = 0; j < polys.Count; j++)
                {
                    Point3d probe_pt = RProbes[i].Point.Value;
                    RProbes[i].VFtoPolys[j] = FFactorProbe(probe_pt, polys[j], Obst);
                }
            });

            // normalize results
            for (int i = 0; i < RProbes.Count; i++)
            {
                double total = 0;
                for (int j = 0; j < RProbes[i].VFtoPolys.Length; j++)
                {
                    total += RProbes[i].VFtoPolys[j];
                }
                double scale = 1 / total;
                for (int j = 0; j < RProbes[i].VFtoPolys.Length; j++)
                {
                    RProbes[i].VFtoPolys[j] *= scale;
                }
            }
        }

        public double FFactorProbe(Point3d probe_pt, RPolygon p1, Mesh Obst)
        {
            Vector3d probe_n = p1.Centroid.Value - probe_pt;
            probe_n.Unitize();
            if (p1.Normal.Value * probe_n > 0.0001) return 0.0; //if normals don't face each other return 0

            Plane pl = new Plane(p1.Centroid.Value, p1.Normal.Value);
            if (pl.DistanceTo(probe_pt) < 0) return 0.0; // if the other face is behind the test face return 0

            double f = 0.0;

            Vector3d dv;
            dv = p1.Centroid.Value - probe_pt;
            double r = dv.Length;
            if (r < 0.1) return 0.0;

            double cosThetaI = dv * probe_n / (dv.Length * probe_n.Length);
            double cosThetaJ = -dv * p1.Normal.Value / (dv.Length * p1.Normal.Value.Length);
            f = ((cosThetaI * cosThetaJ) / (4 * Math.PI * r * r)) * p1.Area;

            //only do occlusion test for large view factors -- zero all others
            if (f < 0.00001) return 0.0;

            Vector3d dv_forRaycast = (p1.Centroid.Value + (0.01 * p1.Normal.Value)) - (probe_pt + (0.01 * probe_n));
            Line line = new Line(p1.Centroid.Value + (0.01 * p1.Normal.Value), probe_pt + (0.01 * probe_n));
            int[] fid;
            var pts = Rhino.Geometry.Intersect.Intersection.MeshLine(Obst, line, out fid);
            if (pts.Length > 0) return 0.0;

            return f;
        }

        //Compute Form factors taking into account occlusions from a list of meshes
        public void BuildFFMatrix(Mesh Obst)
        {
            int Ps = polys.Count;
            //F = new double[Ps, Ps];

            F = new double[Ps][];
            for (int i = 0; i < Ps; i++)
            {
                F[i] = new double[Ps];
            }

            xk0 = new double[Ps];
            xk1 = new double[Ps];
            b = new double[Ps];

            for (int i = 0; i < Ps; ++i)
            {
                xk0[i] = 0.0;
                xk1[i] = 0.0;
                b[i] = polys[i].rin;
            }

            double Fij = 0.0;

            // DO NOT USE THIS - THE F[i][j] IS NOT THREAD SAFE
            // System.Threading.Tasks.Parallel.For(0, Ps, j =>
            //  {
            for (int j = 0; j < Ps; ++j)
            {
                for (int i = j; i < Ps; ++i)
                {
                    if (i == j)
                    {
                        F[j][i] = 0.0;
                    }
                    else
                    {
                        Fij = FFactor(polys[i], polys[j], Obst);
                        F[j][i] = Fij * polys[i].Area;
                        F[i][j] = Fij * polys[j].Area;
                    }
                }
            }
            // });
        }

        //Computes the form factor between two polygons. It returns
        // 0.0 if the polygons are facing in opposite ways or are nearly coplanar or too close to each other
        public double FFactor(RPolygon p0, RPolygon p1, Mesh Obst)
        {
            // --- 6/25/2020
            if (p0.Normal.Value * p1.Normal.Value > 0.0001) return 0.0; //if normals don't face each other return 0
            Plane pl = new Plane(p0.Centroid.Value, p0.Normal.Value);
            if (pl.DistanceTo(p1.Centroid.Value) < 0) return 0.0; // if the other face is behind the test face return 0
                                                                  // ---

            double f = 0.0;

            Vector3d dv;
            dv = p1.Centroid.Value - p0.Centroid.Value;
            double r = dv.Length;
            if (r < 0.1) return 0.0;
            //dv *= (1.0 / r);

            double cosThetaI = dv * p0.Normal.Value / (dv.Length * p0.Normal.Value.Length);
            double cosThetaJ = -dv * p1.Normal.Value / (dv.Length * p1.Normal.Value.Length);

            //if (Math.Abs(dv * p0.n) < 0.0001 && Math.Abs(dv * p1.n) < 0.0001) return 0.0;
            //if ((dv * p0.n) < 0.0001 && -(dv * p1.n) < 0.0001) return 0.0;

            // if (cosThetaI < 0.0001 && cosThetaJ < 0.0001) return 0.0;

            f = ((cosThetaI * cosThetaJ) / (Math.PI * r * r));//*p0.area * p1.area;

            //if (f < 0.0) return 0.0;

            //only do occlusion test for large view factors -- zero all others
            if (f < 0.0000001) return 0.0;

            Vector3d dv_forRaycast = (p1.Centroid.Value + (0.01 * p1.Normal.Value)) - (p0.Centroid.Value + (0.01 * p0.Normal.Value));
            Line line = new Line(p1.Centroid.Value + (0.01 * p1.Normal.Value), p0.Centroid.Value + (0.01 * p0.Normal.Value));
            int[] fid;
            var pts = Rhino.Geometry.Intersect.Intersection.MeshLine(Obst, line, out fid);
            if (pts.Length > 0) return 0.0;

            //Ray3d ry = new Ray3d(p0.cen + 0.01 * p0.n, dv_forRaycast);
            //double il = Rhino.Geometry.Intersect.Intersection.MeshRay(Obst, ry);
            //if (il > 0.0 && il < dv_forRaycast.Length) return 0.0;

            return f;
        }

        //Adds a mesh to the system. For each Face in the mesh it adds
        //a polygon to the list of polygon and sets its emission and reflectivity to rad and refl
        public void AddMesh(Mesh _ms, double rad, double refl, string matName)
        {
            if (_ms == null) return;

            _ms.FaceNormals.ComputeFaceNormals();

            for (int i = 0; i < _ms.Faces.Count; ++i)
            {
                RPolygon pg = new RPolygon();
                polys.Add(pg);

                pg.Centroid.Value = _ms.Faces.GetFaceCenter(i);
                pg.Normal.Value = _ms.FaceNormals[i];
                pg.Normal.Value.Unitize();

                pg.rin = rad;
                pg.rout = 0.0;
                pg.refl = refl;

                pg.Name = matName;

                if (_ms.Faces[i].IsQuad)
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);
                    Point3d v3 = new Point3d(_ms.Vertices[_ms.Faces[i].D]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);
                    Vector3d n2 = Vector3d.CrossProduct(v2 - v0, v3 - v0);

                    pg.Area = n1.Length * 0.5 + n2.Length * 0.5;

                    pg.Mesh.Value = new Mesh();
                    pg.Mesh.Value.Vertices.Add(v0);
                    pg.Mesh.Value.Vertices.Add(v1);
                    pg.Mesh.Value.Vertices.Add(v2);
                    pg.Mesh.Value.Vertices.Add(v3);
                    pg.Mesh.Value.Faces.AddFace(0, 1, 2, 3);
                }
                else
                {
                    Point3d v0 = new Point3d(_ms.Vertices[_ms.Faces[i].A]);
                    Point3d v1 = new Point3d(_ms.Vertices[_ms.Faces[i].B]);
                    Point3d v2 = new Point3d(_ms.Vertices[_ms.Faces[i].C]);

                    Vector3d n1 = Vector3d.CrossProduct(v1 - v0, v2 - v0);

                    pg.Area = n1.Length * 0.5;

                    pg.Mesh.Value = new Mesh();
                    pg.Mesh.Value.Vertices.Add(v0);
                    pg.Mesh.Value.Vertices.Add(v1);
                    pg.Mesh.Value.Vertices.Add(v2);
                    pg.Mesh.Value.Faces.AddFace(0, 1, 2);
                }
            }
        }

        public void AddProbes(List<Point3d> pts)
        {
            foreach (Point3d p in pts)
            {
                RProbes.Add(new RProbe() { Point = new EddyPoint(p), VFtoPolys = new double[polys.Count] });
            }
        }

        public void AddProbes(List<RProbe> pts)
        {
            foreach (var p in pts)
            {
                p.VFtoPolys = new double[polys.Count];
                RProbes.Add(p);
            }
        }

        //Used for visualization. Its purpose is to calculate the vertex colours form the faces values for each mesh
        public void ColorMesh(Mesh ms, ref int pcount, double mult)
        {
            ms.VertexColors.CreateMonotoneMesh(Color.Black);
            double[] radv = new double[ms.Vertices.Count];
            double[] radcount = new double[ms.Vertices.Count];
            for (int i = 0; i < radv.Length; ++i)
            {
                radv[i] = 0.0;
                radcount[i] = 0.0;
            }

            for (int i = 0; i < ms.Faces.Count; ++i)
            {
                radv[ms.Faces[i].A] += polys[pcount].rout * polys[pcount].Area;
                radcount[ms.Faces[i].A] += polys[pcount].Area;

                radv[ms.Faces[i].B] += polys[pcount].rout * polys[pcount].Area;
                radcount[ms.Faces[i].B] += polys[pcount].Area;

                radv[ms.Faces[i].C] += polys[pcount].rout * polys[pcount].Area;
                radcount[ms.Faces[i].C] += polys[pcount].Area;

                if (ms.Faces[i].IsQuad)
                {
                    radv[ms.Faces[i].D] += polys[pcount].rout * polys[pcount].Area;
                    radcount[ms.Faces[i].D] += polys[pcount].Area;
                }

                pcount++;
            }

            double mm = 0.0;
            if (maxv != 0.0) mm = 255.0 * mult / maxv;
            for (int i = 0; i < radv.Length; ++i)
            {
                if (radcount[i] != 0.0) radv[i] /= radcount[i];
                int col = (int)(radv[i] * mm + 0.5);
                if (col < 0) col = 0;
                else if (col > 255) col = 255;

                ms.VertexColors.SetColor(i, col, col, col);
            }
        }

        public void Clear()
        {
            polys.Clear();
            F = null;
            xk0 = null;
            xk1 = null;
            b = null;
        }

        //Do one iteration step of Gauss-Seidel method.
        public void Iterate()
        {
            if (F == null) return;

            for (int i = 0; i < polys.Count; ++i)
            {
                xk1[i] = b[i];

                for (int j = i + 1; j < polys.Count; ++j)
                {
                    xk1[i] -= F[j][i] * xk0[j];
                }
                for (int j = 0; j < i; ++j)
                {
                    xk1[i] -= F[j][i] * xk1[j];
                }
                xk1[i] /= F[i][i];
            }

            maxv = 0.0;
            for (int i = 0; i < polys.Count; ++i)
            {
                xk0[i] = xk1[i];

                polys[i].rout = xk0[i];
                if (Math.Abs(polys[i].rout) > maxv) maxv = Math.Abs(polys[i].rout);
            }
        }
    };
}