using EddyLib;
using EddyLib.Radiation;
using EddyLib.Thermal;
using EddyLib.UI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy.Components.Radiation
{
    public class ThermalSystem_Component : GH_Component
    {

        ThermalSystem ThermalSimulation;

        /// <summary>
        /// Initializes a new instance of the ThermalSystem_Component class.
        /// </summary>
        public ThermalSystem_Component()
          : base("Thermal", "Therm", "Thermal System to simulate surface temperatures using EnergyPlus " + EddyVersion.toString(), EddyVersion.Name, "X | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "N", "Project name", GH_ParamAccess.item, "MyStudy");
            pManager.AddTextParameter("Dir", "D", "Working directory name", GH_ParamAccess.item, @"C:\Temp\Eddy3d");
            pManager.AddTextParameter("Weather", "W", "Weather filepath", GH_ParamAccess.item, DefaultDirectoriesAndPaths.DefaultWeather);


            pManager.AddGenericParameter("System", "RSS", "RadiationSimulationSystem", GH_ParamAccess.item);


            pManager.AddBooleanParameter("Run", "R", "Run simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string name = "";
            string workDir = "";
            string weatherPath = "";


            DA.GetData(0, ref name);
            DA.GetData(1, ref workDir);
            DA.GetData(2, ref weatherPath);


            // ---------------------
            // Get the RSurf objects
            // ---------------------

            IGH_Goo system = null;
             if (!DA.GetData(3, ref system)) { }

            RadiationSimulationSystem RSystem;

            if (!system.CastTo<RadiationSimulationSystem>(out RSystem)) return;




            bool RUN = false;
            bool HidePopUp = false;
            DA.GetData(4, ref RUN);






            if (!File.Exists(weatherPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Weather file could not be found");
                return;
            }
            Weather weather = new Weather(weatherPath);


            ThermalSimulation = new ThermalSystem(RSystem);



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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("04b1c5e2-c626-42e0-87f3-1d87f0b9c5a0"); }
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

            if (ThermalSimulation == null) return false;

       

            if (cts.IsCancellationRequested) return false;


            var data = ThermalSimulation.RunEP(true, cts.Token);

 

            return true;
        }
    }
}