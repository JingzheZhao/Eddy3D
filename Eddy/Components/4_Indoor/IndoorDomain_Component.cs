using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using Eddy.Analytics;
using EddyLib;
using EddyLib.BCs;
using EddyLib.Docker;
using EddyLib.Helpers;
using EddyLib.Indoor;
using EddyLib.Indoor.FunctionObjects;
using EddyLib.UI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Medallion.Shell;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Eddy.Components.Indoor
{
    public class IndoorDomain_Component : GH_Component
    {
        private const string EngineNameOpenFoamBlueCfd = "OpenFOAM (BlueCFD)";
        private const string EngineNameOpenFoamDocker = "OpenFOAM (Docker)";
        private int iterations = 1;
        private double numFuncObj = 1;
        private string BaseWorkingDir = "";
        private SimEngine _selectedEngine;

        /// <summary>
        /// Initializes a new instance of the IndoorDomain class.
        /// </summary>
        ///

        public IndoorDomain_Component()
          : base("Indoor Simulation", "IndoorSim",
@"Indoor Airflow Solver

Simulates buoyancy-driven airflow, temperature distribution, and contaminant transport within an indoor space.

Uses OpenFOAM 12's 'foamRun -solver fluid' solver.
Requires connected walls, inlets, outlets, and optional heat sources.

" + EddyVersion.toString(),
              EddyVersion.Name, "9 | Indoor")
        {
            _selectedEngine = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? SimEngine.BlueCFD
                : SimEngine.Docker;
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendSeparator(menu);
            Menu_AppendItem(menu, EngineNameOpenFoamBlueCfd, (s, e) => SetEngine(SimEngine.BlueCFD),
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows), _selectedEngine == SimEngine.BlueCFD);
            Menu_AppendItem(menu, EngineNameOpenFoamDocker, (s, e) => SetEngine(SimEngine.Docker),
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
            pManager.AddParameter(new Param_IndoorBC_Wall(),
                "Geo", "Geo",
                "Indoor CFD Walls",
                GH_ParamAccess.list);

            pManager.AddParameter(new Param_IndoorBC_Inlet(),
                "Inlet", "In",
                "Indoor CFD Inlets",
                GH_ParamAccess.list);

            pManager.AddParameter(new Param_IndoorBC_Outlet(),
                "Outlet", "Out",
                "Indoor CFD Outlets",
                GH_ParamAccess.list);

            pManager.AddParameter(new Param_FunctionObject(),
                "Function Objects", "FOs",
                "Indoor CFD Function Objects",
                GH_ParamAccess.list);
            pManager[3].Optional = true;

            pManager.AddTextParameter(
                "Directory", "Dir",
                "Working Directory",
                GH_ParamAccess.item, Path.Combine(DefaultDirectoriesAndPaths.CasesDir, "IndoorProject"));
            pManager[4].Optional = true;

            pManager.AddPointParameter(
                "Point Inside", "PInside",
                "Point inside domain.",
                GH_ParamAccess.item);

            pManager.AddNumberParameter(
                "CellSize", "Cs",
                "Cell Size",
                GH_ParamAccess.item, 1);
            pManager[6].Optional = true;

            pManager.AddIntegerParameter(
                "Iterations", "Iter",
                "Iterations for Simulation.",
                GH_ParamAccess.item, 1);
            pManager[7].Optional = true;

            pManager.AddIntegerParameter(
                "CPUs", "CPUs",
                "Number of CPUs to decompose the simulation with.",
                GH_ParamAccess.item, 2);
            pManager[8].Optional = true;

            pManager.AddBooleanParameter(
                GH_Strings.Common.RunMeshing, GH_Strings.Common.RunMeshingNick,
                GH_Strings.Common.RunMeshingDesc,
                GH_ParamAccess.item, false);
            pManager[9].Optional = true;

            pManager.AddBooleanParameter(
                GH_Strings.Common.RunSimulation, GH_Strings.Common.RunSimulationNick,
                GH_Strings.Common.RunSimulationDesc,
                GH_ParamAccess.item, false);
            pManager[10].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Indoor simulation result for post-processing", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var WallGoos = new List<IndoorWallGoo>();
            var InletGoos = new List<IndoorInletGoo>();
            var OutletGoos = new List<IndoorOutletGoo>();
            //  var VolumetricHeatSourceGoos = new List<VolumetricHeatSourceGoo>();
            var FunctionObjectGoos = new List<FunctionObjectGoo>();

            var Walls = new List<IndoorBC.Wall>();
            var Inlets = new List<IndoorBC.Inlet>();
            var Outlets = new List<IndoorBC.Outlet>();
            var FunctionObjects = new List<EddyLib.Indoor.FunctionObject>();

            DA.GetDataList(0, WallGoos);
            DA.GetDataList(1, InletGoos);
            DA.GetDataList(2, OutletGoos);

            foreach (var o in WallGoos)
            {
                if (o != null && o.Value != null && o.Value.Geometry != null) Walls.Add(o.Value);
            }
            foreach (var o in InletGoos)
            {
                if (o != null && o.Value != null && o.Value.Geometry != null) Inlets.Add(o.Value);
            }
            foreach (var o in OutletGoos)
            {
                if (o != null && o.Value != null && o.Value.Geometry != null) Outlets.Add(o.Value);
            }

            string dirInput = "";
            DA.GetData(4, ref dirInput);
            if (dirInput == null) dirInput = "";

            string resolvedWorkingDir;
            try
            {
                resolvedWorkingDir = Path.GetFullPath(DefaultDirectoriesAndPaths.ResolveWorkingDirectory(dirInput));
                Directory.CreateDirectory(resolvedWorkingDir);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid or inaccessible working directory: " + ex.Message);
                return;
            }

            BaseWorkingDir = DirectoryHelpers.EnsureTrailingBackslash(resolvedWorkingDir);

            Point3d pointInsideDomain = new Point3d();
            DA.GetData(5, ref pointInsideDomain);
            double cellSize = 1;
            DA.GetData(6, ref cellSize);

            int endTime = 2000;
            DA.GetData(7, ref endTime);



            // Function Objects

            var FOs = new List<FunctionObject>();
            var FO_GHWrappers = new List<FunctionObjectGoo>();

            DA.GetDataList(3, FO_GHWrappers);

            bool hasError = false;

            foreach (var gobj in FO_GHWrappers)
            {
                if (gobj == null || gobj.Value == null || gobj.Value.Geometry == null) continue;
                switch (gobj.Value)
                {
                    case VolumetricHeatSource vhs:
                        FOs.Add(vhs);
                        break;

                    case MomentumSinkIndoor msi:
                        FOs.Add(msi);
                        break;

                    case MomentumSource ms:
                        FOs.Add(ms);
                        break;

                    case ViralEmitter ve:
                        FOs.Add(ve);
                        break;

                    case CO2Emitter ce:
                        FOs.Add(ce);
                        break;

                    default:
                        hasError = true;
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "One or more function objects are invalid.");
                        break;
                }
            }

            if (hasError)
            {
                // Handle the error accordingly, e.g., return or throw an exception
                return;
            }

            int CPUs = 2;
            DA.GetData(8, ref CPUs);
            if (CPUs < 2) { CPUs = 2; }
            ; // Indor is not setup up for single CPU currently

            var dom = new IndoorDomain(endTime, BaseWorkingDir, cellSize, pointInsideDomain, Walls, Inlets, Outlets, FOs, CPUs);
            //var domGoo = new IndoorDomaingGoo(dom);



            var runSettings = new OFRunSettings(
                endTime: endTime,
                CPUs: CPUs,
                simEngine: _selectedEngine);
            var meshSettings = new OFMeshSettings();

            var RES = new OFResult(dom, runSettings, meshSettings, BaseWorkingDir);

            bool runMeshing = false;
            DA.GetData(9, ref runMeshing);
            bool runSimulation = false;
            DA.GetData(10, ref runSimulation);

            #region START PROCESSES

            Message = _selectedEngine.ToString();
            iterations = endTime;

            if ((runMeshing || runSimulation) && canRun)
            {
                Analytics.Analytics.TrackSimulationRun(
                    "indoor",
                    Analytics.Analytics.GetAnalyticsEngine(_selectedEngine));

                if (_selectedEngine == SimEngine.Docker)
                {
                    // Docker: run OpenFOAM commands interactively (mesh/sim/full)
                    RunDockerProcesses(CPUs, BaseWorkingDir, runMeshing, runSimulation);
                }
                else
                {
                    // BlueCFD: run via batch file (mesh/sim/full)
                    string batchToRun = null;
                    if (runMeshing && runSimulation)
                    {
                        batchToRun = Path.Combine(this.BaseWorkingDir, "run_all.bat");
                    }
                    else if (runMeshing)
                    {
                        batchToRun = Path.Combine(this.BaseWorkingDir, "run_mesh.bat");
                    }
                    else if (runSimulation)
                    {
                        batchToRun = Path.Combine(this.BaseWorkingDir, "run_sim.bat");
                    }

                    if (!string.IsNullOrWhiteSpace(batchToRun))
                    {
                        Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, batchToRun, taskComplete);
                    }
                }
            }

            #endregion START PROCESSES

            DA.SetData(0, RES);

            canRun = true;
        }

        public FunctionObject CastToFO(GH_ObjectWrapper gobj)
        {
            return (FunctionObject)gobj.Value;
        }

        /// <summary>
        /// Hack function to convert indoor domain to result for the probing component
        /// </summary>
        /// <param name="IndoorDOM"></param>
        /// <param name="Res"></param>

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_Indoor_Domain;

                // return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{E9C2B577-8E30-4945-8982-D99AB92A1759}"); }
        }

        private bool canRun = true;

        public void taskComplete(object sender, System.EventArgs e)
        {
            canRun = false;
            this.ExpireSolution(true);
        }

        private void DoWork(CancellationTokenSource cts)
        {
            var success = RunSlowSimulation(cts, 2);
        }

        private async Task DoWorkAsync(CancellationTokenSource cts)
        {
            await Task.Run(() =>
            {
                DoWork(cts);
            });
        }

        public bool RunSlowSimulation(CancellationTokenSource cts, int nthreads = 1)
        {
            // -----------------------------
            // run the simulation
            // -----------------------------

            int steps = (int)(1962 + 37 + (9 * numFuncObj) + 1731 + (26 * iterations));
            int stepCnt = 0;

            string allBat = Path.Combine(this.BaseWorkingDir, "run_all.bat");

            Console.WriteLine("Run Eddy Simulation...");

            var runIndoorEddy = Command.Run("cmd.exe", new[] { "" },
  options => options.WorkingDirectory(this.BaseWorkingDir).CancellationToken(cts.Token));
            runIndoorEddy.StandardInput.WriteLine("cd " + this.BaseWorkingDir);
            runIndoorEddy.StandardInput.WriteLine(allBat);
            runIndoorEddy.StandardInput.WriteLine("exit");

            int cnt = 0;

            string line;
            while ((line = runIndoorEddy.StandardOutput.ReadLine()) != null)
            {
                Console.WriteLine(line);
                Interlocked.Increment(ref stepCnt);
                Console.WriteLine(ProgressWriter.ProgressKey + (100 * stepCnt / steps).ToString(CultureInfo.InvariantCulture));
                cnt++;
            }

            runIndoorEddy.Wait();

            return true;
        }

        private void RunDockerProcesses(int cpus, string caseDir, bool runMeshing, bool runSimulation)
        {
            var runner = new DockerRunner();

            var cmds = new List<string>();

            if (runMeshing)
            {
                cmds.Add("blockMesh");
                cmds.Add("surfaceFeatures");
                cmds.Add("decomposePar -force");
                cmds.Add(string.Format("mpiexec -np {0} snappyHexMesh -overwrite -parallel", cpus));
                cmds.Add("reconstructParMesh -constant");
                cmds.Add("renumberMesh -overwrite");
            }

            if (runSimulation)
            {
                cmds.Add("topoSet");
                cmds.Add("renumberMesh -overwrite");
                cmds.Add("decomposePar -force");
                cmds.Add(string.Format("mpiexec -np {0} foamRun -solver fluid -parallel", cpus));
                cmds.Add("reconstructPar");
            }

            cmds.Add("echo 'INDOOR SIMULATION DONE'");
            cmds.Add("read -p 'Press Enter to close...'");

            var chain = DockerRunner.BuildCommandChain(cmds);
            runner.RunInteractive(chain, caseDir);
        }
    }
}
