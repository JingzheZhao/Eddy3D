using EddyLib;
using EddyLib.OutdoorComfort;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy
{
    public class WorkerWithProgBarComponent : GH_Component
    {
        // exposure
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }

        /// <summary>
        /// Initializes a new instance of the WorkerWithProgBarComponent class.
        /// </summary>
        public WorkerWithProgBarComponent()
          : base("Wind Factors2", "Wind Factors2", @"Wind Factors2

Based on the probed simulation and the weather data, this component calculates wind velocities, wind factors for each probing point [8760 hourly branches x number of probing points].
The wind factors are calculated based on the wind velocity and direction for each hour which is scaled up/down accordingly given probing height from ground.
For this, we support either a look-up for the closest simulated wind direction or an interpolation between the closest two wind directions.
" + EddyVersion.toString(),
              EddyVersion.Name, "6 | Outdoor Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Eddy Result", GH_ParamAccess.item);
            pManager.AddPointParameter("Probing points", "Points", "List of probing points (caution: might have been culled)", GH_ParamAccess.list);
            pManager.AddVectorParameter("Wind Velocity", "U", @"Wind Velocity [DataTree] where the [branches] are the wind directions and the [items] are the values for each probing point.", GH_ParamAccess.tree);

            pManager.AddBooleanParameter("R", "R", "R", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Wind Factors Spatial", "WFS", @"Wind Amplification Factors Spatial

Wind Amplification Factors (dimensionless wind velocity) for each simulated wind direction.
This yields a datatree of the size [Number of simulated wind directions x number of sensor points].", GH_ParamAccess.item);

            pManager.AddGenericParameter("Wind Factors Annual", "WFA", @"Wind Factors Annual

Wind Factors multiplied with the corresponding EPW wind velocity from the nearest simulated wind direction for every hour of the year.
This yields a datatree of the size [8760 h x number of sensor points].", GH_ParamAccess.item);
            pManager.AddTextParameter("T", "T", "T", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool RUN = false;
            bool HidePopUp = false;
            DA.GetData(3, ref RUN);
            // redirect stderr
            var errors = new StringWriter();
            Console.SetError(errors);

            bool interpolate = false;

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
            get { return new Guid("{866C6A80-DBD2-4DBD-B959-890F9E4E84CC}"); }
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
            Console.WriteLine("Starting Simulation");

            for (int i = 0; i < iter; i++)
            {
                if (!cts.IsCancellationRequested)
                {
                    System.Threading.Thread.Sleep(100);

                    Console.WriteLine("Simulation: " + i);
                    double pct = 100 * i / iter;
                    Console.WriteLine(ProgressWriter.ProgressKey + pct);
                }
            }
            return true;
        }
    }
}