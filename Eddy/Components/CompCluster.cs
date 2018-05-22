using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;
using EddyLib;
using Eddy.Properties;
using System.Linq;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Cluster : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Cluster()
          : base("Cluster", "Cluster",
              "Cluster",
              "Eddy", "PostProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Data", "Data", "Data", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Bins", "Bins", "Number of bins", GH_ParamAccess.item, 8);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Centroids", "C", "Centroids", GH_ParamAccess.item);
            pManager.AddGenericParameter("Breaks", "B", "Natural Breaks", GH_ParamAccess.item);

            //pManager.AddGenericParameter("Means", "M", "Means", GH_ParamAccess.item);

        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var data = new List<double>();
            DA.GetDataList(0, data);

            int bins = 8;
            DA.GetData(1, ref bins);

            KMpt1D[] kmd = new KMpt1D[data.Count];
            for (int i = 0; i < data.Count; i++) {
                kmd[i] = new KMpt1D(i, data[i]);
            }

            var results = KMeans.Cluster<KMpt1D>(kmd, bins, 5000);
            var Centroids = new List<double>();
            foreach (int i in results.Centroids) {
                Centroids.Add(data[i]);
            }


            var breaks = JenksFisher.CreateJenksFisherBreaksArray(data, bins);



            DA.SetDataList(0, Centroids);
            DA.SetDataList(1, breaks);


        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resources.Eddy_cluster;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{1B77B6EE-4464-4FA0-BF96-C8E94AEDC273}"); }
        }
    }
}
