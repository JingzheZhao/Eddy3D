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
            : base("Brep to Grid Points", "Brep2Grid",
                "Create regular grid points on Brep surfaces",
                "Eddy3d", "1 | Wind")
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
            pManager.AddGeometryParameter("Breps", "B", "Brep surfaces, surfaces, or meshes to create grid on", GH_ParamAccess.list);
            pManager.AddNumberParameter("Spacing", "S", "Grid spacing in meters", GH_ParamAccess.item, 10.0);
            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item, true);
            pManager[0].Optional = false;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        ///     Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Grid points on surfaces", GH_ParamAccess.list);
            pManager.AddTextParameter("Status", "S", "Status message", GH_ParamAccess.item);
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
        ///     Create regular grid on Brep surfaces - matches Python implementation exactly
        /// </summary>
        private List<Point3d> CreateGridOnBreps(List<Brep> brepList, double spacing)
        {
            if (brepList == null || brepList.Count == 0 || spacing <= 0)
                return new List<Point3d>();

            // Get combined bounding box of all breps
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

            var points = new List<Point3d>();

            // Grid bounds
            double xMin = bbox.Min.X;
            double xMax = bbox.Max.X;
            double yMin = bbox.Min.Y;
            double yMax = bbox.Max.Y;
            double zMin = bbox.Min.Z - spacing;
            double zMax = bbox.Max.Z + spacing;

            // Fixed tolerance like Python version
            const double tolerance = 0.1;

            // Generate grid points - exact Python logic
            for (double x = xMin; x <= xMax; x += spacing)
            {
                for (double y = yMin; y <= yMax; y += spacing)
                {
                    // Create vertical ray
                    Point3d rayStart = new Point3d(x, y, zMin);
                    Point3d rayEnd = new Point3d(x, y, zMax);
                    Vector3d rayDir = rayEnd - rayStart;
                    Ray3d ray = new Ray3d(rayStart, rayDir);

                    // Test intersection with each brep
                    foreach (var brep in brepList)
                    {
                        if (brep == null)
                            continue;

                        // Shoot ray - get max 1 hit per geometry like Python
                        var intersections = Intersection.RayShoot(ray, new[] { brep }, 1);

                        if (intersections != null && intersections.Length > 0)
                        {
                            foreach (var pt in intersections)
                            {
                                // Get closest point on brep
                                Point3d closest = brep.ClosestPoint(pt);

                                if (closest != Point3d.Unset && pt.DistanceTo(closest) < tolerance)
                                {
                                    // Check if point is on a face
                                    bool isOnFace = false;

                                    foreach (var face in brep.Faces)
                                    {
                                        double u, v;
                                        if (face.ClosestPoint(pt, out u, out v))
                                        {
                                            Point3d facePt = face.PointAt(u, v);

                                            if (pt.DistanceTo(facePt) < tolerance)
                                            {
                                                var relation = face.IsPointOnFace(u, v);
                                                if (relation != PointFaceRelation.Exterior)
                                                {
                                                    isOnFace = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }

                                    if (isOnFace)
                                    {
                                        points.Add(pt);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return points;
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
                DA.SetData(1, "Idle");
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