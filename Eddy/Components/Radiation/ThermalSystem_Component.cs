using EddyLib;
using EddyLib.Radiation;
using EddyLib.UI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
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
        
            pManager.AddGenericParameter("Res", "Res", "Radiation Simulation Result", GH_ParamAccess.item);


            pManager.AddBooleanParameter("Run", "R", "Run simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ThermSys", "TS", "Thermal System", GH_ParamAccess.item);
            pManager.AddTextParameter("Result", "R", "Result file path", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

           
            // ---------------------
            // Get the RSurf objects
            // ---------------------

            IGH_Goo system = null;
             if (!DA.GetData(0, ref system)) { }

            MRTSimulationResultProto RSystem;

            if (!system.CastTo<MRTSimulationResultProto>(out RSystem)) return;




            bool RUN = false;
            bool HidePopUp = false;
            DA.GetData(1, ref RUN);





 


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


            DA.SetData(0, ThermalSimulation);
            DA.SetData(1, ThermalSimulation.BaseWorkingDir + @"\" + ThermalSimulation.ProjectName + ".mrt.eddy");

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

            if (cts.IsCancellationRequested) return false;
            ThermalSimulation.ComputeMRT(true, cts.Token);

            if (cts.IsCancellationRequested) return false;
            var proto  = ThermalSimulation.SaveResults(true, cts.Token);


            return true;
        }
    }
}