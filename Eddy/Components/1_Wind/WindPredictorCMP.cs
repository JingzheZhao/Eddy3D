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
            pManager.AddNumberParameter("Filter Margin", "filter_margin",
                "Margin (in meters) to mask out from the outer perimeter of the prediction plane due to unstable boundary effects. Default = 100.0",
                GH_ParamAccess.item, 100.0);
            pManager.AddTextParameter("Palette", "Palette",
                "Color palette name ('jet', 'viridis', 'plasma', 'magma', 'inferno', 'turbo', 'coolwarm', 'pastel', 'gray'). Default = 'jet'",
                GH_ParamAccess.item, "jet");
            pManager.AddIntervalParameter("Legend Domain", "Domain",
                "Optional custom domain [min, max] to lock the color bounds. If empty, the colors scale dynamically to the data.",
                GH_ParamAccess.item);
            pManager.AddBooleanParameter("Interpolate", "Interpolate",
                "If true, generates a smooth, continuous interpolated mesh. If false, generates a pixelated blocky mesh.",
                GH_ParamAccess.item, true);

            pManager[0].Optional = false;
            pManager[1].Optional = false;
            pManager[2].Optional = false;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
            pManager[8].Optional = true;
            pManager[9].Optional = true;
            pManager[10].Optional = true;
            pManager[11].Optional = true;
        }

        // ──────────────────────────────────────────────
        // OUTPUTS — X, Y, predicted wind speed
        // ──────────────────────────────────────────────
        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("X", "X",
                "X coordinate (m) of each valid input point.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Y", "Y",
                "Y coordinate (m) of each valid input point.",
                GH_ParamAccess.list);
            pManager.AddNumberParameter("Wind Speed", "W",
                "Predicted wind speed at each valid input point. Points outside the domain or inside the filter margin are culled.",
                GH_ParamAccess.list);
            pManager.AddMeshParameter("Grid Mesh", "M",
                "A fast-rendering contiguous coloured preview mesh of the predictions.",
                GH_ParamAccess.item);
            pManager.AddMeshParameter("Legend Mesh", "LM",
                "A colored mesh strip acting as a visual legend.",
                GH_ParamAccess.item);
            pManager.AddGenericParameter("Legend Points", "LP",
                "Locations for the legend text in the 3D viewport (Generic type to prevent red cross preview).",
                GH_ParamAccess.list);
            pManager.AddTextParameter("Legend Values", "LV",
                "Text values corresponding to the generated legend.",
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
            _session = null;

            var opts = new SessionOptions();
            opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            string activeProvider = "CPU";

            if (useGpu)
            {
                try
                {
                    opts.AppendExecutionProvider_DML(0);  // device 0 = default GPU
                    _session = new InferenceSession(onnxPath, opts);
                    activeProvider = "DirectML (GPU)";
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        $"DirectML GPU initialization failed ({ex.Message}) — falling back to CPU.");
                    
                    opts.Dispose();
                    opts = new SessionOptions();
                    opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                    _session = new InferenceSession(onnxPath, opts);
                    activeProvider = "CPU (GPU fallback)";
                }
            }
            else
            {
                _session = new InferenceSession(onnxPath, opts);
            }

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

        private static List<Color> GetPalette(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return new List<Color>();
            name = name.Trim().ToLowerInvariant();

            switch (name)
            {
                case "viridis":
                    return new List<Color> {
                        Color.FromArgb(68, 1, 84),
                        Color.FromArgb(59, 82, 139),
                        Color.FromArgb(33, 145, 140),
                        Color.FromArgb(94, 201, 98),
                        Color.FromArgb(253, 231, 37)
                    };
                case "plasma":
                    return new List<Color> {
                        Color.FromArgb(13, 8, 135),
                        Color.FromArgb(126, 3, 168),
                        Color.FromArgb(204, 71, 120),
                        Color.FromArgb(248, 149, 64),
                        Color.FromArgb(240, 249, 33)
                    };
                case "magma":
                    return new List<Color> {
                        Color.FromArgb(0, 0, 4),
                        Color.FromArgb(81, 18, 124),
                        Color.FromArgb(182, 54, 121),
                        Color.FromArgb(251, 136, 97),
                        Color.FromArgb(252, 253, 191)
                    };
                case "inferno":
                     return new List<Color> {
                        Color.FromArgb(0, 0, 4),
                        Color.FromArgb(87, 16, 110),
                        Color.FromArgb(187, 55, 84),
                        Color.FromArgb(249, 142, 9),
                        Color.FromArgb(252, 255, 164)
                    };
                case "gray":
                case "greys":
                     return new List<Color> {
                        Color.FromArgb(0, 0, 0),
                        Color.FromArgb(255, 255, 255)
                    };
                case "coolwarm":
                     return new List<Color> {
                        Color.FromArgb(59, 76, 192),
                        Color.FromArgb(180, 204, 233),
                        Color.FromArgb(221, 221, 221),
                        Color.FromArgb(244, 154, 123),
                        Color.FromArgb(180, 4, 38)
                    };
                case "turbo":
                    return new List<Color> {
                        Color.FromArgb(48, 18, 59),
                        Color.FromArgb(70, 107, 238),
                        Color.FromArgb(40, 188, 235),
                        Color.FromArgb(50, 242, 152),
                        Color.FromArgb(164, 252, 60),
                        Color.FromArgb(238, 207, 58),
                        Color.FromArgb(251, 126, 33),
                        Color.FromArgb(208, 47, 5),
                        Color.FromArgb(122, 4, 3)
                    };
                case "pastel":
                     return new List<Color> {
                        Color.FromArgb(186, 225, 255),
                        Color.FromArgb(186, 255, 201),
                        Color.FromArgb(255, 255, 186),
                        Color.FromArgb(255, 223, 186),
                        Color.FromArgb(255, 179, 186)
                    };
                case "jet":
                default:
                    return new List<Color>();
            }
        }

        private static float SafeFloat(double v)
        {
            return (double.IsNaN(v) || double.IsInfinity(v)) ? 0f : (float)v;
        }

        private static Color GetColorFromPalette(double t, List<Color> customColors)
        {
            if (t < 0) t = 0;
            if (t > 1) t = 1;

            if (customColors == null || customColors.Count == 0)
            {
                // Default Jet
                double hue = 240.0 * (1.0 - t);
                return ColorFromHSV(hue, 1.0, 1.0);
            }

            if (customColors.Count == 1) return customColors[0];

            double scaled = t * (customColors.Count - 1);
            int idx = (int)Math.Floor(scaled);
            if (idx >= customColors.Count - 1) return customColors[customColors.Count - 1];

            double localT = scaled - idx;
            Color c1 = customColors[idx];
            Color c2 = customColors[idx + 1];

            int r = (int)Math.Round(c1.R + (c2.R - c1.R) * localT);
            int g = (int)Math.Round(c1.G + (c2.G - c1.G) * localT);
            int b = (int)Math.Round(c1.B + (c2.B - c1.B) * localT);
            int a = (int)Math.Round(c1.A + (c2.A - c1.A) * localT);

            return Color.FromArgb(a, r, g, b);
        }

        private static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0) return Color.FromArgb(255, v, t, p);
            else if (hi == 1) return Color.FromArgb(255, q, v, p);
            else if (hi == 2) return Color.FromArgb(255, p, v, t);
            else if (hi == 3) return Color.FromArgb(255, p, q, v);
            else if (hi == 4) return Color.FromArgb(255, t, p, v);
            else return Color.FromArgb(255, v, p, q);
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
            double filterMargin = 100.0;
            string paletteName = "jet";
            Interval customDomain = Interval.Unset;
            bool interpolate = false;

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
            DA.GetData(8, ref filterMargin);
            DA.GetData(9, ref paletteName);
            bool domainProvided = DA.GetData(10, ref customDomain) && customDomain.IsValid;
            DA.GetData(11, ref interpolate);

            var customColors = GetPalette(paletteName);

            if (windDirs.Count == 0) windDirs.Add(0.0);

            if (points.Count == 0 || geometryList.Count == 0)
            {
                Message = "Connect Points & Buildings";
                return;
            }

            // ── Pre-process Geometry deeply into Meshes for speed ──
            var mergedMesh = new Mesh();

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
                            mergedMesh.Append(bm);
                        }
                    }
                }
                else if (geo is Mesh mesh && mesh.IsValid)
                {
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

                sdfArr[i] = SafeRound(-minDist, 2);
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

                    // Feature statistics for dynamic filter margin shape
                    bool isCircular = false;
                    double centerX = 0, centerY = 0, maxRadius = 0;
                    double minPx = 0, maxPx = 0, minPy = 0, maxPy = 0;

                    if (filterMargin > 0.0 && count > 0)
                    {
                        minPx = xCoordsArr[0]; maxPx = xCoordsArr[0];
                        minPy = yCoordsArr[0]; maxPy = yCoordsArr[0];
                        for (int i = 1; i < count; i++)
                        {
                            if (xCoordsArr[i] < minPx) minPx = xCoordsArr[i];
                            if (xCoordsArr[i] > maxPx) maxPx = xCoordsArr[i];
                            if (yCoordsArr[i] < minPy) minPy = yCoordsArr[i];
                            if (yCoordsArr[i] > maxPy) maxPy = yCoordsArr[i];
                        }

                        centerX = (minPx + maxPx) / 2.0;
                        centerY = (minPy + maxPy) / 2.0;

                        double maxRadSqr = 0;
                        for (int i = 0; i < count; i++)
                        {
                            double dx = xCoordsArr[i] - centerX;
                            double dy = yCoordsArr[i] - centerY;
                            double r2 = dx * dx + dy * dy;
                            if (r2 > maxRadSqr) maxRadSqr = r2;
                        }
                        maxRadius = Math.Sqrt(maxRadSqr);

                        double halfW = (maxPx - minPx) / 2.0;
                        double halfH = (maxPy - minPy) / 2.0;
                        double minHalf = Math.Min(halfW, halfH);

                        if (minHalf > 0 && (maxRadius / minHalf) < 1.15)
                        {
                            isCircular = true;
                        }
                    }

                    for (int i = 0; i < count; i++)
                    {
                        if (!validMask[i])
                        {
                            windField[i] = double.NaN;
                            continue;
                        }

                        if (filterMargin > 0.0)
                        {
                            if (isCircular)
                            {
                                double dx = xCoordsArr[i] - centerX;
                                double dy = yCoordsArr[i] - centerY;
                                double dist = Math.Sqrt(dx * dx + dy * dy);
                                if (dist > maxRadius - filterMargin)
                                {
                                    windField[i] = double.NaN;
                                    continue;
                                }
                            }
                            else
                            {
                                if (xCoordsArr[i] < minPx + filterMargin || xCoordsArr[i] > maxPx - filterMargin ||
                                    yCoordsArr[i] < minPy + filterMargin || yCoordsArr[i] > maxPy - filterMargin)
                                {
                                    windField[i] = double.NaN;
                                    continue;
                                }
                            }
                        }

                        int ix = idxXArr[i];
                        int iy = idxYArr[i];

                        // Clamp to non-negative (physics: wind speed ≥ 0)
                        // The network outputs dimensionless values (normalized by training speed),
                        // so we must multiply by the reference speed (locURef) to denormalize back to m/s
                        float raw = outputTensor[0, 0, iy, ix];
                        double pred = Math.Max(raw * locURef, 0.0);
                        windField[i] = Math.Round(pred, 4);

                        if (pred < predMin) predMin = pred;
                        if (pred > predMax) predMax = pred;
                    }

                    var outX = new List<double>();
                    var outY = new List<double>();
                    var outW = new List<double>();

                    var previewMesh = new Mesh();
                    double halfX = X_STEP / 2.0;
                    double halfY = Y_STEP / 2.0;

                    // Determine max/min for color mapping
                    double validMin = double.MaxValue;
                    double validMax = double.MinValue;
                    
                    if (domainProvided)
                    {
                        validMin = customDomain.Min;
                        validMax = customDomain.Max;
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (!double.IsNaN(windField[i]))
                            {
                                if (windField[i] < validMin) validMin = windField[i];
                                if (windField[i] > validMax) validMax = windField[i];
                            }
                        }
                    }

                    if (interpolate)
                    {
                        int[,] gridToIdx = new int[IMG_H, IMG_W];
                        for (int iy = 0; iy < IMG_H; iy++)
                            for (int ix = 0; ix < IMG_W; ix++)
                                gridToIdx[iy, ix] = -1;

                        for (int i = 0; i < count; i++)
                        {
                            if (!double.IsNaN(windField[i]))
                            {
                                double w = windField[i];
                                outX.Add(xCoordsArr[i]);
                                outY.Add(yCoordsArr[i]);
                                outW.Add(w);

                                double t = validMax > validMin ? (w - validMin) / (validMax - validMin) : 0.0;
                                Color c = GetColorFromPalette(t, customColors);

                                gridToIdx[idxYArr[i], idxXArr[i]] = previewMesh.Vertices.Count;
                                previewMesh.Vertices.Add(points[i]);
                                previewMesh.VertexColors.Add(c);
                            }
                        }

                        for (int iy = 0; iy < IMG_H - 1; iy++)
                        {
                            for (int ix = 0; ix < IMG_W - 1; ix++)
                            {
                                int v00 = gridToIdx[iy, ix];
                                int v10 = gridToIdx[iy, ix + 1];
                                int v11 = gridToIdx[iy + 1, ix + 1];
                                int v01 = gridToIdx[iy + 1, ix];

                                if (v00 >= 0 && v10 >= 0 && v11 >= 0 && v01 >= 0)
                                    previewMesh.Faces.AddFace(v00, v10, v11, v01);
                                else if (v00 >= 0 && v10 >= 0 && v01 >= 0)
                                    previewMesh.Faces.AddFace(v00, v10, v01);
                                else if (v10 >= 0 && v11 >= 0 && v01 >= 0)
                                    previewMesh.Faces.AddFace(v10, v11, v01);
                                else if (v00 >= 0 && v11 >= 0 && v01 >= 0)
                                    previewMesh.Faces.AddFace(v00, v11, v01);
                                else if (v00 >= 0 && v10 >= 0 && v11 >= 0)
                                    previewMesh.Faces.AddFace(v00, v10, v11);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < count; i++)
                        {
                            if (!double.IsNaN(windField[i]))
                            {
                                double w = windField[i];
                                outX.Add(xCoordsArr[i]);
                                outY.Add(yCoordsArr[i]);
                                outW.Add(w);

                                // Calculate local color
                                double t = validMax > validMin ? (w - validMin) / (validMax - validMin) : 0.0;
                                Color c = GetColorFromPalette(t, customColors);

                                Point3d pt = points[i];
                                int vc = previewMesh.Vertices.Count;
                                
                                previewMesh.Vertices.Add(pt.X - halfX, pt.Y - halfY, pt.Z);
                                previewMesh.Vertices.Add(pt.X + halfX, pt.Y - halfY, pt.Z);
                                previewMesh.Vertices.Add(pt.X + halfX, pt.Y + halfY, pt.Z);
                                previewMesh.Vertices.Add(pt.X - halfX, pt.Y + halfY, pt.Z);
                                
                                previewMesh.Faces.AddFace(vc, vc + 1, vc + 2, vc + 3);
                                
                                previewMesh.VertexColors.Add(c);
                                previewMesh.VertexColors.Add(c);
                                previewMesh.VertexColors.Add(c);
                                previewMesh.VertexColors.Add(c);
                            }
                        }
                    }

                    Mesh legendMesh = new Mesh();
                    var legendPts = new List<Point3d>();
                    var legendVals = new List<string>();

                    if (validMax > validMin && count > 0)
                    {
                        double legWidth = Math.Max((maxPx - minPx) * 0.5, 400.0);
                        double legHeight = Math.Max(legWidth * 0.05, 15.0);
                        
                        double startX = 0.0 - legWidth * 0.5;
                        double startY = -500.0 - legHeight * 0.5;
                        double zLevel = points[0].Z;

                        int steps = 20;
                        for (int i = 0; i <= steps; i++)
                        {
                            double t = (double)i / steps;
                            double x = startX + t * legWidth;

                            Color c = GetColorFromPalette(t, customColors);

                            legendMesh.Vertices.Add(x, startY, zLevel);
                            legendMesh.Vertices.Add(x, startY + legHeight, zLevel);
                            legendMesh.VertexColors.Add(c);
                            legendMesh.VertexColors.Add(c);

                            if (i > 0)
                            {
                                int vc = legendMesh.Vertices.Count;
                                legendMesh.Faces.AddFace(vc - 4, vc - 2, vc - 1, vc - 3);
                            }

                            if (i % 5 == 0)
                            {
                                double val = validMin + t * (validMax - validMin);
                                legendVals.Add($"{val:F2} m/s");
                                legendPts.Add(new Point3d(x, startY - legHeight * 1.5, zLevel));
                            }
                        }
                    }

                    // ── Set outputs ──
                    DA.SetDataList(0, outX);
                    DA.SetDataList(1, outY);
                    DA.SetDataList(2, outW);
                    DA.SetData(3, previewMesh);
                    DA.SetData(4, legendMesh);
                    DA.SetDataList(5, legendPts);
                    DA.SetDataList(6, legendVals);

                    int validCount = outW.Count;
                    Message = $"Pts: {validCount}/{count} | {sw.ElapsedMilliseconds} ms";

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
