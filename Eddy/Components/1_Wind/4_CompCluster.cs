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
    public class WindRoseCluster_Component : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.quarternary;

        /// <summary>
        /// Initializes a new instance of the WindRoseCluster_Component class.
        /// </summary>
        public WindRoseCluster_Component()
          : base(
              "Wind Rose Cluster", 
              "Cluster", 
              @"Group wind directions into clusters based on statistical occurrence.

Reduces a full wind rose (e.g. 36 directions) into a smaller budget (e.g. 8)
for faster simulation while maintaining representative wind patterns.

" + EddyVersion.toString(),
              EddyVersion.Name, 
              "1 | Wind")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter(
                "Directions", "Dir", 
                "Wind directions (0-360°) from weather data.", 
                GH_ParamAccess.list);

            pManager.AddIntegerParameter(
                "Budget", "N", 
                "Target number of wind directions to simulate. Default: 8", 
                GH_ParamAccess.item, 8);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Centroids", "Cent", "Cluster centroid directions.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Distinct Centroids", "Dcent", "Sorted list of unique cluster centroids.", GH_ParamAccess.list);
            pManager.AddPointParameter("Clusters", "Clus", "Data tree of points in each cluster.", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Breaks", "Brk", "Jenks-Fisher natural breaks.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Total Distance", "Dist", "Total clustering distance (error).", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var dirsDeg = new List<double>();
            if (!DA.GetDataList("Directions", dirsDeg)) return;

            int budget = 8;
            DA.GetData("Budget", ref budget);

            var kmd = new KMpt[dirsDeg.Count];
            for (int i = 0; i < dirsDeg.Count; i++)
            {
                double rad = dirsDeg[i] * Math.PI / 180.0;
                var v = Vector3d.YAxis;
                v.Rotate(rad, Vector3d.ZAxis);
                kmd[i] = new KMpt(i, v.X, v.Y, v.Z);
            }

            var results = KMeans.Cluster<KMpt>(kmd, budget, 5000, null, 1);
            
            var centroids = results.Centroids.Select(i => dirsDeg[i]).ToList();
            var distinctCentroids = results.Centroids.Distinct().Select(i => dirsDeg[i]).OrderBy(d => d).ToList();

            var clusters = new DataTree<Point3d>();
            for (int i = 0; i < results.Clusters.Length; i++)
            {
                var c = results.Clusters[i];
                var path = new Grasshopper.Kernel.Data.GH_Path(i);
                foreach (var pt in c)
                {
                    clusters.Add(new Point3d(pt.X, pt.Y, pt.Z), path);
                }
            }

            DA.SetDataList("Centroids", centroids);
            DA.SetDataList("Distinct Centroids", distinctCentroids);
            DA.SetDataTree(2, clusters);

            var breaks = JenksFisher.CreateJenksFisherBreaksArray(dirsDeg, budget);
            DA.SetDataList("Breaks", breaks);
            DA.SetData("Total Distance", results.TotalDistance);
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