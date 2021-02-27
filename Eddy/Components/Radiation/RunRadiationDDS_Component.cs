using EddyLib;
using EddyLib.Radiance;
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

        RadiationSimulationDDS RadiationSimulation;

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
            pManager.AddTextParameter("Name", "N", "Project name", GH_ParamAccess.item, "MyStudy");
            pManager.AddTextParameter("Dir", "D", "Working directory name", GH_ParamAccess.item, @"C:\Temp\Eddy3d");
            pManager.AddTextParameter("Weather", "W", "Weather filepath", GH_ParamAccess.item, DefaultDirectoriesAndPaths.DefaultWeather);

            pManager.AddMeshParameter("Model", "M", "Model", GH_ParamAccess.list);
            pManager.AddMeshParameter("Probes", "P", "Probes, analysis surface", GH_ParamAccess.list);

            pManager.AddBooleanParameter("Run", "R", "Run simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Result", "R", "Result file path", GH_ParamAccess.item);
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


            List<Mesh> modelMeshes = new List<Mesh>();
            List<Mesh> surfMeshes = new List<Mesh>();

            DA.GetDataList(3, modelMeshes);
            DA.GetDataList(4, surfMeshes);



            bool RUN = false;
            bool HidePopUp = false;
            DA.GetData(5, ref RUN);


            Mesh buildingGeometry = new Mesh();
            foreach (var m in modelMeshes)
            {
                m.Vertices.CullUnused();
                buildingGeometry.Append(m);
            }



            if (!File.Exists(weatherPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Weather file could not be found");
                return;
            }
            Weather weather = new Weather(weatherPath);

            RadiationSimulation = new RadiationSimulationDDS(name, workDir, buildingGeometry, surfMeshes, weather);

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

            if(RadiationSimulation != null){
                string resultFilePath = RadiationSimulation.BaseWorkingDir + "/" + RadiationSimulation.ProjectName + ".Radiation.bin";
                DA.SetData(0, resultFilePath);
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

            var success = RunSlowSimulation(cts, 2);

        }
        private async Task DoWorkAsync(CancellationTokenSource cts)
        {
            await Task.Run(() =>
            {
                DoWork(cts);
            });
        }

        public bool RunSlowSimulation(CancellationTokenSource cts,  int nthreads = 1)
        {

            if (RadiationSimulation == null) return false;

            // write scene rad file
            Console.WriteLine("Starting DDS Simulation");


                if (!cts.IsCancellationRequested)
                {
                    RadiationSimulation.RunDDS(true);
                    //Console.WriteLine("Simulation: " + i);
                    //double pct = 100 * i / iter;
                    //Console.WriteLine(ProgressWriter.ProgressKey + pct);
                }
         
            return true;
        }

    }




}