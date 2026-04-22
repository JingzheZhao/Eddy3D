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
              GH_Strings.Cluster.Name,
              GH_Strings.Cluster.Nick,
              GH_Strings.Cluster.Desc + EddyVersion.toString(),
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
                GH_Strings.Cluster.Directions, GH_Strings.Cluster.DirectionsNick,
                GH_Strings.Cluster.DirectionsDesc,
                GH_ParamAccess.list);

            pManager.AddIntegerParameter(
                GH_Strings.Cluster.Budget, GH_Strings.Cluster.BudgetNick,
                GH_Strings.Cluster.BudgetDesc,
                GH_ParamAccess.item, 8);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter(GH_Strings.Cluster.Centroids, GH_Strings.Cluster.CentroidsNick, GH_Strings.Cluster.CentroidsDesc, GH_ParamAccess.list);
            pManager.AddNumberParameter(GH_Strings.Cluster.DistinctCentroids, GH_Strings.Cluster.DistinctCentroidsNick, GH_Strings.Cluster.DistinctCentroidsDesc, GH_ParamAccess.list);
            pManager.AddPointParameter(GH_Strings.Cluster.Clusters, GH_Strings.Cluster.ClustersNick, GH_Strings.Cluster.ClustersDesc, GH_ParamAccess.tree);
            pManager.AddNumberParameter(GH_Strings.Cluster.Breaks, GH_Strings.Cluster.BreaksNick, "Jenks-Fisher natural breaks.", GH_ParamAccess.list);
            pManager.AddNumberParameter(GH_Strings.Cluster.Distance, GH_Strings.Cluster.DistanceNick, "Total clustering distance (error).", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var dirsDeg = new List<double>();
            if (!DA.GetDataList(GH_Strings.Cluster.Directions, dirsDeg)) return;

            int budget = 8;
            DA.GetData(GH_Strings.Cluster.Budget, ref budget);

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

            DA.SetDataList(GH_Strings.Cluster.Centroids, centroids);
            DA.SetDataList(GH_Strings.Cluster.DistinctCentroids, distinctCentroids);
            DA.SetDataTree(Params.IndexOfOutputParam(GH_Strings.Cluster.Clusters), clusters);

            var breaks = JenksFisher.CreateJenksFisherBreaksArray(dirsDeg, budget);
            DA.SetDataList(GH_Strings.Cluster.Breaks, breaks);
            DA.SetData(GH_Strings.Cluster.Distance, results.TotalDistance);
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