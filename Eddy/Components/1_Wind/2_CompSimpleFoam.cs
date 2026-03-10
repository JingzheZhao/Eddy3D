using Eddy.Properties;
using EddyLib;
using EddyLib.OpenFOAM;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
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
        private SimEngine _selectedEngine;

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
            Menu_AppendItem(menu, "BlueCFD", (s, e) => SetEngine(SimEngine.BlueCFD),
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows), _selectedEngine == SimEngine.BlueCFD);
            Menu_AppendItem(menu, "Docker", (s, e) => SetEngine(SimEngine.Docker),
                true, _selectedEngine == SimEngine.Docker);
        }

        private void SetEngine(SimEngine engine)
        {
            _selectedEngine = engine;
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
                _selectedEngine = (SimEngine)reader.GetInt32("SelectedEngine");
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
            // mode to select simulation environment
            Message = _selectedEngine.ToString();

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
            if (!DA.GetData(GH_Strings.Common.Domain, ref gobj)) { }

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

            // run settings
            //-----------------

            OFRunSettings RunSettings = new OFRunSettings();
            GH_ObjectWrapper gobjRunSet = null;
            if (DA.GetData(3, ref gobjRunSet))
            {
                if (gobjRunSet?.Value is OFRunSettings)
                {
                    RunSettings = (OFRunSettings)gobjRunSet.Value;
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                        "Run Settings input is invalid. Using defaults.");
                }
            }

            // Apply selected engine
            RunSettings.simEngine = _selectedEngine;

            WarnIfSelectedEngineIsUnavailable();

            // Error Handling

            if (!RunSettings.IdenticalMPI && RunSettings.CPUs > 1 && RunSettings.BlueCFDIsInstalled)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "To use multiple CPUs, you need to ensure to use the same msmpi.dll for both Windows and BlueCFD. This is a BlueCFD issue and will hopefully be fixed in a future version."); return;
            }

            // working directory
            //------------------

            string baseWorkingDirectory = "";
            DA.GetData(GH_Strings.Common.WorkingDir, ref baseWorkingDirectory);

            // Resolve simple case names to full paths under the platform-specific Eddy3D cases folder
            baseWorkingDirectory = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(baseWorkingDirectory);

            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }

            var sep = Path.DirectorySeparatorChar.ToString();
            DirectoryInfo parentDir = Directory.GetParent(baseWorkingDirectory.EndsWith(sep) ? baseWorkingDirectory : string.Concat(baseWorkingDirectory, sep));
            var myParentDir = parentDir.Parent.FullName;

            if (myParentDir == @"C:\" || myParentDir == "/")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please use an additional subfolder for Eddy3D simulations."); return;
            }

            baseWorkingDirectory = Utilities.Directories.FixDirectories(baseWorkingDirectory);

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

            bool makeTrees = false;
            bool runSimulation = false;
            bool runMeshing = false;

            DA.GetData(GH_Strings.Common.MakeTrees, ref makeTrees);
            DA.GetData(GH_Strings.Common.RunSimulation, ref runSimulation);
            DA.GetData(GH_Strings.Common.RunMeshing, ref runMeshing);

            if ((runMeshing || runSimulation) && canRun)
            {
                Analytics.Analytics.TrackSimulationRun(
                    "outdoor",
                    Analytics.Analytics.GetAnalyticsEngine(_selectedEngine));
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
                    OpenCommandFile(Path.Combine(scriptsDir, "run_sim_all" + dockerScriptExt));
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
                    Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, Path.Combine(baseWorkingDirectory, "Scripts", "run_sim_all.bat"), taskComplete);
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

        private void WarnIfSelectedEngineIsUnavailable()
        {
            string engineName = _selectedEngine == SimEngine.Docker ? "Docker" : "BlueCFD";
            bool installed = true;
            string details = string.Empty;
            bool foundStatus = false;

            if (EngineInstallStatusCache.TryRead(EngineInstallStatusCache.EddyCachePath, out var snapshot, out _))
            {
                foundStatus = EngineInstallStatusCache.TryGetEngineStatus(snapshot, engineName, out installed, out details);
            }

            if (!foundStatus)
            {
                installed = CheckEngineLive(engineName, out details);
            }

            if (!installed)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    string.Format("{0} is selected but not installed. {1}", engineName, details));
            }
        }

        private static bool CheckEngineLive(string engineName, out string details)
        {
            details = string.Empty;

            try
            {
                if (engineName == "Docker")
                {
                    DefaultDirectoriesAndPaths.CheckDocker();
                    details = "Docker is installed and running.";
                    return true;
                }

                DefaultDirectoriesAndPaths.CheckBlueCfd();
                details = "blueCFD is installed.";
                return true;
            }
            catch (Exception ex)
            {
                details = ex.Message;
                return false;
            }
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

        private static void OpenCommandFile(string path)
        {
            if (!System.IO.File.Exists(path)) return;

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
                    Arguments = string.Format("\"{0}\"", path),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
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
                    Arguments = string.Format("\"{0}\"", path),
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
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
            var quotedPath = string.Format("\"{0}\"", path);

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = "cmd /k " + quotedPath,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = false
                });
            }
            catch
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/k " + quotedPath,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    CreateNoWindow = false
                });
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
