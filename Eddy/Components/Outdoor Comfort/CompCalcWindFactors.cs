using System;
using System.Collections.Generic;
using System.IO;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompCalcWindFactors : GH_Component
    {
        // exposure
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}

        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompCalcWindFactors()
          : base("Calculate WindFactors", "CalcWindFactors", "PostProcessing", "Eddy", "6 | Outdoor Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Res", "Res", "Res", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            pManager.AddNumberParameter("U", "U", "U", GH_ParamAccess.item);
            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);
            pManager.AddPointParameter("Probes", "Probes", "Probes", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("WF", "WF", "WF", GH_ParamAccess.item);
            // pManager.AddGenericParameter("MRT_T", "MRT_T", "MRT_T", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            OFResult RES = null;
            DA.GetData(0, ref RES);

            //// Hour of the year
            //List<int> hours = new List<int>() { 0 };
            //DA.GetDataList(1, hours);

            List<Point3d> probes = new List<Point3d>();
            DA.GetDataList("Probes", probes);
            var numberOfProbes = probes.Count;
            var probesArr = probes.ToArray();

            bool run = false;
            DA.GetData("Run", ref run);

            List<Vector3d> U = new List<Vector3d>();
            DA.GetDataList("U", U);

            #region Load weather

            Console.WriteLine("Load weather data...");

            //Weather data...

            if (RES.Domain.BCond.epwFilePath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Without a weather file (.epw) connected you will not be able to perform outdoor comfort calculations.");
                return;
            }

            Weather weather = new Weather();
            weather.LoadWeatherData(RES.Domain.BCond.epwFilePath);

            #endregion Load weather

            // this is all still MRT

            var Matrix = new double[8760, numberOfProbes];

            var csvWindFactors = RES.WorkingDirectory + @"WindFactors.csv";

            WindFactors wf = null;

            if (File.Exists(csvWindFactors) && !run)
            {
                Matrix = RadianceFiles.readCSVFile(csvWindFactors);

                var numberOfProbesCSV = Matrix.GetUpperBound(1) + 1;
                if (numberOfProbesCSV == numberOfProbes)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated MRT results have been loaded.");

                    wf = new WindFactors(RES.WorkingDirectory, RES.Domain.BCond, weather, U);
                    wf.windFactors = Matrix;

                    DA.SetData(0, wf);
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated MRT array has the wrong number of probing points. Please recalculate.");
                }
            }

            // Array casting
            //var MRT = Matrix.Cast<double[]>().ToArray();
            //double[][] MRT2 = ((object[][])Matrix).Select(x => x.Select(y => Convert.ToDouble(y)).ToArray()).ToArray();

            if (run)
            {
                //  [x][]  time
                //  [][x]  points

                wf = new WindFactors(RES.WorkingDirectory, RES.Domain.BCond, weather);

                if (GH_Document.IsEscapeKeyDown())
                {
                    GH_Document GHDocument = OnPingDocument();
                    GHDocument.RequestAbortSolution();
                }

                Utilities._2DArray2CSV(wf.windFactors, csvWindFactors, true, 1);

                DA.SetData(0, wf);
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_calMRT;

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{F165ADD0-A047-409E-A008-DD9CF82605F3}");
    }
}