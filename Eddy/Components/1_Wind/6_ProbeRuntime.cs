using Eddy.Components.Indoor.Params;
using EddyLib;
using EddyLib.Docker;
using EddyLib.FluidX3D;
using EddyLib.Helpers;
using EddyLib.Indoor;
using EddyLib.Radiation;
using EddyLib.Strings;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Eddy
{
    internal sealed class OpenFoamPointProbeRequest
    {
        public OFResult Result { get; set; }
        public List<Point3d> Points { get; set; } = new List<Point3d>();
        public string ProbeName { get; set; } = string.Empty;
        public int InterpolationScheme { get; set; } = 3;
        public int FieldIndex { get; set; }
        public bool Run { get; set; }
        public bool CanRun { get; set; } = true;
        public EventHandler ProbeCompleted { get; set; }
        public Action<List<string>, string> RunDockerProbing { get; set; }
        public Action<GH_RuntimeMessageLevel, string> AddRuntimeMessage { get; set; }
        public Action<string> SetComponentMessage { get; set; }
    }

    internal sealed class OpenFoamPointProbeResult
    {
        public List<Point3d> Points { get; } = new List<Point3d>();
        public GH_Structure<GH_Number> ScalarTree { get; } = new GH_Structure<GH_Number>();
        public GH_Structure<GH_Vector> VectorTree { get; } = new GH_Structure<GH_Vector>();
        public fieldType FieldType { get; set; } = fieldType.vector;
        public bool ShouldStop { get; set; }
        public bool CanRunAfter { get; set; } = true;
    }

    internal static class OpenFoamPointProbeRunner
    {
        public static OpenFoamPointProbeResult Execute(OpenFoamPointProbeRequest request)
        {
            var output = new OpenFoamPointProbeResult();
            Action<GH_RuntimeMessageLevel, string> addMessage =
                request.AddRuntimeMessage ?? ((_, __) => { });

            OFResult res = request.Result;
            if (res == null)
            {
                output.ShouldStop = true;
                return output;
            }

            request.SetComponentMessage?.Invoke(res.RunSettings.simEngine.ToString());
            if (res.RunSettings.simEngine == SimEngine.FluidX3D)
            {
                addMessage(GH_RuntimeMessageLevel.Warning, "Connected result uses FluidX3D. Use FluidX3D probe mode.");
                output.ShouldStop = true;
                return output;
            }

            List<Point3d> points = Probing.DeduplicateProbePointsForOpenFoam(
                request.Points ?? new List<Point3d>(),
                out int removedDuplicateProbeCount);
            output.Points.AddRange(points);

            if (removedDuplicateProbeCount > 0)
            {
                addMessage(
                    GH_RuntimeMessageLevel.Remark,
                    $"Removed {removedDuplicateProbeCount} duplicate probe points after OpenFOAM coordinate formatting.");
            }

            string probeName = request.ProbeName ?? string.Empty;
            if (probeName == string.Empty)
            {
                probeName = "test";
                addMessage(
                    GH_RuntimeMessageLevel.Remark,
                    "Please provide a unique name for this probing instance, otherwise a new instance will overwrite the results.");
            }

            if (!Utilities.IsValidProbeName(probeName))
            {
                addMessage(
                    GH_RuntimeMessageLevel.Error,
                    "Probe name contains invalid characters. Only alphanumeric, underscore, and dash are allowed.");
                output.ShouldStop = true;
                return output;
            }

            if (char.IsDigit(probeName.First()))
            {
                addMessage(GH_RuntimeMessageLevel.Error, "Please make sure name doesn't start with digit.");
                output.ShouldStop = true;
                return output;
            }

            int numberOfProbes = points.Count;
            if (numberOfProbes < 1)
            {
                addMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of points to the component.");
                output.ShouldStop = true;
                return output;
            }

            string ofField = OFField.ReformatOFFields(request.FieldIndex);
            OFField currField = new OFField(ofField, probeName, request.InterpolationScheme);
            output.FieldType = currField.FieldType;

            if (request.Run
                && res.RunSettings.simEngine == SimEngine.Docker
                && (res.Domain is OFCylDomain || res.Domain is OFBoxDomain))
            {
                string scriptExt = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".bat" : ".command";
                string scriptPath = Path.Combine(res.WorkingDirectory, "Scripts", "copy_mesh_to_wind_dirs" + scriptExt);
                addMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Docker mode is active. If you switched from BlueCFD to Docker after meshing, run \""
                    + scriptPath
                    + "\" once to copy meshes into all integer wind-direction folders before probing.");
            }

            if (res.Domain is OFCylDomain || res.Domain is OFBoxDomain)
            {
                var radDir = Path.Combine(res.WorkingDirectory, "Rad");
                if (!Directory.Exists(radDir))
                {
                    Directory.CreateDirectory(radDir);
                }

                RadianceFiles.writePTS(Path.Combine(radDir, "sensors.pts"), points);
            }

            string meshDir = string.Empty;
            if (res.Domain is OFCylDomain)
            {
                meshDir = res.MeshSettings.meshPolyMeshDir;
            }
            else if (res.Domain is OFBoxDomain)
            {
                meshDir = Path.Combine(res.WorkingDirectory, res.Domain.BCond.WindDirections[0].ToString(), "constant", "polyMesh");
            }
            else if (res.Domain is IndoorDomain)
            {
                meshDir = Path.Combine(res.WorkingDirectory, "constant", "polyMesh");
            }

            if (Directory.Exists(meshDir) == false)
            {
                addMessage(GH_RuntimeMessageLevel.Warning, "The mesh folder " + meshDir + " does not exist. Please create a mesh first.");
            }
            else if (DirectoryHelpers.IsEmpty(meshDir))
            {
                addMessage(GH_RuntimeMessageLevel.Warning, "The mesh folder " + meshDir + " is empty. Can't retrieve probes from a mesh that does not exist.");
            }

            int threshold = 100000;
            if (points.Count > threshold)
            {
                addMessage(GH_RuntimeMessageLevel.Warning, "Probing more than " + threshold + " points may slow Grasshopper down considerably.");
            }

            if (res.RunSettings.writeInterval > 1 && currField.FieldName == "total(p)_coeff")
            {
                addMessage(GH_RuntimeMessageLevel.Warning, ReturnMsg.ProbingFuncObjects(res, currField));
            }

            if (res.Domain is OFCylDomain || res.Domain is OFBoxDomain)
            {
                ExecuteOutdoorProbe(request, output, res, points, probeName, currField, addMessage);
            }
            else if (res.Domain is IndoorDomain)
            {
                ExecuteIndoorProbe(request, output, res, points, probeName, currField, addMessage);
            }

            if (!output.ShouldStop && currField.FieldType == fieldType.vector)
            {
                ReportOutsideDomainVectors(output, addMessage);
            }

            return output;
        }

        public static void WriteOutputs(IGH_DataAccess da, OpenFoamPointProbeResult result)
        {
            if (result.FieldType == fieldType.scalar)
            {
                da.SetDataTree(1, result.ScalarTree);
                da.SetDataList(0, result.Points);
            }
            else if (result.FieldType == fieldType.vector)
            {
                da.SetDataTree(1, result.VectorTree);
                da.SetDataList(0, result.Points);
            }
        }

        private static void ExecuteOutdoorProbe(
            OpenFoamPointProbeRequest request,
            OpenFoamPointProbeResult output,
            OFResult res,
            List<Point3d> points,
            string probeName,
            OFField currField,
            Action<GH_RuntimeMessageLevel, string> addMessage)
        {
            try
            {
                StringBuilder command = new StringBuilder();
                var dockerProbeCmds = new List<string>();

                for (int i = 0; i < res.Domain.BCond.WindDirections.Count; i++)
                {
                    string windDir = res.Domain.BCond.WindDirections[i].ToString();
                    string windDirPath = Path.Combine(res.WorkingDirectory, windDir);
                    string iter = Utilities.GetLastIterationFromDirectory(windDirPath).ToString();
                    string fp = Path.Combine(windDirPath, iter, "U");

                    if (!File.Exists(fp))
                    {
                        addMessage(
                            GH_RuntimeMessageLevel.Warning,
                            "The last iteration \""
                            + iter
                            + "\" of the wind direction \""
                            + res.Domain.BCond.WindDirections[i]
                            + "\" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
                    }

                    string pathToPointFile = Path.Combine(windDirPath, "constant", "polyMesh", "points");
                    string systemDir = Path.Combine(windDirPath, "system");
                    if (!Directory.Exists(systemDir))
                    {
                        Directory.CreateDirectory(systemDir);
                    }

                    string path = Path.Combine(systemDir, probeName);
                    File.WriteAllText(path, OFExecDicts.SampleProbes(points, currField));

                    if (!File.Exists(pathToPointFile))
                    {
                        addMessage(GH_RuntimeMessageLevel.Warning, ReturnMsg.MeshDoesntExist(pathToPointFile));
                        if (request.Run)
                        {
                            output.ShouldStop = true;
                            return;
                        }

                        continue;
                    }

                    if (res.RunSettings.simEngine == SimEngine.Docker)
                    {
                        if (i > 0)
                        {
                            dockerProbeCmds.Add("cd " + DockerConfig.CaseMountPoint);
                        }

                        string source = string.Format("{0}/mesh/constant/polyMesh", DockerConfig.CaseMountPoint);
                        string targetConstant = string.Format("{0}/{1}/constant", DockerConfig.CaseMountPoint, windDir);
                        string target = string.Format("{0}/polyMesh", targetConstant);
                        dockerProbeCmds.Add(string.Format("if [ ! -d \"{0}\" ]; then echo \"ERROR: Mesh source not found at {0}\"; exit 1; fi", source));
                        dockerProbeCmds.Add(string.Format("mkdir -p \"{0}\"", targetConstant));
                        dockerProbeCmds.Add(string.Format("rm -rf \"{0}\"", target));
                        dockerProbeCmds.Add(string.Format("cp -r \"{0}\" \"{1}\"", source, target));
                        dockerProbeCmds.Add(string.Format("cd {0}", windDir));
                        dockerProbeCmds.Add(string.Format("rm -rf \"postProcessing/{0}\"", currField.ProbeName));
                        dockerProbeCmds.Add(string.Format("postProcess -func {0} -latestTime", currField.ProbeName));
                    }
                    else
                    {
                        int latestTime = Probing.GetLatestTime(windDirPath, res, currField);
                        command.AppendLine(
                            @"if exist """
                            + windDir
                            + @"\postProcessing\"
                            + probeName
                            + @""" rmdir /S /Q """
                            + windDir
                            + @"\postProcessing\"
                            + probeName
                            + @"""");
                        command.AppendLine(@"foamPostProcess -case " + windDir + " -func " + probeName + " -time " + latestTime);
                    }
                }

                if (request.Run && request.CanRun)
                {
                    Eddy.Analytics.Analytics.TrackSimulationRun(
                        "probing",
                        Eddy.Analytics.Analytics.GetAnalyticsEngine(res.RunSettings.simEngine));

                    if (res.RunSettings.simEngine == SimEngine.Docker)
                    {
                        request.RunDockerProbing?.Invoke(dockerProbeCmds, res.WorkingDirectory);
                        addMessage(
                            GH_RuntimeMessageLevel.Remark,
                            "Docker probing launched in an interactive terminal. Wait for it to finish, then set Run=false and recompute to load results.");
                    }
                    else
                    {
                        var cmdArg = BatFiles.BlueCfdScriptBuilder.BuildBlueCfdBatch(new List<string> { command.ToString() }, res.WorkingDirectory, RunMode.Canvas);
                        Utilities.StartProcess.StartBatchScriptCMDNT(cmdArg, false, true, true, true, request.ProbeCompleted);
                    }

                    output.CanRunAfter = request.CanRun;
                    output.ShouldStop = true;
                    return;
                }

                for (int i = 0; i < res.Domain.BCond.WindDirections.Count; i++)
                {
                    string currentCaseDir = Path.Combine(res.WorkingDirectory, res.Domain.BCond.WindDirections[i].ToString());
                    string pathToProbeFile = Probing.GetPathToProbedResults(currentCaseDir, currField, res);
                    if (File.Exists(pathToProbeFile))
                    {
                        if (currField.FieldType == fieldType.vector)
                        {
                            Probing vectors = new Probing(points, currentCaseDir, res.WorkingDirectory, currField, res, request.Run, res.Domain.BCond.WindDirections[i].ToString());
                            output.VectorTree.AppendRange(vectors.ResultVec, new GH_Path(i));
                        }
                        else
                        {
                            Probing scalars = new Probing(points, currentCaseDir, res.WorkingDirectory, currField, res, request.Run, res.Domain.BCond.WindDirections[i].ToString());
                            output.ScalarTree.AppendRange(scalars.ResultScalar, new GH_Path(i));
                        }
                    }
                    else
                    {
                        addMessage(GH_RuntimeMessageLevel.Warning, ReturnMsg.FieldDoesntExist(currentCaseDir, currField));
                    }
                }
            }
            catch (Exception ex)
            {
                addMessage(GH_RuntimeMessageLevel.Error, ex.ToString());
            }
        }

        private static void ExecuteIndoorProbe(
            OpenFoamPointProbeRequest request,
            OpenFoamPointProbeResult output,
            OFResult res,
            List<Point3d> points,
            string probeName,
            OFField currField,
            Action<GH_RuntimeMessageLevel, string> addMessage)
        {
            try
            {
                StringBuilder command = new StringBuilder();
                string pathToPointFile = Path.Combine(res.WorkingDirectory, "constant", "polyMesh", "points");
                string systemDir = Path.Combine(res.WorkingDirectory, "system");
                if (!Directory.Exists(systemDir))
                {
                    Directory.CreateDirectory(systemDir);
                }

                string path = Path.Combine(systemDir, probeName);
                File.WriteAllText(path, OFExecDicts.SampleProbes(points, currField));

                if (!File.Exists(pathToPointFile))
                {
                    addMessage(GH_RuntimeMessageLevel.Warning, ReturnMsg.MeshDoesntExist(pathToPointFile));
                    output.ShouldStop = true;
                    return;
                }

                if (res.RunSettings.simEngine != SimEngine.Docker)
                {
                    int latestTime = Probing.GetLatestTime(res.WorkingDirectory, res, currField);
                    command.AppendLine(@"if exist ""postProcessing\" + probeName + @""" rmdir /S /Q ""postProcessing\" + probeName + @"""");
                    command.AppendLine("foamPostProcess -func " + probeName + " -time " + latestTime);
                }

                if (request.Run && request.CanRun)
                {
                    Eddy.Analytics.Analytics.TrackSimulationRun(
                        "probing",
                        Eddy.Analytics.Analytics.GetAnalyticsEngine(res.RunSettings.simEngine));

                    if (res.RunSettings.simEngine == SimEngine.Docker)
                    {
                        var dockerCmds = new List<string>
                        {
                            string.Format("rm -rf \"postProcessing/{0}\"", currField.ProbeName),
                            string.Format("postProcess -func {0} -latestTime", currField.ProbeName)
                        };
                        request.RunDockerProbing?.Invoke(dockerCmds, res.WorkingDirectory);
                        addMessage(
                            GH_RuntimeMessageLevel.Remark,
                            "Docker probing launched in an interactive terminal. Wait for it to finish, then set Run=false and recompute to load results.");
                    }
                    else
                    {
                        var cmdArg = BatFiles.BlueCfdScriptBuilder.BuildBlueCfdBatch(new List<string> { command.ToString() }, res.WorkingDirectory, RunMode.Canvas);
                        Utilities.StartProcess.StartBatchScriptCMDNT(cmdArg, false, true, true, true, request.ProbeCompleted);
                    }

                    output.CanRunAfter = request.CanRun;
                    output.ShouldStop = true;
                    return;
                }

                string currentCaseDir = res.WorkingDirectory;
                string pathToProbeFile = Probing.GetPathToProbedResults(currentCaseDir, currField, res);
                if (File.Exists(pathToProbeFile))
                {
                    if (currField.FieldType == fieldType.vector)
                    {
                        Probing vectors = new Probing(points, currentCaseDir, res.WorkingDirectory, currField, res, request.Run);
                        output.VectorTree.AppendRange(vectors.ResultVec, new GH_Path(0));
                    }
                    else
                    {
                        Probing scalars = new Probing(points, currentCaseDir, res.WorkingDirectory, currField, res, request.Run);
                        output.ScalarTree.AppendRange(scalars.ResultScalar, new GH_Path(0));
                    }
                }
                else
                {
                    addMessage(GH_RuntimeMessageLevel.Warning, ReturnMsg.FieldDoesntExist(currentCaseDir, currField));
                }
            }
            catch (Exception ex)
            {
                addMessage(GH_RuntimeMessageLevel.Error, ex.ToString());
            }
        }

        private static void ReportOutsideDomainVectors(
            OpenFoamPointProbeResult output,
            Action<GH_RuntimeMessageLevel, string> addMessage)
        {
            try
            {
                if (output.VectorTree.IsEmpty)
                {
                    return;
                }

                var list = output.VectorTree.get_Branch(new GH_Path(0));
                var listVecs = new List<GH_Vector>();
                foreach (object item in list)
                {
                    listVecs.Add((GH_Vector)item);
                }

                int[] indicesOfExtremeProbes = Probing.ReturnProbeIndicesOutsideDomain(listVecs);
                if (indicesOfExtremeProbes.Length > 0)
                {
                    addMessage(GH_RuntimeMessageLevel.Remark, ReturnMsg.PointsOutsideDomain(indicesOfExtremeProbes));
                }
            }
            catch (Exception)
            {
                addMessage(GH_RuntimeMessageLevel.Warning, ReturnMsg.PleaseRunProbingComponent());
            }
        }
    }

    internal sealed class FluidX3DPointProbeRequest
    {
        public object ResultValue { get; set; }
        public List<Point3d> PointsInput { get; set; } = new List<Point3d>();
        public int QuantityInt { get; set; }
        public int TimeModeInt { get; set; } = 2;
        public double TargetTimeSeconds { get; set; }
        public Interval TimeWindow { get; set; } = new Interval(0.0, 30.0);
        public bool Run { get; set; }
        public Action<GH_RuntimeMessageLevel, string> AddRuntimeMessage { get; set; }
        public Action<string> SetComponentMessage { get; set; }
    }

    internal sealed class FluidX3DPointProbeResult
    {
        public List<Point3d> Points { get; } = new List<Point3d>();
        public GH_Structure<GH_Vector> VelocityTree { get; set; } = new GH_Structure<GH_Vector>();
        public GH_Structure<GH_Number> RhoTree { get; set; } = new GH_Structure<GH_Number>();
        public List<Vector3d> VelocityAverage { get; set; } = new List<Vector3d>();
        public List<double> RhoAverage { get; set; } = new List<double>();
        public List<double> SampledTimes { get; set; } = new List<double>();
        public List<double> SampledSteps { get; set; } = new List<double>();
        public List<string> SampledFiles { get; set; } = new List<string>();
        public int OutsideCount { get; set; }
    }

    internal sealed class FluidX3DPointProbeState
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

        public FluidX3DPointProbeResult Execute(FluidX3DPointProbeRequest request)
        {
            Action<GH_RuntimeMessageLevel, string> addMessage =
                request.AddRuntimeMessage ?? ((_, __) => { });

            var output = new FluidX3DPointProbeResult();
            double startTimeSeconds = request.TimeWindow.T0;
            double endTimeSeconds = request.TimeWindow.T1;

            bool runJustPressed = request.Run && !_lastRunState;
            if (runJustPressed && request.PointsInput.Count > 0)
            {
                CacheProbePoints(request.PointsInput);
            }

            List<Point3d> points = GetEffectiveProbePoints(request.PointsInput, request.Run);
            output.Points.AddRange(points);

            string resolvedCaseDir = string.Empty;
            TryResolveCaseDirectoryFromResult(request.ResultValue, out resolvedCaseDir);

            bool probeSucceeded = false;

            try
            {
                if (!request.Run)
                {
                    request.SetComponentMessage?.Invoke("Toggle 'Run' to probe");
                    return OutputOrCached(output);
                }

                if (points.Count == 0)
                {
                    addMessage(GH_RuntimeMessageLevel.Warning, "At least one probe point is required.");
                    request.SetComponentMessage?.Invoke("Connect probe points");
                    return OutputOrCached(output);
                }

                FluidX3DVtkProbeQuantity quantity = request.QuantityInt == 1
                    ? FluidX3DVtkProbeQuantity.DensityRho
                    : FluidX3DVtkProbeQuantity.VelocityU;

                FluidX3DVtkProbeTimeMode timeMode = request.TimeModeInt switch
                {
                    1 => FluidX3DVtkProbeTimeMode.ClosestPhysicalTime,
                    2 => FluidX3DVtkProbeTimeMode.AverageOverRange,
                    _ => FluidX3DVtkProbeTimeMode.Latest
                };

                string probeDir = ResolveProbeDirectory(resolvedCaseDir);

                if (string.IsNullOrWhiteSpace(probeDir))
                {
                    addMessage(GH_RuntimeMessageLevel.Warning, "FluidX3D RES input is required.");
                    request.SetComponentMessage?.Invoke("Connect a valid result");
                    return OutputOrCached(output);
                }

                request.SetComponentMessage?.Invoke(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} | {1}",
                    quantity == FluidX3DVtkProbeQuantity.VelocityU ? "U" : "rho",
                    timeMode == FluidX3DVtkProbeTimeMode.Latest
                        ? "Latest"
                        : timeMode == FluidX3DVtkProbeTimeMode.ClosestPhysicalTime
                            ? "Closest T"
                            : "Avg[T0,T1]"));

                if (!Directory.Exists(probeDir))
                {
                    throw new DirectoryNotFoundException(
                        "FluidX3D output folder not found. Case directory: "
                        + resolvedCaseDir
                        + ". Checked: "
                        + probeDir);
                }

                if (!ContainsFieldVtkFiles(probeDir, quantity))
                {
                    throw new FileNotFoundException("No matching VTK files found for selected field in: " + probeDir);
                }

                FluidX3DVtkProbeRequest probeRequest = new FluidX3DVtkProbeRequest
                {
                    ExportDirectory = probeDir,
                    Quantity = quantity,
                    TimeMode = timeMode,
                    TargetTimeSeconds = request.TargetTimeSeconds,
                    StartTimeSeconds = startTimeSeconds,
                    EndTimeSeconds = endTimeSeconds
                };

                for (int i = 0; i < points.Count; i++)
                {
                    Point3d p = points[i];
                    probeRequest.RhinoPoints.Add(new FluidX3DPoint3(p.X, p.Y, p.Z));
                }

                FluidX3DVtkProbeResult result = FluidX3DVtkProber.Probe(probeRequest);
                output.SampledTimes.AddRange(result.SampledTimesSeconds);
                for (int i = 0; i < result.SampledSteps.Count; i++)
                {
                    output.SampledSteps.Add(result.SampledSteps[i]);
                }

                output.SampledFiles.AddRange(result.SampledFiles);
                output.OutsideCount = result.OutsideDomainPointCount;
                if (!string.IsNullOrWhiteSpace(result.Status))
                {
                    addMessage(GH_RuntimeMessageLevel.Remark, result.Status);
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
                            output.VelocityTree.Append(new GH_Vector(new Vector3d(v.X, v.Y, v.Z)), path);
                        }
                    }

                    if (result.AverageVectors != null)
                    {
                        for (int i = 0; i < result.AverageVectors.Length; i++)
                        {
                            FluidX3DPoint3 v = result.AverageVectors[i];
                            output.VelocityAverage.Add(new Vector3d(v.X, v.Y, v.Z));
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
                            output.RhoTree.Append(new GH_Number(values[i]), path);
                        }
                    }

                    if (result.AverageScalars != null)
                    {
                        output.RhoAverage.AddRange(result.AverageScalars);
                    }
                }

                probeSucceeded = true;
            }
            catch (Exception ex)
            {
                addMessage(GH_RuntimeMessageLevel.Error, ex.Message);
            }
            finally
            {
                _lastRunState = request.Run;
            }

            if (probeSucceeded)
            {
                CacheProbeOutputs(output);
            }
            else if (_hasCachedOutputs)
            {
                ApplyCachedOutputs(output);
            }

            return output;
        }

        public static void WriteOutputs(IGH_DataAccess da, FluidX3DPointProbeResult result)
        {
            da.SetDataList(0, result.Points);
            da.SetDataTree(1, result.VelocityTree);
            da.SetDataTree(2, result.RhoTree);
            da.SetDataList(3, result.VelocityAverage);
            da.SetDataList(4, result.RhoAverage);
            da.SetDataList(5, result.SampledTimes);
            da.SetDataList(6, result.SampledSteps);
            da.SetDataList(7, result.SampledFiles);
            da.SetData(8, result.OutsideCount);
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

        private FluidX3DPointProbeResult OutputOrCached(FluidX3DPointProbeResult output)
        {
            if (_hasCachedOutputs)
            {
                ApplyCachedOutputs(output);
            }

            return output;
        }

        private void CacheProbeOutputs(FluidX3DPointProbeResult result)
        {
            _cachedVelocityTree = result.VelocityTree ?? new GH_Structure<GH_Vector>();
            _cachedRhoTree = result.RhoTree ?? new GH_Structure<GH_Number>();
            _cachedVelocityAverage = new List<Vector3d>(result.VelocityAverage ?? new List<Vector3d>());
            _cachedRhoAverage = new List<double>(result.RhoAverage ?? new List<double>());
            _cachedSampledTimes = new List<double>(result.SampledTimes ?? new List<double>());
            _cachedSampledSteps = new List<double>(result.SampledSteps ?? new List<double>());
            _cachedSampledFiles = new List<string>(result.SampledFiles ?? new List<string>());
            _cachedOutsideCount = result.OutsideCount;
            _hasCachedOutputs = true;
        }

        private void ApplyCachedOutputs(FluidX3DPointProbeResult output)
        {
            output.VelocityTree = _cachedVelocityTree;
            output.RhoTree = _cachedRhoTree;
            output.VelocityAverage = new List<Vector3d>(_cachedVelocityAverage);
            output.RhoAverage = new List<double>(_cachedRhoAverage);
            output.SampledTimes = new List<double>(_cachedSampledTimes);
            output.SampledSteps = new List<double>(_cachedSampledSteps);
            output.SampledFiles = new List<string>(_cachedSampledFiles);
            output.OutsideCount = _cachedOutsideCount;
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
    }
}
