using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.BCs;
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
using System.Threading;
using System.Threading.Tasks;

namespace Eddy.Components.Indoor
{
    public class IndoorDomain_Component : GH_Component
    {
        private int iterations = 1;
        private double numFuncObj = 1;
        private string BaseWorkingDir = "";

        /// <summary>
        /// Initializes a new instance of the IndoorDomain class.
        /// </summary>
        ///

        public IndoorDomain_Component() 
          : base("Indoor Simulation", "IndoorSim", 
@"Indoor Airflow Solver

Simulates buoyancy-driven airflow, temperature distribution, and contaminant transport within an indoor space.

Uses OpenFOAM's 'buoyantSimpleFoam' solver.
Requires connected walls, inlets, outlets, and optional heat sources.

" + EddyVersion.toString(), 
              EddyVersion.Name, "9 | Indoor")
        {
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
                GH_ParamAccess.item, @"C:\Eddy3D-Cases\IndoorProject\");
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
                "Run", "Run", 
                "Run case setup routines and simulation", 
                GH_ParamAccess.item, false);
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

            string dir = "";
            DA.GetData(4, ref dir);
            if (dir == null) dir = "";
            BaseWorkingDir = Utilities.EnsureTrailingBackslash(dir);

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
            if (CPUs < 2) { CPUs = 2; }; // Indor is not setup up for single CPU currently

            var dom = new IndoorDomain(endTime, dir, cellSize, pointInsideDomain, Walls, Inlets, Outlets, FOs, CPUs  );
            //var domGoo = new IndoorDomaingGoo(dom);

         

            var runSettings = new OFRunSettings(iter: endTime );
            var meshSettings = new OFMeshSettings();

            var RES = new OFResult(dom, runSettings, meshSettings, dir);

            bool RUN = false;
            DA.GetData(9, ref RUN);

            #region START PROCESSES

            iterations = endTime;

            bool HidePopUp = true;

            bool runWithConsoleWindow = true;

            if (runWithConsoleWindow)
            {
                if (RUN)
                {
                    if (canRun)
                    {
                        string runall = this.BaseWorkingDir + @"\run_all.bat";
                        Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, runall, taskComplete);
                    }
                }
            }
            else   // @Zoe and @Patrick --> this seems to be a dead code section since runWithConsoleWindow is always true. Did the ProgressDialog version not work for you?
            {
                try
                {
                    // redirect stderr
                    var errors = new StringWriter();
                    Console.SetError(errors);
                    if (RUN)
                    {
                        if (HidePopUp)
                        {
                            DoWork(new CancellationTokenSource());
                        }
                        else
                        {
                            // show progress form
                            var progress = new ProgressDialog(DoWorkAsync);
                            progress.ShowModal();
                            // if user cancellation, abort solution
                            if (progress.Canceled)
                            {
                                OnPingDocument().RequestAbortSolution();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); return;
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

            string allBat = this.BaseWorkingDir + @"\run_all.bat";

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
    }
}