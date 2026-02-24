using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompInterpolateUMag : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.senary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompInterpolateUMag()
          : base("InterpolateUMag", "InterpolateUMag", @"Interpolate UMag for GAN applications.
" + EddyVersion.toString(),
              EddyVersion.Name, "5 | Post-Processing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Current Points", "CP", "Current Points", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Current UMag", "UMag", "Current UMag", GH_ParamAccess.tree);
            pManager.AddPointParameter("New Points", "NP", "New Points", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Average By Number of Points", "AB", "Average By", GH_ParamAccess.item);
            pManager.AddPointParameter("Center Point", "CP", "Center Point", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Wind Directions", "WDir", "Wind Directions", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Points", "P", "Points", GH_ParamAccess.tree);
            pManager.AddNumberParameter("U Mag", "UMag", "U Mag", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        ///

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetDataTree("Current Points", out GH_Structure<GH_Point> OldPoints)) { return; }
            if (!DA.GetDataTree("Current UMag", out GH_Structure<GH_Number> OldUMag)) { return; }

            List<Point3d> NewPointsL = new List<Point3d>();
            if (!DA.GetDataList("New Points", NewPointsL)) return;

            int AverageBy = 0;
            if (!DA.GetData("Average By Number of Points", ref AverageBy)) return;

            Point3d CenterPoint = new Point3d();
            if (!DA.GetData("Center Point", ref CenterPoint)) return;

            List<int> WindDirs = new List<int>();
            if (!DA.GetDataList("Wind Directions", WindDirs)) return;

            if (OldPoints.DataCount == 0 || OldUMag.DataCount == 0) return;

            DataTree<double> newUMag = new DataTree<double>();
            var newPoints = NewPointsL.ToArray();

            for (int b = 0; b < OldUMag.Branches.Count; b++)
            {
                var tf = Transform.Rotation(Utilities.Deg2Rad(WindDirs[b]), CenterPoint);

                GH_Point[] pArr = OldPoints.Branches[b].ToArray();

                // Rotate into position
                foreach (GH_Point p in pArr)
                {
                    p.Transform(tf);
                }

                for (int p = 0; p < newPoints.Length; p++)
                {
                    double avgU = 0.0;
                    int[] cIdx = GetClosestIndices(pArr, newPoints[p], AverageBy);

                    for (int cp = 0; cp < cIdx.Length; cp++)
                    {
                        var add = OldUMag.Branches[b][cIdx[cp]];
                        avgU += add.Value / cIdx.Length;
                    }
                    newUMag.Add(avgU, new GH_Path(b));
                }
            }

            DA.SetDataList(0, newPoints);
            DA.SetDataTree(1, newUMag);
        }

        public int[] GetClosestIndices(GH_Point[] PC, Point3d P, int N)
        {
            var PtsOrderedByDistance = PC.OrderBy(point => Math.Pow(point.Value.X - P.X, 2) + Math.Pow(point.Value.Y - P.Y, 2));
            var AverageByList = PtsOrderedByDistance.Take(N).ToArray();

            var ClosestIndices = new int[N];

            for (int cp = 0; cp < N; cp++)
            {
                ClosestIndices[cp] = Array.IndexOf(PC, AverageByList[cp]);
            }

            return ClosestIndices;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

        // You can add image files to your project resources and access them like this:
         Resources.Eddy_misc;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{81865177-073F-4251-8ED7-EFF11F6886CA}");
    }
}