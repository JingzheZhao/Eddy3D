using EddyLib;
using EddyLib.UI;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy
{
    public class RunRadiationDDS_Component : GH_Component
    {
        
        // exposure
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}

        /// <summary>
        /// Initializes a new instance of the WorkerWithProgBarComponent class.
        /// </summary>
        public RunRadiationDDS_Component()
          : base("Radiation", "Rad", "Radiation exposure simulated with Radiance DDS method " + EddyVersion.toString(), EddyVersion.Name, "X | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Model", "M", "Model", GH_ParamAccess.list);
            pManager.AddMeshParameter("Probes", "P", "Probes, analysis surface", GH_ParamAccess.list);


            pManager.AddBooleanParameter("R", "R", "R", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("T", "T", "T", GH_ParamAccess.item );
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<Mesh> modelMeshes = new List<Mesh>();
            List<Mesh> surfMeshes = new List<Mesh>();

            DA.GetDataList(0, modelMeshes);
            DA.GetDataList(1, surfMeshes);



            bool RUN = false;
            bool HidePopUp = false;
            DA.GetData(2, ref RUN);






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
            get { return new Guid("{E30D4DA7-D2CE-4DA2-9AA1-7322DC44F310}"); }
        }


        private void DoWork(CancellationTokenSource cts)
        {
           
            var success = RunSlowSimulation(cts, 100, 2);
                   
        }
        private async Task DoWorkAsync(CancellationTokenSource cts)
        {
            await Task.Run(() =>
            {
                DoWork(cts);
            });
        }

        public bool RunSlowSimulation(CancellationTokenSource cts, int iter = 100, int nthreads = 1)
        {
             

            // write scene rad file
            Console.WriteLine("Starting DDS Simulation");


            for (int i = 0; i < iter; i++)
            {

                if (!cts.IsCancellationRequested)
                {
                    System.Threading.Thread.Sleep(100);

                     Console.WriteLine("Simulation: " + i );
                    double pct = 100 * i / iter;
                    Console.WriteLine(ProgressWriter.ProgressKey + pct);
                }
            }
            return true;
        }

    }




}