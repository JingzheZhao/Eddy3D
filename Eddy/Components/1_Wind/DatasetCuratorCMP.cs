using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Eddy.Properties;
using EddyLib;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace Eddy
{
    public class DatasetCuratorCMP : GH_Component
    {
        public DatasetCuratorCMP()
            : base("Dataset Curator", "DataCurator",
                "Compute wind dataset features: SDF, building height, relative Z, normalized wind speed, coordinates, and direction components",
                "Eddy3D", "4 | ML")
        {
        }

        protected override Bitmap Icon => Resources.Eddy_dataset;

        public override Guid ComponentGuid => new Guid("{A7B8C9D0-1E2F-3A4B-5C6D-7E8F9A0B1C2D}");

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
                "Wind direction(s) in degrees. 0 => (x=0,y=-1). Positive clockwise. Optional. Default = 0.0",
                GH_ParamAccess.list, 0.0);
            pManager.AddBooleanParameter("Run", "Run", "Write CSV and Batch files when true. Recommended to use a toggle.", GH_ParamAccess.item, false);

            pManager[0].Optional = false;
            pManager[1].Optional = false;
            pManager[2].Optional = false;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("SDF", "SDF",
                "Signed distance (m) from point to nearest building surface. Always negative: 0 at surface, more negative further away.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Bldg_height", "Bldg_height",
                "Maximum building top Z (m) under the point's XY footprint. 0 if none.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Z_relative", "Z_relative",
                "Relative sampling height (m): point_Z - domain_min_Z + pedestrian_level.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("U_over_Uref", "U_over_Uref",
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
            pManager.AddTextParameter("CSV_Path", "CSV", "Paths to the generated CSV files.", GH_ParamAccess.list);
        }

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

        private Tuple<double, double, bool> ComputeSDFAndHeight(Point3d point, List<Tuple<string, object>> geometryList)
        {
            double minDist = double.MaxValue;
            double heightHere = 0.0;
            bool inside = false;


            foreach (var geomTuple in geometryList)
            {
                string kind = geomTuple.Item1;
                object geom = geomTuple.Item2;

                if (kind == "brep")
                {
                    Brep brep = (Brep)geom;

                    if (brep.IsPointInside(point, 1e-6, true))
                    {
                        inside = true;
                    }

                    Point3d closestPoint = brep.ClosestPoint(point);
                    double dist = point.DistanceTo(closestPoint);
                    if (dist < minDist)
                        minDist = dist;

                    // Optimization: Check XY Bounding Box first
                    BoundingBox bbox = brep.GetBoundingBox(true);
                    if (point.X >= bbox.Min.X && point.X <= bbox.Max.X &&
                        point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y)
                    {
                        // Ray intersection for accurate height (handles courtyards)
                        var verticalLine = new Line(new Point3d(point.X, point.Y, -1000), new Point3d(point.X, point.Y, 1000));
                        Curve[] overlaps;
                        Point3d[] intersectionPts;
                        bool hit = Rhino.Geometry.Intersect.Intersection.CurveBrep(verticalLine.ToNurbsCurve(), brep, 1e-6, out overlaps, out intersectionPts);

                        if (hit && intersectionPts != null)
                        {
                            foreach (var pt in intersectionPts)
                            {
                                if (pt.Z > heightHere)
                                    heightHere = pt.Z;
                            }
                        }
                    }
                }
                else if (kind == "mesh")
                {
                    Mesh mesh = (Mesh)geom;

                    if (mesh.IsPointInside(point, 1e-6, true))
                    {
                        inside = true;
                    }

                    Point3d closestPoint = mesh.ClosestPoint(point);
                    double dist = point.DistanceTo(closestPoint);
                    if (dist < minDist)
                        minDist = dist;

                    // Optimization: Check XY Bounding Box first
                    BoundingBox bbox = mesh.GetBoundingBox(true);
                    if (point.X >= bbox.Min.X && point.X <= bbox.Max.X &&
                        point.Y >= bbox.Min.Y && point.Y <= bbox.Max.Y)
                    {
                        // Ray intersection for Mesh - Shoot from sky down to find roof
                        var verticalRay = new Ray3d(new Point3d(point.X, point.Y, 1000), -Vector3d.ZAxis);
                        double t = Rhino.Geometry.Intersect.Intersection.MeshRay(mesh, verticalRay);

                        if (t >= 0.0)
                        {
                            // Ray start is 1000. Direction is down (-1).
                            // Point = Start + t * Dir
                            // Z = 1000 + t * (-1) = 1000 - t
                            double hitZ = 1000.0 - t;
                            if (hitZ > heightHere)
                                heightHere = hitZ;
                        }
                    }
                }
            }

            if (minDist == double.MaxValue)
                minDist = 0.0;

            return new Tuple<double, double, bool>(minDist, heightHere, inside);
        }

        private double SafeRound(double value, int decimals)
        {
            if (double.IsNaN(value))
                return double.NaN;
            return Math.Round(value, decimals);
        }

        private double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var points = new List<Point3d>();
            var geometryList = new List<GeometryBase>();
            string caseDir = string.Empty;
            double uRef = 3.0;
            double zRef = 10.0;
            double pedestrianLevel = 1.8;
            List<double> windDirs = new List<double>();
            bool run = false;

            if (!DA.GetDataList(0, points)) return;
            if (!DA.GetDataList(1, geometryList)) return;
            if (!DA.GetData(2, ref caseDir) || string.IsNullOrWhiteSpace(caseDir))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "case_dir is required");
                return;
            }

            bool uRefProvided = DA.GetData(3, ref uRef);
            DA.GetData(4, ref zRef);
            DA.GetData(5, ref pedestrianLevel);
            DA.GetDataList(6, windDirs);
            DA.GetData(7, ref run);

            if (windDirs.Count == 0) windDirs.Add(0.0);

            if (!run)
            {
                Message = "Toggle 'Run' to start";
                return;
            }

            if (points.Count == 0 || geometryList.Count == 0)
            {
                Message = "Connect Points & Buildings";
                return;
            }

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

            var rtree = new RTree();
            for (int i = 0; i < resolvedGeometry.Count; i++)
            {
                GeometryBase g = (GeometryBase)resolvedGeometry[i].Item2;
                rtree.Insert(g.GetBoundingBox(true), i);
            }

            double minZ = double.MaxValue;
            foreach (var pt in points)
            {
                if (pt.Z < minZ)
                    minZ = pt.Z;
            }

            int count = points.Count;
            var sdfArr = new double[count];
            var bldgHeightArr = new double[count];
            var zRelativeArr = new double[count];
            var uAtZArr = new double[count];
            var xCoordsArr = new double[count];
            var yCoordsArr = new double[count];

            // Local copies for thread safety
            double loc_pedestrianLevel = pedestrianLevel;
            double loc_minZ = minZ;
            double loc_zRef = zRef;
            double loc_uRef = uRef;
            bool loc_uRefProvided = uRefProvided;
            var loc_resolvedGeometry = resolvedGeometry;
            var loc_rtree = rtree;

            System.Threading.Tasks.Parallel.For(0, count, i =>
            {
                Point3d pt = points[i];
                xCoordsArr[i] = SafeRound(pt.X, 2);
                yCoordsArr[i] = SafeRound(pt.Y, 2);

                double minDist = double.MaxValue;
                double heightHere = 0.0;

                // 1. RTree Height Search
                var searchBox = new BoundingBox(pt.X - 1e-6, pt.Y - 1e-6, -1e10, pt.X + 1e-6, pt.Y + 1e-6, 1e10);

                loc_rtree.Search(searchBox, (sender, args) =>
                {
                    int geomIndex = args.Id;
                    var geomTuple = loc_resolvedGeometry[geomIndex];
                    string kind = geomTuple.Item1;

                    if (kind == "brep")
                    {
                        Brep brep = (Brep)geomTuple.Item2;
                        BoundingBox bbox = brep.GetBoundingBox(true);
                        if (pt.X >= bbox.Min.X && pt.X <= bbox.Max.X && pt.Y >= bbox.Min.Y && pt.Y <= bbox.Max.Y)
                        {
                            var verticalLine = new Line(new Point3d(pt.X, pt.Y, -1000), new Point3d(pt.X, pt.Y, 1000));
                            Curve[] overlaps;
                            Point3d[] intersectionPts;
                            bool hit = Rhino.Geometry.Intersect.Intersection.CurveBrep(verticalLine.ToNurbsCurve(), brep, 1e-6, out overlaps, out intersectionPts);

                            if (hit && intersectionPts != null)
                            {
                                foreach (var p in intersectionPts)
                                {
                                    if (p.Z > heightHere) heightHere = p.Z;
                                }
                            }
                        }
                    }
                    else if (kind == "mesh")
                    {
                        Mesh mesh = (Mesh)geomTuple.Item2;
                        BoundingBox bbox = mesh.GetBoundingBox(true);
                        if (pt.X >= bbox.Min.X && pt.X <= bbox.Max.X && pt.Y >= bbox.Min.Y && pt.Y <= bbox.Max.Y)
                        {
                            var verticalRay = new Ray3d(new Point3d(pt.X, pt.Y, 1000), -Vector3d.ZAxis);
                            double tVal = Rhino.Geometry.Intersect.Intersection.MeshRay(mesh, verticalRay);
                            if (tVal >= 0.0)
                            {
                                double hitZ = 1000.0 - tVal;
                                if (hitZ > heightHere) heightHere = hitZ;
                            }
                        }
                    }
                });

                // 2. SDF Calculation
                foreach (var geomTuple in loc_resolvedGeometry)
                {
                    string kind = geomTuple.Item1;
                    GeometryBase geom = (GeometryBase)geomTuple.Item2;

                    if (kind == "brep")
                    {
                        Brep brep = (Brep)geom;
                        BoundingBox bbox = brep.GetBoundingBox(true);
                        double boxDist = bbox.ClosestPoint(pt).DistanceTo(pt);
                        if (boxDist < minDist)
                        {
                            Point3d cp = brep.ClosestPoint(pt);
                            double d = cp.DistanceTo(pt);
                            if (d < minDist) minDist = d;
                        }
                    }
                    else if (kind == "mesh")
                    {
                        Mesh mesh = (Mesh)geom;
                        BoundingBox bbox = mesh.GetBoundingBox(true);
                        double boxDist = bbox.ClosestPoint(pt).DistanceTo(pt);
                        if (boxDist < minDist)
                        {
                            Point3d cp = mesh.ClosestPoint(pt);
                            double d = cp.DistanceTo(pt);
                            if (d < minDist) minDist = d;
                        }
                    }
                }

                if (minDist == double.MaxValue) minDist = 0.0;
                sdfArr[i] = SafeRound(-minDist, 2);
                bldgHeightArr[i] = SafeRound(heightHere, 2);

                double sensorAbs = pt.Z;
                double mount = sensorAbs - loc_minZ + loc_pedestrianLevel;
                zRelativeArr[i] = SafeRound(mount, 2);

                double uAtZ = double.NaN;
                if (mount > 0 && loc_zRef > 0)
                {
                    double denom = Math.Log(loc_zRef);
                    double ratio = Math.Log(mount) / denom;
                    uAtZ = loc_uRef * ratio;
                }

                double uAtZRounded = SafeRound(uAtZ, 2);
                if (loc_uRefProvided && loc_uRef != 0.0 && !double.IsNaN(uAtZRounded))
                    uAtZArr[i] = SafeRound(uAtZRounded / loc_uRef, 2);
                else
                    uAtZArr[i] = uAtZRounded;
            });

            // Populate Output Lists
            DA.SetDataList(0, sdfArr);
            DA.SetDataList(1, bldgHeightArr);
            DA.SetDataList(2, zRelativeArr);
            DA.SetDataList(3, uAtZArr);
            DA.SetDataList(4, xCoordsArr);
            DA.SetDataList(5, yCoordsArr);

            // Compute DirSin/Cos for first direction
            var dirSinList = new List<double>();
            var dirCosList = new List<double>();
            // ... (rest is same)

            if (!uRefProvided)
            {
                Message = $"Points processed: {points.Count}\nU_ref not provided - U_over_Uref left un-normalized";
            }
            else if (uRef == 0.0)
            {
                Message = $"Points processed: {points.Count}";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "U_ref == 0.0 – cannot normalize U_over_Uref");
            }
            else
            {
                Message = $"Points processed: {points.Count}";
            }

            string datasetFolder = EnsureDatasetFolderExists(caseDir);
            string caseName = Path.GetFileName(caseDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var csvPaths = new List<string>();

            // Explicitly use UTF-8 without BOM for maximum compatibility with Python
            var utf8NoBom = new UTF8Encoding(false);

            for (int d = 0; d < windDirs.Count; d++)
            {
                double currentDir = windDirs[d];
                double rad = currentDir * Math.PI / 180.0;
                double currentDirSin = Math.Round(Clamp(-Math.Sin(rad), -1.0, 1.0), 6);
                double currentDirCos = Math.Round(Clamp(-Math.Cos(rad), -1.0, 1.0), 6);

                string datasetFile = Path.Combine(datasetFolder, $"{caseName}_{currentDir}.csv");

                var sb = new StringBuilder();
                sb.AppendLine("X,Y,Z_relative,SDF,Bldg_height,U_over_Uref,dir_sin,dir_cos");
                for (int i = 0; i < points.Count; i++)
                {
                    sb.AppendLine($"{xCoordsArr[i]},{yCoordsArr[i]},{zRelativeArr[i]},{sdfArr[i]},{bldgHeightArr[i]},{uAtZArr[i]},{currentDirSin},{currentDirCos}");
                }

                try
                {
                    File.WriteAllText(datasetFile, sb.ToString(), utf8NoBom);
                    csvPaths.Add(datasetFile);
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to save CSV for {currentDir}°: {ex.Message}");
                }

                if (d == 0)
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        dirSinList.Add(currentDirSin);
                        dirCosList.Add(currentDirCos);
                    }
                }
            }

            DA.SetDataList(6, dirSinList);
            DA.SetDataList(7, dirCosList);
            DA.SetDataList(8, csvPaths);

            if (csvPaths.Count > 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Successfully generated {csvPaths.Count} CSV files in: {datasetFolder}");
            }

            EnsureScriptsFolderExists(caseDir);
            EnsureBatchFileExists(caseDir);
        }

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

                // No longer remarking here to keep UI clean
                return datasetDir;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to create Dataset folder: {ex.Message}");
                return null;
            }
        }

        private void EnsureScriptsFolderExists(string baseDirectory)
        {
            try
            {
                if (!Directory.Exists(baseDirectory)) return;
                var scriptsDir = Path.Combine(baseDirectory, "Scripts");
                if (!Directory.Exists(scriptsDir))
                    Directory.CreateDirectory(scriptsDir);

                var utf8NoBom = new UTF8Encoding(false);

                string scriptPath = Path.Combine(scriptsDir, "add_mag_u.py");
                if (!File.Exists(scriptPath))
                {
                    File.WriteAllText(scriptPath, MagUScriptContent, utf8NoBom);
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Created script at: {scriptPath}");
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to create Scripts folder/file: {ex.Message}");
            }
        }

        private void EnsureBatchFileExists(string baseDirectory)
        {
            try
            {
                if (!Directory.Exists(baseDirectory)) return;
                var utf8NoBom = new UTF8Encoding(false);

                string batchPath = Path.Combine(baseDirectory, "Scripts", "save_results_to_dataset.bat");
                if (!File.Exists(batchPath))
                {
                    File.WriteAllText(batchPath, BatchFileContent, utf8NoBom);
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Created batch file at: {batchPath}");
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to create batch file: {ex.Message}");
            }
        }

        private const string BatchFileContent = @"@echo off
setlocal enabledelayedexpansion
cd /d ""%~dp0..""

set ""LOG=save_results.log""
echo ================================================== > ""!LOG!""
echo Starting Batch Processing (CSV Mode)... >> ""!LOG!""
echo Starting Batch Processing (CSV Mode)...

for %%I in (.) do set ""CaseName=%%~nxI""

set ""Script=Scripts\add_mag_u.py""

echo.
echo --------------------------------------------------
echo Processing All Directions...
echo -------------------------------------------------- >> ""!LOG!""
echo Processing All Directions... >> ""!LOG!""

REM Argument 1: CaseName (to find CSVs), Argument 2: Case Directory
python ""%Script%"" ""!CaseName!"" ""."" >> ""!LOG!"" 2>&1

echo.
echo ==================================================
echo Batch Processing Complete.
echo Check !LOG! for details.
echo ==================================================
";

        private const string MagUScriptContent = @"import csv
import math
import sys
import os
import re

# Reference velocity used for normalization.
# mag_U  is divided by UREF          (units: m/s  -> dimensionless U/Uref)
# k      is divided by UREF**2       (units: m^2/s^2 -> dimensionless k/Uref^2)
UREF = 5.0

SENTINEL_THRESHOLD = 1e100  # OpenFOAM -DBL_MAX ~ -1.7976931e+307


def _strip_comments(text):
    return re.sub(r'#.*', '', text)


def parse_u_file(u_file_path):
    """"""Parse an OpenFOAM vector probe file ('U').

    Format (single timestep):
        # header lines starting with '#'
        600 (Ux0 Uy0 Uz0) (Ux1 Uy1 Uz1) ...

    Using regex on parenthesised triplets inherently skips the leading
    timestamp token, avoiding the off-by-one bug of add_mag_u.py.

    Returns a list of normalized magnitudes (|U| / UREF) or NaN for
    sentinel / malformed entries.
    """"""
    if not os.path.exists(u_file_path):
        return None

    with open(u_file_path, 'r', encoding='utf-8') as f:
        full_text = f.read()

    cleaned = _strip_comments(full_text)
    triplets = re.findall(r'\(([^)]+)\)', cleaned)

    magnitudes = []
    for trip in triplets:
        parts = trip.split()
        if len(parts) != 3:
            magnitudes.append(float('nan'))
            continue
        try:
            u, v, w = float(parts[0]), float(parts[1]), float(parts[2])
            if (math.isnan(u) or math.isinf(u) or abs(u) > SENTINEL_THRESHOLD
                    or math.isnan(v) or math.isinf(v) or abs(v) > SENTINEL_THRESHOLD
                    or math.isnan(w) or math.isinf(w) or abs(w) > SENTINEL_THRESHOLD):
                magnitudes.append(float('nan'))
            else:
                magnitudes.append(math.sqrt(u * u + v * v + w * w) / UREF)
        except Exception:
            magnitudes.append(float('nan'))

    print(f'  -> Parsed {len(magnitudes)} U vectors.')
    return magnitudes


def parse_k_file(k_file_path):
    """"""Parse an OpenFOAM scalar probe file ('k').

    Format (single timestep):
        # header lines starting with '#'
        600 k0 k1 k2 ...

    The first whitespace-separated token is the timestamp; we drop it.
    k is normalized by UREF**2 so its magnitude range is compatible with
    the mag_U channel (U/Uref) in the training targets.

    Returns a list of normalized k values (k / UREF^2) or NaN for
    sentinel / malformed entries.
    """"""
    if not os.path.exists(k_file_path):
        return None

    with open(k_file_path, 'r', encoding='utf-8') as f:
        full_text = f.read()

    cleaned = _strip_comments(full_text)
    # Defensive: strip any accidental parens
    cleaned = cleaned.replace('(', ' ').replace(')', ' ')
    tokens = cleaned.split()

    if not tokens:
        return []

    # Drop timestamp (first token).
    tokens = tokens[1:]

    k_norm = UREF * UREF
    ks = []
    for t in tokens:
        try:
            val = float(t)
            if math.isnan(val) or math.isinf(val) or abs(val) > SENTINEL_THRESHOLD:
                ks.append(float('nan'))
            else:
                ks.append(val / k_norm)
        except Exception:
            ks.append(float('nan'))

    print(f'  -> Parsed {len(ks)} k scalars.')
    return ks


def main():
    if len(sys.argv) < 3:
        print('Usage: python add_uk.py <case_name> <case_dir>')
        return

    case_name = sys.argv[1]
    case_dir = sys.argv[2]

    # Discover direction subdirectories (names that look like floats).
    possible_dirs = [
        d for d in os.listdir(case_dir)
        if os.path.isdir(os.path.join(case_dir, d)) and d.replace('.', '', 1).isdigit()
    ]
    directions = sorted(possible_dirs, key=float)

    for direction in directions:
        csv_path = os.path.join(case_dir, 'Dataset', f'{case_name}_{direction}.csv')
        pp_path = os.path.join(case_dir, direction, 'postProcessing', 'ttt')

        if not os.path.exists(csv_path) or not os.path.exists(pp_path):
            continue

        # Latest time directory.
        subdirs = [d for d in os.listdir(pp_path) if os.path.isdir(os.path.join(pp_path, d))]
        numeric_dirs = sorted(
            [d for d in subdirs if d.replace('.', '', 1).isdigit()],
            key=float, reverse=True,
        )
        if not numeric_dirs:
            continue

        time_dir = numeric_dirs[0]
        u_file_path = os.path.join(pp_path, time_dir, 'U')
        k_file_path = os.path.join(pp_path, time_dir, 'k')

        if not os.path.exists(u_file_path) or not os.path.exists(k_file_path):
            print(f'\n--- Skipping {direction} deg: missing U or k file ---')
            continue

        print(f'\n--- Processing {direction} deg ---')
        magnitudes = parse_u_file(u_file_path)
        ks = parse_k_file(k_file_path)
        if not magnitudes or ks is None:
            continue

        if abs(len(magnitudes) - len(ks)) > 0:
            print(f'  WARNING: U/k length mismatch '
                  f'(U={len(magnitudes)}, k={len(ks)}). Using the shorter.')

        # Read CSV.
        try:
            with open(csv_path, 'r', newline='', encoding='utf-8') as f:
                reader = csv.DictReader(f)
                rows = list(reader)
                fieldnames = list(reader.fieldnames)
        except Exception as e:
            print(f'  Error reading CSV: {e}')
            continue

        if 'mag_U' not in fieldnames:
            fieldnames.append('mag_U')
        if 'k' not in fieldnames:
            fieldnames.append('k')

        limit = min(len(rows), len(magnitudes), len(ks))
        new_rows = []
        dropped = 0
        for i in range(limit):
            row = rows[i]
            mag = magnitudes[i]
            k_val = ks[i]
            try:
                sdf = float(row.get('SDF', 0))
            except Exception:
                sdf = 0.0

            mag_nan = math.isnan(mag)
            k_nan = math.isnan(k_val)

            if mag_nan or k_nan:
                # Mirror legacy behavior: inside/near buildings (SDF<10) -> zero;
                # otherwise drop the point.
                if sdf < 10:
                    row['mag_U'] = 0.0 if mag_nan else mag
                    row['k'] = 0.0 if k_nan else k_val
                else:
                    dropped += 1
                    continue
            else:
                row['mag_U'] = mag
                row['k'] = k_val

            new_rows.append(row)

        # Write back.
        try:
            with open(csv_path, 'w', newline='', encoding='utf-8') as f:
                writer = csv.DictWriter(f, fieldnames=fieldnames)
                writer.writeheader()
                writer.writerows(new_rows)
        except Exception as e:
            print(f'  Error writing CSV: {e}')
            continue

        print(f'  -> Wrote {len(new_rows)} rows (dropped {dropped}) to '
              f'{os.path.basename(csv_path)}')


if __name__ == '__main__':
    main()
";
    }
}