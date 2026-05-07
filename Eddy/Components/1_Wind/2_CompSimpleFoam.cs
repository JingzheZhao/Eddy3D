using Eddy.Properties;
using EddyLib;
using EddyLib.FluidX3D;
using EddyLib.Helpers;
using EddyLib.OpenFOAM;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class SimpleFoam : GH_Component
    {
        private const string EngineNameOpenFoamBlueCfd = "OpenFOAM (BlueCFD)";
        private const string EngineNameOpenFoamDocker = "OpenFOAM (Docker)";
        private const string EngineNameFluidX3D = "FluidX3D";
        private SimEngine _selectedEngine;
        private string _autoWorkingDirectory = string.Empty;

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public SimpleFoam()
          : base(GH_Strings.SimpleFoam.Name, GH_Strings.SimpleFoam.Nick,
GH_Strings.SimpleFoam.Desc + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
            _selectedEngine = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? SimEngine.BlueCFD
                : SimEngine.Docker;

            EddyLib.Web.UpdateChecker.CheckForUpdateAsync();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, EngineNameOpenFoamBlueCfd, (s, e) => SetEngine(SimEngine.BlueCFD),
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows), _selectedEngine == SimEngine.BlueCFD);
            Menu_AppendItem(menu, EngineNameOpenFoamDocker, (s, e) => SetEngine(SimEngine.Docker),
                true, _selectedEngine == SimEngine.Docker);
            Menu_AppendItem(menu, EngineNameFluidX3D, (s, e) => SetEngine(SimEngine.FluidX3D),
                true, _selectedEngine == SimEngine.FluidX3D);
        }

        private void SetEngine(SimEngine engine)
        {
            _selectedEngine = engine;
            NormalizeSelectedEngineForCurrentPlatform();
            ExpireSolution(true);
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("SelectedEngine", (int)_selectedEngine);
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            if (reader.ItemExists("SelectedEngine"))
            {
                int raw = reader.GetInt32("SelectedEngine");
                if (Enum.IsDefined(typeof(SimEngine), raw))
                {
                    _selectedEngine = (SimEngine)raw;
                }
            }

            NormalizeSelectedEngineForCurrentPlatform();
            return base.Read(reader);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                GH_Strings.Common.Domain, GH_Strings.Common.DomainNick,
                GH_Strings.Common.DomainDesc,
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                GH_Strings.Common.WorkingDir, GH_Strings.Common.WorkingDirNick,
                GH_Strings.Common.WorkingDirDesc,
                GH_ParamAccess.item, DefaultDirectoriesAndPaths.CasesDir);
            pManager[1].Optional = true;

            pManager.AddGenericParameter(
                GH_Strings.Common.MeshSettings, GH_Strings.Common.MeshSettingsNick,
                GH_Strings.Common.MeshSettingsDesc,
                GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddGenericParameter(
                GH_Strings.Common.RunSettings, GH_Strings.Common.RunSettingsNick,
                GH_Strings.Common.RunSettingsDesc,
                GH_ParamAccess.item);
            pManager[3].Optional = true;

            pManager.AddBooleanParameter(
                GH_Strings.Common.RunMeshing, GH_Strings.Common.RunMeshingNick,
                GH_Strings.Common.RunMeshingDesc,
                GH_ParamAccess.item, false);

            pManager.AddBooleanParameter(
                GH_Strings.Common.MakeTrees, GH_Strings.Common.MakeTreesNick,
                GH_Strings.Common.MakeTreesDesc,
                GH_ParamAccess.item, false);

            pManager.AddBooleanParameter(
                GH_Strings.Common.RunSimulation, GH_Strings.Common.RunSimulationNick,
                GH_Strings.Common.RunSimulationDesc,
                GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(GH_Strings.Common.Result, GH_Strings.Common.ResultNick, GH_Strings.Common.ResultDesc, GH_ParamAccess.item);
            pManager.AddBooleanParameter(GH_Strings.SimpleFoam.MeshDone, GH_Strings.SimpleFoam.MeshDoneNick, GH_Strings.SimpleFoam.MeshDone, GH_ParamAccess.item);
            pManager.AddBooleanParameter(GH_Strings.SimpleFoam.SimDone, GH_Strings.SimpleFoam.SimDoneNick, GH_Strings.SimpleFoam.SimDone, GH_ParamAccess.list);
            pManager.AddTextParameter(
                GH_Strings.SimpleFoam.MeshRemainingTime,
                GH_Strings.SimpleFoam.MeshRemainingTimeNick,
                GH_Strings.SimpleFoam.MeshRemainingTime,
                GH_ParamAccess.item);
            pManager.AddTextParameter(
                GH_Strings.SimpleFoam.SimRemainingTime,
                GH_Strings.SimpleFoam.SimRemainingTimeNick,
                GH_Strings.SimpleFoam.SimRemainingTime,
                GH_ParamAccess.list);
        }

        private bool canRun = true;

        public void taskComplete(object sender, System.EventArgs e)
        {
            //RhinoApp.WriteLine("Sim complete");
            canRun = false;
            this.ExpireSolution(true);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool engineAutoSwitched = NormalizeSelectedEngineForCurrentPlatform();

            // mode to select simulation environment
            Message = _selectedEngine.ToString();
            if (engineAutoSwitched)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Remark,
                    "BlueCFD is not available on macOS. Switched Wind Simulation engine to Docker.");
            }

            if (EddyLib.Web.UpdateChecker.IsUpdateAvailable)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"A new version of Eddy3D is available: {EddyLib.Web.UpdateChecker.LatestVersion}\n" +
                    "Please visit https://github.com/Eddy3D-Dev/Eddy3D/releases to download.");
            }

            // read inputs
            //------------

            // domain
            //-------

            OFBaseDomain DOM;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(GH_Strings.Common.Domain, ref gobj))
            {
                return;
            }

            if ((gobj.Value is OFCylDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            else if ((gobj.Value is OFBoxDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid domain object"); return;
            }

            GH_ObjectWrapper gobjRunSet = null;
            DA.GetData(3, ref gobjRunSet);

            // working directory
            //------------------

            string baseWorkingDirectory = "";
            DA.GetData(GH_Strings.Common.WorkingDir, ref baseWorkingDirectory);
            baseWorkingDirectory = ResolveAutoWorkingDirectoryWhenDirIsUnwired(baseWorkingDirectory);

            // Resolve simple case names to full paths under the platform-specific Eddy3D cases folder
            baseWorkingDirectory = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(baseWorkingDirectory);

            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }

            var sep = Path.DirectorySeparatorChar.ToString();
            DirectoryInfo parentDir = Directory.GetParent(baseWorkingDirectory.EndsWith(sep) ? baseWorkingDirectory : string.Concat(baseWorkingDirectory, sep));
            var myParentDir = parentDir?.Parent?.FullName ?? string.Empty;

            if (myParentDir == @"C:\" || myParentDir == "/")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please use an additional subfolder for Eddy3D simulations."); return;
            }

            baseWorkingDirectory = DirectoryHelpers.EnsureTrailingBackslash(baseWorkingDirectory);

            // meshing settings
            //-----------------

            OFMeshSettings MeshSettings = new OFMeshSettings(); // sets default mesh settings

            GH_ObjectWrapper gobjMeshSet = null;
            if (DA.GetData(GH_Strings.Common.MeshSettings, ref gobjMeshSet))
            {
                if (gobjMeshSet.Value is OFMeshSettings)
                {
                    MeshSettings = (OFMeshSettings)gobjMeshSet.Value;
                }
            }
            MeshSettings.SetDirectories(baseWorkingDirectory);

            bool makeTrees = false;
            bool runSimulation = false;
            bool runMeshing = false;

            DA.GetData(GH_Strings.Common.MakeTrees, ref makeTrees);
            DA.GetData(GH_Strings.Common.RunSimulation, ref runSimulation);
            DA.GetData(GH_Strings.Common.RunMeshing, ref runMeshing);

            if (!IsEngineSupportedOnCurrentPlatform(_selectedEngine))
            {
                string unsupportedMessage = GetEngineDisplayName(_selectedEngine)
                    + " is not supported on this operating system. Simulation was skipped.";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, unsupportedMessage);
                SetSkippedOutputs(
                    DA,
                    DOM,
                    baseWorkingDirectory,
                    MeshSettings,
                    BuildOpenFoamRunSettings(gobjRunSet),
                    unsupportedMessage);
                return;
            }

            if (!TryEnsureSelectedEngineAvailable(out string engineDetails))
            {
                string unavailableMessage = GetEngineDisplayName(_selectedEngine)
                    + " is selected but not installed. " + engineDetails
                    + " Simulation was skipped.";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, unavailableMessage);
                SetSkippedOutputs(
                    DA,
                    DOM,
                    baseWorkingDirectory,
                    MeshSettings,
                    BuildOpenFoamRunSettings(gobjRunSet),
                    unavailableMessage);
                return;
            }

            if (_selectedEngine == SimEngine.FluidX3D)
            {
                RunFluidX3DSimulation(
                    DA,
                    DOM,
                    gobjRunSet,
                    baseWorkingDirectory,
                    MeshSettings,
                    runMeshing,
                    runSimulation);
                canRun = true;
                return;
            }

            OFRunSettings RunSettings = BuildOpenFoamRunSettings(gobjRunSet, warnOnInvalid: true);
            RunSettings.simEngine = _selectedEngine;

            if (!RunSettings.IdenticalMPI && RunSettings.CPUs > 1 && RunSettings.BlueCFDIsInstalled)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "To use multiple CPUs, you need to ensure to use the same msmpi.dll for both Windows and BlueCFD. This is a BlueCFD issue and will hopefully be fixed in a future version."); return;
            }

            #region RUN BLOCKMESH

            if (DOM is OFBoxDomain)
            {
                RunBlockMesh.RunBox((OFBoxDomain)DOM, MeshSettings, RunSettings, baseWorkingDirectory);
            }
            else
            {
                RunBlockMesh.RunCyl((OFCylDomain)DOM, MeshSettings, RunSettings, baseWorkingDirectory);
            }

            #endregion RUN BLOCKMESH

            #region RUN SNAPPY HEX

            RunSnappy.Run(DOM, MeshSettings, RunSettings, out string logfileOutput);

            #endregion RUN SNAPPY HEX

            #region RUN SIMULATION

            if (RunSettings.endTime == 0 || RunSettings.purgeWrite == 0 || RunSettings.writeInterval == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid inputs.");
                return;
            }

            #region Trees

            if (DOM.Trees.Count > 0)
            {
                var trees = new TreeObject(DOM, MeshSettings);
            }
            else
            {
                TreeObject.RemoveDicts(DOM, MeshSettings);
            }

            #endregion Trees

            RunFoamSimulation.Run(DOM, MeshSettings, RunSettings, baseWorkingDirectory);

            #endregion RUN SIMULATION

            #region START PROCESSES

            if ((runMeshing || runSimulation) && canRun)
            {
                Analytics.Analytics.TrackSimulationRun(
                    "outdoor",
                    Analytics.Analytics.GetAnalyticsEngine(_selectedEngine));
            }

            bool hasExistingIterations = false;
            if (runSimulation && !runMeshing && canRun)
            {
                foreach (var windDir in DOM.BCond.WindDirections)
                {
                    var caseDir = Path.Combine(baseWorkingDirectory, windDir.ToString());
                    if (OpenFOAMHelpers.HasIterationFolders(caseDir))
                    {
                        hasExistingIterations = true;
                        break;
                    }
                }
                if (hasExistingIterations)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        "Existing iteration results found. Continuing simulation from the last time step.");
                }
            }

            if (_selectedEngine == SimEngine.Docker)
            {
                // Docker: launch scripts (.command on macOS, .bat on Windows)
                var scriptsDir = Path.Combine(baseWorkingDirectory, "Scripts");
                var dockerScriptExt = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".bat" : ".command";

                if (makeTrees == true && canRun)
                {
                    OpenCommandFile(Path.Combine(scriptsDir, "run_make_trees" + dockerScriptExt));
                }

                if (runMeshing == true && runSimulation == true && canRun)
                {
                    Utilities.DeletePhi(MeshSettings, DOM);
                    OpenCommandFile(Path.Combine(scriptsDir, "run" + dockerScriptExt));
                }
                else if (runMeshing == true && runSimulation == false && canRun)
                {
                    Utilities.DeletePhi(MeshSettings, DOM);
                    OpenCommandFile(Path.Combine(scriptsDir, "run_mesh" + dockerScriptExt));
                }
                else if (runMeshing == false && runSimulation == true && canRun)
                {
                    Utilities.DeletePhi(MeshSettings, DOM);
                    string simScript = hasExistingIterations ? "run_sim_continue_all" : "run_sim_all";
                    OpenCommandFile(Path.Combine(scriptsDir, simScript + dockerScriptExt));
                }
            }
            else
            {
                // BlueCFD: run via batch files (Windows only)
                if (makeTrees == true && canRun)
                {
                    Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, Path.Combine(baseWorkingDirectory, "Scripts", "run_make_trees.bat"), taskComplete);
                }

                if (runMeshing == true && runSimulation == true && canRun)
                {
                    Utilities.DeletePhi(MeshSettings, DOM);
                    Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, Path.Combine(baseWorkingDirectory, "Scripts", "run.bat"), taskComplete);
                }
                else if (runMeshing == true && runSimulation == false && canRun)
                {
                    Utilities.DeletePhi(MeshSettings, DOM);
                    Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, Path.Combine(baseWorkingDirectory, "Scripts", "run_mesh.bat"), taskComplete);
                }
                else if (runMeshing == false && runSimulation == true && canRun)
                {
                    Utilities.DeletePhi(MeshSettings, DOM);
                    string simBat = hasExistingIterations ? "run_sim_continue_all.bat" : "run_sim_all.bat";
                    Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, Path.Combine(baseWorkingDirectory, "Scripts", simBat), taskComplete);
                }
            }

            #endregion START PROCESSES

            OFResult RES = new OFResult(DOM, RunSettings, MeshSettings, baseWorkingDirectory);
            DA.SetData(GH_Strings.Common.Result, RES);

            var windDirs = DOM?.BCond?.WindDirections ?? new System.Collections.Generic.List<int>();

            var parseOptions = new OpenFOAMLogParseOptions { RollingWindow = 5 };

            var meshLog = OpenFOAMLogLocator.FindLatestMeshingLog(MeshSettings.meshWorkingDir);
            var meshStatus = OpenFOAMLogParser.ParseMeshingLog(meshLog, parseOptions);

            var simDone = new System.Collections.Generic.List<bool>(windDirs.Count);
            var simulationRemainingTime = new System.Collections.Generic.List<string>(windDirs.Count);
            var simStatuses = new System.Collections.Generic.List<OpenFOAMLogStatus>(windDirs.Count);
            foreach (var dir in windDirs)
            {
                var caseDir = Path.Combine(baseWorkingDirectory, dir.ToString());
                var simLog = OpenFOAMLogLocator.FindLatestSimulationLog(caseDir);
                var simStatus = OpenFOAMLogParser.ParseSimulationLog(simLog,
                    new OpenFOAMLogParseOptions { RollingWindow = 5, TotalIterations = RunSettings.endTime });

                simStatuses.Add(simStatus);
                simDone.Add(simStatus.IsFinished);
                simulationRemainingTime.Add(OpenFOAMStatusFormatter.FormatRemainingTime(simStatus));
            }
            ApplyQueuedSimulationRemainingTimePredictions(simStatuses, simulationRemainingTime);

            DA.SetData(GH_Strings.SimpleFoam.MeshDone, meshStatus.IsFinished);
            DA.SetDataList(GH_Strings.SimpleFoam.SimDone, simDone);
            DA.SetData(
                GH_Strings.SimpleFoam.MeshRemainingTime,
                OpenFOAMStatusFormatter.FormatRemainingTime(meshStatus));
            DA.SetDataList(
                GH_Strings.SimpleFoam.SimRemainingTime,
                simulationRemainingTime);

            canRun = true;
        }

        private string ResolveAutoWorkingDirectoryWhenDirIsUnwired(string workingDirInput)
        {
            bool hasDirSource = Params != null
                && Params.Input != null
                && Params.Input.Count > 1
                && Params.Input[1].SourceCount > 0;

            if (hasDirSource)
            {
                _autoWorkingDirectory = string.Empty;
                return workingDirInput;
            }

            if (!ShouldAutoGenerateCaseDirectory(workingDirInput))
            {
                _autoWorkingDirectory = string.Empty;
                return workingDirInput;
            }

            if (!string.IsNullOrWhiteSpace(_autoWorkingDirectory))
            {
                return _autoWorkingDirectory;
            }

            _autoWorkingDirectory = CreateTimestampedCaseDirectoryPath();
            return _autoWorkingDirectory;
        }

        private static bool ShouldAutoGenerateCaseDirectory(string workingDirInput)
        {
            if (string.IsNullOrWhiteSpace(workingDirInput))
            {
                return true;
            }

            string normalizedInput = NormalizePathSafe(workingDirInput);
            string normalizedCasesRoot = NormalizePathSafe(DefaultDirectoriesAndPaths.CasesDir);
            if (string.IsNullOrWhiteSpace(normalizedInput) || string.IsNullOrWhiteSpace(normalizedCasesRoot))
            {
                return false;
            }

            StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            return string.Equals(normalizedInput, normalizedCasesRoot, comparison);
        }

        private static string NormalizePathSafe(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path.Trim())
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string CreateTimestampedCaseDirectoryPath()
        {
            string casesRoot = Path.GetFullPath(DefaultDirectoriesAndPaths.CasesDir);
            Directory.CreateDirectory(casesRoot);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string caseNameBase = "Case_" + timestamp;
            string candidate = Path.Combine(casesRoot, caseNameBase);
            int suffix = 1;

            while (Directory.Exists(candidate))
            {
                candidate = Path.Combine(
                    casesRoot,
                    caseNameBase + "_" + suffix.ToString(CultureInfo.InvariantCulture));
                suffix++;
            }

            return candidate;
        }

        private OFRunSettings BuildOpenFoamRunSettings(
            GH_ObjectWrapper runSettingsWrapper,
            bool warnOnInvalid = false)
        {
            OFRunSettings runSettings = new OFRunSettings();
            if (runSettingsWrapper?.Value == null)
            {
                return runSettings;
            }

            if (runSettingsWrapper.Value is OFRunSettings provided)
            {
                return provided;
            }

            if (warnOnInvalid)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Run Settings input is invalid for OpenFOAM. Using defaults.");
            }

            return runSettings;
        }

        private static FluidX3DRunSettings BuildFluidX3DRunSettings(
            GH_ObjectWrapper runSettingsWrapper)
        {
            try
            {
                return BuildFluidX3DRunSettingsCore(runSettingsWrapper);
            }
            catch (TypeLoadException)
            {
                return null;
            }
        }

        private static FluidX3DRunSettings BuildFluidX3DRunSettingsCore(
            GH_ObjectWrapper runSettingsWrapper)
        {
            if (runSettingsWrapper?.Value is FluidX3DRunSettings fluidSettings)
            {
                return fluidSettings;
            }

            return new FluidX3DRunSettings();
        }

        private void RunFluidX3DSimulation(
            IGH_DataAccess DA,
            OFBaseDomain domain,
            GH_ObjectWrapper runSettingsWrapper,
            string baseWorkingDirectory,
            OFMeshSettings meshSettings,
            bool runMeshing,
            bool runSimulation)
        {
            try
            {
                RunFluidX3DSimulationCore(
                    DA, domain, runSettingsWrapper,
                    baseWorkingDirectory, meshSettings,
                    runMeshing, runSimulation);
            }
            catch (TypeLoadException ex)
            {
                string message =
                    "Could not load FluidX3D types. This usually means an outdated "
                    + "EddyLib.dll is installed (e.g. from a previous Eddy3D package). "
                    + "Please close Rhino, update or reinstall Eddy3D, then reopen Rhino.\n"
                    + "Details: " + ex.Message;
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
                OFRunSettings fallback = new OFRunSettings();
                fallback.simEngine = SimEngine.FluidX3D;
                SetSkippedOutputs(DA, domain, baseWorkingDirectory, meshSettings, fallback, message);
            }
        }

        private void RunFluidX3DSimulationCore(
            IGH_DataAccess DA,
            OFBaseDomain domain,
            GH_ObjectWrapper runSettingsWrapper,
            string baseWorkingDirectory,
            OFMeshSettings meshSettings,
            bool runMeshing,
            bool runSimulation)
        {
            FluidX3DRunSettings fluidRunSettings = BuildFluidX3DRunSettings(runSettingsWrapper);

            if (fluidRunSettings == null)
            {
                throw new TypeLoadException(
                    "EddyLib.FluidX3D.FluidX3DRunSettings could not be loaded.");
            }

            if (runSettingsWrapper?.Value is OFRunSettings)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "OpenFOAM Run Settings are not supported for FluidX3D. "
                    + "Connect 'FluidX3D Run Settings'. Using FluidX3D defaults.");
            }
            else if (runSettingsWrapper?.Value != null && !(runSettingsWrapper.Value is FluidX3DRunSettings))
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Run Settings input is not a FluidX3D Run Settings object. Using FluidX3D defaults.");
            }

            bool prepareCase = runMeshing || runSimulation;
            bool launchSolver = runSimulation;

            OFRunSettings resultRunSettings = new OFRunSettings();
            resultRunSettings.simEngine = SimEngine.FluidX3D;

            try
            {
                FluidX3DCaseRunResult result = FluidX3DCaseRunner.Execute(
                    domain,
                    baseWorkingDirectory,
                    fluidRunSettings,
                    prepareCase,
                    launchSolver);

                OFResult res = new OFResult(domain, resultRunSettings, meshSettings, baseWorkingDirectory)
                {
                    EngineCaseDirectory = result.CaseRoot ?? string.Empty
                };
                DA.SetData(GH_Strings.Common.Result, res);

                bool meshDone = result.Prepared || Directory.Exists(result.CaseRoot);
                bool simDoneSingle = Directory.Exists(result.ExportDirectory)
                    && Directory.GetFiles(result.ExportDirectory, "u-*.vtk", SearchOption.TopDirectoryOnly).Length > 0;

                DA.SetData(GH_Strings.SimpleFoam.MeshDone, meshDone);
                DA.SetDataList(GH_Strings.SimpleFoam.SimDone, new List<bool> { simDoneSingle });
                DA.SetData(
                    GH_Strings.SimpleFoam.MeshRemainingTime,
                    prepareCase ? "Prepared" : "Set Run Meshing=true");
                DA.SetDataList(
                    GH_Strings.SimpleFoam.SimRemainingTime,
                    new List<string>
                    {
                        runSimulation
                            ? (result.Launched ? "Launched (monitor terminal)." : "Run failed.")
                            : "Set Run Simulation=true"
                    });

                if (!string.IsNullOrWhiteSpace(result.Status))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, result.Status);
                }
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, ex.Message);
                SetSkippedOutputs(
                    DA,
                    domain,
                    baseWorkingDirectory,
                    meshSettings,
                    resultRunSettings,
                    ex.Message);
            }
        }

        private void SetSkippedOutputs(
            IGH_DataAccess DA,
            OFBaseDomain domain,
            string baseWorkingDirectory,
            OFMeshSettings meshSettings,
            OFRunSettings runSettings,
            string reason)
        {
            OFRunSettings safeRunSettings = runSettings ?? new OFRunSettings();
            safeRunSettings.simEngine = _selectedEngine;

            OFMeshSettings safeMeshSettings = meshSettings ?? new OFMeshSettings();
            if (string.IsNullOrWhiteSpace(safeMeshSettings.baseWorkingDir)
                && !string.IsNullOrWhiteSpace(baseWorkingDirectory))
            {
                safeMeshSettings.SetDirectories(baseWorkingDirectory);
            }

            DA.SetData(
                GH_Strings.Common.Result,
                new OFResult(domain, safeRunSettings, safeMeshSettings, baseWorkingDirectory));

            int statusCount = Math.Max(1, domain?.BCond?.WindDirections?.Count ?? 0);
            DA.SetData(GH_Strings.SimpleFoam.MeshDone, false);
            DA.SetDataList(GH_Strings.SimpleFoam.SimDone, Enumerable.Repeat(false, statusCount).ToList());
            DA.SetData(GH_Strings.SimpleFoam.MeshRemainingTime, "Skipped");
            DA.SetDataList(
                GH_Strings.SimpleFoam.SimRemainingTime,
                Enumerable.Repeat("Skipped: " + reason, statusCount).ToList());
        }

        private bool TryEnsureSelectedEngineAvailable(out string details)
        {
            details = string.Empty;
            string engineName = GetEngineDisplayName(_selectedEngine);
            string cachedDetails = string.Empty;
            bool hasCachedStatus = false;

            if (EngineInstallStatusCache.TryRead(EngineInstallStatusCache.EddyCachePath, out var snapshot, out _))
            {
                hasCachedStatus = EngineInstallStatusCache.TryGetEngineStatus(
                    snapshot,
                    engineName,
                    out _,
                    out cachedDetails);

                if (!hasCachedStatus)
                {
                    hasCachedStatus = EngineInstallStatusCache.TryGetEngineStatus(
                        snapshot,
                        GetEngineLegacyCacheName(_selectedEngine),
                        out _,
                        out cachedDetails);
                }
            }

            bool installed = CheckEngineLive(_selectedEngine, out string liveDetails);
            if (installed)
            {
                details = liveDetails;
                return true;
            }

            if (hasCachedStatus && !string.IsNullOrWhiteSpace(cachedDetails))
            {
                details = string.IsNullOrWhiteSpace(liveDetails)
                    ? cachedDetails
                    : liveDetails + " " + cachedDetails;
            }
            else
            {
                details = liveDetails;
            }

            return false;
        }

        private static bool IsEngineSupportedOnCurrentPlatform(SimEngine engine)
        {
            if (engine == SimEngine.BlueCFD)
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }

            return true;
        }

        private bool NormalizeSelectedEngineForCurrentPlatform()
        {
            if (_selectedEngine == SimEngine.BlueCFD
                && RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _selectedEngine = SimEngine.Docker;
                return true;
            }

            return false;
        }

        private static string GetEngineDisplayName(SimEngine engine)
        {
            switch (engine)
            {
                case SimEngine.Docker:
                    return EngineNameOpenFoamDocker;
                case SimEngine.FluidX3D:
                    return EngineNameFluidX3D;
                default:
                    return EngineNameOpenFoamBlueCfd;
            }
        }

        private static string GetEngineLegacyCacheName(SimEngine engine)
        {
            switch (engine)
            {
                case SimEngine.Docker:
                    return "Docker";
                case SimEngine.FluidX3D:
                    return "FluidX3D";
                default:
                    return "BlueCFD";
            }
        }

        private static bool CheckEngineLive(SimEngine engine, out string details)
        {
            details = string.Empty;

            try
            {
                if (engine == SimEngine.Docker)
                {
                    DefaultDirectoriesAndPaths.CheckDocker();
                    details = EngineNameOpenFoamDocker + " is installed and running.";
                    return true;
                }

                if (engine == SimEngine.BlueCFD)
                {
                    DefaultDirectoriesAndPaths.CheckBlueCfd();
                    details = EngineNameOpenFoamBlueCfd + " is installed.";
                    return true;
                }

                return TryResolveInstalledFluidX3D(out _, out details);
            }
            catch (Exception ex)
            {
                details = ex.Message;
                return false;
            }
        }

        private static bool TryResolveInstalledFluidX3D(out string sourceRoot, out string details)
        {
            sourceRoot = null;

            var candidates = new List<string>();
            string fromEnv = Environment.GetEnvironmentVariable("EDDY_FLUIDX3D_SOURCE");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                candidates.Add(Path.GetFullPath(fromEnv.Trim()));
            }

            candidates.Add(Path.GetFullPath(FluidX3DAblWorkflow.GetDefaultSourceDirectory()));

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate) || !seen.Add(candidate))
                {
                    continue;
                }

                if (Directory.Exists(candidate) && FluidX3DAblWorkflow.IsValidSourceDirectory(candidate))
                {
                    sourceRoot = candidate;
                    details = "FluidX3D source found at: " + candidate;
                    return true;
                }
            }

            details = "FluidX3D source not found. Install it via 'Install Engines' or set EDDY_FLUIDX3D_SOURCE.";
            return false;
        }

        private static void ApplyQueuedSimulationRemainingTimePredictions(
            IList<OpenFOAMLogStatus> statuses,
            IList<string> remainingTimeText)
        {
            if (statuses == null || remainingTimeText == null || statuses.Count == 0 || statuses.Count != remainingTimeText.Count)
                return;

            var observedDurations = new List<TimeSpan>();
            for (int i = 0; i < statuses.Count; i++)
            {
                var estimated = EstimateCaseDuration(statuses[i]);
                if (estimated.HasValue && estimated.Value.TotalSeconds > 0)
                    observedDurations.Add(estimated.Value);
            }

            if (observedDurations.Count == 0)
                return;

            var avgSeconds = observedDurations.Average(x => x.TotalSeconds);
            if (avgSeconds <= 0)
                return;

            var averageCaseDuration = TimeSpan.FromSeconds(avgSeconds);
            var cumulativeRemaining = TimeSpan.Zero;

            for (int i = 0; i < statuses.Count; i++)
            {
                var status = statuses[i];
                if (status == null || status.HasError || status.IsFinished)
                    continue;

                TimeSpan remainingForCase;
                if (status.EstimatedRemaining.HasValue)
                {
                    remainingForCase = status.EstimatedRemaining.Value;
                }
                else if (!status.HasLog)
                {
                    remainingForCase = averageCaseDuration;
                }
                else
                {
                    continue;
                }

                if (remainingForCase < TimeSpan.Zero)
                    remainingForCase = TimeSpan.Zero;

                cumulativeRemaining += remainingForCase;

                if (!status.HasLog)
                {
                    remainingTimeText[i] = "~" + OpenFOAMStatusFormatter.FormatTimeSpan(cumulativeRemaining);
                }
            }
        }

        private static TimeSpan? EstimateCaseDuration(OpenFOAMLogStatus status)
        {
            if (status == null || status.HasError || !status.HasLog)
                return null;

            if (status.ExecutionTimeSeconds.HasValue && status.ExecutionTimeSeconds.Value > 0)
            {
                var elapsed = TimeSpan.FromSeconds(status.ExecutionTimeSeconds.Value);
                if (status.IsFinished)
                    return elapsed;

                if (status.EstimatedRemaining.HasValue)
                    return elapsed + status.EstimatedRemaining.Value;

                return elapsed;
            }

            if (!status.IsFinished && status.EstimatedRemaining.HasValue)
                return status.EstimatedRemaining.Value;

            return null;
        }

        private static bool IsPathSafe(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            char[] metachars = new[] { '&', '|', ';', '$', '`', '\'', '"', '\n', '\r', '<', '>' };
            return path.IndexOfAny(metachars) == -1;
        }

        private static void OpenCommandFile(string path)
        {
            if (!System.IO.File.Exists(path)) return;
            if (!IsPathSafe(path)) return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OpenBatchFile(path);
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/usr/bin/open",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add(path);
                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    p?.WaitForExit();
                }
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "xdg-open",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                psi.ArgumentList.Add(path);
                System.Diagnostics.Process.Start(psi);
            }
            catch
            {
                // Ignore if xdg-open isn't available.
            }
        }

        private static void OpenBatchFile(string path)
        {
            var workingDir = Path.GetDirectoryName(path) ?? string.Empty;

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wt.exe",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };
                psi.ArgumentList.Add("cmd");
                psi.ArgumentList.Add("/k");
                psi.ArgumentList.Add(path);
                System.Diagnostics.Process.Start(psi);
            }
            catch
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };
                psi.ArgumentList.Add("/k");
                psi.ArgumentList.Add(path);
                System.Diagnostics.Process.Start(psi);
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.Eddy_simulation;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{7FF4A70C-DB4E-473C-BDC0-606CE58A979A}");
    }
}
