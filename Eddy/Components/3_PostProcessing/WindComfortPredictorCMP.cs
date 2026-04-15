using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EddyLib;
using EddyLib.BCs;
using EddyLib.OutdoorComfort;

namespace Eddy
{
    public class WindComfortPredictorCMP : GH_Component
    {
        // ── Results cache ─────────────────────────────────────────────────────────
        // Key: fingerprint of all heavy inputs (speeds, dirs, EPW, params).
        // Value: per-metric result arrays so a metric toggle is instant.
        private string _cacheKey = null;
        private double[] _weibullKappas = null;
        private double[] _weibullLambdas = null;
        private readonly Dictionary<int, (double[] ranks, string[] letters, string[] classes)>
            _metricCache = new Dictionary<int, (double[], string[], string[])>();

        public WindComfortPredictorCMP()
          : base("Wind Comfort Predictor (ML)", "WindComfortML",
              "Calculate Pedestrian Wind Comfort using predicted wind fields from the ONNX model.",
              "Eddy3D", "4 | ML")
        {
        }

        public override Guid ComponentGuid => new Guid("{C9D0E1F2-3B4C-5D6E-7F8A-9B0C1D2E3F4A}");

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Eddy_windFactors;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Wind Speeds", "W", "Predicted wind speeds (m/s) as a DataTree from WindPredictor.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Wind Directions", "Dirs", "Simulated wind directions (degrees) corresponding to the branches.", GH_ParamAccess.list);
            pManager.AddTextParameter("EPW Path", "EPW", "Path to the .epw weather file.", GH_ParamAccess.item);
            pManager.AddNumberParameter("z_ref", "z_ref", "Reference height for the simulations (m). Default = 10.0", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("z_0", "z_0", "Roughness length (m). Default = 0.1", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("U_ref_sim", "Uref_sim", "Reference wind speed used during simulation/prediction (m/s). Default = 3.0", GH_ParamAccess.item, 3.0);
            pManager.AddIntegerParameter("Metric", "Metric", "Comfort metric to use.", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Interpolate", "Interp", "Interpolate between wind directions. Default = true", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Run", "Run", "Run the comfort prediction calculation.", GH_ParamAccess.item, false);

            var types = Enum.GetNames(typeof(WindComfortHelper.PedCmftMetric));
            Param_Integer param = pManager[6] as Param_Integer;
            for (int i = 0; i < types.Length; i++) param.AddNamedValue(types[i], i);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Comfort Rank", "Rank", "Wind comfort rank (integer).", GH_ParamAccess.list);
            pManager.AddTextParameter("Class Letter", "Letter", "Wind comfort class letter (e.g., A, B, C).", GH_ParamAccess.list);
            pManager.AddTextParameter("Class", "Class", "Wind comfort class description.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<GH_Number> speedTree;
            var windDirs = new List<double>();
            string epwPath = "";
            double zRef = 10.0;
            double z0 = 0.1;
            double uRefSim = 3.0;
            int metricInt = 0;
            bool interpolate = true;
            bool run = false;

            if (!DA.GetDataTree(0, out speedTree)) return;
            if (!DA.GetDataList(1, windDirs)) return;
            if (!DA.GetData(2, ref epwPath)) return;
            DA.GetData(3, ref zRef);
            DA.GetData(4, ref z0);
            DA.GetData(5, ref uRefSim);
            DA.GetData(6, ref metricInt);
            DA.GetData(7, ref interpolate);
            DA.GetData(8, ref run);

            if (!run)
            {
                Message = "Paused";
                return;
            }

            if (speedTree.PathCount == 0 || windDirs.Count == 0) return;
            if (!System.IO.File.Exists(epwPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "EPW file not found.");
                return;
            }

            int numProbes = speedTree.Branches[0].Count;
            int numDirs   = windDirs.Count;
            int numHours  = 8760;

            // ── Cache fingerprint (everything except metric) ──────────────────────
            // Sample a few speed values so we don't hash 128k numbers every solve.
            double s0 = speedTree.Branches[0].Count > 0 ? speedTree.Branches[0][0].Value : 0;
            double sN = speedTree.Branches[0].Count > 0 ? speedTree.Branches[0][numProbes - 1].Value : 0;
            string newCacheKey = $"{numProbes}|{numDirs}|{epwPath}|{zRef}|{z0}|{uRefSim}|{interpolate}|{s0:R}|{sN:R}";

            // ── Instant return when only metric changed ───────────────────────────
            if (newCacheKey == _cacheKey && _metricCache.TryGetValue(metricInt, out var hit))
            {
                DA.SetDataList(0, hit.ranks);
                DA.SetDataList(1, hit.letters);
                DA.SetDataList(2, hit.classes);
                var metricLabel = (WindComfortHelper.PedCmftMetric)metricInt;
                Message = $"Metric: {metricLabel} (cached)";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"Returned cached results for {numProbes} probes.");
                return;
            }

            // Heavy inputs changed → clear per-metric cache
            if (newCacheKey != _cacheKey)
            {
                _cacheKey = newCacheKey;
                _metricCache.Clear();
            }

            // 1. Load Weather
            Weather weather;
            try { weather = new Weather(epwPath); }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to load EPW: {ex.Message}");
                return;
            }

            // 2. Build spatial factors [probe, dir]
            double hProbe = 1.8;
            double refScale = BC.ScaleABL(uRefSim, zRef, z0, hProbe);
            var spatialFactors = new double[numProbes, numDirs];
            for (int d = 0; d < numDirs; d++)
            {
                var branch = d < speedTree.PathCount ? speedTree.Branches[d] : null;
                if (branch == null) continue;
                int n = Math.Min(numProbes, branch.Count);
                for (int p = 0; p < n; p++)
                    spatialFactors[p, d] = branch[p].Value / refScale;
            }

            // 3. Pre-scale EPW speeds once
            var epwScaled = new double[numHours];
            for (int h = 0; h < numHours; h++)
                epwScaled[h] = BC.ScaleABL(weather.WindSpeed[h], zRef, z0, hProbe);

            // 4. Pre-compute per-hour direction interpolation weights ───────────────
            // This moves WindSystem lookups (O(numDirs) each) OUT of the 128k-probe loop.
            // Cost: O(8760 × numDirs) instead of O(numProbes × 8760 × numDirs).
            int[] simulatedDirsInt = windDirs.Select(d => (int)Math.Round(d)).ToArray();

            var hourLowIdx = new int[numHours];
            var hourHighIdx = new int[numHours];
            var hourLowWt   = new double[numHours];
            var hourHighWt  = new double[numHours];

            if (!interpolate)
            {
                // Non-interpolated: snap each EPW hour to the closest simulated direction
                var closest = EddyLib.OutdoorComfort.WindSystem.GetClosestWindDirs(weather, simulatedDirsInt);
                int[] snapped = closest.Item1;
                for (int h = 0; h < numHours; h++)
                {
                    hourLowIdx[h]  = snapped[h];
                    hourHighIdx[h] = snapped[h];
                    hourLowWt[h]   = 1.0;
                    hourHighWt[h]  = 0.0;
                }
            }
            else
            {
                // Interpolated: compute angular weights once per EPW hour
                for (int h = 0; h < numHours; h++)
                {
                    int weatherDir = weather.WindDirection[h];
                    int il = WindSystem.ReturnNextLowerIndex(simulatedDirsInt, weatherDir);
                    int ih = WindSystem.ReturnNextHigherIndex(simulatedDirsInt, weatherDir);
                    int dl = simulatedDirsInt[il];
                    int dh = simulatedDirsInt[ih];

                    hourLowIdx[h]  = il;
                    hourHighIdx[h] = ih;

                    if (dl == dh)
                    {
                        hourLowWt[h]  = 1.0;
                        hourHighWt[h] = 0.0;
                    }
                    else
                    {
                        double distL = WindSystem.DistanceBetweenWindDirs(weatherDir, dl);
                        double distH = WindSystem.DistanceBetweenWindDirs(weatherDir, dh);
                        double sum   = distL + distH;
                        hourLowWt[h]  = 1.0 - (distL / sum);
                        hourHighWt[h] = 1.0 - (distH / sum);
                    }
                }
            }

            // 5. Compute metric thresholds
            var metric = (WindComfortHelper.PedCmftMetric)metricInt;
            var tid = WindComfortMetricsWeibull.ThresholdInfo(metric);

            // 6. Parallel Weibull exceedance
            var ranks   = new double[numProbes];
            var letters = new string[numProbes];
            var classes = new string[numProbes];

            // If we have valid pre-calculated Weibull parameters for this hash, use them
            bool useCachedParams = (_weibullKappas != null && _weibullLambdas != null && _weibullKappas.Length == numProbes);
            if (!useCachedParams)
            {
                _weibullKappas = new double[numProbes];
                _weibullLambdas = new double[numProbes];
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            using (var tlBuf = new ThreadLocal<double[]>(() => new double[numHours]))
            {
                Parallel.For(0, numProbes, p =>
                {
                    if (!useCachedParams)
                    {
                        double[] buf = tlBuf.Value;
                        for (int h = 0; h < numHours; h++)
                        {
                            double ratio = spatialFactors[p, hourLowIdx[h]]  * hourLowWt[h]
                                         + spatialFactors[p, hourHighIdx[h]] * hourHighWt[h];
                            buf[h] = epwScaled[h] * ratio;
                        }
                        WindComfortMetricsWeibull.GetWeibullParams(buf, out double kappa, out double lambda);
                        _weibullKappas[p] = kappa;
                        _weibullLambdas[p] = lambda;
                    }

                    var result = WindComfortMetricsWeibull.CalcExceedanceFromParams(_weibullKappas[p], _weibullLambdas[p], tid);
                    ranks[p]   = result.Cat;
                    letters[p] = result.ClassLetter;
                    classes[p] = result.Class;
                });
            }

            sw.Stop();

            // 7. Cache and output
            _metricCache[metricInt] = (ranks, letters, classes);

            DA.SetDataList(0, ranks);
            DA.SetDataList(1, letters);
            DA.SetDataList(2, classes);

            Message = $"Metric: {metric}";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                useCachedParams ? $"Metric checked {numProbes} probes in {sw.ElapsedMilliseconds} ms." : $"Weibull estimated {numProbes} probes in {sw.ElapsedMilliseconds} ms.");
        }
    }
}
