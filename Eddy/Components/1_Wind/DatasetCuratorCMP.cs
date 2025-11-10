using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Eddy.Properties;
using EddyLib;
using System.IO;

namespace Eddy
{
    public class DatasetCuratorCMP : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the DatasetCuratorCMP class.
        /// </summary>
        public DatasetCuratorCMP()
            : base("Dataset Curator", "DataCurator",
                "Compute wind dataset features: SDF, building height, relative Z, normalized wind speed, coordinates, and direction components",
                "Eddy3d", "4 | ML")
        {
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override Bitmap Icon => Resources.Eddy_dataset; // Replace with appropriate icon

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{A7B8C9D0-1E2F-3A4B-5C6D-7E8F9A0B1C2D}");

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points",
                "List of 3D points. Each point's Z must be absolute elevation (m). Order defines output order.",
                GH_ParamAccess.list);
            pManager.AddGeometryParameter("Buildings", "Buildings",
                "List of Brep or Mesh objects representing buildings. Used for SDF and building-height lookup.",
                GH_ParamAccess.list);
            pManager.AddTextParameter("case_dir", "case_dir",
                "Directory path where Dataset folder will be created. Required.",
                GH_ParamAccess.item);
            pManager.AddNumberParameter("U_ref", "U_ref",
                "Reference wind speed (m/s). Optional. Default = 3.0",
                GH_ParamAccess.item, 3.0);
            pManager.AddNumberParameter("z_ref", "z_ref",
                "Reference height for log-law (m). Optional. Default = 10.0",
                GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("pedestrian_level", "pedestrian_level",
                "Pedestrian mount height (m) added to relative Z. Optional. Default = 1.8",
                GH_ParamAccess.item, 1.8);
            pManager.AddNumberParameter("wind_dir", "wind_dir",
                "Wind direction in degrees. 0 => (x=0,y=-1). Positive clockwise. Optional. Default = 0.0",
                GH_ParamAccess.item, 0.0);

            pManager[0].Optional = false;
            pManager[1].Optional = false;
            pManager[2].Optional = false;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("SDF", "SDF",
                "Signed distance (m) from point to nearest building surface. Positive = outside, Negative = inside.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Bldg_height", "Bldg_height",
                "Maximum building top Z (m) under the point's XY footprint. 0 if none.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Z_relative", "Z_relative",
                "Relative sampling height (m): point_Z - domain_min_Z + pedestrian_level.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("U_at_z", "U_at_z",
                "Local inlet speed at Z_relative (m/s) computed as U_ref * ln(Z_relative)/ln(z_ref). NaN if invalid.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("X_coords", "X_coords",
                "X coordinate (m) of each input point.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Y_coords", "Y_coords",
                "Y coordinate (m) of each input point.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("dir_sin", "dir_sin",
                "Wind X-component (sin) following convention: 0° => 0.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("dir_cos", "dir_cos",
                "Wind Y-component (−cos) following convention: 0° => -1.",
                GH_ParamAccess.list);
        }

