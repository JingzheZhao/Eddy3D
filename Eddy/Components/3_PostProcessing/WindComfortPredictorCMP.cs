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
        private double[] _weibullKappasRoof = null;
        private double[] _weibullLambdasRoof = null;
        private readonly Dictionary<int, (double[] ranks, string[] letters, string[] classes, double[] ranksRoof, string[] lettersRoof, string[] classesRoof, Mesh comfortMesh, Mesh legendMesh, List<Point3d> legendPts, List<string> legendLetters, string metricName, List<string> classExps)>
            _metricCache = new Dictionary<int, (double[] ranks, string[] letters, string[] classes, double[] ranksRoof, string[] lettersRoof, string[] classesRoof, Mesh comfortMesh, Mesh legendMesh, List<Point3d> legendPts, List<string> legendLetters, string metricName, List<string> classExps)>();

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
            pManager.AddPointParameter("Points", "Pts", "Analysis points for mesh visualization.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Wind Speeds", "W", "Predicted wind speeds (m/s) as a DataTree from WindPredictor.", GH_ParamAccess.tree);
            pManager.AddTextParameter("EPW Path", "EPW", "Path to the .epw weather file.", GH_ParamAccess.item);
            pManager.AddNumberParameter("z_ref", "z_ref", "Reference height for the simulations (m). Default = 10.0", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("z_0", "z_0", "Roughness length (m). Default = 1.0", GH_ParamAccess.item, 1.0);
            pManager.AddIntegerParameter("Metric", "Metric", "Comfort metric to use.", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Interpolate", "Interp", "Interpolate between wind directions. Default = true", GH_ParamAccess.item, true);
            pManager.AddBooleanParameter("Fast Mode (MoM)", "Fast", "Use Method of Moments for ultra-fast Weibull estimation. Default = true", GH_ParamAccess.item, true);
            pManager.AddGenericParameter("Boundary Conditions", "BC", "Optional simulation metadata to automate z_ref, z_0, and U_ref_sim.", GH_ParamAccess.item);
            pManager.AddNumberParameter("TKE", "k", "Turbulent kinetic energy (m²/s²) as a DataTree from WindPredictor. When provided, GEM (Gust Equivalent Mean) is used: GEM = U + g × √(2k/3). Peak factor g is auto-set per metric.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Wind Speeds (Roof)", "W_roof",
                "Optional roof-level wind speeds (m/s) DataTree.",
                GH_ParamAccess.tree);
            pManager.AddNumberParameter("TKE (Roof)", "k_roof",
                "Optional roof-level TKE (m²/s²) DataTree.",
                GH_ParamAccess.tree);
            pManager.AddGeometryParameter("Buildings", "Buildings",
                "Optional list of Brep or Mesh objects representing buildings. Needed to elevate roof-level comfort meshes to correct heights.",
                GH_ParamAccess.list);

            var types = Enum.GetNames(typeof(WindComfortHelper.PedCmftMetric));
            Param_Integer param = pManager[5] as Param_Integer;
            for (int i = 0; i < types.Length; i++) param.AddNamedValue(types[i], i);
            pManager[8].Optional  = true;
            pManager[9].Optional  = true;
            pManager[10].Optional = true;
            pManager[11].Optional = true;
            pManager[12].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Comfort Rank", "Rank", "Wind comfort rank (integer).", GH_ParamAccess.list);
            pManager.AddTextParameter("Class Letter", "Letter", "Wind comfort class letter (e.g., A, B, C).", GH_ParamAccess.list);
            pManager.AddTextParameter("Class", "Class", "Wind comfort class description.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Comfort Mesh", "M", "Colored mesh representing comfort levels.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Legend Mesh", "LM", "Legend mesh for comfort categories.", GH_ParamAccess.item);
            pManager.AddPointParameter("Legend Points", "LP", "Label points for the legend letters.", GH_ParamAccess.list);
            pManager.AddTextParameter("Legend Letters", "LV", "Letters (A-S) for the legend.", GH_ParamAccess.list);
            pManager.AddTextParameter("Metric Name", "MetricName", "The name of the currently active comfort/safety standard.", GH_ParamAccess.item);
            pManager.AddTextParameter("Category Explanations", "Explanations", "Detailed descriptions for each class (e.g., A: Sitting Long).", GH_ParamAccess.list);
            pManager.AddNumberParameter("Comfort Rank (Roof)", "Rank_Roof", "Wind comfort rank (integer) for roof level.", GH_ParamAccess.list);
            pManager.AddTextParameter("Class Letter (Roof)", "Letter_Roof", "Wind comfort class letter for roof level.", GH_ParamAccess.list);
            pManager.AddTextParameter("Class (Roof)", "Class_Roof", "Wind comfort class description for roof level.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var points = new List<Point3d>();
            GH_Structure<GH_Number> speedTree;
            var windDirs = new List<double>();
            string epwPath = "";
            double zRef = 10.0;
            double z0 = 1.0;
            double uRefSim = 5.0;
            int metricInt = 0;
            bool interpolate = true;
            bool useMoM = true;

            GH_Structure<GH_Number> kTree = null;
            GH_Structure<GH_Number> wRoofTree = null;
            GH_Structure<GH_Number> kRoofTree = null;
            var geometryList = new List<GeometryBase>();

            if (!DA.GetDataList(0, points)) return;
            if (!DA.GetDataTree(1, out speedTree)) return;
            if (!DA.GetData(2, ref epwPath)) return;
            DA.GetData(5, ref metricInt);
            DA.GetData(6, ref interpolate);
            DA.GetData(7, ref useMoM);
            DA.GetDataTree(9, out kTree);
            DA.GetDataTree(10, out wRoofTree);
            DA.GetDataTree(11, out kRoofTree);
            DA.GetDataList(12, geometryList);

            bool doRoof = wRoofTree != null && wRoofTree.PathCount > 0 && wRoofTree.Branches[0].Count > 0;
            bool hasKRoof = doRoof && kRoofTree != null && kRoofTree.PathCount > 0 && kRoofTree.Branches[0].Count > 0;

            bool hasK = kTree != null && kTree.PathCount > 0 && kTree.Branches[0].Count > 0;
            double peakFactor = GetPeakFactor((WindComfortHelper.PedCmftMetric)metricInt);

            // ── Automated Scenario Link ───────────────────────────────────────────
            object bcRaw = null;
            bool bcLinked = DA.GetData(8, ref bcRaw) && bcRaw != null;
            if (bcLinked)
            {
                // Unwrap GH_ObjectWrapper if the generic parameter wraps it
                if (bcRaw is Grasshopper.Kernel.Types.GH_ObjectWrapper wrapper)
                    bcRaw = wrapper.Value;

                EddyLib.BCs.BC firstBC = null;
                EddyLib.BCs.BCCollection bcColl = null;

                if (bcRaw is EddyLib.BCs.BCCollection bcc && bcc.BCs.Count > 0)
                {
                    bcColl = bcc;
                    firstBC = bcc.BCs[0];
                    // Extract wind directions from the collection
                    windDirs = bcc.WindDirections.Select(d => (double)d).ToList();
                    // Use EPW path from the collection if available and user didn't override
                    if (!string.IsNullOrEmpty(bcc.epwFilePath) && (string.IsNullOrEmpty(epwPath) || !System.IO.File.Exists(epwPath)))
                        epwPath = bcc.epwFilePath;
                }
                else if (bcRaw is EddyLib.BCs.ABL linkedABL)
                {
                    firstBC = linkedABL;
                    if (linkedABL.SimulatedDirections != null && linkedABL.SimulatedDirections.Count > 0)
                        windDirs = new List<double>(linkedABL.SimulatedDirections);
                    if (!string.IsNullOrEmpty(linkedABL.EPWPath) && (string.IsNullOrEmpty(epwPath) || !System.IO.File.Exists(epwPath)))
                        epwPath = linkedABL.EPWPath;
                }
                else if (bcRaw is EddyLib.BCs.BC linkedBC)
                {
                    firstBC = linkedBC;
                    if (linkedBC.SimulatedDirections != null && linkedBC.SimulatedDirections.Count > 0)
                        windDirs = new List<double>(linkedBC.SimulatedDirections);
                }

                if (firstBC != null)
                {
                    z0 = firstBC.z0;
                    uRefSim = firstBC.URef;

                    if (firstBC is EddyLib.BCs.ABL ablTyped)
                        zRef = ablTyped.zref;

                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        $"Using automated Boundary Conditions: Uref={uRefSim}, zref={zRef}, z0={z0}" +
                        (bcColl != null ? $", {bcColl.BCs.Count} direction(s)" : ""));
                }
            }

            if (windDirs == null || windDirs.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No wind directions provided from simulation link.");
                return;
            }
            else
            {
                DA.GetData(3, ref zRef);
                DA.GetData(4, ref z0);
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
            double k0 = hasK && kTree.Branches[0].Count > 0 ? kTree.Branches[0][0].Value : -1;
            string newCacheKey = $"{numProbes}|{numDirs}|{epwPath}|{zRef}|{z0}|{uRefSim}|{interpolate}|{useMoM}|{s0:R}|{sN:R}|{hasK}|{peakFactor:R}|{k0:R}|{doRoof}|{geometryList.Count}";
 
            // ── Instant return when only metric changed ───────────────────────────
            if (newCacheKey == _cacheKey && _metricCache.TryGetValue(metricInt, out var hit))
            {
                DA.SetDataList(0, hit.ranks);
                DA.SetDataList(1, hit.letters);
                DA.SetDataList(2, hit.classes);
                DA.SetData(3, hit.comfortMesh);
                DA.SetData(4, hit.legendMesh);
                DA.SetDataList(5, hit.legendPts);
                DA.SetDataList(6, hit.legendLetters);
                DA.SetData(7, hit.metricName);
                DA.SetDataList(8, hit.classExps);
                DA.SetDataList(9, hit.ranksRoof);
                DA.SetDataList(10, hit.lettersRoof);
                DA.SetDataList(11, hit.classesRoof);

                Message = $"v0.8.0\nMetric: {hit.metricName} (cached)";
                return;
            }

            // Heavy inputs changed → clear per-metric cache and math buffers
            if (newCacheKey != _cacheKey)
            {
                _cacheKey = newCacheKey;
                _metricCache.Clear();
                _weibullKappas = null;
                _weibullLambdas = null;
                _weibullKappasRoof = null;
                _weibullLambdasRoof = null;
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
            double logRef = Math.Log((zRef + z0) / z0);
            double logProbe = Math.Log((hProbe + z0) / z0);
            double ablFactor = logProbe / logRef; // Pre-calculated constant factor

            var mergedMesh = new Mesh();
            if (doRoof && geometryList.Count > 0)
            {
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
                            if (bm.Faces.Count > 0) mergedMesh.Append(bm);
                        }
                    }
                    else if (geo is Mesh mesh && mesh.IsValid) mergedMesh.Append(mesh);
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
                                if (bm.Faces.Count > 0) mergedMesh.Append(bm);
                            }
                        }
                    }
                }
            }

            var bldgHeightArr = new double[numProbes];
            if (doRoof && mergedMesh.IsValid && mergedMesh.Faces.Count > 0)
            {
                Parallel.For(0, numProbes, p =>
                {
                    var pt = points[p];
                    var verticalRay = new Ray3d(new Point3d(pt.X, pt.Y, 1000), -Vector3d.ZAxis);
                    double tVal = Rhino.Geometry.Intersect.Intersection.MeshRay(mergedMesh, verticalRay);
                    if (tVal >= 0.0) bldgHeightArr[p] = 1000.0 - tVal;
                });
            }

            var spatialFactors = new double[numProbes, numDirs];
            var kValues = hasK ? new double[numProbes, numDirs] : null;
            var spatialFactorsRoof = doRoof ? new double[numProbes, numDirs] : null;
            var kValuesRoof = hasKRoof ? new double[numProbes, numDirs] : null;

            Parallel.For(0, numDirs, d =>
            {
                var branch = d < speedTree.PathCount ? speedTree.Branches[d] : null;
                var kBranch = hasK && d < kTree.PathCount ? kTree.Branches[d] : null;
                var roofBranch = doRoof && d < wRoofTree.PathCount ? wRoofTree.Branches[d] : null;
                var roofKBranch = hasKRoof && d < kRoofTree.PathCount ? kRoofTree.Branches[d] : null;

                if (branch != null)
                {
                    int n = Math.Min(numProbes, branch.Count);
                    for (int p = 0; p < n; p++)
                    {
                        spatialFactors[p, d] = branch[p].Value / (uRefSim * ablFactor);
                        if (kBranch != null && p < kBranch.Count)
                            kValues[p, d] = Math.Max(kBranch[p].Value, 0.0);
                        
                        if (doRoof && roofBranch != null && p < roofBranch.Count)
                        {
                            spatialFactorsRoof[p, d] = roofBranch[p].Value / (uRefSim * ablFactor);
                            if (roofKBranch != null && p < roofKBranch.Count)
                                kValuesRoof[p, d] = Math.Max(roofKBranch[p].Value, 0.0);
                        }
                    }
                }
            });

            if (hasK)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"GEM mode: g={peakFactor:F1} (auto-set for {(WindComfortHelper.PedCmftMetric)metricInt}), TKE branches={kTree.PathCount}");
            else
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No TKE (k) input connected — using mean wind speed only. Comfort results assume spatially uniform turbulence intensity (~20%). For GEM-based gust-accurate assessment, use the advanced U with TKE model.");

            // 3. Pre-scale EPW speeds once
            var epwScaled = new double[numHours];
            for (int h = 0; h < numHours; h++)
                epwScaled[h] = weather.WindSpeed[h] * ablFactor;

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
            var ranksRoof   = new double[numProbes];
            var lettersRoof = new string[numProbes];
            var classesRoof = new string[numProbes];

            // If we have valid pre-calculated Weibull parameters for this hash, use them
            bool useCachedParams = (_weibullKappas != null && _weibullLambdas != null && _weibullKappas.Length == numProbes);
            if (!useCachedParams)
            {
                _weibullKappas = new double[numProbes];
                _weibullLambdas = new double[numProbes];
                if (doRoof)
                {
                    _weibullKappasRoof = new double[numProbes];
                    _weibullLambdasRoof = new double[numProbes];
                }
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            using (var tlBuf = new ThreadLocal<double[]>(() => new double[numHours]))
            using (var tlBufRoof = new ThreadLocal<double[]>(() => new double[numHours]))
            using (var tlWork = new ThreadLocal<double[]>(() => new double[numHours]))
            {
                Parallel.For(0, numProbes, p =>
                {
                    if (!useCachedParams)
                    {
                        double[] buf = tlBuf.Value;
                        double[] bufRoof = doRoof ? tlBufRoof.Value : null;
                        double[] work = tlWork.Value;
                        for (int h = 0; h < numHours; h++)
                        {
                            int iL = hourLowIdx[h];
                            int iH = hourHighIdx[h];
                            double wL = hourLowWt[h];
                            double wH = hourHighWt[h];

                            double ratio = spatialFactors[p, iL] * wL + spatialFactors[p, iH] * wH;
                            double uMean = epwScaled[h] * ratio;

                            if (kValues != null)
                            {
                                double kInterp = kValues[p, iL] * wL + kValues[p, iH] * wH;
                                double sigmaU = Math.Sqrt(2.0 * kInterp / 3.0);
                                buf[h] = uMean + peakFactor * sigmaU;
                            }
                            else buf[h] = uMean;

                            if (doRoof)
                            {
                                double ratioRoof = spatialFactorsRoof[p, iL] * wL + spatialFactorsRoof[p, iH] * wH;
                                double uMeanRoof = epwScaled[h] * ratioRoof;
                                if (kValuesRoof != null)
                                {
                                    double kInterpRoof = kValuesRoof[p, iL] * wL + kValuesRoof[p, iH] * wH;
                                    double sigmaURoof = Math.Sqrt(2.0 * kInterpRoof / 3.0);
                                    bufRoof[h] = uMeanRoof + peakFactor * sigmaURoof;
                                }
                                else bufRoof[h] = uMeanRoof;
                            }
                        }
                        
                        if (useMoM)
                        {
                            WindComfortMetricsWeibull.GetWeibullParamsMoM(buf, out double kappa, out double lambda);
                            _weibullKappas[p] = kappa;
                            _weibullLambdas[p] = lambda;
                            if (doRoof)
                            {
                                WindComfortMetricsWeibull.GetWeibullParamsMoM(bufRoof, out double kappaR, out double lambdaR);
                                _weibullKappasRoof[p] = kappaR;
                                _weibullLambdasRoof[p] = lambdaR;
                            }
                        }
                        else
                        {
                            WindComfortMetricsWeibull.GetWeibullParams(buf, out double kappa, out double lambda, work);
                            _weibullKappas[p] = kappa;
                            _weibullLambdas[p] = lambda;
                            if (doRoof)
                            {
                                WindComfortMetricsWeibull.GetWeibullParams(bufRoof, out double kappaR, out double lambdaR, work);
                                _weibullKappasRoof[p] = kappaR;
                                _weibullLambdasRoof[p] = lambdaR;
                            }
                        }
                    }

                    var result = WindComfortMetricsWeibull.CalcExceedanceFromParams(_weibullKappas[p], _weibullLambdas[p], tid);
                    ranks[p] = result.Cat;
                    letters[p] = result.ClassLetter;
                    classes[p] = result.Class;
                    if (doRoof)
                    {
                        var resultRoof = WindComfortMetricsWeibull.CalcExceedanceFromParams(_weibullKappasRoof[p], _weibullLambdasRoof[p], tid);
                        ranksRoof[p] = resultRoof.Cat;
                        lettersRoof[p] = resultRoof.ClassLetter;
                        classesRoof[p] = resultRoof.Class;
                    }
                });
            }

            sw.Stop();
            long t6 = sw.ElapsedMilliseconds;
            sw.Restart();

            // 7. Visualization (Mesh & Legend)
            var comfortMesh = new Mesh();
            int meshCapacity = doRoof ? numProbes * 8 : numProbes * 4;
            int facesCapacity = doRoof ? numProbes * 2 : numProbes;
            comfortMesh.Vertices.Capacity = meshCapacity;
            comfortMesh.Faces.Capacity = facesCapacity;
            comfortMesh.VertexColors.Capacity = meshCapacity;
            var legendMesh = new Mesh();
            var legendPts = new List<Point3d>();
            var legendLetters = letters.Distinct()
                .OrderBy(l => l.Length > 1 ? l : " " + l) // Simple sort to put A-E before S
                .ToList();
            
            // Re-sort specifically for Davenport/Lawson if needed, or just unique list
            // Best approach: get them in order from tid
            legendLetters = tid.Values.OrderBy(v => v.Cat).Select(v => v.ClassLetter).Distinct().ToList();

            string mName = metric.ToString();
            var explanations = tid.Values.OrderBy(v => v.Cat).Select(v => $"{v.ClassLetter}\n{v.Class}").Distinct().ToList();
            
            
            double tileSide = 2.0; // Default tile size
            if (points.Count > 1) 
            {
                // Try to estimate tile size from first two points if they are close
                double dist = points[0].DistanceTo(points[1]);
                if (dist > 0.01 && dist < 10.0) tileSide = dist;
            }
            double hSide = tileSide * 0.5;

            for (int p = 0; p < numProbes; p++)
            {
                if (p >= points.Count) break;
                
                System.Drawing.Color c = GetComfortColor(letters[p], metric);
                Point3d pt = points[p];
                
                // Add vertices and faces sequentially for simplicity if not optimizing further,
                // but pre-calculate colors and geometry if possible.
                // Actually, let's use the fastest Mesh approach: SetVertices/SetColors.
            }
            
            // Optimization: Parallel Mesh Generation
            int maxTiles = doRoof ? numProbes * 2 : numProbes;
            var verts = new Point3d[maxTiles * 4];
            var colors = new System.Drawing.Color[maxTiles * 4];
            var faces = new MeshFace[maxTiles];
            
            int nextTileIndex = 0;

            Parallel.For(0, numProbes, p =>
            {
                if (p >= points.Count) return;
                
                System.Drawing.Color c = GetComfortColor(letters[p], metric);
                Point3d pt = points[p];
                
                int tileIdx = System.Threading.Interlocked.Increment(ref nextTileIndex) - 1;
                int vIdx = tileIdx * 4;

                verts[vIdx + 0] = new Point3d(pt.X - hSide, pt.Y - hSide, pt.Z);
                verts[vIdx + 1] = new Point3d(pt.X + hSide, pt.Y - hSide, pt.Z);
                verts[vIdx + 2] = new Point3d(pt.X + hSide, pt.Y + hSide, pt.Z);
                verts[vIdx + 3] = new Point3d(pt.X - hSide, pt.Y + hSide, pt.Z);

                colors[vIdx + 0] = c;
                colors[vIdx + 1] = c;
                colors[vIdx + 2] = c;
                colors[vIdx + 3] = c;

                faces[tileIdx] = new MeshFace(vIdx, vIdx + 1, vIdx + 2, vIdx + 3);

                if (doRoof && bldgHeightArr[p] > 0.0)
                {
                    System.Drawing.Color cr = GetComfortColor(lettersRoof[p], metric);
                    double zRoof = bldgHeightArr[p] + 1.8;
                    
                    int tileIdxRoof = System.Threading.Interlocked.Increment(ref nextTileIndex) - 1;
                    int vIdxR = tileIdxRoof * 4;

                    verts[vIdxR + 0] = new Point3d(pt.X - hSide, pt.Y - hSide, zRoof);
                    verts[vIdxR + 1] = new Point3d(pt.X + hSide, pt.Y - hSide, zRoof);
                    verts[vIdxR + 2] = new Point3d(pt.X + hSide, pt.Y + hSide, zRoof);
                    verts[vIdxR + 3] = new Point3d(pt.X - hSide, pt.Y + hSide, zRoof);

                    colors[vIdxR + 0] = cr;
                    colors[vIdxR + 1] = cr;
                    colors[vIdxR + 2] = cr;
                    colors[vIdxR + 3] = cr;

                    faces[tileIdxRoof] = new MeshFace(vIdxR, vIdxR + 1, vIdxR + 2, vIdxR + 3);
                }
            });

            int validVerts = nextTileIndex * 4;
            var finalVerts = new Point3d[validVerts];
            var finalColors = new System.Drawing.Color[validVerts];
            var finalFaces = new MeshFace[nextTileIndex];
            
            Array.Copy(verts, finalVerts, validVerts);
            Array.Copy(colors, finalColors, validVerts);
            Array.Copy(faces, finalFaces, nextTileIndex);

            comfortMesh.Vertices.AddVertices(finalVerts);
            comfortMesh.VertexColors.SetColors(finalColors);
            comfortMesh.Faces.AddFaces(finalFaces);
            
            sw.Stop();
            long t7 = sw.ElapsedMilliseconds;

            // Report per-step timing for performance tuning
            Message = $"v0.7.1-Optimized\nStep 6: {t6}ms\nStep 7: {t7}ms";

            // Store in cache for next solve (only if specific metric outputs changed)
            _metricCache[metricInt] = (ranks, letters, classes, ranksRoof, lettersRoof, classesRoof, comfortMesh, legendMesh, legendPts, legendLetters, mName, explanations);

            // Legend Positioning
            if (points.Count > 0)
            {
                var bbox = new BoundingBox(points);
                double legWidth = Math.Max(bbox.Max.X - bbox.Min.X, 200.0);
                double legHeight = legWidth * 0.03; // Fixed height ratio to total width
                double blockW = legWidth / (double)legendLetters.Count;
                
                double startX = bbox.Center.X - (legWidth * 0.5);
                double startY = bbox.Min.Y - (legHeight * 5.0);
                double zLevel = bbox.Min.Z;

                for (int i = 0; i < legendLetters.Count; i++)
                {
                    string label = legendLetters[i];
                    System.Drawing.Color c = GetComfortColor(label, metric);
                    double x0 = startX + i * blockW;
                    double x1 = x0 + blockW;

                    int vc = legendMesh.Vertices.Count;
                    legendMesh.Vertices.Add(x0, startY, zLevel);
                    legendMesh.Vertices.Add(x1, startY, zLevel);
                    legendMesh.Vertices.Add(x1, startY + legHeight, zLevel);
                    legendMesh.Vertices.Add(x0, startY + legHeight, zLevel);
                    
                    legendMesh.Faces.AddFace(vc, vc + 1, vc + 2, vc + 3);
                    legendMesh.VertexColors.Add(c);
                    legendMesh.VertexColors.Add(c);
                    legendMesh.VertexColors.Add(c);
                    legendMesh.VertexColors.Add(c);

                    legendPts.Add(new Point3d(x0 + blockW * 0.5, startY - legHeight * 1.2, zLevel));
                }
            }

            // 8. Cache and output
            _metricCache[metricInt] = (ranks, letters, classes, ranksRoof, lettersRoof, classesRoof, comfortMesh, legendMesh, legendPts, legendLetters, mName, explanations);
 
            DA.SetDataList(0, ranks);
            DA.SetDataList(1, letters);
            DA.SetDataList(2, classes);
            DA.SetData(3, comfortMesh);
            DA.SetData(4, legendMesh);
            DA.SetDataList(5, legendPts);
            DA.SetDataList(6, legendLetters);
            DA.SetData(7, mName);
            DA.SetDataList(8, explanations);
            DA.SetDataList(9, ranksRoof);
            DA.SetDataList(10, lettersRoof);
            DA.SetDataList(11, classesRoof);
 
            string gemTag = hasK ? $" (GEM, g={peakFactor:F1})" : "";
            Message = $"v0.8.0{gemTag}\nStep 6: {t6}ms\nStep 7: {t7}ms";
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                useCachedParams ? $"Metric checked {numProbes} probes in {sw.ElapsedMilliseconds} ms." : $"Weibull estimated {numProbes} probes{gemTag} in {sw.ElapsedMilliseconds} ms.");
        }

        /// <summary>
        /// Returns the gust peak factor (g) for GEM calculation per comfort standard.
        /// GEM = U_mean + g × σ_u, where σ_u = √(2k/3).
        /// </summary>
        private static double GetPeakFactor(WindComfortHelper.PedCmftMetric metric)
        {
            switch (metric)
            {
                case WindComfortHelper.PedCmftMetric.LawsonLDDC:   return 3.5;
                case WindComfortHelper.PedCmftMetric.Lawson2001:    return 3.5;
                case WindComfortHelper.PedCmftMetric.LawsonGeneral: return 3.5;
                case WindComfortHelper.PedCmftMetric.NEN8100Comfort: return 3.0;
                case WindComfortHelper.PedCmftMetric.NEN8100Safety:  return 3.0;
                case WindComfortHelper.PedCmftMetric.Davenport:     return 3.0;
                default:                                            return 3.0;
            }
        }

        private System.Drawing.Color GetComfortColor(string letter, WindComfortHelper.PedCmftMetric metric)
        {
            // Specialized override for NEN 8100 Safety (Traffic Light Colors)
            if (metric == WindComfortHelper.PedCmftMetric.NEN8100Safety)
            {
                switch (letter)
                {
                    case "A": return System.Drawing.Color.ForestGreen;
                    case "B": return System.Drawing.Color.Orange;
                    case "C": return System.Drawing.Color.Firebrick;
                    default: return System.Drawing.Color.Gray;
                }
            }

            switch (letter)
            {
                case "A": return System.Drawing.Color.Blue;
                case "B": return System.Drawing.Color.Cyan;
                case "C": return System.Drawing.Color.Lime;
                case "D": return System.Drawing.Color.Yellow;
                case "E": return System.Drawing.Color.Red;
                case "S": 
                case "S15":
                case "S20": return System.Drawing.Color.DarkMagenta;
                default: return System.Drawing.Color.Gray;
            }
        }
    }
}
