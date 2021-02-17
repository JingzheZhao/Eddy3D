using Eddy.Properties;
using EddyLib;
using EddyLib.OutdoorComfort;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompCalcWindComfort : GH_Component
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
        public CompCalcWindComfort()
          : base("Annual Wind Comfort", "Wind Comfort", @"Wind Comfort

" + EddyVersion.toString(),
              EddyVersion.Name, "7 | Metrics")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Wind Factors Annual", "WFA", @"Wind Factors Annual Object", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Wind Comfort Index", "WCmftIdx", "Select a Wind Comfort Index with a right click.", GH_ParamAccess.item, 0);

            //Using an enum to generate the dropdown items
            var types = Enum.GetNames(typeof(EddyLib.OutdoorComfort.WindComfort.PCIdx));
            Param_Integer param = pManager[1] as Param_Integer;

            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }

            //pManager.AddBooleanParameter("Run", "Run", "Run the calculation", GH_ParamAccess.item);

            //pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Wind Comfort", "WCmft", @"Wind Comfort

Simplified evaluation of annual wind velocites according to specific comfort indices.
Binning is done by evaluating actual observed wind velocities for every hour, not by calculating maximum allowable exceedance probability given the wind statistic (to be released later).

General Lawson

1 - A > 1.8 m/s < 2 % Sitting Long
2 - B > 3.6 m/s < 2 % Sitting Short
3 - C > 5.3 m/s < 2 % Walking Leisurely
4 - D > 7.6 m/s > 5 % Walking Fast
5 - E > 7.6 m/s >= 2 % Uncomfortable

Lawson LDDC

1 - A > 2.5 m/s < 5 % Frequent sitting
2 - B > 4 m/s < 5 % Occasional sitting
3 - C > 6 m/s < 5 % Standing
4 - D > 8 m/s < 5 % Walking
5 - E > 8 m/s > 5 % Uncomfortable
6 - S > 15 m/s > 0.022 % Unsafe

Lawson 2001

1 - A   > 4 m/s < 5 % Sitting
2 - B   > 6 m/s < 5 % Standing
3 - C   > 8 m/s < 5 % Strolling
4 - D   > 10 m/s < 5 % Business Walking
5 - E   > 10 m/s > 5 % Uncomfortable
6 - S15 > 15 m/s > 0.023 % Unsafe frail
7 - S20 > 20 m/s > 0.023 % Unsafe all

Davenport

1 - A > 3.6 m/s < 1.5 % Sitting Long
2 - B > 5.3 m/s < 1.5 % Sitting Short
3 - C > 7.6 m/s < 1.5 % Walking Leisurely
4 - D > 9.8 m/s < 1.5 % Walking Fast
5 - E > 9.8 m/s >= 1.5 % Uncomfortable
6 - S > 15.1 m/s >= 0.01 % Dangerous

NEN8100

1 - A > 5 m/s < 2.5 % Sitting Long
2 - B > 5 m/s < 5 % Sitting Short
3 - C > 5 m/s < 10 % Walking Leisurely
4 - D > 5 m/s < 20 % Walking Fast
5 - E > 5 m/s > 20 % Uncomfortable
6 - S > 15 m/s > 0.05 % Dangerous", GH_ParamAccess.list);
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
            //// mode to select environment
            //if (!interpolate) { Message = "No interpolation"; }
            //else { Message = "Interpolation"; }

            WindFactorsAnnual WFA = null;
            DA.GetData(0, ref WFA);

            int cmftidx = 0;
            DA.GetData("Wind Comfort Index", ref cmftidx);
            WindComfort.PCIdx cmftcmftindex = (WindComfort.PCIdx)cmftidx;

            //List<Point3d> probes = new List<Point3d>();
            //DA.GetDataList("Probing points", probes);

            //bool run = false;
            //DA.GetData("Run", ref run);

            // Do not use DataTree inside actual components, it is only meant to be used inside
            // script components. Use GH_Structure instead.
            // https://www.grasshopper3d.com/forum/topics/getdatatree-fro-a-datatree-point3d DataTree
            // and GH_Structure are annoyingly similar yet non-overlapping classes.GH_Structure is
            // used by Grasshopper itself to store data, DataTree is a version that was made
            // specifically for the use inside script components.This part of the SDK is a mess but
            // there's nothing we can do about it at this point.

            //Grasshopper.Kernel.Data.GH_Structure<Grasshopper.Kernel.Types.IGH_Goo> U = null;
            //DA.GetDataTree("U", out U);//
            //DA.GetDataTree("Wind Velocity", out GH_Structure<GH_Vector> U);

            //DA.GetDataTree("U", out DataTree<Vector> U);

            #region Wind Comfort

            var wc = new WindComfort(WFA, cmftcmftindex);

            if (GH_Document.IsEscapeKeyDown())
            {
                GH_Document GHDocument = OnPingDocument();
                GHDocument.RequestAbortSolution();
            }

            DA.SetDataList(0, wc.ValuesPedestrianWindComfort);

            #endregion Wind Comfort
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_windFactors;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{15983E86-27F6-4F75-ABB4-16C329E7FFCB}");
    }
}