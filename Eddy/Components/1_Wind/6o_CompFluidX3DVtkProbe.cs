using Eddy.Properties;
using EddyLib;
using EddyLib.FluidX3D;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Eddy
{
    public class FluidX3DVtkProbe_Component : GH_Component
    {
        private readonly List<Point3d> _cachedProbePoints = new List<Point3d>();
        private bool _hasCachedProbePoints;
        private bool _lastRunState;
        private GH_Structure<GH_Vector> _cachedVelocityTree = new GH_Structure<GH_Vector>();
        private GH_Structure<GH_Number> _cachedRhoTree = new GH_Structure<GH_Number>();
        private List<Vector3d> _cachedVelocityAverage = new List<Vector3d>();
        private List<double> _cachedRhoAverage = new List<double>();
        private List<double> _cachedSampledTimes = new List<double>();
        private List<double> _cachedSampledSteps = new List<double>();
        private List<string> _cachedSampledFiles = new List<string>();
        private int _cachedOutsideCount;
        private bool _hasCachedOutputs;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public FluidX3DVtkProbe_Component()
          : base(
              GH_Strings.FluidX3DProbe.Name,
              GH_Strings.FluidX3DProbe.Nick,
              GH_Strings.FluidX3DProbe.Desc + EddyVersion.toString(),
              EddyVersion.Name,
              "1 | Wind")
        {
        }

        public override void CreateAttributes()
        {
            Attributes = new ProbeRunButtonAttributes(this);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                GH_Strings.FluidX3DProbe.Result,
                GH_Strings.FluidX3DProbe.ResultNick,
                GH_Strings.FluidX3DProbe.ResultDesc,
                GH_ParamAccess.item);
            pManager[0].Optional = false;

            pManager.AddPointParameter(
                GH_Strings.FluidX3DProbe.Points,
                GH_Strings.FluidX3DProbe.PointsNick,
                GH_Strings.FluidX3DProbe.PointsDesc,
                GH_ParamAccess.list);

            pManager.AddIntegerParameter(
                GH_Strings.FluidX3DProbe.Quantity,
                GH_Strings.FluidX3DProbe.QuantityNick,
                GH_Strings.FluidX3DProbe.QuantityDesc,
                GH_ParamAccess.item,
                0);
            Param_Integer quantityParam = pManager[2] as Param_Integer;
            quantityParam?.AddNamedValue("Velocity U", 0);
            quantityParam?.AddNamedValue("Density rho", 1);

            pManager.AddIntegerParameter(
                GH_Strings.FluidX3DProbe.TimeMode,
                GH_Strings.FluidX3DProbe.TimeModeNick,
                GH_Strings.FluidX3DProbe.TimeModeDesc,
                GH_ParamAccess.item,
                2);
            Param_Integer modeParam = pManager[3] as Param_Integer;
            modeParam?.AddNamedValue("Latest", 0);
            modeParam?.AddNamedValue("Closest physical time", 1);
            modeParam?.AddNamedValue("Average over [T0, T1]", 2);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.TargetTime,
                GH_Strings.FluidX3DProbe.TargetTimeNick,
                GH_Strings.FluidX3DProbe.TargetTimeDesc,
                GH_ParamAccess.item,
                0.0);

            pManager.AddIntervalParameter(
                GH_Strings.FluidX3DProbe.TimeWindow,
                GH_Strings.FluidX3DProbe.TimeWindowNick,
                GH_Strings.FluidX3DProbe.TimeWindowDesc,
                GH_ParamAccess.item,
                new Interval(0.0, 30.0));
            pManager[5].Optional = false;

            pManager.AddParameter(CreateRunToggleParam());
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter(
                GH_Strings.FluidX3DProbe.PointsOut,
                GH_Strings.FluidX3DProbe.PointsOutNick,
                GH_Strings.FluidX3DProbe.PointsOutDesc,
                GH_ParamAccess.list);

            pManager.AddVectorParameter(
                GH_Strings.FluidX3DProbe.VelocityByTime,
                GH_Strings.FluidX3DProbe.VelocityByTimeNick,
                GH_Strings.FluidX3DProbe.VelocityByTimeDesc,
                GH_ParamAccess.tree);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.DensityByTime,
                GH_Strings.FluidX3DProbe.DensityByTimeNick,
                GH_Strings.FluidX3DProbe.DensityByTimeDesc,
                GH_ParamAccess.tree);

            pManager.AddVectorParameter(
                GH_Strings.FluidX3DProbe.VelocityAverage,
                GH_Strings.FluidX3DProbe.VelocityAverageNick,
                GH_Strings.FluidX3DProbe.VelocityAverageDesc,
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.DensityAverage,
                GH_Strings.FluidX3DProbe.DensityAverageNick,
                GH_Strings.FluidX3DProbe.DensityAverageDesc,
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.SampledTimes,
                GH_Strings.FluidX3DProbe.SampledTimesNick,
                GH_Strings.FluidX3DProbe.SampledTimesDesc,
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DProbe.SampledSteps,
                GH_Strings.FluidX3DProbe.SampledStepsNick,
                GH_Strings.FluidX3DProbe.SampledStepsDesc,
                GH_ParamAccess.list);

            pManager.AddTextParameter(
                GH_Strings.FluidX3DProbe.SampledFiles,
                GH_Strings.FluidX3DProbe.SampledFilesNick,
                GH_Strings.FluidX3DProbe.SampledFilesDesc,
                GH_ParamAccess.list);

            pManager.AddIntegerParameter(
                GH_Strings.FluidX3DProbe.OutsideCount,
                GH_Strings.FluidX3DProbe.OutsideCountNick,
                GH_Strings.FluidX3DProbe.OutsideCountDesc,
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper resultWrapper = null;
            List<Point3d> pointsInput = new List<Point3d>();
            int quantityInt = 0;
            int timeModeInt = 2;
            double targetTimeSeconds = 0.0;
            Interval timeWindow = new Interval(0.0, 30.0);
            bool run = false;

            if (!DA.GetData(0, ref resultWrapper))
            {
                _lastRunState = false;
                return;
            }

            if (!DA.GetDataList(1, pointsInput))
            {
                _lastRunState = false;
                return;
            }

            DA.GetData(2, ref quantityInt);
            DA.GetData(3, ref timeModeInt);
            DA.GetData(4, ref targetTimeSeconds);
            DA.GetData(5, ref timeWindow);
            DA.GetData(6, ref run);
            run = run || ConsumeToggleRun(6);
            double startTimeSeconds = timeWindow.T0;
            double endTimeSeconds = timeWindow.T1;

            bool runJustPressed = run && !_lastRunState;
            if (runJustPressed && pointsInput.Count > 0)
            {
                CacheProbePoints(pointsInput);
            }

            List<Point3d> points = GetEffectiveProbePoints(pointsInput, run);

            string resolvedCaseDir = string.Empty;
            TryResolveCaseDirectoryFromResult(resultWrapper?.Value, out resolvedCaseDir);

            DA.SetDataList(0, points);

            GH_Structure<GH_Vector> velocityTree = new GH_Structure<GH_Vector>();
            GH_Structure<GH_Number> rhoTree = new GH_Structure<GH_Number>();
            List<Vector3d> velocityAverage = new List<Vector3d>();
            List<double> rhoAverage = new List<double>();
            List<double> sampledTimes = new List<double>();
            List<double> sampledSteps = new List<double>();
            List<string> sampledFiles = new List<string>();
            int outsideCount = 0;
            bool probeSucceeded = false;

            try
            {
                if (!run)
                {
                    Message = "Toggle 'Run' to probe";
                    WriteOutputsOrCached(
                        DA,
                        velocityTree,
                        rhoTree,
                        velocityAverage,
                        rhoAverage,
                        sampledTimes,
                        sampledSteps,
                        sampledFiles,
                        outsideCount);
                    return;
                }

                if (points.Count == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "At least one probe point is required.");
                    Message = "Connect probe points";
                    WriteOutputsOrCached(
                        DA,
                        velocityTree,
                        rhoTree,
                        velocityAverage,
                        rhoAverage,
                        sampledTimes,
                        sampledSteps,
                        sampledFiles,
                        outsideCount);
                    return;
                }

                FluidX3DVtkProbeQuantity quantity = quantityInt == 1
                    ? FluidX3DVtkProbeQuantity.DensityRho
                    : FluidX3DVtkProbeQuantity.VelocityU;

                FluidX3DVtkProbeTimeMode timeMode = timeModeInt switch
                {
                    1 => FluidX3DVtkProbeTimeMode.ClosestPhysicalTime,
                    2 => FluidX3DVtkProbeTimeMode.AverageOverRange,
                    _ => FluidX3DVtkProbeTimeMode.AverageOverRange
                };

                string probeDir = ResolveProbeDirectory(resolvedCaseDir);

                if (string.IsNullOrWhiteSpace(probeDir))
                {
                    AddRuntimeMessage(
                        GH_RuntimeMessageLevel.Warning,
                        "FluidX3D RES input is required.");
                    Message = "Connect a valid result";
                    WriteOutputsOrCached(
                        DA,
                        velocityTree,
                        rhoTree,
                        velocityAverage,
                        rhoAverage,
                        sampledTimes,
                        sampledSteps,
                        sampledFiles,
                        outsideCount);
                    return;
                }

                Message = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} | {1}",
                    quantity == FluidX3DVtkProbeQuantity.VelocityU ? "U" : "rho",
                    timeMode == FluidX3DVtkProbeTimeMode.Latest
                        ? "Latest"
                        : timeMode == FluidX3DVtkProbeTimeMode.ClosestPhysicalTime
                            ? "Closest T"
                            : "Avg[T0,T1]");

                if (!Directory.Exists(probeDir))
                {
                    throw new DirectoryNotFoundException(
                        "FluidX3D output folder not found. Case directory: " + resolvedCaseDir
                        + ". Checked: " + probeDir);
                }

                if (!ContainsFieldVtkFiles(probeDir, quantity))
                {
                    throw new FileNotFoundException(
                        "No matching VTK files found for selected field in: " + probeDir);
                }

                FluidX3DVtkProbeRequest request = new FluidX3DVtkProbeRequest
                {
                    ExportDirectory = probeDir,
                    Quantity = quantity,
                    TimeMode = timeMode,
                    TargetTimeSeconds = targetTimeSeconds,
                    StartTimeSeconds = startTimeSeconds,
                    EndTimeSeconds = endTimeSeconds
                };

                for (int i = 0; i < points.Count; i++)
                {
                    Point3d p = points[i];
                    request.RhinoPoints.Add(new FluidX3DPoint3(p.X, p.Y, p.Z));
                }

                FluidX3DVtkProbeResult result = FluidX3DVtkProber.Probe(request);

                sampledTimes.AddRange(result.SampledTimesSeconds);
                for (int i = 0; i < result.SampledSteps.Count; i++)
                {
                    sampledSteps.Add(result.SampledSteps[i]);
                }

                sampledFiles.AddRange(result.SampledFiles);
                outsideCount = result.OutsideDomainPointCount;
                if (!string.IsNullOrWhiteSpace(result.Status))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, result.Status);
                }

                if (quantity == FluidX3DVtkProbeQuantity.VelocityU)
                {
                    for (int t = 0; t < result.VectorValuesByTime.Count; t++)
                    {
                        GH_Path path = new GH_Path(t);
                        FluidX3DPoint3[] values = result.VectorValuesByTime[t];
                        for (int i = 0; i < values.Length; i++)
                        {
                            FluidX3DPoint3 v = values[i];
                            velocityTree.Append(new GH_Vector(new Vector3d(v.X, v.Y, v.Z)), path);
                        }
                    }

                    if (result.AverageVectors != null)
                    {
                        for (int i = 0; i < result.AverageVectors.Length; i++)
                        {
                            FluidX3DPoint3 v = result.AverageVectors[i];
                            velocityAverage.Add(new Vector3d(v.X, v.Y, v.Z));
                        }
                    }
                }
                else
                {
                    for (int t = 0; t < result.ScalarValuesByTime.Count; t++)
                    {
                        GH_Path path = new GH_Path(t);
                        double[] values = result.ScalarValuesByTime[t];
                        for (int i = 0; i < values.Length; i++)
                        {
                            rhoTree.Append(new GH_Number(values[i]), path);
                        }
                    }

                    if (result.AverageScalars != null)
                    {
                        rhoAverage.AddRange(result.AverageScalars);
                    }
                }

                probeSucceeded = true;
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            }
            finally
            {
                _lastRunState = run;
            }

            if (probeSucceeded)
            {
                CacheProbeOutputs(
                    velocityTree,
                    rhoTree,
                    velocityAverage,
                    rhoAverage,
                    sampledTimes,
                    sampledSteps,
                    sampledFiles,
                    outsideCount);
            }
            else if (_hasCachedOutputs)
            {
                WriteCachedOutputs(DA);
                return;
            }

            WriteOutputs(
                DA,
                velocityTree,
                rhoTree,
                velocityAverage,
                rhoAverage,
                sampledTimes,
                sampledSteps,
                sampledFiles,
                outsideCount);
        }

        private void CacheProbePoints(List<Point3d> points)
        {
            _cachedProbePoints.Clear();
            _cachedProbePoints.AddRange(points);
            _hasCachedProbePoints = _cachedProbePoints.Count > 0;
        }

        private List<Point3d> GetEffectiveProbePoints(List<Point3d> pointsInput, bool run)
        {
            if (!run || !_hasCachedProbePoints)
            {
                return pointsInput;
            }

            return new List<Point3d>(_cachedProbePoints);
        }

        private void CacheProbeOutputs(
            GH_Structure<GH_Vector> velocityTree,
            GH_Structure<GH_Number> rhoTree,
            List<Vector3d> velocityAverage,
            List<double> rhoAverage,
            List<double> sampledTimes,
            List<double> sampledSteps,
            List<string> sampledFiles,
            int outsideCount)
        {
            _cachedVelocityTree = velocityTree ?? new GH_Structure<GH_Vector>();
            _cachedRhoTree = rhoTree ?? new GH_Structure<GH_Number>();
            _cachedVelocityAverage = velocityAverage != null
                ? new List<Vector3d>(velocityAverage)
                : new List<Vector3d>();
            _cachedRhoAverage = rhoAverage != null
                ? new List<double>(rhoAverage)
                : new List<double>();
            _cachedSampledTimes = sampledTimes != null
                ? new List<double>(sampledTimes)
                : new List<double>();
            _cachedSampledSteps = sampledSteps != null
                ? new List<double>(sampledSteps)
                : new List<double>();
            _cachedSampledFiles = sampledFiles != null
                ? new List<string>(sampledFiles)
                : new List<string>();
            _cachedOutsideCount = outsideCount;
            _hasCachedOutputs = true;
        }

        private void WriteCachedOutputs(IGH_DataAccess DA)
        {
            WriteOutputs(
                DA,
                _cachedVelocityTree,
                _cachedRhoTree,
                _cachedVelocityAverage,
                _cachedRhoAverage,
                _cachedSampledTimes,
                _cachedSampledSteps,
                _cachedSampledFiles,
                _cachedOutsideCount);
        }

        private void WriteOutputsOrCached(
            IGH_DataAccess DA,
            GH_Structure<GH_Vector> velocityTree,
            GH_Structure<GH_Number> rhoTree,
            List<Vector3d> velocityAverage,
            List<double> rhoAverage,
            List<double> sampledTimes,
            List<double> sampledSteps,
            List<string> sampledFiles,
            int outsideCount)
        {
            if (_hasCachedOutputs)
            {
                WriteCachedOutputs(DA);
                return;
            }

            WriteOutputs(
                DA,
                velocityTree,
                rhoTree,
                velocityAverage,
                rhoAverage,
                sampledTimes,
                sampledSteps,
                sampledFiles,
                outsideCount);
        }

        private static void WriteOutputs(
            IGH_DataAccess DA,
            GH_Structure<GH_Vector> velocityTree,
            GH_Structure<GH_Number> rhoTree,
            List<Vector3d> velocityAverage,
            List<double> rhoAverage,
            List<double> sampledTimes,
            List<double> sampledSteps,
            List<string> sampledFiles,
            int outsideCount)
        {
            DA.SetDataTree(1, velocityTree);
            DA.SetDataTree(2, rhoTree);
            DA.SetDataList(3, velocityAverage);
            DA.SetDataList(4, rhoAverage);
            DA.SetDataList(5, sampledTimes);
            DA.SetDataList(6, sampledSteps);
            DA.SetDataList(7, sampledFiles);
            DA.SetData(8, outsideCount);
        }

        private static bool TryResolveCaseDirectoryFromResult(object value, out string caseDir)
        {
            caseDir = string.Empty;

            if (!(value is OFResult result))
            {
                return false;
            }

            string caseFromResult = result.EngineCaseDirectory ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(caseFromResult))
            {
                caseDir = caseFromResult;
                return true;
            }

            string workingDir = result.WorkingDirectory ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(workingDir))
            {
                caseDir = workingDir;
                return true;
            }

            return false;
        }

        private static bool ContainsFieldVtkFiles(string directory, FluidX3DVtkProbeQuantity quantity)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return false;
            }

            string pattern = quantity == FluidX3DVtkProbeQuantity.VelocityU ? "u-*.vtk" : "rho-*.vtk";
            return Directory.GetFiles(directory, pattern, SearchOption.TopDirectoryOnly).Length > 0;
        }

        private static string ResolveProbeDirectory(string caseDirInput)
        {
            if (string.IsNullOrWhiteSpace(caseDirInput))
            {
                return string.Empty;
            }

            string fullPath = Path.GetFullPath(caseDirInput.Trim());
            foreach (string candidate in EnumerateProbeDirectoryCandidates(fullPath))
            {
                if (LooksLikeProbeDirectory(candidate))
                {
                    return candidate;
                }
            }

            string caseRoot = fullPath;
            if (new DirectoryInfo(fullPath).Name.Equals("Engine", StringComparison.OrdinalIgnoreCase))
            {
                string parent = Directory.GetParent(fullPath)?.FullName;
                if (!string.IsNullOrWhiteSpace(parent))
                {
                    caseRoot = parent;
                }
            }

            string nestedCaseRoot = Path.Combine(fullPath, "FluidX3D");
            if (Directory.Exists(nestedCaseRoot))
            {
                caseRoot = nestedCaseRoot;
            }

            return Path.Combine(caseRoot, "VTK");
        }

        private static IEnumerable<string> EnumerateProbeDirectoryCandidates(string fullPath)
        {
            StringComparison comparison = StringComparison.OrdinalIgnoreCase;
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string candidate in BuildProbeDirectoryCandidates(fullPath))
            {
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }

                string normalized;
                try
                {
                    normalized = Path.GetFullPath(candidate);
                }
                catch
                {
                    continue;
                }

                if (seen.Add(normalized))
                {
                    yield return normalized;
                }
            }

            if (fullPath.EndsWith(Path.Combine("FluidX3D", "Engine"), comparison))
            {
                string root = Directory.GetParent(fullPath)?.Parent?.FullName;
                if (!string.IsNullOrWhiteSpace(root))
                {
                    string vtk = Path.Combine(root, "VTK");
                    if (seen.Add(vtk))
                    {
                        yield return vtk;
                    }
                }
            }
        }

        private static IEnumerable<string> BuildProbeDirectoryCandidates(string fullPath)
        {
            yield return fullPath;
            yield return Path.Combine(fullPath, "VTK");
            yield return Path.Combine(fullPath, "bin", "export");
            yield return Path.Combine(fullPath, "Engine", "bin", "export");
            yield return Path.Combine(fullPath, "FluidX3D", "VTK");
            yield return Path.Combine(fullPath, "FluidX3D", "bin", "export");
            yield return Path.Combine(fullPath, "FluidX3D", "Engine", "bin", "export");

            DirectoryInfo info = new DirectoryInfo(fullPath);
            if (info.Name.Equals("Engine", StringComparison.OrdinalIgnoreCase) && info.Parent != null)
            {
                yield return Path.Combine(info.Parent.FullName, "VTK");
                yield return Path.Combine(info.FullName, "bin", "export");
            }
        }

        private static bool LooksLikeProbeDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string folderName = new DirectoryInfo(path).Name;
            if (folderName.Equals("vtk", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!Directory.Exists(path))
            {
                return false;
            }

            return Directory.GetFiles(path, "*.vtk", SearchOption.TopDirectoryOnly).Length > 0;
        }

        private bool ConsumeToggleRun(int inputIndex)
        {
            if (inputIndex < 0
                || inputIndex >= Params.Input.Count
                || !(Params.Input[inputIndex] is GH_ToggleParam toggle)
                || !toggle.Toggle)
            {
                return false;
            }

            OnPingDocument()?.ScheduleSolution(5, _ => toggle.SetToggle(false));
            return true;
        }

        private static GH_ToggleParam CreateRunToggleParam()
        {
            GH_ToggleParam param = new GH_ToggleParam(
                GH_Strings.FluidX3DProbe.Run,
                GH_Strings.FluidX3DProbe.RunNick,
                GH_Strings.FluidX3DProbe.RunDesc)
            {
                Access = GH_ParamAccess.item,
                Optional = false
            };
            return param;
        }

        protected override System.Drawing.Bitmap Icon => Resources.Eddy_visualProbs;

        public override Guid ComponentGuid => new Guid("{81A47466-AA49-45C4-8D40-5CBDA5004300}");
    }
}
