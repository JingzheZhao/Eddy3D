using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Eddy.Properties;
using EddyLib;


namespace Eddy
{
    public class BrepGridPointsCMP : GH_Component
    {
        /// <summary>
        ///     Initializes a new instance of the BrepGridPointsCMP class.
        /// </summary>
        public BrepGridPointsCMP()
            : base("Brep to Grid Points", "Brep2Pts",
@"Generate regular grid points on surfaces for CFD probing.

Creates evenly-spaced probe locations for velocity sampling.
Works with Breps, surfaces, or meshes.

" + EddyVersion.toString(),
                "Eddy3D", "1 | Wind")
        {
        }

        /// <summary>
        ///     Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                    // You can add image files to your project resources and access them like this:
                    Resources.Eddy_brep2points;

        /// <summary>
        ///     Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{4cd4a90b-87fc-463f-a2ba-a128f1b27ce7}");
        /// <summary>
        ///     Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Geo", "Breps, surfaces, or meshes to sample.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Spacing", "Space", "Grid spacing. Units: m. Default: 10", GH_ParamAccess.item, 10.0);
            pManager.AddBooleanParameter("Run", "Run!", "Generate grid points.", GH_ParamAccess.item, true);
            pManager[0].Optional = false;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        ///     Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Pts", "Grid sampling points for Probe component.", GH_ParamAccess.list);
            pManager.AddTextParameter("Status", "Msg", "Processing status message.", GH_ParamAccess.item);
        }

