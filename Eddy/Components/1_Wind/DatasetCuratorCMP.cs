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
                "Signed distance (m) from point to nearest building surface. Positive = outside, Negative = inside.",
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
                Message = "Run is false - Idle";
                return;
            }

            if (points.Count == 0 || geometryList.Count == 0)
            {
                Message = "Missing Points or Buildings";
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
                bool inside = false;

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
                         if (minDist > 0 && brep.IsPointInside(pt, 1e-6, true)) inside = true;
                         
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
                         if (minDist > 0 && mesh.IsPointInside(pt, 1e-6, true)) inside = true;
                         
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
                sdfArr[i] = SafeRound(inside ? -minDist : minDist, 2);
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

def parse_u_file(u_file_path):
    magnitudes = []
    if not os.path.exists(u_file_path):
        return None
    
    with open(u_file_path, 'r', encoding='utf-8') as f:
        full_text = f.read()
    
    # Remove header comments and parens
    data_text = re.sub(r'#.*', '', full_text)
    cleaned_text = data_text.replace('(', ' ').replace(')', ' ')
    tokens = cleaned_text.split()
    
    count = len(tokens) // 3
    print(f'  -> Parsed {count} vectors.')
    
    for i in range(0, len(tokens), 3):
        if i + 2 >= len(tokens): break
        try:
            u, v, w = float(tokens[i]), float(tokens[i+1]), float(tokens[i+2])
            if math.isnan(u) or math.isinf(u) or abs(u) > 1e100:
                mag = float('nan')
            else:
                mag = math.sqrt(u*u + v*v + w*w) / 5.0
        except:
            mag = float('nan')
        magnitudes.append(mag)
    return magnitudes

def main():
    if len(sys.argv) < 3:
        print('Usage: python add_mag_u.py <case_name> <case_dir>')
        return

    case_name = sys.argv[1]
    case_dir = sys.argv[2]
    
    # Discover directions based on subdirectories in case_dir
    possible_dirs = [d for d in os.listdir(case_dir) if os.path.isdir(os.path.join(case_dir, d)) and d.replace('.','',1).isdigit()]
    directions = sorted(possible_dirs, key=float)

    for direction in directions:
        csv_path = os.path.join(case_dir, 'Dataset', f'{case_name}_{direction}.csv')
        u_search_path = os.path.join(case_dir, direction, 'postProcessing', 'ttt')
        
        if not os.path.exists(csv_path) or not os.path.exists(u_search_path):
            continue

        # Find Latest Time
        subdirs = [d for d in os.listdir(u_search_path) if os.path.isdir(os.path.join(u_search_path, d))]
        numeric_dirs = sorted([d for d in subdirs if d.replace('.','',1).isdigit()], key=float, reverse=True)
        if not numeric_dirs: continue
        
        u_file_path = os.path.join(u_search_path, numeric_dirs[0], 'U')
        if not os.path.exists(u_file_path): continue

        print(f'\n--- Processing {direction} deg ---')
        magnitudes = parse_u_file(u_file_path)
        if not magnitudes: continue

        # Process CSV
        try:
            with open(csv_path, 'r', newline='', encoding='utf-8') as f:
                reader = csv.DictReader(f)
                rows = list(reader)
                fieldnames = reader.fieldnames

            if 'mag_U' not in fieldnames:
                fieldnames.append('mag_U')

            new_rows = []
            limit = min(len(rows), len(magnitudes))
            
            for i in range(limit):
                row = rows[i]
                mag = magnitudes[i]
                sdf = float(row.get('SDF', 0))
                
                if math.isnan(mag):
                    if sdf < 10: row['mag_U'] = 0.0
                    else: continue
                else:
                    row['mag_U'] = mag
                new_rows.append(row)

            # Write back to the SAME file
            with open(csv_path, 'w', newline='', encoding='utf-8') as f:
                writer = csv.DictWriter(f, fieldnames=fieldnames)
                writer.writeheader()
                writer.writerows(new_rows)
            
            print(f'  -> Results written to: {os.path.basename(csv_path)}')
        except Exception as e:
            print(f'  Error: {e}')

if __name__ == '__main__':
    main()
";
    }
}