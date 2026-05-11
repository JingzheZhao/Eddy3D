using EddyLib.UI;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    public partial class MRT_Simulation_System
    {
        #region VIEW FACTOR SYSTEM

        private static List<RPolygon> MakeRPolygons(Mesh _ms, RadiationSurfaceType type, string matName, SimulationType simtype, double rad = 0, double refl = 0.5)
        {
            List<RPolygon> polys = new List<RPolygon>();
            if (_ms == null) return polys;

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
                pg.Type = type;
                pg.SimulationType = simtype;

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
            return polys;
        }

        public List<string> UniqueSurfaceTypesInModel = new List<string>();

        // Test-only seam: parameterless ctor so unit tests can build a minimal
        // instance (Probes + Polys) and exercise the view-factor kernels in isolation.
        internal MRT_Simulation_System() { }

        //Sum up view factors to the different materials in the model
        internal void BuildVFToProbesByMaterial()
        {
            // Distinct on the enum first to avoid per-poly Enum.ToString() allocations.
            var surfaceTypes = Polys.Select(s => s.Type).Distinct().ToList();
            UniqueSurfaceTypesInModel = surfaceTypes.Select(t => t.ToString()).ToList();

            // Enum → unique-list index, O(1) lookup, no per-poly string allocation.
            var typeToIndex = new Dictionary<RadiationSurfaceType, int>(surfaceTypes.Count);
            for (int i = 0; i < surfaceTypes.Count; i++)
            {
                typeToIndex[surfaceTypes[i]] = i;
            }

            int[] polyTypeIndices = new int[Polys.Count];
            for (int j = 0; j < Polys.Count; j++)
            {
                polyTypeIndices[j] = typeToIndex[Polys[j].Type];
            }

            // Single Parallel.For: allocate dict, aggregate into bucket array, accumulate
            // total, normalize, and emit dict entries -- one pass per probe.
            Parallel.For(0, Probes.Count, i =>
            {
                var probe = Probes[i];
                probe.VFtoMaterial = new Dictionary<string, double>(UniqueSurfaceTypesInModel.Count);

                double[] tempMaterialVF = new double[UniqueSurfaceTypesInModel.Count];
                double total = 0;
                for (int j = 0; j < Polys.Count; j++)
                {
                    double vf = probe.VFtoPolys[j];
                    tempMaterialVF[polyTypeIndices[j]] += vf;
                    total += vf;
                }

                // NaN-safety: only scale when there is something to scale.
                if (total > 0)
                {
                    double scale = 1.0 / total;
                    for (int j = 0; j < UniqueSurfaceTypesInModel.Count; j++)
                    {
                        probe.VFtoMaterial.Add(UniqueSurfaceTypesInModel[j], tempMaterialVF[j] * scale);
                    }
                }
                else
                {
                    for (int j = 0; j < UniqueSurfaceTypesInModel.Count; j++)
                    {
                        probe.VFtoMaterial.Add(UniqueSurfaceTypesInModel[j], 0.0);
                    }
                }
            });
        }

        //Compute Form factors taking into account occlusions from a list of meshes
        internal void BuildVFToProbes(Mesh Obst)
        {
            // Single Parallel.For: allocate array, compute, accumulate total, and
            // normalize -- one pass per probe. Better cache locality than three
            // separate passes.
            Parallel.For(0, Probes.Count, i =>
            {
                var probe = Probes[i];
                probe.VFtoPolys = new double[Polys.Count];
                Point3d probe_pt = probe.Point.Value;
                double total = 0;

                for (int j = 0; j < Polys.Count; j++)
                {
                    double f = FFactorProbe(probe_pt, Polys[j], Obst);
                    probe.VFtoPolys[j] = f;
                    total += f;
                }

                // NaN-safety: only scale when probe saw at least one polygon.
                if (total > 0)
                {
                    double scale = 1.0 / total;
                    for (int j = 0; j < Polys.Count; j++) probe.VFtoPolys[j] *= scale;
                }
            });

            FindPolysSeenByProbes();
        }

        internal double FFactorProbe(Point3d probe_pt, RPolygon p1, Mesh Obst)
        {
            // Hoist field accesses so we read each property once.
            Point3d p1Centroid = p1.Centroid.Value;
            Vector3d p1Normal = p1.Normal.Value;

            Vector3d dv = p1Centroid - probe_pt;
            double r2 = dv.X * dv.X + dv.Y * dv.Y + dv.Z * dv.Z;
            if (r2 < 0.01) return 0.0; // r < 0.1 -- polys too close

            double r = Math.Sqrt(r2);
            Vector3d probe_n = dv / r;

            // Single cosThetaJ check folds in two old early-exit branches:
            //   1) old: (p1.Normal * probe_n > 0.0001) return 0   [normals don't face]
            //   2) old: pl.DistanceTo(probe_pt) < 0 return 0      [probe behind polygon]
            // Both are equivalent to (-dv · p1.Normal) / r < 0.0001, since
            // probe_n = dv/r so p1.Normal · probe_n = -cosThetaJ.
            double cosThetaJ = -(dv * p1Normal) / r;
            if (cosThetaJ < 0.0001) return 0.0;

            // cosThetaI collapses to 1 because probe_n = dv/r (the same direction as dv),
            // so dv · probe_n / (|dv| * |probe_n|) = r / (r * 1) = 1.
            double f = (cosThetaJ / (4 * Math.PI * r2)) * p1.Area;

            //only do occlusion test for large view factors -- zero all others
            if (f < 0.00001) return 0.0;

            Line line = new Line(p1Centroid + (0.01 * p1Normal), probe_pt + (0.01 * probe_n));
            var pts = Rhino.Geometry.Intersect.Intersection.MeshLine(Obst, line, out _);
            if (pts.Length > 0) return 0.0;

            return f;
        }

        internal void FindPolysSeenByProbes()
        {
            // Parallel over polygons; each iteration writes to its own Polys[j] slot.
            // Preserve the old "+= into pre-existing SeenByProbes, then divide-if-nonzero"
            // semantics so callers that pre-populate SeenByProbes keep the same behavior.
            Parallel.For(0, Polys.Count, j =>
            {
                double total = Polys[j].SeenByProbes;
                for (int i = 0; i < Probes.Count; i++)
                {
                    total += Probes[i].VFtoPolys[j];
                }
                if (total != 0)
                {
                    Polys[j].SeenByProbes = total / Probes.Count;
                }
                // else: leave Polys[j].SeenByProbes unchanged
            });
        }

        public double maxv = 0.0;

        public double[][] F;

        public double[] xk0;

        public double[] xk1;

        public double[] b;

        //Compute Form factors taking into account occlusions from a list of meshes
        private void BuildFFMatrix(Mesh Obst)
        {
            int Ps = Polys.Count;

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
                b[i] = Polys[i].rin;
            }

            // Parallelized: each (i,j) pair with i>j is visited exactly once,
            // so each cell F[x][y] is written exactly once — no data race.
            Parallel.For(0, Ps, j =>
            {
                for (int i = j; i < Ps; ++i)
                {
                    if (i == j)
                    {
                        F[j][i] = 0.0;
                    }
                    else
                    {
                        double Fij = FFactor(Polys[i], Polys[j], Obst);
                        F[j][i] = Fij * Polys[i].Area;
                        F[i][j] = Fij * Polys[j].Area;
                    }
                }
            });
        }

        //Computes the form factor between two polygons. It returns
        // 0.0 if the polygons are facing in opposite ways or are nearly coplanar or too close to each other
        internal double FFactor(RPolygon p0, RPolygon p1, Mesh Obst)
        {
            // Hoist field accesses so we read each property once.
            Vector3d p0Normal = p0.Normal.Value;
            Vector3d p1Normal = p1.Normal.Value;
            Point3d p0Centroid = p0.Centroid.Value;
            Point3d p1Centroid = p1.Centroid.Value;

            // Normals facing the same way → polys don't see each other.
            if (p0Normal * p1Normal > 0.0001) return 0.0;

            Vector3d dv = p1Centroid - p0Centroid;
            double r2 = dv.X * dv.X + dv.Y * dv.Y + dv.Z * dv.Z;
            if (r2 < 0.01) return 0.0; // r < 0.1 -- polys too close

            double r = Math.Sqrt(r2);

            // Old "Plane(p0).DistanceTo(p1.Centroid) < 0" check is equivalent to cosThetaI < 0.
            // The slightly tighter < 0.0001 threshold matches what the old f < 0.0000001
            // filter already rejected (cosThetaI very small ⇒ tiny f ⇒ filtered).
            double cosThetaI = (dv * p0Normal) / r;
            if (cosThetaI < 0.0001) return 0.0;

            double cosThetaJ = -(dv * p1Normal) / r;
            if (cosThetaJ < 0.0001) return 0.0;

            double f = (cosThetaI * cosThetaJ) / (Math.PI * r2);

            //only do occlusion test for large view factors -- zero all others
            if (f < 0.0000001) return 0.0;

            Line line = new Line(p1Centroid + (0.01 * p1Normal), p0Centroid + (0.01 * p0Normal));
            var pts = Rhino.Geometry.Intersect.Intersection.MeshLine(Obst, line, out _);
            if (pts.Length > 0) return 0.0;

            return f;
        }

        //Do one iteration step of Gauss-Seidel method.
        private void Iterate()
        {
            if (F == null) return;

            for (int i = 0; i < Polys.Count; ++i)
            {
                xk1[i] = b[i];

                for (int j = i + 1; j < Polys.Count; ++j)
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
            for (int i = 0; i < Polys.Count; ++i)
            {
                xk0[i] = xk1[i];

                Polys[i].rout = xk0[i];
                if (Math.Abs(Polys[i].rout) > maxv) maxv = Math.Abs(Polys[i].rout);
            }
        }

        #endregion VIEW FACTOR SYSTEM
    }
}