        /// <summary>
        /// Resolve geometry to Breps and Meshes
        /// </summary>
        private List<Tuple<string, object>> ResolveToGeometry(GeometryBase geometry)
        {
            var result = new List<Tuple<string, object>>();

            if (geometry == null)
                return result;

            if (geometry is Brep brep && brep.IsValid)
            {
                result.Add(new Tuple<string, object>("brep", brep));
                return result;
            }

            if (geometry is Mesh mesh && mesh.IsValid)
            {
                result.Add(new Tuple<string, object>("mesh", mesh));
                return result;
            }

            if (geometry is Surface surface)
            {
                var brepFromSurface = Brep.CreateFromSurface(surface);
                if (brepFromSurface != null && brepFromSurface.IsValid)
                {
                    result.Add(new Tuple<string, object>("brep", brepFromSurface));
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Compute signed distance and building height for a point
        /// </summary>
        private Tuple<double, double, bool> ComputeSDFAndHeight(Point3d point, List<Tuple<string, object>> geometryList)
        {
            double minDist = double.MaxValue;
            double heightHere = 0.0;
            bool inside = false;
            Point3d xy = new Point3d(point.X, point.Y, 0.0);

            foreach (var geomTuple in geometryList)
            {
                string kind = geomTuple.Item1;
                object geom = geomTuple.Item2;

                if (kind == "brep")
                {
                    Brep brep = (Brep)geom;

                    // Check if point is inside
                    if (brep.IsPointInside(point, 1e-6, true))
                    {
                        inside = true;
                    }

                    // Get closest point
                    Point3d closestPoint = brep.ClosestPoint(point);
                    double dist = point.DistanceTo(closestPoint);
                    if (dist < minDist)
                        minDist = dist;

                    // Get building height
                    BoundingBox bbox = brep.GetBoundingBox(true);
                    if (bbox.Contains(xy))
                    {
                        double h = bbox.Max.Z;
                        if (h > heightHere)
                            heightHere = h;
                    }
                }
                else if (kind == "mesh")
                {
                    Mesh mesh = (Mesh)geom;

                    // Check if point is inside
                    if (mesh.IsPointInside(point, 1e-6, true))
                    {
                        inside = true;
                    }

                    // Get closest point
                    Point3d closestPoint = mesh.ClosestPoint(point);
                    double dist = point.DistanceTo(closestPoint);
                    if (dist < minDist)
                        minDist = dist;

                    // Get building height
                    BoundingBox bbox = mesh.GetBoundingBox(true);
                    if (bbox.Contains(xy))
                    {
                        double h = bbox.Max.Z;
                        if (h > heightHere)
                            heightHere = h;
                    }
                }
            }

            if (minDist == double.MaxValue)
                minDist = 0.0;

            return new Tuple<double, double, bool>(minDist, heightHere, inside);
        }

        /// <summary>
        /// Round value to specified decimals, preserving NaN
        /// </summary>
        private double SafeRound(double value, int decimals)
        {
            if (double.IsNaN(value))
                return double.NaN;
            return Math.Round(value, decimals);
        }

        /// <summary>
        /// Clamp value between min and max
        /// </summary>
        private double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var points = new List<Point3d>();
            var geometryList = new List<GeometryBase>();
            string caseDir = string.Empty;
            double uRef = 3.0;
            double zRef = 10.0;
            double pedestrianLevel = 1.8;
            double windDir = 0.0;

            // Get inputs
            if (!DA.GetDataList(0, points))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No points provided");
                return;
            }

            if (!DA.GetDataList(1, geometryList))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No buildings provided");
                return;
            }

            if (!DA.GetData(2, ref caseDir) || string.IsNullOrWhiteSpace(caseDir))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "case_dir is required");
                return;
            }

            bool uRefProvided = DA.GetData(3, ref uRef);
            DA.GetData(4, ref zRef);
            DA.GetData(5, ref pedestrianLevel);
            DA.GetData(6, ref windDir);

            // Validate inputs
            if (points.Count == 0 || geometryList.Count == 0)
            {
                Message = "Missing Points or Buildings";
                return;
            }

            // Resolve geometry
            var resolvedGeometry = new List<Tuple<string, object>>();
            foreach (var geo in geometryList)
            {
                var resolved = ResolveToGeometry(geo);
                resolvedGeometry.AddRange(resolved);
            }

            if (resolvedGeometry.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid geometry found");
                return;
            }

            // Wind direction components: 0° => (x=0, y=-1), clockwise positive
            double angRad = windDir * Math.PI / 180.0;
            double dirXComp = -Math.Sin(angRad);
            double dirYComp = -Math.Cos(angRad);

            // Find minimum Z
            double minZ = double.MaxValue;
            foreach (var pt in points)
            {
                if (pt.Z < minZ)
                    minZ = pt.Z;
            }

            // Output lists
            var sdfList = new List<double>();
            var bldgHeightList = new List<double>();
            var zRelativeList = new List<double>();
            var uAtZList = new List<double>();
            var xCoordsList = new List<double>();
            var yCoordsList = new List<double>();
            var dirSinList = new List<double>();
            var dirCosList = new List<double>();

            // Process each point
            foreach (var pt in points)
            {
                // Coordinates
                xCoordsList.Add(SafeRound(pt.X, 2));
                yCoordsList.Add(SafeRound(pt.Y, 2));

                // Direction components (clip and round to 6 decimals)
                dirSinList.Add(Math.Round(Clamp(dirXComp, -1.0, 1.0), 6));
                dirCosList.Add(Math.Round(Clamp(dirYComp, -1.0, 1.0), 6));

                // Compute SDF and building height
                var result = ComputeSDFAndHeight(pt, resolvedGeometry);
                double minDist = result.Item1;
                double heightHere = result.Item2;
                bool inside = result.Item3;

                sdfList.Add(SafeRound(inside ? -minDist : minDist, 2));
                bldgHeightList.Add(SafeRound(heightHere, 2));

                // Z_relative
                double sensorAbs = pt.Z;
                double mount = sensorAbs - minZ + pedestrianLevel;
                zRelativeList.Add(SafeRound(mount, 2));

                // U_at_z
                double uAtZ;
                try
                {
                    if (mount <= 0 || zRef <= 0)
                    {
                        uAtZ = double.NaN;
                    }
                    else
                    {
                        double denom = Math.Log(zRef);
                        double ratio = Math.Log(mount) / denom;
                        uAtZ = uRef * ratio;
                    }
                }
                catch
                {
                    uAtZ = double.NaN;
                }

                // Round and normalize by U_ref if provided
                double uAtZRounded = SafeRound(uAtZ, 2);
                if (uRefProvided && uRef != 0.0 && !double.IsNaN(uAtZRounded))
                {
                    uAtZList.Add(SafeRound(uAtZRounded / uRef, 2));
                }
                else
                {
                    uAtZList.Add(uAtZRounded);
                }
            }

            // Set outputs
            DA.SetDataList(0, sdfList);
            DA.SetDataList(1, bldgHeightList);
            DA.SetDataList(2, zRelativeList);
            DA.SetDataList(3, uAtZList);
            DA.SetDataList(4, xCoordsList);
            DA.SetDataList(5, yCoordsList);
            DA.SetDataList(6, dirSinList);
            DA.SetDataList(7, dirCosList);

            // Set message
            if (!uRefProvided)
            {
                Message = $"Points processed: {points.Count}\nU_ref not provided - U_at_z left un-normalized";
            }
            else if (uRef == 0.0)
            {
                Message = $"Points processed: {points.Count}";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "U_ref == 0.0 – cannot normalize U_at_z");
            }
            else
            {
                Message = $"Points processed: {points.Count}";
            }

            // Ensure Dataset folder exists in the specified case directory
            EnsureDatasetFolderExists(caseDir);
        }

        /// <summary>
        /// Ensure a Dataset folder exists in the specified directory.
        /// Returns the created/existing folder path, or null on failure.
        /// </summary>
        private string EnsureDatasetFolderExists(string baseDirectory)
        {
            try
            {
                if (!Directory.Exists(baseDirectory))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Base directory does not exist: {baseDirectory}");
                    return null;
                }

                var datasetDir = Path.Combine(baseDirectory, "Dataset");
                if (!Directory.Exists(datasetDir))
                    Directory.CreateDirectory(datasetDir);

                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Dataset folder ensured at: {datasetDir}");
                return datasetDir;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to create Dataset folder: {ex.Message}");
                return null;
            }
        }
    }
}