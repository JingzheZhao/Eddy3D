using EddyLib.UI;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    public class MRT_Simulation_System
    {
        public int TOTAL = 0;
        public int STEP = 0;

        public int methodsteps = 2;

        public string BaseWorkingDir = "";
        public string CFDDataPath = "";

        public MRT_Simulation_Settings Settings = new MRT_Simulation_Settings();

        public Weather Weather;

        public List<RProbe> Probes;

        public List<RSurface> RSurfaces;

        public List<RPolygon> Polys = new List<RPolygon>();

        public List<Mesh> ProbeMeshes;

        // Aux geometry
        public Mesh SkyDomeForVF;

        public Mesh HighPolyNoSky;
        public Mesh LowPolyNoSky;

        // Sub-Systems
        public RadiationSystem RadiationSystem;

        public ThermalSystem ThermalSystem;

        public ComfortSystem ComfortSystem;

        public MRT_Simulation_System(string baseWorkingDir, Weather weather, List<RSurface> rsurfaces, List<RProbe> rprobes, string cfd_data_path)
        {
            BaseWorkingDir = baseWorkingDir;
            RSurfaces = rsurfaces;
            Weather = weather;

            Probes = rprobes;

            CFDDataPath = cfd_data_path;

            // ---------------------
            // Make a unified mesh radiance
            // ---------------------
            HighPolyNoSky = new Mesh();
            foreach (var rs in RSurfaces)
            {
                if (rs == null) continue;
                //if (rs.Type == RSurface.RadiationSurfaceType.Sky) continue;
                if (rs.HighPoly != null)
                {
                    rs.HighPoly.Vertices.CullUnused();
                    HighPolyNoSky.Append(rs.HighPoly);
                }
            }

            LowPolyNoSky = new Mesh();
            foreach (var rs in RSurfaces)
            {
                if (rs == null) continue;
                //if (rs.Type == RSurface.RadiationSurfaceType.Sky) continue;
                if (rs.LowPoly != null)
                {
                    rs.LowPoly.Vertices.CullUnused();
                    LowPolyNoSky.Append(rs.LowPoly);
                }
            }

            // ---------------------
            // Make a sky dome for VF calculation
            // ---------------------

            var bb = HighPolyNoSky.GetBoundingBox(false);
            var center = new Point3d(bb.Center.X, bb.Center.Y, bb.Min.Z);
            var radius = bb.Diagonal.Length;

            Sphere sphere = new Sphere(center, radius);
            var sphereM = Mesh.CreateQuadSphere(sphere, 4);
            SkyDomeForVF = new Mesh();

            if (sphereM != null)
            {
                sphereM.FaceNormals.ComputeFaceNormals();
                var findex = new List<int>();
                for (int i = 0; i < sphereM.Faces.Count; i++)
                {
                    var dot = sphereM.FaceNormals[i] * Vector3d.ZAxis;
                    if (dot > 0 - 0.0001)
                    {
                        findex.Add(i);
                    }
                }

                SkyDomeForVF.Append(sphereM.Faces.ExtractFaces(findex));
                SkyDomeForVF.FaceNormals.ComputeFaceNormals();

                SkyDomeForVF.Flip(true, true, true);
            }

            // ---------------------
            // Setup polygons
            // ---------------------
            foreach (var rs in this.RSurfaces)
            {
                this.Polys.AddRange(rs.Polys);
            }
            this.Polys.AddRange(MakeRPolygons(SkyDomeForVF, RadiationSurfaceType.Sky, "SKY", SimulationType.Ignore));
            // set unique ids
            int idcnt = 0;
            foreach (var p in this.Polys)
            { p.ID = idcnt; idcnt++; }

            // ---------------------
            // Setup working dir
            // ---------------------

            var dirs = new List<String>() { baseWorkingDir, baseWorkingDir + @"\Rad\", baseWorkingDir + @"\Ep\" };

            foreach (string d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }

            this.RadiationSystem = new RadiationSystem(this.BaseWorkingDir, this.Weather, this.RSurfaces, this.Probes, this.Polys, this.HighPolyNoSky);
            this.ThermalSystem = new ThermalSystem(this.BaseWorkingDir, this.Weather, this.Probes, this.Polys, this.LowPolyNoSky, this.Settings.CummulativeViewFactorCutoff, this.Settings.SmallFaceCutoff);
            this.ComfortSystem = new ComfortSystem(this.BaseWorkingDir, this.Weather, this.Probes, this.Polys, this.CFDDataPath, this.Settings.WindScalingFactor);

            TOTAL += this.methodsteps + RadiationSystem.methodsteps + ThermalSystem.methodsteps + ComfortSystem.methodsteps;
        }

        public bool RunVF(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            return RunVF_Internal(run, ct, steps, ref stepCnt);
        }

        private bool RunVF_Internal(bool run, CancellationToken ct, int steps, ref int stepCnt)
        {
            Stopwatch sp = new Stopwatch();

            Console.WriteLine("Computing probe view factors...");
            this.BuildVFToProbes(HighPolyNoSky);
            this.BuildVFToProbesByMaterial();
            Console.WriteLine("Probe view factors: " + sp.ElapsedMilliseconds + " ms"); sp.Restart(); Interlocked.Increment(ref stepCnt);
            Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));

            if (Settings.ComputeLongWaveExchangeEnergyPlus)
            {
                // optional
                Console.WriteLine("Computing polygon view factors...");
                this.BuildFFMatrix(HighPolyNoSky);

                Console.WriteLine("Polygon view factors: " + sp.ElapsedMilliseconds + " ms"); sp.Stop(); Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
            }
            return true;
        }

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

        //Sum up view factors to the different materials in the model
        private void BuildVFToProbesByMaterial()
        {
            UniqueSurfaceTypesInModel = Polys.Select(s => s.Type.ToString()).ToHashSet().ToList();

            // set up dictionary
            for (int i = 0; i < Probes.Count; i++)
            {
                Probes[i].VFtoMaterial = new Dictionary<string, double>();
                for (int j = 0; j < UniqueSurfaceTypesInModel.Count; j++)
                {
                    Probes[i].VFtoMaterial.Add(UniqueSurfaceTypesInModel[j], 0);
                }
            }

            for (int i = 0; i < Probes.Count; i++)
            {
                for (int j = 0; j < Polys.Count; j++)
                {
                    Probes[i].VFtoMaterial[Polys[j].Type.ToString()] += Probes[i].VFtoPolys[j];
                }
            }

            // normalize results
            for (int i = 0; i < Probes.Count; i++)
            {
                double total = 0;
                for (int j = 0; j < UniqueSurfaceTypesInModel.Count; j++)
                {
                    total += Probes[i].VFtoMaterial[UniqueSurfaceTypesInModel[j]];
                }
                double scale = 1 / total;
                for (int j = 0; j < UniqueSurfaceTypesInModel.Count; j++)
                {
                    Probes[i].VFtoMaterial[UniqueSurfaceTypesInModel[j]] *= scale;
                }
            }
        }

        //Compute Form factors taking into account occlusions from a list of meshes
        private void BuildVFToProbes(Mesh Obst)
        {
            foreach (var p in Probes)
            {
                p.VFtoPolys = new double[Polys.Count];
            }

            Parallel.For(0, Probes.Count, i =>
            {
                for (int j = 0; j < Polys.Count; j++)
                {
                    Point3d probe_pt = Probes[i].Point.Value;
                    Probes[i].VFtoPolys[j] = FFactorProbe(probe_pt, Polys[j], Obst);
                }
            });

            // normalize results
            for (int i = 0; i < Probes.Count; i++)
            {
                double total = 0;
                for (int j = 0; j < Probes[i].VFtoPolys.Length; j++)
                {
                    total += Probes[i].VFtoPolys[j];
                }
                double scale = 1 / total;
                for (int j = 0; j < Probes[i].VFtoPolys.Length; j++)
                {
                    Probes[i].VFtoPolys[j] *= scale;
                }
            }
            FindPolysSeenByProbes();
        }

        private double FFactorProbe(Point3d probe_pt, RPolygon p1, Mesh Obst)
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

        private void FindPolysSeenByProbes()
        {
            for (int j = 0; j < Polys.Count; j++)
            {
                for (int i = 0; i < Probes.Count; i++)
                {
                    Polys[j].SeenByProbes += Probes[i].VFtoPolys[j];
                }
            }

            for (int j = 0; j < Polys.Count; j++)
            {
                if (Polys[j].SeenByProbes != 0)
                {
                    Polys[j].SeenByProbes /= Probes.Count;
                }
            }
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
                        Fij = FFactor(Polys[i], Polys[j], Obst);
                        F[j][i] = Fij * Polys[i].Area;
                        F[i][j] = Fij * Polys[j].Area;
                    }
                }
            }
            // });
        }

        //Computes the form factor between two polygons. It returns
        // 0.0 if the polygons are facing in opposite ways or are nearly coplanar or too close to each other
        private double FFactor(RPolygon p0, RPolygon p1, Mesh Obst)
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