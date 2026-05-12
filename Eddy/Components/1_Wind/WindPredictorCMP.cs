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
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;

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
        private InferenceSession[] _sessions;
        private string _cachedOnnxPath;
        private bool _cachedUseGpu;
        // Set to true when DML fails at inference time so future runs skip DML for this model
        private bool _dmlRuntimeFailed = false;
        private bool _isDirectMLActive = false;

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

            string assemblyDir = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);

            string rid = GetRuntimeIdentifier();
            string nativeDir = Path.Combine(assemblyDir, "runtimes", rid, "native");

            NativeLibrary.SetDllImportResolver(
                typeof(InferenceSession).Assembly,
                (libraryName, assembly, searchPath) =>
                {
                    foreach (string candidate in EnumerateCandidates(nativeDir, libraryName))
                    {
                        if (File.Exists(candidate)
                            && NativeLibrary.TryLoad(candidate, out IntPtr handle))
                        {
                            return handle;
                        }
                    }
                    return IntPtr.Zero;
                });
        }

        private static string GetRuntimeIdentifier()
        {
            string arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => "arm64",
                Architecture.X64 => "x64",
                _ => "x64",
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return $"win-{arch}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return $"osx-{arch}";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return $"linux-{arch}";
            return $"win-{arch}";
        }

        private static IEnumerable<string> EnumerateCandidates(string nativeDir, string libraryName)
        {
            yield return Path.Combine(nativeDir, libraryName);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!libraryName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    yield return Path.Combine(nativeDir, libraryName + ".dll");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (!libraryName.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase))
                {
                    yield return Path.Combine(nativeDir, "lib" + libraryName + ".dylib");
                    yield return Path.Combine(nativeDir, libraryName + ".dylib");
                }
            }
            else
            {
                if (!libraryName.EndsWith(".so", StringComparison.OrdinalIgnoreCase))
                {
                    yield return Path.Combine(nativeDir, "lib" + libraryName + ".so");
                    yield return Path.Combine(nativeDir, libraryName + ".so");
                }
            }
        }

        public WindPredictorCMP()
            : base("Wind Predictor", "WindPredict",
                "Run ONNX wind-field prediction end-to-end.\n" +
                "Computes SDF, building height, Z_relative, U/Uref, direction features from geometry,\n" +
                "assembles the 8-channel input tensor, runs ONNX inference, and outputs predicted wind speeds.\n" +
                "Supports legacy 1ch (U), 2ch (U + k) and new 4ch (U + k + U_roof + k_roof) models.",
                "Eddy3D", "4 | ML")
        {
        }

        protected override Bitmap Icon => Resources.Eddy_wind_predictor;

        public override Guid ComponentGuid => new Guid("{B8C9D0E1-2F3A-4B5C-6D7E-8F9A0B1C2D3E}");

        // ──────────────────────────────────────────────
        // INPUTS
        // ──────────────────────────────────────────────
        private static readonly string[] PaletteNames = { "jet", "viridis", "plasma", "magma", "inferno", "turbo", "coolwarm", "pastel", "gray" };
        private static readonly string[] FieldNames = { "Velocity", "Turbulent Kinetic Energy" };
        private static readonly string[] LevelNames = { "Pedestrian Level", "Roof Level" };
        private static readonly string[] InterpolateNames = { "Flat", "Smooth" };

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "Points",
                "List of 3D points. Each point's Z must be absolute elevation (m).",
                GH_ParamAccess.list);
            pManager.AddGeometryParameter("Buildings", "Buildings",
                "List of Brep or Mesh objects representing buildings.",
                GH_ParamAccess.list);
            pManager.AddTextParameter("Model", "Model",
                "Full file path to the ONNX model. Connect the FilePath output from the ML Model component.",
                GH_ParamAccess.item);
            pManager.AddGenericParameter("Boundary Conditions", "BC",
                "Boundary conditions from the ABL or Uniform Flow component. " +
                "U_ref, z_ref, z_0, wind directions, and EPW path are extracted automatically.",
                GH_ParamAccess.item);
            pManager.AddNumberParameter("pedestrian_level", "pedestrian_level",
                "Pedestrian mount height (m). Default = 1.8",
                GH_ParamAccess.item, 1.8);
            pManager.AddNumberParameter("Filter Margin", "filter_margin",
                "Margin (in meters) to mask out from the outer perimeter of the prediction plane due to unstable boundary effects. Default = 100.0",
                GH_ParamAccess.item, 100.0);
            pManager.AddIntegerParameter("Palette", "Palette",
                "Color palette for visualization.",
                GH_ParamAccess.item, 0);
            pManager.AddIntervalParameter("Legend Domain", "Domain",
                "Optional custom domain [min, max] to lock the color bounds. If empty, the colors scale dynamically to the data.",
                GH_ParamAccess.item);
            pManager.AddIntegerParameter("Interpolate", "Interpolate",
                "Visualization style. Flat (pixelated) vs Smooth (interpolated colors).",
                GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Field", "Field",
                "Field to visualize. Affects M, LM, LP, LV outputs.",
                GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Level", "Level",
                "Visualization level. Roof Level only applies when a 4-channel model is loaded.",
                GH_ParamAccess.item, 1);

            // Palette dropdown with named values (right-click fallback)
            var paletteParam = pManager[6] as Param_Integer;
            for (int i = 0; i < PaletteNames.Length; i++)
                paletteParam.AddNamedValue(PaletteNames[i], i);

            // Interpolate dropdown with named values (right-click fallback)
            var interpParam = pManager[8] as Param_Integer;
            for (int i = 0; i < InterpolateNames.Length; i++)
                interpParam.AddNamedValue(InterpolateNames[i], i);

            // Field dropdown with named values (right-click fallback)
            var fieldParam = pManager[9] as Param_Integer;
            for (int i = 0; i < FieldNames.Length; i++)
                fieldParam.AddNamedValue(FieldNames[i], i);

            // Level dropdown with named values (right-click fallback)
            var levelParam = pManager[10] as Param_Integer;
            for (int i = 0; i < LevelNames.Length; i++)
                levelParam.AddNamedValue(LevelNames[i], i);

            pManager[0].Optional = false;  // Points
            pManager[1].Optional = false;  // Buildings
            pManager[2].Optional = false;  // Model
            pManager[3].Optional = false;  // BC
            pManager[4].Optional = true;   // pedestrian_level
            pManager[5].Optional = true;   // filter_margin
            pManager[6].Optional = true;   // Palette
            pManager[7].Optional = true;   // Domain
            pManager[8].Optional = true;   // Interpolate
            pManager[9].Optional = true;   // Field
            pManager[10].Optional = true;  // Level
        }

        // ──────────────────────────────────────────────
        // Inline Dropdown UI
        // ──────────────────────────────────────────────
        public override void CreateAttributes()
        {
            m_attributes = new DropdownComponentAttributes(this, new DropdownComponentAttributes.DropdownDef[]
            {
                new DropdownComponentAttributes.DropdownDef(6, PaletteNames, 0),     // Palette
                new DropdownComponentAttributes.DropdownDef(8, InterpolateNames, 1), // Interpolate
                new DropdownComponentAttributes.DropdownDef(9, FieldNames, 0),       // Field
                new DropdownComponentAttributes.DropdownDef(10, LevelNames, 1)       // Level
            });
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
            pManager.AddNumberParameter("Values", "V",
                "Predicted field values at each valid input point. " +
                "Outputs wind speed (m/s) or turbulent kinetic energy (m²/s²) depending on the Field input. " +
                "Branches represent different wind directions.",
                GH_ParamAccess.tree);
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
            pManager.AddGenericParameter("Boundary Conditions", "BC",
                "Automated simulation boundary conditions metadata.",
                GH_ParamAccess.item);
            pManager.AddNumberParameter("Values (Roof)", "V_roof",
                "Predicted roof-level field values at each valid input point. " +
                "Outputs wind speed or TKE depending on the Field input. " +
                "Branches represent different wind directions. Empty unless a 4-channel model is used.",
                GH_ParamAccess.tree);
        }

        // ──────────────────────────────────────────────
        // ONNX session management
        // ──────────────────────────────────────────────
        private InferenceSession[] GetSessions(string onnxPath, bool useGpu, int dop)
        {
            // If DML failed at runtime for this model, treat as CPU-only
            bool effectiveGpu = useGpu && !_dmlRuntimeFailed;
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            bool isMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            
            // DirectML: the GPU serializes inference regardless of session count, so
            // more sessions just add graph-compilation overhead (~0.5 s each).
            // Cap at 2 for light pipelining (one submits while the other runs).
            int requiredSessions = (effectiveGpu && isWindows) ? Math.Min(dop, 2) : 1;

            if (_sessions != null && _cachedOnnxPath == onnxPath && _cachedUseGpu == effectiveGpu && _sessions.Length == requiredSessions)
                return _sessions;

            if (_sessions != null)
            {
                foreach (var s in _sessions) s?.Dispose();
            }

            _sessions = new InferenceSession[requiredSessions];

            _cachedOnnxPath = onnxPath;
            _cachedUseGpu = effectiveGpu;
            _isDirectMLActive = false;

            string activeProvider = "CPU";

            if (requiredSessions > 1 && effectiveGpu && isWindows)
            {
                byte[] modelBytes = null;
                try
                {
                    modelBytes = File.ReadAllBytes(onnxPath);
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to read ONNX file: {ex.Message}");
                    return _sessions;
                }

                _isDirectMLActive = true;
                activeProvider = "DirectML (GPU)";
                bool failed = false;
                Exception firstEx = null;

                System.Threading.Tasks.Parallel.For(0, requiredSessions, i =>
                {
                    if (failed) return;
                    try
                    {
                        var opts = new SessionOptions();
                        opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                        opts.AppendExecutionProvider_DML(0);
                        _sessions[i] = new InferenceSession(modelBytes, opts);
                    }
                    catch (Exception ex)
                    {
                        failed = true;
                        firstEx = ex;
                    }
                });

                if (failed)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectML GPU initialization failed ({firstEx?.Message}) — falling back to CPU.");
                    foreach (var s in _sessions) s?.Dispose();
                    
                    effectiveGpu = false;
                    _isDirectMLActive = false;
                    requiredSessions = 1;
                    Array.Resize(ref _sessions, 1);

                    var cpuOpts = new SessionOptions();
                    cpuOpts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                    _sessions[0] = new InferenceSession(modelBytes, cpuOpts);
                    activeProvider = "CPU (DML init failed)";
                }
            }
            else
            {
                var opts = new SessionOptions();
                opts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

                if (effectiveGpu && isWindows)
                {
                    try
                    {
                        opts.AppendExecutionProvider_DML(0);  // device 0 = default GPU
                        _sessions[0] = new InferenceSession(onnxPath, opts);
                        activeProvider = "DirectML (GPU)";
                        _isDirectMLActive = true;
                    }
                    catch (Exception ex)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"DirectML GPU initialization failed ({ex.Message}) — falling back to CPU.");
                        opts.Dispose();
                        var cpuOpts = new SessionOptions();
                        cpuOpts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                        _sessions[0] = new InferenceSession(onnxPath, cpuOpts);
                        activeProvider = "CPU (DML init failed)";
                        effectiveGpu = false;
                        _isDirectMLActive = false;
                    }
                }
                else if (effectiveGpu && isMac)
                {
                    try
                    {
                        var coreMlOptions = new Dictionary<string, string>
                        {
                            { "ModelFormat", "MLProgram" },
                            { "MLComputeUnits", "CPUAndGPU" },
                        };
                        opts.AppendExecutionProvider("CoreML", coreMlOptions);
                        _sessions[0] = new InferenceSession(onnxPath, opts);
                        activeProvider = "CoreML (ML Program, GPU)";
                    }
                    catch (Exception ex)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"CoreML initialization failed ({ex.Message}) — falling back to CPU.");
                        opts.Dispose();
                        var cpuOpts = new SessionOptions();
                        cpuOpts.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                        _sessions[0] = new InferenceSession(onnxPath, cpuOpts);
                        activeProvider = "CPU (CoreML init failed)";
                        effectiveGpu = false;
                    }
                }
                else
                {
                    _sessions[0] = new InferenceSession(onnxPath, opts);
                    if (_dmlRuntimeFailed) activeProvider = "CPU (GPU runtime fallback)";
                    else activeProvider = "CPU (no GPU provider on this platform)";
                    effectiveGpu = false;
                }
            }

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Provider: {activeProvider} (Instances: {_sessions.Length})");

            var inputMeta = _sessions[0].InputMetadata;
            foreach (var kv in inputMeta)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"ONNX input: \"{kv.Key}\" shape=[{string.Join(",", kv.Value.Dimensions)}]");
            }

            return _sessions;
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
            double uRef = 5.0;
            double zRef = 10.0;
            double z0 = 1.0;
            double pedestrianLevel = 1.8;
            var windDirs = new List<double>();
            double filterMargin = 100.0;
            Interval customDomain = Interval.Unset;
            bool interpolate = false;
            EddyLib.BCs.BCCollection inputBCCollection = null;
            string epwPathFromBC = null;

            if (!DA.GetDataList(0, points)) return;
            if (!DA.GetDataList(1, geometryList)) return;
            if (!DA.GetData(2, ref onnxPath) || string.IsNullOrWhiteSpace(onnxPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Model path is required. Connect the FilePath output from the ML Model component.");
                return;
            }
            if (!File.Exists(onnxPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Model file not found: {onnxPath}");
                return;
            }

            // ── BC input (required — provides U_ref, z_ref, z0, wind directions) ──
            object bcRaw = null;
            if (!DA.GetData(3, ref bcRaw) || bcRaw == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary Conditions (BC) input is required. Connect an ABL or Uniform Flow component.");
                return;
            }

            // Unwrap GH_ObjectWrapper if the generic parameter wraps it
            if (bcRaw is Grasshopper.Kernel.Types.GH_ObjectWrapper wrapper)
                bcRaw = wrapper.Value;

            if (bcRaw is EddyLib.BCs.BCCollection bcc)
            {
                inputBCCollection = bcc;
            }
            else if (bcRaw is EddyLib.BCs.BC singleBC)
            {
                // Wrap a single BC into a collection for uniform handling
                inputBCCollection = new EddyLib.BCs.BCCollection();
                inputBCCollection.AddBoundaryCondition(singleBC);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"BC input type not recognized ({bcRaw.GetType().Name}). Expected BCCollection or BC from an ABL / Uniform Flow component.");
                return;
            }

            if (inputBCCollection.BCs.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "BC input contains no boundary conditions.");
                return;
            }

            // Extract parameters from the first BC in the collection
            var firstBC = inputBCCollection.BCs[0];
            uRef = firstBC.URef;
            z0 = firstBC.z0;

            if (firstBC is EddyLib.BCs.ABL ablBC)
                zRef = ablBC.zref;

            // Collect wind directions from all BCs in the collection
            windDirs = inputBCCollection.WindDirections.Select(d => (double)d).ToList();

            // Capture EPW path if available
            if (!string.IsNullOrEmpty(inputBCCollection.epwFilePath))
                epwPathFromBC = inputBCCollection.epwFilePath;

            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                $"BC: U_ref={uRef}, z_ref={zRef}, z_0={z0}, " +
                $"directions=[{string.Join(", ", windDirs.Select(d => d.ToString("F0")))}]" +
                (epwPathFromBC != null ? $", EPW={System.IO.Path.GetFileName(epwPathFromBC)}" : ""));

            DA.GetData(4, ref pedestrianLevel);
            DA.GetData(5, ref filterMargin);
            int paletteIndex = 0;
            DA.GetData(6, ref paletteIndex);
            string paletteName = (paletteIndex >= 0 && paletteIndex < PaletteNames.Length)
                ? PaletteNames[paletteIndex] : "jet";
            bool domainProvided = DA.GetData(7, ref customDomain) && customDomain.IsValid;
            int interpolateIndex = 1;
            DA.GetData(8, ref interpolateIndex);
            interpolate = (interpolateIndex == 1);
            bool showK = false;
            int fieldIndex = 0;
            DA.GetData(9, ref fieldIndex);
            showK = (fieldIndex == 1);
            bool showRoof = true;
            int levelIndex = 1;
            DA.GetData(10, ref levelIndex);
            showRoof = (levelIndex == 1);

            var customColors = GetPalette(paletteName);

            if (windDirs.Count == 0) windDirs.Add(0.0);

            if (windDirs.Count != 1 && windDirs.Count != 8 && windDirs.Count != 16)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"Wind Predictor is optimised for 1, 8, or 16 wind directions (got {windDirs.Count}). " +
                    "Results may be less accurate for comfort analysis with other counts.");

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
                if (locURef != 0.0 && !double.IsNaN(uAtZRounded))
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

            // Feature statistics for filter margin pre-computation
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
                if (minHalf > 0 && (maxRadius / minHalf) < 1.15) isCircular = true;
            }

            // Pre-compute visibility/culling array to avoid redundant math in the loop
            var isCulledArr = new bool[count];
            if (filterMargin > 0.0)
            {
                System.Threading.Tasks.Parallel.For(0, count, i =>
                {
                    if (!validMask[i]) return;
                    bool culled = false;
                    if (isCircular)
                    {
                        double dx = xCoordsArr[i] - centerX;
                        double dy = yCoordsArr[i] - centerY;
                        if (Math.Sqrt(dx * dx + dy * dy) > maxRadius - filterMargin) culled = true;
                    }
                    else
                    {
                        if (xCoordsArr[i] < minPx + filterMargin || xCoordsArr[i] > maxPx - filterMargin ||
                            yCoordsArr[i] < minPy + filterMargin || yCoordsArr[i] > maxPy - filterMargin) culled = true;
                    }
                    isCulledArr[i] = culled;
                });
            }

            // Channels 2-5: scatter point features onto grid
            for (int i = 0; i < count; i++)
            {
                if (!validMask[i]) continue;

                int ix = idxXArr[i];
                int iy = idxYArr[i];

                tensor[0, 2, iy, ix] = SafeFloat(zRelativeArr[i]);
                tensor[0, 3, iy, ix] = SafeFloat(sdfArr[i]);
                tensor[0, 4, iy, ix] = SafeFloat(bldgHeightArr[i]);
                tensor[0, 5, iy, ix] = SafeFloat(uAtZArr[i]);
            }

            // ──────────────────────────────────────────
            // 3. Loop over directions and run ONNX inference
            // ──────────────────────────────────────────
            var speedTree = new GH_Structure<GH_Number>();
            var kTree = new GH_Structure<GH_Number>();
            var uRoofTree = new GH_Structure<GH_Number>();
            var kRoofTree = new GH_Structure<GH_Number>();
            var previewMesh = new Mesh();
            var legendMesh = new Mesh();
            var legendPts = new List<Point3d>();
            var legendVals = new List<string>();
            var outX = new List<double>();
            var outY = new List<double>();
            var outOriginalIndex = new List<int>();
            bool coordsCollected = false;

            string inferenceMode = "?";
            long inferenceOnlyMs = 0;
            int inferenceThreads = 1;

            try
            {
                int N = windDirs.Count;
                int reqDop = Math.Min(N, Math.Max(1, Environment.ProcessorCount));
                
                var sessions = GetSessions(onnxPath, true, reqDop);
                string inputName = sessions[0].InputMetadata.Keys.First();

                var sw = System.Diagnostics.Stopwatch.StartNew();
                int channelSize = IMG_H * IMG_W;
                int featureBlock = 6 * channelSize;
                var baseFeatures = tensor.Buffer.Span.Slice(0, featureBlock);

                // Pre-build one input tensor per direction (sin/cos pre-baked into channels 6, 7).
                // This lets multiple threads call session.Run() concurrently without sharing tensor state.
                var perDirInputs = new List<NamedOnnxValue>[N];
                for (int d = 0; d < N; d++)
                {
                    var t = new DenseTensor<float>(new[] { 1, X_CH, IMG_H, IMG_W });
                    var span = t.Buffer.Span;
                    baseFeatures.CopyTo(span.Slice(0, featureBlock));

                    double rad = windDirs[d] * Math.PI / 180.0;
                    float dSin = SafeFloat(Math.Round(Clamp(-Math.Sin(rad), -1.0, 1.0), 6));
                    float dCos = SafeFloat(Math.Round(Clamp(-Math.Cos(rad), -1.0, 1.0), 6));

                    int ch6Off = 6 * channelSize;
                    int ch7Off = 7 * channelSize;
                    for (int i = 0; i < count; i++)
                    {
                        if (!validMask[i]) continue;
                        int p = idxYArr[i] * IMG_W + idxXArr[i];
                        span[ch6Off + p] = dSin;
                        span[ch7Off + p] = dCos;
                    }

                    perDirInputs[d] = new List<NamedOnnxValue>
                    {
                        NamedOnnxValue.CreateFromTensor(inputName, t),
                    };
                }

                // Run inference for all directions in parallel. InferenceSession.Run is thread-safe.
                var perDirRaw   = new double[N][];
                var perDirK     = new double[N][];
                var perDirURoof = new double[N][];
                var perDirKRoof = new double[N][];
                int outChannels = 1;
                // Multi-session (DirectML pipelining) → one thread per session.
                // Single session (CoreML / CPU) → InferenceSession.Run is thread-safe,
                // so multiple threads can share it.
                int dop = sessions.Length > 1 ? sessions.Length : reqDop;
                inferenceThreads = dop;
                inferenceMode = N > 1
                    ? (sessions.Length > 1
                        ? $"parallel-per-direction × {dop} ({sessions.Length} DML sessions)"
                        : $"parallel-per-direction × {dop}")
                    : "single-direction";

                var sessionQueue = new System.Collections.Concurrent.ConcurrentQueue<InferenceSession>(sessions);

                var inferSw = System.Diagnostics.Stopwatch.StartNew();
                System.Threading.Tasks.Parallel.For(0, N,
                    new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = dop },
                    d =>
                    {
                        InferenceSession localSession;
                        if (sessions.Length == 1)
                        {
                            localSession = sessions[0];
                        }
                        else
                        {
                            while (!sessionQueue.TryDequeue(out localSession))
                            {
                                System.Threading.Thread.Yield();
                            }
                        }

                        using var results = localSession.Run(perDirInputs[d]);
                        
                        if (sessions.Length > 1)
                        {
                            sessionQueue.Enqueue(localSession);
                        }

                        var outputTensor = results.First().AsTensor<float>();
                        int oc = outputTensor.Dimensions[1];
                        if (d == 0) outChannels = oc;

                        // Output convention (verified against stats.pt for all model variants):
                        //   U channels (0, 2): trained on U/Uref (Uref-normalized, dimensionless).
                        //                       Multiply by user-supplied Uref to get m/s.
                        //   k channels (1, 3): trained on physical k in m²/s². Use as-is.
                        var raw      = new double[count];
                        var rawK     = oc >= 2 ? new double[count] : null;
                        var rawURoof = oc >= 4 ? new double[count] : null;
                        var rawKRoof = oc >= 4 ? new double[count] : null;
                        for (int i = 0; i < count; i++)
                        {
                            if (!validMask[i] || isCulledArr[i])
                            {
                                raw[i] = double.NaN;
                                if (rawK     != null) rawK[i]     = double.NaN;
                                if (rawURoof != null) rawURoof[i] = double.NaN;
                                if (rawKRoof != null) rawKRoof[i] = double.NaN;
                                continue;
                            }
                            float r = outputTensor[0, 0, idxYArr[i], idxXArr[i]];
                            raw[i] = Math.Max(r * locURef, 0.0);   // U/Uref → m/s
                            if (rawK != null)
                            {
                                float rK = outputTensor[0, 1, idxYArr[i], idxXArr[i]];
                                rawK[i] = Math.Max(rK, 0.0);        // already m²/s²
                            }
                            if (rawURoof != null)
                            {
                                float rUR = outputTensor[0, 2, idxYArr[i], idxXArr[i]];
                                rawURoof[i] = Math.Max(rUR * locURef, 0.0); // U_roof/Uref → m/s
                            }
                            if (rawKRoof != null)
                            {
                                float rKR = outputTensor[0, 3, idxYArr[i], idxXArr[i]];
                                rawKRoof[i] = Math.Max(rKR, 0.0);   // already m²/s²
                            }
                        }
                        perDirRaw[d]   = raw;
                        perDirK[d]     = rawK;
                        perDirURoof[d] = rawURoof;
                        perDirKRoof[d] = rawKRoof;
                    });
                inferSw.Stop();
                inferenceOnlyMs = inferSw.ElapsedMilliseconds;

                // Sequentially build the GH trees (GH_Structure mutation is not thread-safe).
                for (int d = 0; d < N; d++)
                {
                    var path = new GH_Path(d);
                    var branchSpeeds = new List<GH_Number>();
                    var branchK     = perDirK[d]     != null ? new List<GH_Number>() : null;
                    var branchURoof = perDirURoof[d] != null ? new List<GH_Number>() : null;
                    var branchKRoof = perDirKRoof[d] != null ? new List<GH_Number>() : null;

                    for (int i = 0; i < count; i++)
                    {
                        if (double.IsNaN(perDirRaw[d][i])) continue;

                        if (!coordsCollected)
                        {
                            outX.Add(xCoordsArr[i]);
                            outY.Add(yCoordsArr[i]);
                            outOriginalIndex.Add(i);
                        }
                        branchSpeeds.Add(new GH_Number(Math.Round(perDirRaw[d][i], 4)));
                        if (branchK     != null) branchK.Add(new GH_Number(Math.Round(perDirK[d][i], 6)));
                        if (branchURoof != null) branchURoof.Add(new GH_Number(Math.Round(perDirURoof[d][i], 4)));
                        if (branchKRoof != null) branchKRoof.Add(new GH_Number(Math.Round(perDirKRoof[d][i], 6)));
                    }

                    speedTree.AppendRange(branchSpeeds, path);
                    if (branchK     != null) kTree.AppendRange(branchK, path);
                    if (branchURoof != null) uRoofTree.AppendRange(branchURoof, path);
                    if (branchKRoof != null) kRoofTree.AppendRange(branchKRoof, path);
                    coordsCollected = true;
                }

                sw.Stop();

                    if (kTree.PathCount == 0)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ONNX model has a single output channel (U only). For turbulent kinetic energy (k) output, use the advanced U with TKE model.");
                    if (showRoof && uRoofTree.PathCount == 0)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Show Roof requested but the ONNX model is not 4-channel — falling back to pedestrian-level visualization.");

                    // ── Preview Mesh & Outputs ──
                    // Pedestrian-level mesh is ALWAYS shown (driven by ShowK).
                    // ShowRoof adds an *additional* mesh at the actual building rooftop heights,
                    // only on building footprints. It does not replace or hide the pedestrian mesh.
                    bool roofAvailable = uRoofTree.PathCount > 0 && uRoofTree.Branches[0].Count > 0;
                    bool kAvailable    = kTree.PathCount > 0 && kTree.Branches[0].Count > 0;
                    bool vizK    = showK && kAvailable;

                    GH_Structure<GH_Number> vizTree = vizK ? kTree : speedTree;
                    string vizUnit = vizK ? "m²/s²" : "m/s";

                    // Optional roof overlay (added on top of pedestrian mesh at correct world-Z)
                    GH_Structure<GH_Number> roofVizTree = null;
                    if (showRoof && roofAvailable)
                        roofVizTree = vizK ? kRoofTree : uRoofTree;

                    // If user requested TKE but the model has no k output, skip visualization entirely.
                    bool skipViz = showK && !kAvailable;
                    if (skipViz)
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                            "Turbulent Kinetic Energy field selected but this ONNX model does not output TKE — visualization mesh skipped. Use a U+k or U+k+roof model for TKE visualization.");

                    if (windDirs.Count == 1 && !skipViz)
                    {
                        double validMin = double.MaxValue, validMax = double.MinValue;
                        if (domainProvided) { validMin = customDomain.Min; validMax = customDomain.Max; }
                        else
                        {
                            if (vizTree.PathCount > 0)
                            {
                                foreach (var val in vizTree.Branches[0])
                                {
                                    if (val.Value < validMin) validMin = val.Value;
                                    if (val.Value > validMax) validMax = val.Value;
                                }
                            }
                            // Include roof values in the range so colors stay consistent across both meshes
                            if (roofVizTree != null && roofVizTree.PathCount > 0)
                            {
                                foreach (var val in roofVizTree.Branches[0])
                                {
                                    if (val.Value < validMin) validMin = val.Value;
                                    if (val.Value > validMax) validMax = val.Value;
                                }
                            }
                        }

                        Func<int, int, double[,], bool[,], double> GetSmoothedValue = (cy, cx, grid, valid) =>
                        {
                            double sum = 0;
                            int count = 0;
                            if (cy - 1 >= 0 && cx - 1 >= 0 && valid[cy - 1, cx - 1]) { sum += grid[cy - 1, cx - 1]; count++; }
                            if (cy - 1 >= 0 && cx < IMG_W && valid[cy - 1, cx]) { sum += grid[cy - 1, cx]; count++; }
                            if (cy < IMG_H && cx - 1 >= 0 && valid[cy, cx - 1]) { sum += grid[cy, cx - 1]; count++; }
                            if (cy < IMG_H && cx < IMG_W && valid[cy, cx]) { sum += grid[cy, cx]; count++; }
                            return count > 0 ? sum / count : 0.0;
                        };

                        var firstVals = vizTree.Branches[0];
                        double halfX = X_STEP / 2.0;
                        double halfY = Y_STEP / 2.0;

                        double[,] valGrid = null;
                        bool[,] validGrid = null;
                        if (interpolate)
                        {
                            valGrid = new double[IMG_H, IMG_W];
                            validGrid = new bool[IMG_H, IMG_W];
                            for (int j = 0; j < outX.Count; j++)
                            {
                                int origIdx = outOriginalIndex[j];
                                valGrid[idxYArr[origIdx], idxXArr[origIdx]] = firstVals[j].Value;
                                validGrid[idxYArr[origIdx], idxXArr[origIdx]] = true;
                            }
                        }

                        for (int j = 0; j < outX.Count; j++)
                        {
                            int origIdx = outOriginalIndex[j];
                            int ix = idxXArr[origIdx];
                            int iy = idxYArr[origIdx];

                            Point3d pt = points[origIdx];
                            int vc = previewMesh.Vertices.Count;
                            
                            previewMesh.Vertices.Add(pt.X - halfX, pt.Y - halfY, pt.Z);
                            previewMesh.Vertices.Add(pt.X + halfX, pt.Y - halfY, pt.Z);
                            previewMesh.Vertices.Add(pt.X + halfX, pt.Y + halfY, pt.Z);
                            previewMesh.Vertices.Add(pt.X - halfX, pt.Y + halfY, pt.Z);
                            previewMesh.Faces.AddFace(vc, vc + 1, vc + 2, vc + 3);

                            if (interpolate)
                            {
                                double wBL = GetSmoothedValue(iy, ix, valGrid, validGrid);
                                double wBR = GetSmoothedValue(iy, ix + 1, valGrid, validGrid);
                                double wTR = GetSmoothedValue(iy + 1, ix + 1, valGrid, validGrid);
                                double wTL = GetSmoothedValue(iy + 1, ix, valGrid, validGrid);

                                double tBL = validMax > validMin ? (wBL - validMin) / (validMax - validMin) : 0.0;
                                double tBR = validMax > validMin ? (wBR - validMin) / (validMax - validMin) : 0.0;
                                double tTR = validMax > validMin ? (wTR - validMin) / (validMax - validMin) : 0.0;
                                double tTL = validMax > validMin ? (wTL - validMin) / (validMax - validMin) : 0.0;

                                previewMesh.VertexColors.Add(GetColorFromPalette(tBL, customColors));
                                previewMesh.VertexColors.Add(GetColorFromPalette(tBR, customColors));
                                previewMesh.VertexColors.Add(GetColorFromPalette(tTR, customColors));
                                previewMesh.VertexColors.Add(GetColorFromPalette(tTL, customColors));
                            }
                            else
                            {
                                double w = firstVals[j].Value;
                                double t = validMax > validMin ? (w - validMin) / (validMax - validMin) : 0.0;
                                Color c = GetColorFromPalette(t, customColors);
                                previewMesh.VertexColors.Add(c);
                                previewMesh.VertexColors.Add(c);
                                previewMesh.VertexColors.Add(c);
                                previewMesh.VertexColors.Add(c);
                            }
                        }

                        // ── Optional roof-level overlay ──
                        // Append small tiles at each building's actual rooftop world-Z, only where
                        // a building footprint exists. Pedestrian mesh below stays untouched.
                        if (roofVizTree != null && roofVizTree.PathCount > 0)
                        {
                            var roofVals = roofVizTree.Branches[0];
                            int nRoofElements = 0;

                            double[,] roofValGrid = null;
                            bool[,] roofValidGrid = null;
                            if (interpolate)
                            {
                                roofValGrid = new double[IMG_H, IMG_W];
                                roofValidGrid = new bool[IMG_H, IMG_W];
                                for (int j = 0; j < outX.Count && j < roofVals.Count; j++)
                                {
                                    int origIdx = outOriginalIndex[j];
                                    double rawRoofZ = bldgHeightArr[origIdx];
                                    if (rawRoofZ > 0.0)
                                    {
                                        roofValGrid[idxYArr[origIdx], idxXArr[origIdx]] = roofVals[j].Value;
                                        roofValidGrid[idxYArr[origIdx], idxXArr[origIdx]] = true;
                                    }
                                }
                            }

                            for (int j = 0; j < outX.Count && j < roofVals.Count; j++)
                            {
                                int origIdx = outOriginalIndex[j];
                                double rawRoofZ = bldgHeightArr[origIdx];
                                if (rawRoofZ <= 0.0) continue;
                                double zRoof = rawRoofZ + locPedestrianLevel;

                                int ix = idxXArr[origIdx];
                                int iy = idxYArr[origIdx];

                                Point3d ptR = points[origIdx];
                                int vcR = previewMesh.Vertices.Count;
                                previewMesh.Vertices.Add(ptR.X - halfX, ptR.Y - halfY, zRoof);
                                previewMesh.Vertices.Add(ptR.X + halfX, ptR.Y - halfY, zRoof);
                                previewMesh.Vertices.Add(ptR.X + halfX, ptR.Y + halfY, zRoof);
                                previewMesh.Vertices.Add(ptR.X - halfX, ptR.Y + halfY, zRoof);
                                previewMesh.Faces.AddFace(vcR, vcR + 1, vcR + 2, vcR + 3);

                                if (interpolate)
                                {
                                    double wBL = GetSmoothedValue(iy, ix, roofValGrid, roofValidGrid);
                                    double wBR = GetSmoothedValue(iy, ix + 1, roofValGrid, roofValidGrid);
                                    double wTR = GetSmoothedValue(iy + 1, ix + 1, roofValGrid, roofValidGrid);
                                    double wTL = GetSmoothedValue(iy + 1, ix, roofValGrid, roofValidGrid);

                                    double tBL = validMax > validMin ? (wBL - validMin) / (validMax - validMin) : 0.0;
                                    double tBR = validMax > validMin ? (wBR - validMin) / (validMax - validMin) : 0.0;
                                    double tTR = validMax > validMin ? (wTR - validMin) / (validMax - validMin) : 0.0;
                                    double tTL = validMax > validMin ? (wTL - validMin) / (validMax - validMin) : 0.0;

                                    previewMesh.VertexColors.Add(GetColorFromPalette(tBL, customColors));
                                    previewMesh.VertexColors.Add(GetColorFromPalette(tBR, customColors));
                                    previewMesh.VertexColors.Add(GetColorFromPalette(tTR, customColors));
                                    previewMesh.VertexColors.Add(GetColorFromPalette(tTL, customColors));
                                }
                                else
                                {
                                    double w = roofVals[j].Value;
                                    double t = validMax > validMin ? (w - validMin) / (validMax - validMin) : 0.0;
                                    Color cr = GetColorFromPalette(t, customColors);
                                    previewMesh.VertexColors.Add(cr);
                                    previewMesh.VertexColors.Add(cr);
                                    previewMesh.VertexColors.Add(cr);
                                    previewMesh.VertexColors.Add(cr);
                                }
                                nRoofElements++;
                            }

                            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                                $"Roof overlay: {nRoofElements} {(interpolate ? "vertices" : "tiles")} at building roof heights ({(vizK ? "k_roof" : "U_roof")}).");
                        }

                        if (validMax > validMin && outX.Count > 0)
                        {
                            double legMinPx = outX.Min();
                            double legMaxPx = outX.Max();
                            double legWidth = Math.Max((legMaxPx - legMinPx) * 0.5, 400.0);
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
                                    legendVals.Add($"{val:F2} {vizUnit}");
                                    legendPts.Add(new Point3d(x, startY - legHeight * 1.5, zLevel));
                                }
                            }
                        }
                    }

                    DA.SetDataList(0, outX);
                    DA.SetDataList(1, outY);
                    // Output the field selected by the Field input
                    DA.SetDataTree(2, showK && kAvailable ? kTree : speedTree);
                    DA.SetData(3, previewMesh);
                    DA.SetData(4, legendMesh);
                    DA.SetDataList(5, legendPts);
                    DA.SetDataList(6, legendVals);

                    // Output boundary conditions for downstream components (pass-through)
                    // Ensure SimulatedDirections is populated on each BC for downstream use
                    foreach (var bc in inputBCCollection.BCs)
                    {
                        if (bc.SimulatedDirections == null || bc.SimulatedDirections.Count == 0)
                            bc.SimulatedDirections = new System.Collections.Generic.List<double>(windDirs);
                    }
                    DA.SetData(7, inputBCCollection);

                    // Roof-level output (4-channel models only — empty tree otherwise)
                    if (showK && roofAvailable)
                        DA.SetDataTree(8, kRoofTree);
                    else
                        DA.SetDataTree(8, uRoofTree);

                    int activeChannels = uRoofTree.PathCount > 0 ? 4 : (kTree.PathCount > 0 ? 2 : 1);
                    Message = $"Dirs: {windDirs.Count} | {activeChannels}ch | {sw.ElapsedMilliseconds} ms";

                    long avgPerDir = sw.ElapsedMilliseconds / Math.Max(1, windDirs.Count);
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        $"Inference complete for {windDirs.Count} directions ({activeChannels}-channel model) " +
                        $"in {sw.ElapsedMilliseconds} ms (mode: {inferenceMode}, inference-only: {inferenceOnlyMs} ms, " +
                        $"avg: {avgPerDir} ms/dir, threads: {inferenceThreads}).");
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"ONNX inference failed: {ex.Message}");
            }
        }

        // Clean up ONNX session when component is removed
        public override void RemovedFromDocument(GH_Document document)
        {
            if (_sessions != null)
            {
                foreach (var s in _sessions) s?.Dispose();
            }
            _sessions = null;
            _cachedOnnxPath = null;
            base.RemovedFromDocument(document);
        }
    }
}
