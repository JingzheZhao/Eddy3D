using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Eddy
{
    public class CompSkyExposure : GH_Component
    {
        public CompSkyExposure()
          : base("Sky Exposure", "SkyExp",
@"Sky Exposure

Computes the Sky View Factor (SVF) for each input point using the Tregenza sky subdivision.
Casts 145 rays toward the upper hemisphere and returns the fraction of unobstructed sky directions.

Output value range: 0.0 (fully obstructed) to 1.0 (fully open sky).

" + EddyVersion.toString(),
              EddyVersion.Name, "3 | PostProcessing")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter(
                "Positions", "Positions",
                @"List of 3D points at which sky exposure is computed.

Rays are cast from 0.918 m above each input point (pedestrian height offset). Use ground surface points directly as input — no manual height offset needed.

For analysis at a custom height, adjust the Z value of the input points accordingly.",
                GH_ParamAccess.list);

            pManager.AddGeometryParameter(
                "Context", "Context",
                @"Obstructing geometry used for ray intersection. Typical inputs: buildings, trees, walls, canopies.

Ground surface does not need to be included — only geometry that blocks sky visibility matters.
Accepts: Mesh, Brep, Surface.",
                GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter(
                "Sky Exposure", "SkyExp",
                "Sky exposure per point. Range: 0.0 (fully obstructed) to 1.0 (fully open sky).",
                GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var positions = new List<Point3d>();
            var contextGeo = new List<GeometryBase>();

            if (!DA.GetDataList("Positions", positions)) return;
            if (!DA.GetDataList("Context", contextGeo)) return;

            if (positions == null || positions.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Positions list is null or empty.");
                return;
            }
            if (contextGeo == null || contextGeo.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Context list is null or empty.");
                return;
            }

            // Build Tregenza 145 sky directions
            int[]    rowCount = { 30, 30, 24, 24, 18, 12, 6, 1 };
            double[] rowElev  = {  6, 18, 30, 42, 54, 66, 78, 90 };
            const double deg2rad = Math.PI / 180.0;

            var skyDirs = new Vector3d[145];
            int idx = 0;
            for (int r = 0; r < rowCount.Length; r++)
            {
                int    n    = rowCount[r];
                double elev = rowElev[r] * deg2rad;
                double sinE = Math.Sin(elev);
                double cosE = Math.Cos(elev);
                if (n == 1)
                {
                    skyDirs[idx++] = new Vector3d(0, 0, 1);
                }
                else
                {
                    for (int i = 0; i < n; i++)
                    {
                        double az = 2.0 * Math.PI * i / n;
                        skyDirs[idx++] = new Vector3d(cosE * Math.Cos(az), cosE * Math.Sin(az), sinE);
                    }
                }
            }
            int nDirs = idx; // 145

            // Ray origin offset: lift above the point to avoid self-intersection
            const double humanHeight = 1.8;
            const double rayOffset   = humanHeight / 100.0 + humanHeight / 2.0; // 0.918 m

            // Convert context geometry to a single combined mesh
            var combined = new Mesh();
            foreach (var geom in contextGeo)
            {
                if (geom is Mesh m)
                {
                    combined.Append(m);
                }
                else if (geom is Brep b)
                {
                    var bm = Mesh.CreateFromBrep(b, MeshingParameters.FastRenderMesh);
                    if (bm != null) foreach (var x in bm) combined.Append(x);
                }
                else if (geom is Surface s)
                {
                    var sm = Mesh.CreateFromBrep(s.ToBrep(), MeshingParameters.FastRenderMesh);
                    if (sm != null) foreach (var x in sm) combined.Append(x);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                        $"Unsupported geometry type in Context: {geom?.GetType().Name ?? "null"}. Only Mesh, Brep, and Surface are accepted.");
                    return;
                }
            }
            combined.RebuildNormals();

            // Extract point coordinates for thread-safe parallel access
            int nPts = positions.Count;
            double[] ptsX = new double[nPts];
            double[] ptsY = new double[nPts];
            double[] ptsZ = new double[nPts];
            for (int i = 0; i < nPts; i++)
            {
                ptsX[i] = positions[i].X;
                ptsY[i] = positions[i].Y;
                ptsZ[i] = positions[i].Z;
            }

            // Parallel ray casting — outer loop over points, inner loop over 145 sky directions
            double[] result  = new double[nPts];
            var      cDirs   = skyDirs;
            int      cNDirs  = nDirs;
            var      cMesh   = combined;
            double   cOffset = rayOffset;

            Parallel.For(0, nPts, i =>
            {
                var origin = new Point3d(ptsX[i], ptsY[i], ptsZ[i] + cOffset);
                int hits = 0;
                for (int d = 0; d < cNDirs; d++)
                {
                    var ray = new Ray3d(origin, cDirs[d]);
                    double t = Intersection.MeshRay(cMesh, ray);
                    if (t >= 0) hits++;
                }
                result[i] = 1.0 - (double)hits / cNDirs;
            });

            DA.SetDataList(0, result);
        }

        protected override System.Drawing.Bitmap Icon => Resources.Eddy_skyExposure;

        public override Guid ComponentGuid => new Guid("{A3F2B8C1-4D5E-6F70-8192-A3B4C5D6E7F8}");
    }
}
