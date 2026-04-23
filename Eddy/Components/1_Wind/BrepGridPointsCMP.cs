using Grasshopper.Kernel;
using Rhino.Geometry;
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
        ///     Builds the grid in each face's own local plane so spacing is even on
        ///     horizontal, vertical, and arbitrarily-rotated faces alike.
        /// </summary>
        private List<Point3d> CreateGridOnBreps(List<Brep> brepList, double spacing)
        {
            if (brepList == null || brepList.Count == 0 || spacing <= 0)
                return new List<Point3d>();

            const double tolerance = 0.1;
            double keyScale = 1.0 / tolerance;

            var pointSet = new HashSet<long>();
            var points = new List<Point3d>();

            foreach (var brep in brepList)
            {
                if (brep == null || !brep.IsValid) continue;

                foreach (BrepFace face in brep.Faces)
                {
                    var facePts = CreateGridOnFace(face, spacing, tolerance);
                    foreach (var pt in facePts)
                    {
                        long key = PointKey(pt, keyScale);
                        if (pointSet.Add(key))
                            points.Add(pt);
                    }
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

        /// <summary>
        ///     Generates evenly-spaced points on a single BrepFace by building a grid
        ///     inside the face's local plane, then projecting each candidate back onto
        ///     the face and discarding those outside its boundary.
        /// </summary>
        private static List<Point3d> CreateGridOnFace(BrepFace face, double spacing, double tolerance)
        {
            var output = new List<Point3d>();

            // Build a plane aligned to the face at its parametric center.
            var dom0 = face.Domain(0);
            var dom1 = face.Domain(1);
            double midU = dom0.ParameterAt(0.5);
            double midV = dom1.ParameterAt(0.5);

            Plane facePlane;
            if (!face.FrameAt(midU, midV, out facePlane))
                return output;

            // Project the face's world bounding box corners onto the plane to find
            // the extent of the grid in local (s, t) coordinates.
            var bbox = face.GetBoundingBox(true);
            if (!bbox.IsValid) return output;

            double sMin = double.MaxValue, sMax = double.MinValue;
            double tMin = double.MaxValue, tMax = double.MinValue;

            foreach (var corner in bbox.GetCorners())
            {
                Vector3d v = corner - facePlane.Origin;
                double s = v * facePlane.XAxis;
                double t = v * facePlane.YAxis;
                if (s < sMin) sMin = s;
                if (s > sMax) sMax = s;
                if (t < tMin) tMin = t;
                if (t > tMax) tMax = t;
            }

            var sValues = BuildAxisValues(sMin, sMax, spacing);
            var tValues = BuildAxisValues(tMin, tMax, spacing);
            if (sValues.Count == 0 || tValues.Count == 0) return output;

            foreach (var s in sValues)
            {
                foreach (var t in tValues)
                {
                    // Candidate point in the face's local plane.
                    Point3d candidate = facePlane.Origin
                        + s * facePlane.XAxis
                        + t * facePlane.YAxis;

                    // Project candidate onto the actual face surface.
                    double u, v2;
                    if (!face.ClosestPoint(candidate, out u, out v2))
                        continue;

                    Point3d onFace = face.PointAt(u, v2);

                    // Reject if the projection moved too far (e.g. curved face edge).
                    if (candidate.DistanceTo(onFace) >= tolerance)
                        continue;

                    if (face.IsPointOnFace(u, v2) != PointFaceRelation.Exterior)
                        output.Add(onFace);
                }
            }

            return output;
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

        private static List<double> BuildAxisValues(double min, double max, double spacing)
        {
            var values = new List<double>();
            if (spacing <= 0) return values;
            double epsilon = spacing * 1e-9;
            for (double value = min; value <= max + epsilon; value += spacing)
                values.Add(value);
            return values;
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
