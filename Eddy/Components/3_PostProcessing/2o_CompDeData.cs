using Eddy.Properties;
using EddyLib;
using EddyLib.OutdoorComfort;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompDeconstructOCData : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary | GH_Exposure.obscure; ; }
        }

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
" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")
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
            if (!DA.GetData(0, ref gobj) || gobj?.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid Outdoor Thermal Comfort object");
                return;
            }

            var selection = new List<int>();
            DA.GetDataList(1, selection);

            if (gobj.Value is MRT mrt)
            {
                int rowCount = mrt.Values.GetLength(0);
                var selectedHours = ResolveSelection(selection, rowCount);
                if (!ValidateSelection(selectedHours, rowCount, "hours"))
                {
                    return;
                }

                DataTree<double> tree = new DataTree<double>();

                foreach (int hour in selectedHours)
                {
                    AddRowToTree(tree, mrt.Values, hour);
                }

                WarnIfLargeTree(tree);

                DA.SetDataTree(0, tree);
            }
            else if (gobj.Value is WindFactorsTemporal wf)
            {
                int rowCount = wf.ValuesTemporalAtProbingHeight.GetLength(0);
                var selectedHours = ResolveSelection(selection, rowCount);
                if (!ValidateSelection(selectedHours, rowCount, "hours"))
                {
                    return;
                }

                DataTree<double> tree = new DataTree<double>();

                foreach (int hour in selectedHours)
                {
                    AddRowToTree(tree, wf.ValuesTemporalAtProbingHeight, hour);
                }

                WarnIfLargeTree(tree);

                DA.SetDataTree(0, tree);
            }
            else if (gobj.Value is WindFactorsSpatial wfs)
            {
                int windDirs = wfs.ValuesSpatial.GetLength(1);
                var selectedDirs = ResolveSelection(selection, windDirs);
                if (!ValidateSelection(selectedDirs, windDirs, "wind direction"))
                {
                    return;
                }
                DataTree<double> tree = new DataTree<double>();

                foreach (int dir in selectedDirs)
                {
                    AddColumnToTree(tree, wfs.ValuesSpatial, dir);
                }

                WarnIfLargeTree(tree);

                DA.SetDataTree(0, tree);
            }
            else if (gobj.Value is UTCI utci)
            {
                int rowCount = utci.ValuesUTCI.GetLength(0);
                var selectedHours = ResolveSelection(selection, rowCount);
                if (!ValidateSelection(selectedHours, rowCount, "hours"))
                {
                    return;
                }

                DataTree<double> tree = new DataTree<double>();

                foreach (int hour in selectedHours)
                {
                    AddRowToTree(tree, utci.ValuesUTCI, hour);
                }

                WarnIfLargeTree(tree);

                DA.SetDataTree(0, tree);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid Outdoor Thermal Comfort object"); return;
            }
        }

        private static List<int> ResolveSelection(List<int> selection, int maxExclusive)
        {
            if (selection.Count == 1 && selection[0] == -1)
            {
                var all = new List<int>(maxExclusive);
                for (int i = 0; i < maxExclusive; i++)
                {
                    all.Add(i);
                }
                return all;
            }

            return selection;
        }

        private bool ValidateSelection(List<int> selection, int maxExclusive, string selectionType)
        {
            if (selection.Count == 0)
            {
                return true;
            }

            int min = selection[0];
            int max = selection[0];
            for (int i = 1; i < selection.Count; i++)
            {
                int value = selection[i];
                if (value < min) { min = value; }
                if (value > max) { max = value; }
            }

            if (min < 0 || max >= maxExclusive)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Error,
                    $"Selection contains invalid {selectionType} indices. Valid range is 0 to {maxExclusive - 1}.");
                return false;
            }

            return true;
        }

        private static void AddRowToTree(DataTree<double> tree, double[,] values, int rowIndex)
        {
            var path = new GH_Path(rowIndex);
            tree.EnsurePath(path);
            IList<double> branch = tree.Branch(path);
            int columnCount = values.GetLength(1);
            for (int column = 0; column < columnCount; column++)
            {
                branch.Add(values[rowIndex, column]);
            }
        }

        private static void AddColumnToTree(DataTree<double> tree, double[,] values, int columnIndex)
        {
            var path = new GH_Path(columnIndex);
            tree.EnsurePath(path);
            IList<double> branch = tree.Branch(path);
            int rowCount = values.GetLength(0);
            for (int row = 0; row < rowCount; row++)
            {
                branch.Add(values[row, columnIndex]);
            }
        }

        private void WarnIfLargeTree(DataTree<double> tree)
        {
            const double threshold = 1e6;
            if (tree.DataCount > threshold)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, EddyLib.Strings.ReturnMsg.LargeDataTree(threshold));
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
