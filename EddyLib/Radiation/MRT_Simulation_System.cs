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
        public int TOTAL = 0;

        public int STEP = 0;

        public int methodsteps = 2;

        public string BaseWorkingDir = "";

        public string CFDDataPath = "";

        public MRT_Simulation_Settings Settings;

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

        public MRT_Simulation_System(string baseWorkingDir, Weather weather, List<RSurface> rsurfaces, List<RProbe> rprobes, string cfd_data_path, MRT_Simulation_Settings _set)
        {
            BaseWorkingDir = Path.GetFullPath(DefaultDirectoriesAndPaths.ResolveWorkingDirectory(baseWorkingDir));
            RSurfaces = rsurfaces;
            Weather = weather;
            Settings = _set;
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

            var dirs = new List<string>
            {
                BaseWorkingDir,
                Path.Combine(BaseWorkingDir, "Rad"),
                Path.Combine(BaseWorkingDir, "Ep")
            };

            foreach (string d in dirs)
            {
                if (!Directory.Exists(d))
                {
                    Directory.CreateDirectory(d);
                }
            }

            this.RadiationSystem = new RadiationSystem(this.BaseWorkingDir, this.Weather, this.RSurfaces, this.Probes, this.Polys, this.HighPolyNoSky);
            this.ThermalSystem = new ThermalSystem(this.BaseWorkingDir, this.Weather, this.Probes, this.Polys, this.LowPolyNoSky, this.Settings.CumulativeViewFactorCutoffPercentile, this.Settings.SmallFaceCutoff);
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

    }
}
