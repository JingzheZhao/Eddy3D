using EddyLib;
using EddyLib.Radiation;
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
    public class ComfortSystem_Component : GH_Component
    {

        ComfortSystem System;

        /// <summary>
        /// Initializes a new instance of the ThermalSystem_Component class.
        /// </summary>
        public ComfortSystem_Component()
          : base("Comfort", "Comf", "Comfort System " + EddyVersion.toString(), EddyVersion.Name, "X | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        
          

            pManager.AddGenericParameter("MRTRes", "MRTRes", "MRT Simulation Result", GH_ParamAccess.item);
            pManager.AddGenericParameter("VRes", "VRes", "Wind Velocity Simulation Result", GH_ParamAccess.item);
            pManager[1].Optional = true;

            pManager.AddBooleanParameter("Run", "R", "Run simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ComfSys", "CS", "Comf System", GH_ParamAccess.item);
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
            if (system == null) return;
            if (!system.CastTo<MRTSimulationResultProto>(out RSystem)) return;




            bool RUN = false;
            bool HidePopUp = false;
            DA.GetData(2, ref RUN);





 


            System = new ComfortSystem(RSystem);



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


            DA.SetData(0, System);
            DA.SetData(1, System.BaseWorkingDir + @"\" + System.ProjectName + ".utci.eddy");

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
            get { return new Guid("{50868516-DF58-43FC-8A66-9F2B18C62864}"); }
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

            if (System == null) return false;

            

            if (cts.IsCancellationRequested) return false;
            System.ComputeUTCI(true, cts.Token);

            if (cts.IsCancellationRequested) return false;
            var proto  = System.SaveResults(true, cts.Token);


            return true;
        }
    }
}