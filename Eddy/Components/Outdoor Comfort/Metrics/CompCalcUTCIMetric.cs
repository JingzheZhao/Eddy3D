using System;
using System.Collections.Generic;
using System.IO;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompUTCI : GH_Component
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
        public CompUTCI()
          : base("UTCI", "UTCI", @"UTCI.

Calculate the UTCI

" + EddyVersion.toString(),
              EddyVersion.Name, "7 | Outdoor Comfort Metrics")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddGenericParameter("Simulation Result", "Res", "Simulation Result", GH_ParamAccess.item);

            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);
            pManager.AddNumberParameter("Ambient temperature", "AmbTemp", "Ambient temperature", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Mean Radiant Temperature", "MRT", "Mean Radiant Temperature", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Wind velocity", "Wind", "Wind velocity", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Relative humidity", "RH", "Relative Humidity", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddNumberParameter("UTCI [°C]", "UTCI", "UTCI [°C]", GH_ParamAccess.item);

            // pManager.AddGenericParameter("MRT_T", "MRT_T", "MRT_T", GH_ParamAccess.tree);
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
            int tamb = 0;
            DA.GetData("Ambient temperature", ref tamb);

            int wind = 0;
            DA.GetData("Wind velocity", ref wind);

            int mrt = 0;
            DA.GetData("Mean Radiant Temperature", ref mrt);

            int rh = 0;
            DA.GetData("Relative humidity", ref rh);

            var utci = EddyLib.UTCI.CalcUTCI(tamb, rh, wind, mrt);

            DA.SetData(0, utci);
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
        public override Guid ComponentGuid => new Guid("{7D6AE002-2B70-4A79-BD4F-4405A0BD66A4}");
    }
}