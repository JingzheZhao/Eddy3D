using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Cluster : GH_Component
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
        public Cluster()
          : base("Wind Rose Cluster", "Cluster", "Create a Wind Rose Cluster from the weather " + EddyVersion.toString(),
              EddyVersion.Name, "3 | PreProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Dir", "Dir", "Wind directions (deg)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Budget", "B", "Budget of wind directions", GH_ParamAccess.item, 8);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Centroids", "Ce", "Centroids", GH_ParamAccess.item);
            pManager.AddGenericParameter("Distinct Centroids", "DCe", "Distinct Centroids", GH_ParamAccess.item);

            pManager.AddGenericParameter("Clusters", "Cl", "Clusters", GH_ParamAccess.item);
            pManager.AddGenericParameter("Breaks", "B", "Natural Breaks", GH_ParamAccess.item);
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
            var dirsDeg = new List<double>();
            DA.GetDataList(0, dirsDeg);

            int bins = 8;
            DA.GetData(1, ref bins);

            var dirsRad = new List<double>();
            foreach (double d in dirsDeg)
            {
                dirsRad.Add(d * Math.PI / 180.0);
            }

            var dirsVec = new List<Vector3d>();

            foreach (double d in dirsRad)
            {
                var v = Vector3d.YAxis;
                v.Rotate(d, Vector3d.ZAxis);
                dirsVec.Add(v);
            }

            KMpt[] kmd = new KMpt[dirsVec.Count];
            for (int i = 0; i < dirsVec.Count; i++)
            {
                kmd[i] = new KMpt(i, dirsVec[i].X, dirsVec[i].Y, dirsVec[i].Z);
            }

            var results = KMeans.Cluster<KMpt>(kmd, bins, 5000, null, 1);
            var Centroids = new List<double>();
            var DistinctCentroids = new List<double>();

            foreach (int i in results.Centroids)
            {
                Centroids.Add(dirsDeg[i]);
            }

            foreach (int i in results.Centroids.Distinct())
            {
                DistinctCentroids.Add(dirsDeg[i]);
            }

            var Clusters = new DataTree<Point3d>();

            for (int i = 0; i < results.Clusters.Length; i++)
            {
                var c = results.Clusters[i];
                foreach (var pt in c)
                {
                    //Clusters.Add(dirsDeg[pt.Id], new Grasshopper.Kernel.Data.GH_Path(i));
                    Clusters.Add(new Point3d(pt.X, pt.Y, pt.Z), new Grasshopper.Kernel.Data.GH_Path(i));
                }
            }

            DistinctCentroids.Sort();
            var DistinctCentroidsClean = DistinctCentroids.Distinct();

            DA.SetDataList(0, Centroids);
            DA.SetDataList(1, DistinctCentroidsClean);
            DA.SetDataTree(2, Clusters);

            var breaks = JenksFisher.CreateJenksFisherBreaksArray(dirsDeg, bins);
            DA.SetDataList(3, breaks);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
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
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{1B77B6EE-4464-4FA0-BF96-C8E94AEDC273}"); }
        }
    }
}