        /// <summary>
        ///     Resolve input geometry to list of Brep objects
        /// </summary>
        private List<Brep> ResolveToBreps(GeometryBase geometry)
        {
            var result = new List<Brep>();

            if (geometry == null)
                return result;

            if (geometry is Brep brep)
            {
                result.Add(brep);
                return result;
            }

            if (geometry is Surface surface)
            {
                var brepFromSurface = Brep.CreateFromSurface(surface);
                if (brepFromSurface != null)
                {
                    result.Add(brepFromSurface);
                    return result;
                }
            }

            if (geometry is Mesh mesh)
            {
                try
                {
                    var brepFromMesh = Brep.CreateFromMesh(mesh, true);
                    if (brepFromMesh != null && brepFromMesh.IsValid)
                    {
                        result.Add(brepFromMesh);
                        return result;
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }

            return result;
        }

        /// <summary>
        ///     Create regular grid on Brep surfaces.
        ///     Shoots rays along all three axes to capture horizontal AND vertical faces.
        /// </summary>
        private List<Point3d> CreateGridOnBreps(List<Brep> brepList, double spacing)
        {
            if (brepList == null || brepList.Count == 0 || spacing <= 0)
                return new List<Point3d>();

            BoundingBox bbox = BoundingBox.Unset;
            foreach (var brep in brepList)
            {
                if (brep != null && brep.IsValid)
                {
                    var brepBbox = brep.GetBoundingBox(true);
                    if (bbox.IsValid)
                        bbox.Union(brepBbox);
                    else
                        bbox = brepBbox;
                }
            }

            if (!bbox.IsValid)
                return new List<Point3d>();

            const double tolerance = 0.1;
            // Key precision for deduplication: one cell per tolerance voxel
            double keyScale = 1.0 / tolerance;

            var pointSet = new HashSet<long>();
            var points = new List<Point3d>();

            // Shoot rays along X, Y, and Z to handle faces of any orientation.
            // axis 0 = X rays (grid on Y-Z plane) — catches Y/Z-normal vertical faces
            // axis 1 = Y rays (grid on X-Z plane) — catches X/Z-normal vertical faces
            // axis 2 = Z rays (grid on X-Y plane) — catches horizontal faces
            for (int axis = 0; axis < 3; axis++)
            {
                var newPts = ShootRaysAlongAxis(brepList, bbox, spacing, axis, tolerance);
                foreach (var pt in newPts)
                {
                    long key = PointKey(pt, keyScale);
                    if (pointSet.Add(key))
                        points.Add(pt);
                }
            }

            points.Sort((a, b) =>
            {
                int cmpX = a.X.CompareTo(b.X);
                if (cmpX != 0) return cmpX;
                int cmpY = a.Y.CompareTo(b.Y);
                if (cmpY != 0) return cmpY;
                return a.Z.CompareTo(b.Z);
            });
            return points;
        }

        private static long PointKey(Point3d pt, double keyScale)
        {
            // Pack rounded coords into a single long for fast deduplication.
            // Assumes coordinates fit in ~21-bit range each (±1 048 576 units).
            long ix = (long)Math.Round(pt.X * keyScale) + (1 << 20);
            long iy = (long)Math.Round(pt.Y * keyScale) + (1 << 20);
            long iz = (long)Math.Round(pt.Z * keyScale) + (1 << 20);
            return (ix & 0x1FFFFF) | ((iy & 0x1FFFFF) << 21) | ((iz & 0x1FFFFF) << 42);
        }

        /// <summary>
        ///     Shoots rays along the specified axis (0=X, 1=Y, 2=Z) across the bbox grid
        ///     and returns all surface intersection points that lie on a brep face.
        ///     Sequential (no parallelism) to keep RhinoCommon thread-safe on all platforms.
        /// </summary>
        private static List<Point3d> ShootRaysAlongAxis(
            IReadOnlyList<Brep> brepList,
            BoundingBox bbox,
            double spacing,
            int axis,
            double tolerance)
        {
            // a0, a1 are the two grid axes; a2 is the ray direction axis.
            int a0 = (axis + 1) % 3;
            int a1 = (axis + 2) % 3;
            int a2 = axis;

            double a0Min = bbox.Min[a0];
            double a0Max = bbox.Max[a0];
            double a1Min = bbox.Min[a1];
            double a1Max = bbox.Max[a1];
            double a2Min = bbox.Min[a2] - spacing;
            double a2Max = bbox.Max[a2] + spacing;

            var a0Values = BuildAxisValues(a0Min, a0Max, spacing);
            var a1Values = BuildAxisValues(a1Min, a1Max, spacing);
            if (a0Values.Count == 0 || a1Values.Count == 0)
                return new List<Point3d>();

            var output = new List<Point3d>();

            for (int i = 0; i < a0Values.Count; i++)
            {
                for (int j = 0; j < a1Values.Count; j++)
                {
                    var startCoords = new double[3];
                    var endCoords = new double[3];
                    startCoords[a0] = a0Values[i];
                    startCoords[a1] = a1Values[j];
                    startCoords[a2] = a2Min;
                    endCoords[a0] = a0Values[i];
                    endCoords[a1] = a1Values[j];
                    endCoords[a2] = a2Max;

                    var rayStart = new Point3d(startCoords[0], startCoords[1], startCoords[2]);
                    var rayEnd   = new Point3d(endCoords[0],   endCoords[1],   endCoords[2]);
                    var ray = new Ray3d(rayStart, rayEnd - rayStart);

                    foreach (var brep in brepList)
                    {
                        if (brep == null) continue;

                        var hits = Intersection.RayShoot(ray, new[] { brep }, 10);
                        if (hits == null || hits.Length == 0) continue;

                        foreach (var pt in hits)
                        {
                            Point3d closest = brep.ClosestPoint(pt);
                            if (closest == Point3d.Unset || pt.DistanceTo(closest) >= tolerance)
                                continue;

                            if (IsPointOnBrepFace(brep, pt, tolerance))
                                output.Add(pt);
                        }
                    }
                }
            }

            return output;
        }

        private static List<double> BuildAxisValues(double min, double max, double spacing)
        {
            var values = new List<double>();
            if (spacing <= 0)
                return values;

            double epsilon = Math.Abs(spacing) * 1e-9;
            for (double value = min; value <= max + epsilon; value += spacing)
            {
                values.Add(value);
            }
            return values;
        }

        private static bool IsPointOnBrepFace(Brep brep, Point3d pt, double tolerance)
        {
            for (int i = 0; i < brep.Faces.Count; i++)
            {
                var face = brep.Faces[i];
                double u, v;
                if (!face.ClosestPoint(pt, out u, out v))
                    continue;

                Point3d facePt = face.PointAt(u, v);
                if (pt.DistanceTo(facePt) >= tolerance)
                    continue;

                var relation = face.IsPointOnFace(u, v);
                if (relation != PointFaceRelation.Exterior)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var geometryList = new List<GeometryBase>();
            var spacing = 10.0;
            bool run = true;

            if (!DA.GetDataList(0, geometryList))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No geometry provided");
                return;
            }

            DA.GetData(1, ref spacing);
            DA.GetData(2, ref run);

            if (!run)
            {
                DA.SetDataList(0, new List<Point3d>());
                DA.SetData(1, "Toggle 'Run' to start");
                return;
            }

            if (spacing <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Spacing must be greater than 0");
                return;
            }

            // Resolve all geometry to Breps
            var brepList = new List<Brep>();
            foreach (var geo in geometryList)
            {
                var breps = ResolveToBreps(geo);
                brepList.AddRange(breps);
            }

            if (brepList.Count == 0)
            {
                DA.SetDataList(0, new List<Point3d>());
                DA.SetData(1, "No valid Breps found");
                return;
            }

            // Create grid points
            var points = CreateGridOnBreps(brepList, spacing);

            DA.SetDataList(0, points);
            DA.SetData(1, $"Generated {points.Count} grid points with spacing={spacing}");
        }
    }
}
