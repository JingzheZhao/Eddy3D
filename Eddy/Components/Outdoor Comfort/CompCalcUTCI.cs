using System;
using System.Collections.Generic;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompCalcUTCI : GH_Component
    {
        // exposure
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompCalcUTCI()
          : base("UTCI", "UTCI", "PostProcessing", "Eddy", "6 | Outdoor Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Result", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            pManager.AddPointParameter("Probes", "Probes", "Probes", GH_ParamAccess.list);
            //pManager.AddIntegerParameter("Hour", "Hour", "Hour", GH_ParamAccess.item);
            pManager.AddGenericParameter("Wind Factors", "WF", "WF", GH_ParamAccess.item);
            pManager.AddGenericParameter("Mean Radiant Temperature", "MRT", "MRT", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.item);
            pManager.AddGenericParameter("Comfortable Hours", "CH", "Comfortable Hours", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            OFResult RES = null;
            DA.GetData("Result", ref RES);

            // Hour of the year
            //int hour = 0;
            //DA.GetData(1, ref hour);

            bool run = false;
            DA.GetData("Run", ref run);

            WindFactors windFactors = null;
            MRT mrt = null;

            Console.WriteLine("Load weather data...");

            //Weather data...

            Weather weather = new Weather(RES.Domain.BCond.epwFilePath);

            DA.GetData("Wind Factors", ref windFactors);
            if (windFactors == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid WindFactos object.");
                return;
            }

            Console.WriteLine("Loading: Wind data");

            var windDirList = RES.Domain.BCond.windDirs;

            var probes = new List<Point3d>();
            DA.GetDataList("Probes", probes);
            if (probes == null) return;

            //var windDirList = new List<double> { 0, 45, 90, 135, 180, 225, 270, 315 };
            //var windDirList = new List<double>();// { 0, 45, 90, 135, 180, 225, 270, 315 };
            //List<int> windDirList = options.windDirs;

            // Loading MRT data

            DA.GetData("Mean Radiant Temperature", ref mrt);
            if (mrt == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid MRT object.");
                return;
            }

            // load Reduction data -----------------

            Console.WriteLine("Starting UTCI calc...");

            // UTCI here

            var utci = new UTCI(probes.ToArray(), windFactors, weather, mrt, RES.Domain.BCond, RES.WorkingDirectory, run);

            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }

            if (utci == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Either precalculated results could not be loaded or the utci array has not been calculated yet.");
                return;
            }

            if (utci.resultPrecalculated)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The precalculated UTCI results have been loaded.");
            }
            if (utci.wrongNumberOfProbes)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The precalculated UTCI array has the wrong number of probing points. Please recalculate.");
                return;
            }
            DA.SetData(0, utci);
            //DA.SetData(1, utci.ValuesCondition);
            DA.SetDataList(1, utci.ValuesAnnualPercentage);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_calUTCI;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{266C9880-7107-4778-87FD-4F7DA64560F5}");
    }
}