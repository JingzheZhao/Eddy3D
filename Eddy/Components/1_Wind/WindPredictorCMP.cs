using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Eddy.Properties;
using EddyLib;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Eddy
{
    public class WindPredictorCMP : GH_Component
    {
        // ──────────────────────────────────────────────
        // Grid constants — must match training pipeline
        // ──────────────────────────────────────────────
        private const int IMG_H = 504;
        private const int IMG_W = 504;
        private const int X_CH = 8;

        private const double X_MIN = -485.0;
        private const double X_MAX = 521.0;
        private const double Y_MIN = -514.5;
        private const double Y_MAX = 491.5;
        private const double X_STEP = 2.0;
        private const double Y_STEP = 2.0;

        // ── Cached ONNX session ──
        private InferenceSession _session;
        private string _cachedOnnxPath;
        private bool _cachedUseGpu;

        // ── Native library resolver (registered once) ──
        private static bool _resolverRegistered;

        static WindPredictorCMP()
        {
            RegisterNativeResolver();
        }

        private static void RegisterNativeResolver()
        {
            if (_resolverRegistered) return;
            _resolverRegistered = true;

            // Find the directory where this assembly (Eddy.dll/gha) lives
            string assemblyDir = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);

            // The OnnxRuntime NuGet places native DLLs under runtimes/win-x64/native/
            string nativeDir = Path.Combine(assemblyDir, "runtimes", "win-x64", "native");

            // Register a resolver so the CLR can find onnxruntime.dll
            NativeLibrary.SetDllImportResolver(
                typeof(InferenceSession).Assembly,
                (libraryName, assembly, searchPath) =>
                {
                    // Try the runtimes subfolder first
                    string candidate = Path.Combine(nativeDir, libraryName);
                    if (!candidate.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                        candidate += ".dll";

                    if (File.Exists(candidate))
                    {
                        if (NativeLibrary.TryLoad(candidate, out IntPtr handle))
                            return handle;
                    }

                    // Fall back to default resolution
                    return IntPtr.Zero;
                });
        }

        public WindPredictorCMP()
            : base("Wind Predictor", "WindPredict",
                "Run ONNX wind-field prediction end-to-end.\n" +
                "Computes SDF, building height, Z_relative, U/Uref, direction features from geometry,\n" +
                "assembles the 8-channel input tensor, runs ONNX inference, and outputs predicted wind speeds.",
                "Eddy3D", "4 | ML")
        {
        }

        protected override Bitmap Icon => Resources.Eddy_dataset;

        public override Guid ComponentGuid => new Guid("{B8C9D0E1-2F3A-4B5C-6D7E-8F9A0B1C2D3E}");

        // ──────────────────────────────────────────────
        // INPUTS
        // ──────────────────────────────────────────────
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points",
                "List of 3D points. Each point's Z must be absolute elevation (m).",
                GH_ParamAccess.list);
            pManager.AddGeometryParameter("Buildings", "Buildings",
                "List of Brep or Mesh objects representing buildings.",
                GH_ParamAccess.list);
            pManager.AddTextParameter("ONNX Path", "ONNX",
                "Full file path to the exported .onnx model.",
                GH_ParamAccess.item);
            pManager.AddNumberParameter("U_ref", "U_ref",
                "Reference wind speed (m/s). Default = 3.0",
                GH_ParamAccess.item, 3.0);
            pManager.AddNumberParameter("z_ref", "z_ref",
                "Reference height for log-law (m). Default = 10.0",
                GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("pedestrian_level", "pedestrian_level",
                "Pedestrian mount height (m). Default = 1.8",
                GH_ParamAccess.item, 1.8);
            pManager.AddNumberParameter("wind_dir", "wind_dir",
                "Wind direction in degrees. 0 => (x=0,y=-1). Default = 0.0",
                GH_ParamAccess.list, 0.0);
            pManager.AddBooleanParameter("GPU", "GPU",
                "Use DirectML GPU acceleration. Falls back to CPU if unavailable. Default = true.",
                GH_ParamAccess.item, true);

            pManager[0].Optional = false;
            pManager[1].Optional = false;
            pManager[2].Optional = false;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        // ──────────────────────────────────────────────
        // OUTPUTS — X, Y, predicted wind speed
        // ──────────────────────────────────────────────
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("X", "X",
                "X coordinate (m) of each input point.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Y", "Y",
                "Y coordinate (m) of each input point.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Wind Speed", "W",
                "Predicted wind speed at each input point. NaN for points outside the 504×504 grid.",
                GH_ParamAccess.list);
        }

        // ──────────────────────────────────────────────
        // ONNX session management
        // ──────────────────────────────────────────────
        private InferenceSession GetSession(string onnxPath, bool useGpu)
        {
            if (_session != null && _cachedOnnxPath == onnxPath && _cachedUseGpu == useGpu)
                return _session;

            _session?.Dispose();

            var opts = new SessionOptions();
            opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            string activeProvider = "CPU";

            if (useGpu)
            {
                try
                {
                    opts.AppendExecutionProvider_DML(0);  // device 0 = default GPU
                    activeProvider = "DirectML (GPU)";
                }
                catch
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "DirectML GPU not available — falling back to CPU.");
                    activeProvider = "CPU (GPU fallback)";
                }
            }

            _session = new InferenceSession(onnxPath, opts);
            _cachedOnnxPath = onnxPath;
            _cachedUseGpu = useGpu;

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Provider: {activeProvider}");

            var inputMeta = _session.InputMetadata;
            foreach (var kv in inputMeta)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"ONNX input: \"{kv.Key}\" shape=[{string.Join(",", kv.Value.Dimensions)}]");
            }

            return _session;
        }

        // ──────────────────────────────────────────────
        // Geometry helpers (identical to DatasetCuratorCMP)
        // ──────────────────────────────────────────────
        private List<Tuple<string, object>> ResolveToGeometry(GeometryBase geometry)
        {
            var result = new List<Tuple<string, object>>();
            if (geometry == null) return result;

            if (geometry is Brep brep && brep.IsValid)
            {
                result.Add(Tuple.Create("brep", (object)brep));
                return result;
            }
            if (geometry is Mesh mesh && mesh.IsValid)
            {
                result.Add(Tuple.Create("mesh", (object)mesh));
                return result;
            }
            if (geometry is Surface surface)
            {
                var brepFromSurface = Brep.CreateFromSurface(surface);
                if (brepFromSurface != null && brepFromSurface.IsValid)
                {
                    result.Add(Tuple.Create("brep", (object)brepFromSurface));
                    return result;
                }
            }
            return result;
        }

        private static double SafeRound(double value, int decimals)
        {
            return double.IsNaN(value) ? double.NaN : Math.Round(value, decimals);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float SafeFloat(double v)
        {
            return (double.IsNaN(v) || double.IsInfinity(v)) ? 0f : (float)v;
        }

        // ──────────────────────────────────────────────
        // SolveInstance
        // ──────────────────────────────────────────────
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var points = new List<Point3d>();
            var geometryList = new List<GeometryBase>();
            string onnxPath = string.Empty;
            double uRef = 3.0;
            double zRef = 10.0;
            double pedestrianLevel = 1.8;
            var windDirs = new List<double>();
            bool useGpu = true;

            if (!DA.GetDataList(0, points)) return;
            if (!DA.GetDataList(1, geometryList)) return;
            if (!DA.GetData(2, ref onnxPath) || string.IsNullOrWhiteSpace(onnxPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "ONNX path is required");
                return;
            }
            if (!File.Exists(onnxPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"ONNX model not found: {onnxPath}");
                return;
            }

            bool uRefProvided = DA.GetData(3, ref uRef);
            DA.GetData(4, ref zRef);
            DA.GetData(5, ref pedestrianLevel);
            DA.GetDataList(6, windDirs);
            DA.GetData(7, ref useGpu);

            if (windDirs.Count == 0) windDirs.Add(0.0);

            if (points.Count == 0 || geometryList.Count == 0)
            {
                Message = "Missing Points or Buildings";
                return;
            }

            // ── Pre-process Geometry deeply into Meshes for speed ──
            var mergedMesh = new Mesh();
            var individualMeshes = new List<Mesh>();

            foreach (var geo in geometryList)
            {
                if (geo is Brep brep)
                {
                    var meshes = Mesh.CreateFromBrep(brep, MeshingParameters.FastRenderMesh);
                    if (meshes != null)
                    {
                        var bm = new Mesh();
                        foreach (var m in meshes)
                            if (m != null && m.IsValid) bm.Append(m);
                        
                        if (bm.Faces.Count > 0)
                        {
                            individualMeshes.Add(bm);
                            mergedMesh.Append(bm);
                        }
                    }
                }
                else if (geo is Mesh mesh && mesh.IsValid)
                {
                    individualMeshes.Add(mesh);
                    mergedMesh.Append(mesh);
                }
                else if (geo is Surface srf)
                {
                    var brepFromSurface = Brep.CreateFromSurface(srf);
                    if (brepFromSurface != null && brepFromSurface.IsValid)
                    {
                        var meshes = Mesh.CreateFromBrep(brepFromSurface, MeshingParameters.FastRenderMesh);
                        if (meshes != null)
                        {
                            var bm = new Mesh();
                            foreach (var m in meshes)
                                if (m != null && m.IsValid) bm.Append(m);
                            
                            if (bm.Faces.Count > 0)
                            {
                                individualMeshes.Add(bm);
                                mergedMesh.Append(bm);
                            }
                        }
                    }
                }
            }

            if (!mergedMesh.IsValid || mergedMesh.Faces.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid geometry could be meshed");
                return;
            }

            // Build an RTree for fast local inclusion checks
            var bboxTree = new RTree();
            for (int i = 0; i < individualMeshes.Count; i++)
            {
                bboxTree.Insert(individualMeshes[i].GetBoundingBox(true), i);
            }

            double minZ = points.Min(p => p.Z);

            int count = points.Count;
            var sdfArr = new double[count];
            var bldgHeightArr = new double[count];
            var zRelativeArr = new double[count];
            var uAtZArr = new double[count];
            var xCoordsArr = new double[count];
            var yCoordsArr = new double[count];

            // Thread-safe local copies
            double locPedestrianLevel = pedestrianLevel;
            double locMinZ = minZ;
            double locZRef = zRef;
            double locURef = uRef;
            bool locURefProvided = uRefProvided;

            // ──────────────────────────────────────────
            // 1. Parallel feature computation (OPTIMIZED MESH)
            // ──────────────────────────────────────────
            System.Threading.Tasks.Parallel.For(0, count, i =>
            {
                Point3d pt = points[i];
                xCoordsArr[i] = SafeRound(pt.X, 2);
                yCoordsArr[i] = SafeRound(pt.Y, 2);

                double heightHere = 0.0;
                
                // Extremely fast Ray casting for building height on merged mesh
                var verticalRay = new Ray3d(new Point3d(pt.X, pt.Y, 1000), -Vector3d.ZAxis);
                double tVal = Rhino.Geometry.Intersect.Intersection.MeshRay(mergedMesh, verticalRay);
                if (tVal >= 0.0)
                {
                    heightHere = 1000.0 - tVal;
                }

                // extremely fast ClosestPoint on merged mesh
                Point3d cp = mergedMesh.ClosestPoint(pt);
                double minDist = pt.DistanceTo(cp);

                bool inside = false;
                if (minDist > 0) // Only check inside if it is > 0
                {
                    // Search RTree to only test IsPointInside on local buildings
                    var searchBox = new BoundingBox(pt.X - 0.1, pt.Y - 0.1, pt.Z - 0.1, pt.X + 0.1, pt.Y + 0.1, pt.Z + 0.1);
                    bboxTree.Search(searchBox, (sender, args) =>
                    {
                        if (!inside && individualMeshes[args.Id].IsPointInside(pt, 1e-6, true))
                            inside = true;
                    });
                }

                sdfArr[i] = SafeRound(inside ? -minDist : minDist, 2);
                bldgHeightArr[i] = SafeRound(heightHere, 2);

                double mount = pt.Z - locMinZ + locPedestrianLevel;
                zRelativeArr[i] = SafeRound(mount, 2);

                double uAtZ = double.NaN;
                if (mount > 0 && locZRef > 0)
                {
                    double denom = Math.Log(locZRef);
                    double ratio = Math.Log(mount) / denom;
                    uAtZ = locURef * ratio;
                }

                double uAtZRounded = SafeRound(uAtZ, 2);
                if (locURefProvided && locURef != 0.0 && !double.IsNaN(uAtZRounded))
                    uAtZArr[i] = SafeRound(uAtZRounded / locURef, 2);
                else
                    uAtZArr[i] = uAtZRounded;
            });

            // ── Direction components (first wind direction) ──
            double firstDir = windDirs[0];
            double rad0 = firstDir * Math.PI / 180.0;
            double dirSin0 = Math.Round(Clamp(-Math.Sin(rad0), -1.0, 1.0), 6);
            double dirCos0 = Math.Round(Clamp(-Math.Cos(rad0), -1.0, 1.0), 6);

            // ──────────────────────────────────────────
            // 2. Build ONNX input tensor (1, 8, 504, 504)
            // ──────────────────────────────────────────
            var tensor = new DenseTensor<float>(new[] { 1, X_CH, IMG_H, IMG_W });

            // Map world coords → grid indices
            var idxXArr = new int[count];
            var idxYArr = new int[count];
            var validMask = new bool[count];

            for (int i = 0; i < count; i++)
            {
                int ix = (int)Math.Round((xCoordsArr[i] - X_MIN) / X_STEP);
                int iy = (int)Math.Round((yCoordsArr[i] - Y_MIN) / Y_STEP);

                if (ix >= 0 && ix < IMG_W && iy >= 0 && iy < IMG_H)
                {
                    validMask[i] = true;
                    idxXArr[i] = ix;
                    idxYArr[i] = iy;
                }
            }

            // Channel 0-1: canonical coordinate grids (X, Y meshgrid)
            double xLinStep = (X_MAX - X_MIN) / (IMG_W - 1);
            double yLinStep = (Y_MAX - Y_MIN) / (IMG_H - 1);

            for (int row = 0; row < IMG_H; row++)
            {
                float yVal = (float)(Y_MIN + row * yLinStep);
                for (int col = 0; col < IMG_W; col++)
                {
                    float xVal = (float)(X_MIN + col * xLinStep);
                    tensor[0, 0, row, col] = xVal;
                    tensor[0, 1, row, col] = yVal;
                }
            }

            // Channels 2-7: scatter point features onto grid
            float dSin = SafeFloat(dirSin0);
            float dCos = SafeFloat(dirCos0);

            for (int i = 0; i < count; i++)
            {
                if (!validMask[i]) continue;

                int ix = idxXArr[i];
                int iy = idxYArr[i];

                tensor[0, 2, iy, ix] = SafeFloat(zRelativeArr[i]);
                tensor[0, 3, iy, ix] = SafeFloat(sdfArr[i]);
                tensor[0, 4, iy, ix] = SafeFloat(bldgHeightArr[i]);
                tensor[0, 5, iy, ix] = SafeFloat(uAtZArr[i]);
                tensor[0, 6, iy, ix] = dSin;
                tensor[0, 7, iy, ix] = dCos;
            }

            // ──────────────────────────────────────────
            // 3. Run ONNX inference
            // ──────────────────────────────────────────
            try
            {
                var session = GetSession(onnxPath, useGpu);

                // Discover the model's input name dynamically
                string inputName = session.InputMetadata.Keys.First();

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(inputName, tensor)
                };

                var sw = System.Diagnostics.Stopwatch.StartNew();
                using (var results = session.Run(inputs))
                {
                    sw.Stop();

                    // Output shape: (1, 1, 504, 504)
                    var outputTensor = results.First().AsTensor<float>();

                    // Extract per-point predictions
                    var windField = new double[count];
                    double predMin = double.MaxValue, predMax = double.MinValue;

                    for (int i = 0; i < count; i++)
                    {
                        if (!validMask[i])
                        {
                            windField[i] = double.NaN;
                            continue;
                        }

                        int ix = idxXArr[i];
                        int iy = idxYArr[i];

                        // Clamp to non-negative (physics: wind speed ≥ 0)
                        float raw = outputTensor[0, 0, iy, ix];
                        double pred = Math.Max(raw, 0.0);
                        windField[i] = Math.Round(pred, 4);

                        if (pred < predMin) predMin = pred;
                        if (pred > predMax) predMax = pred;
                    }

                    // ── Set outputs ──
                    DA.SetDataList(0, xCoordsArr);
                    DA.SetDataList(1, yCoordsArr);
                    DA.SetDataList(2, windField);

                    int validCount = validMask.Count(v => v);
                    Message = $"Pts: {count} | {sw.ElapsedMilliseconds} ms";

                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        $"Inference: {sw.ElapsedMilliseconds} ms | " +
                        $"Wind dir: {firstDir}° | Valid: {validCount}/{count} | " +
                        $"Pred range: [{predMin:F3}, {predMax:F3}]");
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"ONNX inference failed: {ex.Message}");
            }
        }

        // Clean up ONNX session when component is removed
        public override void RemovedFromDocument(GH_Document document)
        {
            _session?.Dispose();
            _session = null;
            _cachedOnnxPath = null;
            base.RemovedFromDocument(document);
        }
    }
}
