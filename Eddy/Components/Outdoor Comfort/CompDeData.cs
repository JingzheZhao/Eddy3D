using Eddy.Properties;
using EddyLib;
using EddyLib.OutdoorComfort;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompDeconstructOCData : GH_Component
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
        public CompDeconstructOCData()
          : base("Deconstruct Data Objects", "DeData", @"Deconstruct data objects from pedestrian and outdoor thermal comfort post-processings into data trees. This component is slow if a large number of probes are requested.
" + EddyVersion.toString(), EddyVersion.Name, "6 | Outdoor Comfort")
        { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Data", "D", "Data", GH_ParamAccess.item);

            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Select", "S", @"Select the hours you would like to process. Select ""-1"" to process the entire year (8760 h) if you are working with a temporal object, or all existing wind directions if you are working with a spatial object.", GH_ParamAccess.list);

            // pManager.AddPointParameter("Probes", "Probes", "Probes", GH_ParamAccess.list);
            // pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);

            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("Data", "D", "Data", GH_ParamAccess.tree);

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
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            var selection = new List<int>();
            if (!DA.GetDataList(1, selection)) { }

            if (selection.Count == -1)
            {
                if (!(gobj.Value is WindFactorsSpatial))
                {
                    selection = (new int[8760]).Select((o, i) => i).ToList();
                }
            }

            if ((gobj.Value is MRT))
            {
                MRT mrt = null;

                DA.GetData(0, ref mrt);

                DataTree<double> tree = new DataTree<double>();

                foreach (int h in selection)
                {
                    var tempRow = ArrayHelper.CustomArray<double>.GetRow(mrt.Values, h);
                    tree.AddRange(tempRow, new Grasshopper.Kernel.Data.GH_Path(h));
                }

                double threshold = 1e6;
                if (tree.DataCount > threshold)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, EddyLib.Strings.ReturnMsg.LargeDataTree(threshold));
                }

                DA.SetDataTree(0, tree);
            }
            else if ((gobj.Value is WindFactorsAnnual))
            {
                WindFactorsAnnual wf = null;

                DA.GetData(0, ref wf);

                DataTree<double> tree = new DataTree<double>();

                foreach (int h in selection)
                {
                    var tempRow = ArrayHelper.CustomArray<double>.GetRow(wf.ValuesTemporal, h);
                    tree.AddRange(tempRow, new Grasshopper.Kernel.Data.GH_Path(h));
                }

                double threshold = 1e6;
                if (tree.DataCount > threshold)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, EddyLib.Strings.ReturnMsg.LargeDataTree(threshold));
                }

                DA.SetDataTree(0, tree);
            }
            else if ((gobj.Value is WindFactorsSpatial))
            {
                WindFactorsSpatial wfs = null;

                DA.GetData(0, ref wfs);

                int windDirs = ArrayHelper.CustomArray<double>.GetRow(wfs.ValuesSpatial, 0).Count();
                DataTree<double> tree = new DataTree<double>();

                if (selection.Count == -1)
                {
                    selection = (new int[wfs.SimulatedWindDirections.Count]).Select((o, i) => i).ToList();
                }

                foreach (int dir in selection)
                {
                    if (selection.Max() > windDirs || selection.Min() < 0)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, EddyLib.Strings.ReturnMsg.SelectionOutsideWindDirs(selection.Max()));
                        return;
                    }

                    var tempColumn = ArrayHelper.CustomArray<double>.GetColumn(wfs.ValuesSpatial, dir);
                    tree.AddRange(tempColumn, new Grasshopper.Kernel.Data.GH_Path(dir));
                }

                double threshold = 1e6;
                if (tree.DataCount > threshold)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, EddyLib.Strings.ReturnMsg.LargeDataTree(threshold));
                }

                DA.SetDataTree(0, tree);
            }
            else if ((gobj.Value is UTCI))
            {
                UTCI utci = null;

                DA.GetData(0, ref utci);

                DataTree<double> tree = new DataTree<double>();

                foreach (int h in selection)
                {
                    var tempRow = ArrayHelper.CustomArray<double>.GetRow(utci.ValuesUTCI, h);
                    tree.AddRange(tempRow, new Grasshopper.Kernel.Data.GH_Path(h));
                }

                double threshold = 1e6;
                if (tree.DataCount > threshold)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, EddyLib.Strings.ReturnMsg.LargeDataTree(threshold));
                }

                DA.SetDataTree(0, tree);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid Outdoor Thermal Comfort object"); return;
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_decomposeData;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{980B2E62-6FAC-4B17-A302-74376E319BF6}");
    }
}