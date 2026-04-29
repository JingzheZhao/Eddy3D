using Eddy.Properties;
using EddyLib;
using EddyLib.Docker;
using EddyLib.Strings;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Eddy
{
    public class CompProbe : GH_Component, IGH_VariableParameterComponent
    {
        private enum ProbeLayoutMode
        {
            Neutral,
            OpenFoam,
            FluidX3D
        }

        private ProbeLayoutMode _layoutMode = ProbeLayoutMode.Neutral;
        private bool _updatingParameters;
        private bool _subscribed;
        private bool _canRun = true;
        private string _dockerProbingError;

        private IGH_Param _resultInput;
        private IGH_Param _pointsInput;
        private IGH_Param _runInput;
        private IGH_Param _probePointsOutput;

        private readonly FluidX3DPointProbeState _fluidX3DState = new FluidX3DPointProbeState();

        public override GH_Exposure Exposure => GH_Exposure.senary;

        public CompProbe()
          : base(
              "Probe",
              "Probe",
              "Samples simulation results at points. Inputs and outputs adapt to the connected simulation engine.\r\n\r\n" + EddyVersion.toString(),
              EddyVersion.Name,
              "1 | Wind")
        {
        }

        public override void CreateAttributes()
        {
            Attributes = new ProbeRunButtonAttributes(this);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Eddy simulation result.", GH_ParamAccess.item);
            pManager[0].Optional = false;
            _resultInput = pManager[0];
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            SubscribeParameterEvents();
            UpdateLayoutFromConnectedResult(true);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            UnsubscribeParameterEvents();
            base.RemovedFromDocument(document);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            OFResult result = null;
            if (!DA.GetData(0, ref result))
            {
                UpdateLayoutFromConnectedResult(false);
                Message = "Connect Result";
                return;
            }

            ProbeLayoutMode desiredMode = GetMode(result);
            if (ApplyLayout(desiredMode, true))
            {
                return;
            }

            if (_dockerProbingError != null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _dockerProbingError);
                _dockerProbingError = null;
            }

            if (desiredMode == ProbeLayoutMode.FluidX3D)
            {
                SolveFluidX3D(DA, result);
            }
            else
            {
                SolveOpenFoam(DA, result);
            }
        }

        private void SolveOpenFoam(IGH_DataAccess DA, OFResult result)
        {
            List<Point3d> points = new List<Point3d>();
            string probeName = string.Empty;
            int interpolationScheme = 3;
            int fieldIndex = 0;
            bool run = false;

            DA.GetDataList(1, points);
            DA.GetData(2, ref probeName);
            DA.GetData(3, ref interpolationScheme);
            DA.GetData(4, ref fieldIndex);
            DA.GetData(5, ref run);
            run = run || ConsumeToggleRun(5);

            OpenFoamPointProbeResult probeResult = OpenFoamPointProbeRunner.Execute(
                new OpenFoamPointProbeRequest
                {
                    Result = result,
                    Points = points,
                    ProbeName = probeName,
                    InterpolationScheme = interpolationScheme,
                    FieldIndex = fieldIndex,
                    Run = run,
                    CanRun = _canRun,
                    ProbeCompleted = ProbingComplete,
                    RunDockerProbing = RunDockerProbing,
                    AddRuntimeMessage = AddRuntimeMessage,
                    SetComponentMessage = message => Message = message
                });

            _canRun = probeResult.CanRunAfter;
            if (probeResult.ShouldStop)
            {
                return;
            }

            OpenFoamPointProbeRunner.WriteOutputs(DA, probeResult);
            _canRun = true;
        }

        private void SolveFluidX3D(IGH_DataAccess DA, OFResult result)
        {
            List<Point3d> points = new List<Point3d>();
            int quantityInt = 0;
            int timeModeInt = 2;
            double targetTimeSeconds = 0.0;
            Interval timeWindow = new Interval(0.0, 30.0);
            bool run = false;

            DA.GetDataList(1, points);
            DA.GetData(2, ref quantityInt);
            DA.GetData(3, ref timeModeInt);
            DA.GetData(4, ref targetTimeSeconds);
            DA.GetData(5, ref timeWindow);
            DA.GetData(6, ref run);
            run = run || ConsumeToggleRun(6);

            FluidX3DPointProbeResult probeResult = _fluidX3DState.Execute(
                new FluidX3DPointProbeRequest
                {
                    ResultValue = result,
                    PointsInput = points,
                    QuantityInt = quantityInt,
                    TimeModeInt = timeModeInt,
                    TargetTimeSeconds = targetTimeSeconds,
                    TimeWindow = timeWindow,
                    Run = run,
                    AddRuntimeMessage = AddRuntimeMessage,
                    SetComponentMessage = message => Message = message
                });

            FluidX3DPointProbeState.WriteOutputs(DA, probeResult);
        }

        public void ProbingComplete(object sender, EventArgs e)
        {
            _canRun = false;
            ExpireSolution(true);
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

        private void RunDockerProbing(List<string> commands, string workDir)
        {
            _dockerProbingError = null;
            try
            {
                var runner = new DockerRunner();
                var bashCmd = DockerRunner.BuildCommandChain(commands);
                var launchLog = runner.RunInteractive(bashCmd, workDir);
                if (!string.IsNullOrWhiteSpace(launchLog)
                    && launchLog.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _dockerProbingError = "Docker probing launch issue: " + launchLog;
                }
            }
            catch (Exception ex)
            {
                _dockerProbingError = "Docker probing launch exception: " + ex.Message;
            }
        }

        private void SubscribeParameterEvents()
        {
            if (_subscribed)
            {
                return;
            }

            Params.ParameterSourcesChanged += Params_ParameterSourcesChanged;
            if (ResultInput != null)
            {
                ResultInput.ObjectChanged += ResultInput_ObjectChanged;
            }

            _subscribed = true;
        }

        private void UnsubscribeParameterEvents()
        {
            if (!_subscribed)
            {
                return;
            }

            Params.ParameterSourcesChanged -= Params_ParameterSourcesChanged;
            if (ResultInput != null)
            {
                ResultInput.ObjectChanged -= ResultInput_ObjectChanged;
            }

            _subscribed = false;
        }

        private void Params_ParameterSourcesChanged(object sender, GH_ParamServerEventArgs e)
        {
            if (!_updatingParameters)
            {
                UpdateLayoutFromConnectedResult(true);
            }
        }

        private void ResultInput_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            if (!_updatingParameters)
            {
                UpdateLayoutFromConnectedResult(true);
            }
        }

        private void UpdateLayoutFromConnectedResult(bool expire)
        {
            if (TryDetectModeFromResultInput(out ProbeLayoutMode detectedMode))
            {
                ApplyLayout(detectedMode, expire);
            }
        }

        private bool TryDetectModeFromResultInput(out ProbeLayoutMode mode)
        {
            mode = ProbeLayoutMode.Neutral;

            IGH_Param resultParam = ResultInput;
            if (resultParam == null)
            {
                return true;
            }

            if (resultParam.SourceCount == 0)
            {
                mode = ProbeLayoutMode.Neutral;
                return true;
            }

            if (TryReadResultFromParam(resultParam, out OFResult result))
            {
                mode = GetMode(result);
                return true;
            }

            foreach (IGH_Param source in resultParam.Sources)
            {
                if (TryReadResultFromParam(source, out result))
                {
                    mode = GetMode(result);
                    return true;
                }
            }

            return false;
        }

        private static bool TryReadResultFromParam(IGH_Param param, out OFResult result)
        {
            result = null;
            if (param == null || param.VolatileData == null)
            {
                return false;
            }

            var data = param.VolatileData;
            for (int i = 0; i < data.PathCount; i++)
            {
                IList branch = data.get_Branch(i);
                foreach (object item in branch)
                {
                    if (TryUnwrapResult(item, out result))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryUnwrapResult(object value, out OFResult result)
        {
            result = null;
            if (value is OFResult direct)
            {
                result = direct;
                return true;
            }

            if (value is GH_ObjectWrapper wrapper && wrapper.Value is OFResult wrapped)
            {
                result = wrapped;
                return true;
            }

            if (value is IGH_Goo goo)
            {
                OFResult cast = null;
                if (goo.CastTo(out cast) && cast != null)
                {
                    result = cast;
                    return true;
                }
            }

            return false;
        }

        private bool ApplyLayout(ProbeLayoutMode mode, bool expire)
        {
            if (_layoutMode == mode && LayoutMatches(mode))
            {
                return false;
            }

            _updatingParameters = true;
            try
            {
                EnsureCommonParams();

                List<IGH_Param> desiredInputs = BuildInputLayout(mode);
                List<IGH_Param> desiredOutputs = BuildOutputLayout(mode);

                ApplyInputLayout(desiredInputs);
                ApplyOutputLayout(desiredOutputs);

                _layoutMode = mode;
                VariableParameterMaintenance();
                Params.OnParametersChanged();
            }
            finally
            {
                _updatingParameters = false;
            }

            if (expire)
            {
                ExpireSolution(true);
            }

            return true;
        }

        private bool LayoutMatches(ProbeLayoutMode mode)
        {
            return Params.Input.Count == BuildInputLayout(mode).Count
                && Params.Output.Count == BuildOutputLayout(mode).Count;
        }

        private List<IGH_Param> BuildInputLayout(ProbeLayoutMode mode)
        {
            EnsureCommonParams();

            if (mode == ProbeLayoutMode.Neutral)
            {
                return new List<IGH_Param> { _resultInput };
            }

            if (mode == ProbeLayoutMode.OpenFoam)
            {
                return new List<IGH_Param>
                {
                    _resultInput,
                    _pointsInput,
                    CreateOpenFoamNameInput(),
                    CreateOpenFoamInterpolationInput(),
                    CreateOpenFoamFieldInput(),
                    _runInput
                };
            }

            return new List<IGH_Param>
            {
                _resultInput,
                _pointsInput,
                CreateFluidX3DQuantityInput(),
                CreateFluidX3DTimeModeInput(),
                CreateFluidX3DTargetTimeInput(),
                CreateFluidX3DTimeWindowInput(),
                _runInput
            };
        }

        private List<IGH_Param> BuildOutputLayout(ProbeLayoutMode mode)
        {
            EnsureCommonParams();

            if (mode == ProbeLayoutMode.Neutral)
            {
                return new List<IGH_Param>();
            }

            if (mode == ProbeLayoutMode.OpenFoam)
            {
                return new List<IGH_Param>
                {
                    _probePointsOutput,
                    CreateOpenFoamResultOutput()
                };
            }

            return new List<IGH_Param>
            {
                _probePointsOutput,
                CreateVectorOutput(
                    GH_Strings.FluidX3DProbe.VelocityByTime,
                    GH_Strings.FluidX3DProbe.VelocityByTimeNick,
                    GH_Strings.FluidX3DProbe.VelocityByTimeDesc,
                    GH_ParamAccess.tree),
                CreateNumberOutput(
                    GH_Strings.FluidX3DProbe.DensityByTime,
                    GH_Strings.FluidX3DProbe.DensityByTimeNick,
                    GH_Strings.FluidX3DProbe.DensityByTimeDesc,
                    GH_ParamAccess.tree),
                CreateVectorOutput(
                    GH_Strings.FluidX3DProbe.VelocityAverage,
                    GH_Strings.FluidX3DProbe.VelocityAverageNick,
                    GH_Strings.FluidX3DProbe.VelocityAverageDesc,
                    GH_ParamAccess.list),
                CreateNumberOutput(
                    GH_Strings.FluidX3DProbe.DensityAverage,
                    GH_Strings.FluidX3DProbe.DensityAverageNick,
                    GH_Strings.FluidX3DProbe.DensityAverageDesc,
                    GH_ParamAccess.list),
                CreateNumberOutput(
                    GH_Strings.FluidX3DProbe.SampledTimes,
                    GH_Strings.FluidX3DProbe.SampledTimesNick,
                    GH_Strings.FluidX3DProbe.SampledTimesDesc,
                    GH_ParamAccess.list),
                CreateNumberOutput(
                    GH_Strings.FluidX3DProbe.SampledSteps,
                    GH_Strings.FluidX3DProbe.SampledStepsNick,
                    GH_Strings.FluidX3DProbe.SampledStepsDesc,
                    GH_ParamAccess.list),
                CreateTextOutput(
                    GH_Strings.FluidX3DProbe.SampledFiles,
                    GH_Strings.FluidX3DProbe.SampledFilesNick,
                    GH_Strings.FluidX3DProbe.SampledFilesDesc,
                    GH_ParamAccess.list),
                CreateIntegerOutput(
                    GH_Strings.FluidX3DProbe.OutsideCount,
                    GH_Strings.FluidX3DProbe.OutsideCountNick,
                    GH_Strings.FluidX3DProbe.OutsideCountDesc,
                    GH_ParamAccess.item)
            };
        }

        private void ApplyInputLayout(List<IGH_Param> desired)
        {
            HashSet<IGH_Param> keep = new HashSet<IGH_Param>(desired);
            foreach (IGH_Param param in Params.Input.ToList())
            {
                if (!keep.Contains(param))
                {
                    Params.UnregisterInputParameter(param, true);
                }
            }

            for (int i = 0; i < desired.Count; i++)
            {
                IGH_Param param = desired[i];
                int current = Params.Input.IndexOf(param);
                if (current == i)
                {
                    continue;
                }

                if (current >= 0)
                {
                    Params.UnregisterInputParameter(param, false);
                }

                Params.RegisterInputParam(param, i);
            }
        }

        private void ApplyOutputLayout(List<IGH_Param> desired)
        {
            HashSet<IGH_Param> keep = new HashSet<IGH_Param>(desired);
            foreach (IGH_Param param in Params.Output.ToList())
            {
                if (!keep.Contains(param))
                {
                    Params.UnregisterOutputParameter(param, true);
                }
            }

            for (int i = 0; i < desired.Count; i++)
            {
                IGH_Param param = desired[i];
                int current = Params.Output.IndexOf(param);
                if (current == i)
                {
                    continue;
                }

                if (current >= 0)
                {
                    Params.UnregisterOutputParameter(param, false);
                }

                Params.RegisterOutputParam(param, i);
            }
        }

        public bool CanInsertParameter(GH_ParameterSide side, int index) => false;

        public bool CanRemoveParameter(GH_ParameterSide side, int index) => false;

        public IGH_Param CreateParameter(GH_ParameterSide side, int index) => null;

        public bool DestroyParameter(GH_ParameterSide side, int index) => false;

        public void VariableParameterMaintenance()
        {
            EnsureCommonParams();

            ConfigureParam(_resultInput, "Result", "Res", "Eddy simulation result.", GH_ParamAccess.item, false);
            if (_layoutMode == ProbeLayoutMode.OpenFoam)
            {
                ConfigureParam(_pointsInput, "Probing points", "Points", "List of probing points.", GH_ParamAccess.list, false);
                ConfigureParam(_runInput, "Run", "Run", "Run the component.", GH_ParamAccess.item, false);
                ConfigureParam(_probePointsOutput, "Probing points", "Probes", "List of probing points (caution: maybe culled).", GH_ParamAccess.list, false);
            }
            else if (_layoutMode == ProbeLayoutMode.FluidX3D)
            {
                ConfigureParam(_pointsInput, GH_Strings.FluidX3DProbe.Points, GH_Strings.FluidX3DProbe.PointsNick, GH_Strings.FluidX3DProbe.PointsDesc, GH_ParamAccess.list, false);
                ConfigureParam(_runInput, GH_Strings.FluidX3DProbe.Run, GH_Strings.FluidX3DProbe.RunNick, GH_Strings.FluidX3DProbe.RunDesc, GH_ParamAccess.item, false);
                ConfigureParam(_probePointsOutput, GH_Strings.FluidX3DProbe.PointsOut, GH_Strings.FluidX3DProbe.PointsOutNick, GH_Strings.FluidX3DProbe.PointsOutDesc, GH_ParamAccess.list, false);
            }
        }

        private void EnsureCommonParams()
        {
            _resultInput ??= Params.Input.FirstOrDefault(p => p.NickName == "Res") ?? CreateGenericInput("Result", "Res", "Eddy simulation result.", GH_ParamAccess.item, false);
            _pointsInput ??= CreatePointInput("Probe Points", "Pts", "Probe points.", GH_ParamAccess.list, false);
            _runInput ??= CreateBooleanInput("Run", "Run", "Run the component.", GH_ParamAccess.item, false);
            _probePointsOutput ??= CreatePointOutput("Probe Points", "Pts", "Probe points.", GH_ParamAccess.list);
        }

        private static void ConfigureParam(IGH_Param param, string name, string nick, string description, GH_ParamAccess access, bool optional)
        {
            if (param == null)
            {
                return;
            }

            param.Name = name;
            param.NickName = nick;
            param.Description = description;
            param.Access = access;
            param.Optional = optional;
        }

        private static ProbeLayoutMode GetMode(OFResult result)
        {
            return result != null && result.RunSettings != null && result.RunSettings.simEngine == SimEngine.FluidX3D
                ? ProbeLayoutMode.FluidX3D
                : ProbeLayoutMode.OpenFoam;
        }

        private IGH_Param ResultInput
        {
            get
            {
                _resultInput ??= Params.Input.FirstOrDefault(p => p.NickName == "Res");
                return _resultInput;
            }
        }

        private static IGH_Param CreateOpenFoamNameInput()
        {
            return CreateTextInput("Name of instance", "Name", "Name of instance to be probed.", GH_ParamAccess.item, true);
        }

        private static IGH_Param CreateOpenFoamInterpolationInput()
        {
            Param_Integer param = CreateIntegerInput("Interpolation Scheme", "IS", "Interpolation Scheme.", GH_ParamAccess.item, false, 3);
            param.AddNamedValue("cell", 0);
            param.AddNamedValue("cellPoint", 1);
            param.AddNamedValue("cellPointFace", 2);
            param.AddNamedValue("pointMVC", 3);
            param.AddNamedValue("cellPatchConstrained", 4);
            return param;
        }

        private static IGH_Param CreateOpenFoamFieldInput()
        {
            Param_Integer param = CreateIntegerInput("Name of field", "Field", "Name of field to be probed.", GH_ParamAccess.item, false, 0);
            param.AddNamedValue("Velocity (U) [m/s]", 0);
            param.AddNamedValue("Pressure coefficient (total(p)_coeff) [-]", 1);
            param.AddNamedValue("Pressure (p) [m^2/s^2]", 2);
            param.AddNamedValue("Turbulent dissipation rate (epsilon) [m^2/s^3]", 3);
            param.AddNamedValue("Scale of turbulence (omega) [1/s] ", 4);
            param.AddNamedValue("Turbulent kinetic energy (k) [m^2/s^2]", 5);
            param.AddNamedValue("Turbulent viscosity (nut) [m^2/s]", 6);
            param.AddNamedValue("Mass flow (phi) [m^3/s]", 7);
            param.AddNamedValue("Age of air (aoa) [s]", 8);
            param.AddNamedValue("Particle Concentration (covid19) []", 9);
            return param;
        }

        private static IGH_Param CreateOpenFoamResultOutput()
        {
            return CreateGenericOutput(
                "Probing result",
                "Res",
                "Probed results [DataTree] where the branches are the wind directions and the items are the values for each probing point.",
                GH_ParamAccess.tree);
        }

        private static IGH_Param CreateFluidX3DQuantityInput()
        {
            Param_Integer param = CreateIntegerInput(
                GH_Strings.FluidX3DProbe.Quantity,
                GH_Strings.FluidX3DProbe.QuantityNick,
                GH_Strings.FluidX3DProbe.QuantityDesc,
                GH_ParamAccess.item,
                false,
                0);
            param.AddNamedValue("Velocity U", 0);
            param.AddNamedValue("Density rho", 1);
            return param;
        }

        private static IGH_Param CreateFluidX3DTimeModeInput()
        {
            Param_Integer param = CreateIntegerInput(
                GH_Strings.FluidX3DProbe.TimeMode,
                GH_Strings.FluidX3DProbe.TimeModeNick,
                GH_Strings.FluidX3DProbe.TimeModeDesc,
                GH_ParamAccess.item,
                false,
                2);
            param.AddNamedValue("Latest", 0);
            param.AddNamedValue("Closest physical time", 1);
            param.AddNamedValue("Average over [T0, T1]", 2);
            return param;
        }

        private static IGH_Param CreateFluidX3DTargetTimeInput()
        {
            return CreateNumberInput(
                GH_Strings.FluidX3DProbe.TargetTime,
                GH_Strings.FluidX3DProbe.TargetTimeNick,
                GH_Strings.FluidX3DProbe.TargetTimeDesc,
                GH_ParamAccess.item,
                false);
        }

        private static IGH_Param CreateFluidX3DTimeWindowInput()
        {
            return new Param_Interval
            {
                Name = GH_Strings.FluidX3DProbe.TimeWindow,
                NickName = GH_Strings.FluidX3DProbe.TimeWindowNick,
                Description = GH_Strings.FluidX3DProbe.TimeWindowDesc,
                Access = GH_ParamAccess.item,
                Optional = false
            };
        }

        private static IGH_Param CreateGenericInput(string name, string nick, string description, GH_ParamAccess access, bool optional)
        {
            return new Param_GenericObject
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access,
                Optional = optional
            };
        }

        private static IGH_Param CreatePointInput(string name, string nick, string description, GH_ParamAccess access, bool optional)
        {
            return new Param_Point
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access,
                Optional = optional
            };
        }

        private static IGH_Param CreateTextInput(string name, string nick, string description, GH_ParamAccess access, bool optional)
        {
            return new Param_String
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access,
                Optional = optional
            };
        }

        private static Param_Integer CreateIntegerInput(string name, string nick, string description, GH_ParamAccess access, bool optional, int defaultValue)
        {
            Param_Integer param = new Param_Integer
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access,
                Optional = optional
            };
            param.SetPersistentData(defaultValue);
            return param;
        }

        private static IGH_Param CreateBooleanInput(string name, string nick, string description, GH_ParamAccess access, bool optional)
        {
            GH_ToggleParam param = new GH_ToggleParam(name, nick, description)
            {
                Access = access,
                Optional = optional
            };
            return param;
        }

        private static IGH_Param CreateNumberInput(string name, string nick, string description, GH_ParamAccess access, bool optional)
        {
            return new Param_Number
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access,
                Optional = optional
            };
        }

        private static IGH_Param CreatePointOutput(string name, string nick, string description, GH_ParamAccess access)
        {
            return new Param_Point
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access
            };
        }

        private static IGH_Param CreateGenericOutput(string name, string nick, string description, GH_ParamAccess access)
        {
            return new Param_GenericObject
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access
            };
        }

        private static IGH_Param CreateVectorOutput(string name, string nick, string description, GH_ParamAccess access)
        {
            return new Param_Vector
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access
            };
        }

        private static IGH_Param CreateNumberOutput(string name, string nick, string description, GH_ParamAccess access)
        {
            return new Param_Number
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access
            };
        }

        private static IGH_Param CreateTextOutput(string name, string nick, string description, GH_ParamAccess access)
        {
            return new Param_String
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access
            };
        }

        private static IGH_Param CreateIntegerOutput(string name, string nick, string description, GH_ParamAccess access)
        {
            return new Param_Integer
            {
                Name = name,
                NickName = nick,
                Description = description,
                Access = access
            };
        }

        protected override System.Drawing.Bitmap Icon => Resources.Eddy_visualProbs;

        public override Guid ComponentGuid => new Guid("{3C791FD8-47D3-4C69-A417-3F253A795B91}");
    }
}
