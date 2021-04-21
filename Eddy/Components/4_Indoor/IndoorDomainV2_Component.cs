using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using EddyLib.Indoor.FunctionObjects;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Medallion.Shell;
using Rhino.Geometry;
using System.Threading;
using System.Threading.Tasks;
using EddyLib.UI;
using System.Globalization;
using System.IO;

namespace Eddy.Components.Indoor
{
    public class IndoorDomainV2_Component : GH_Component
    {
        int iterations = 1;
        double numFuncObj = 1;
        string BaseWorkingDir = "";


        /// <summary>
        /// Initializes a new instance of the IndoorDomain class.
        /// </summary>
        /// 
        
        public IndoorDomainV2_Component() : base("IndoorDomain", "IDom", "IndoorDomain" + EddyVersion.toString(), EddyVersion.Name, "9 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //0
            pManager.AddParameter(new Param_IndoorBC_Wall(), "Geo", "Geo", "Indoor CFD Walls", GH_ParamAccess.list);
            //1
            pManager.AddParameter(new Param_IndoorBC_Inlet(), "Inlet", "In", "Indoor CFD Inlets", GH_ParamAccess.list);
            //2
            pManager.AddParameter(new Param_IndoorBC_Outlet(), "Outlet", "Out", "Indoor CFD Outlets", GH_ParamAccess.list);
            //3
            //pManager.AddParameter(new Param_VolumetricHeatSource(), "Volumetric Heat Source", "VHS", "Indoor CFD Objects", GH_ParamAccess.list);
            pManager.AddParameter(new Param_FunctionObject(), "Function Objects", "FOs", "Indoor CFD Function Objects", GH_ParamAccess.list);
            pManager[3].Optional = true;
            //4
            pManager.AddTextParameter("Directory", "Dir", "Working Directory", GH_ParamAccess.item, @"C:\Temp\EddyProject");
            pManager[4].Optional = true;
            //5
            pManager.AddPointParameter("Point Inside", "PInside", "Point inside domain.", GH_ParamAccess.item);
            //6
            pManager.AddNumberParameter("CellSize", "Cs", "Cell Size", GH_ParamAccess.item, 1);
            pManager[6].Optional = true;
            //7
            pManager.AddIntegerParameter("Iterations", "Iter", "Iterations for Simulation.", GH_ParamAccess.item, 1);
            pManager[7].Optional = true;
            //8
            pManager.AddBooleanParameter("Run", "Run", "Run case setup routines and simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_IndoorDomain(), "Domain", "Dom", "Indoor CFD Domain", GH_ParamAccess.list);
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
                Walls.Add(o.Value);
            }
            foreach (var o in InletGoos)
            {
                Inlets.Add(o.Value);
            }
            foreach (var o in OutletGoos)
            {
                Outlets.Add(o.Value);
            }
            foreach (var o in FunctionObjectGoos)
            {
                FunctionObjects.Add(o.Value);
            }

            string dir = "";
            DA.GetData(4, ref dir);
            BaseWorkingDir = dir;


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

            for (int i = 0; i < FO_GHWrappers.Count; i++)
            {
                FunctionObjectGoo gobj = null;
                gobj = FO_GHWrappers[i];

                if ((gobj.Value is VolumetricHeatSource))
                {
                    FOs.Add((VolumetricHeatSource)gobj.Value);
                }
                else if ((gobj.Value is MomentumSinkIndoor))
                {
                    FOs.Add((MomentumSinkIndoor)gobj.Value);
                }
                else if ((gobj.Value is MomentumSource))
                {
                    FOs.Add((MomentumSource)gobj.Value);
                }
                else if ((gobj.Value is ViralEmitter))
                {
                    FOs.Add((ViralEmitter)gobj.Value);
                }
                else if ((gobj.Value is CO2Emitter))
                {
                    FOs.Add((CO2Emitter)gobj.Value);
                }

                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid function object"); return;
                }
            }

            var dom = new IndoorDomain(endTime, dir, cellSize, pointInsideDomain, Walls, Inlets, Outlets, FOs);
            var domGoo = new IndoorDomaingGoo(dom);





             #region START PROCESSES
          
            bool RUN = false;
            bool HidePopUp = true;
            iterations = endTime;
            DA.GetData(8, ref RUN);


            bool runWithConsoleWindow = true;

            if (runWithConsoleWindow)
            {
                if (canRun)
                {
                    string runall = this.BaseWorkingDir + @"\run_all.bat";
                    Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, runall, taskComplete);
                }
            }
            else
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


            DA.SetData(0, domGoo);
            canRun = true;

        }


        public FunctionObject CastToFO(GH_ObjectWrapper gobj)
        {
            return (FunctionObject)gobj.Value;
        }

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

            int steps = (int) (1962 + 37 + (9 * numFuncObj) + 1731 + (26 * iterations));
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