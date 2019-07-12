using System;
using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

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
          : base("Deconstruct Data", "DeData", "PostProcessing", "Eddy", "6 | Outdoor Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("In", "In", "In", GH_ParamAccess.item);
            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);
            // pManager.AddPointParameter("Probes", "Probes", "Probes", GH_ParamAccess.list);
            // pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.tree);
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

            if ((gobj.Value is MRT))
            {
                MRT mrt = null;

                DA.GetData(0, ref mrt);

                DataTree<double> tree = new DataTree<double>();

                for (int h = 0; h < 8760; h++)
                {
                    var tempRow = ArrayHelper.CustomArray<double>.GetRow(mrt.Values, h);
                    tree.AddRange(tempRow, new Grasshopper.Kernel.Data.GH_Path(h));
                }

                DA.SetDataTree(0, tree);
            }
            else if ((gobj.Value is WindFactors))
            {
                WindFactors wf = null;

                DA.GetData(0, ref wf);

                DataTree<double> tree = new DataTree<double>();

                for (int h = 0; h < 8760; h++)
                {
                    var tempRow = ArrayHelper.CustomArray<double>.GetRow(wf.Values, h);
                    tree.AddRange(tempRow, new Grasshopper.Kernel.Data.GH_Path(h));
                }

                DA.SetDataTree(0, tree);
            }
            else if ((gobj.Value is UTCI))
            {
                UTCI utci = null;

                DA.GetData(0, ref utci);

                DataTree<double> tree = new DataTree<double>();

                for (int h = 0; h < 8760; h++)
                {
                    var tempRow = ArrayHelper.CustomArray<double>.GetRow(utci.Values, h);
                    tree.AddRange(tempRow, new Grasshopper.Kernel.Data.GH_Path(h));
                }

                DA.SetDataTree(0, tree);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid Outdoor Comfort object"); return;
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_calMRT;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{980B2E62-6FAC-4B17-A302-74376E319BF6}");
    }
}