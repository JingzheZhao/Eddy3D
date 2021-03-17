using Eddy.Properties;
using EddyLib;
using EddyLib.OutdoorComfort;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Windows.Forms;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy

{
    public class CompCalcWindComfort : GH_Component
    {// exposure
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary | GH_Exposure.obscure; ; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompCalcWindComfort()
          : base("Pedestrian Wind Comfort", "Pedestrian Wind Comfort", @"Pedestrian Wind Comfort

Evaluation of annual wind velocites according to specific comfort metrics.
Binning is done by calculating maximum allowable exceedance probability given the wind statistic.
This component assumes probing at 1.75 m above ground.

General Lawson

1 - A   > 1.8 m/s <   2 %   Sitting Long
2 - B   > 3.6 m/s <   2 %   Sitting Short
3 - C   > 5.3 m/s <   2 %   Walking Leisurely
4 - D   > 7.6 m/s >   5 %   Walking Fast
5 - E   > 7.6 m/s >=  2 %   Uncomfortable

Lawson LDDC

1 - A   > 2.5 m/s < 5 %       Frequent sitting
2 - B   > 4 m/s   < 5 %       Occasional sitting
3 - C   > 6 m/s   < 5 %       Standing
4 - D   > 8 m/s   < 5 %       Walking
5 - E   > 8 m/s   > 5 %       Uncomfortable
6 - S   > 15 m/s  > 0.022 %   Unsafe

Lawson 2001

1 - A   > 4 m/s     < 5 %       Sitting
2 - B   > 6 m/s     < 5 %       Standing
3 - C   > 8 m/s     < 5 %       Strolling
4 - D   > 10 m/s    < 5 %       Business Walking
5 - E   > 10 m/s    > 5 %       Uncomfortable
6 - S15 > 15 m/s    > 0.023 %   Unsafe frail
7 - S20 > 20 m/s    > 0.023 %   Unsafe all

Davenport

1 - A   > 3.6 m/s   <   1.5 %   Sitting Long
2 - B   > 5.3 m/s   <   1.5 %   Sitting Short
3 - C   > 7.6 m/s   <   1.5 %   Walking Leisurely
4 - D   > 9.8 m/s   <   1.5 %   Walking Fast
5 - E   > 9.8 m/s   >=  1.5 %   Uncomfortable
6 - S   > 15.1 m/s  >=  0.01 %  Dangerous

NEN8100 Comfort

1 - A   > 5 m/s     < 2.5 %     Sitting Long
2 - B   > 5 m/s     < 5 %       Sitting Short
3 - C   > 5 m/s     < 10 %      Walking Leisurely
4 - D   > 5 m/s     < 20 %      Walking Fast
5 - E   > 5 m/s     > 20 %      Uncomfortable
6 - S   > 15 m/s    > 0.05 %    Dangerous

NEN8100 Safety

1 - A   > 15 m/s    < 0.05 %    No Risk,
2 - B   > 15 m/s    < 0.3 %     Limited Risk
3 - C   > 15 m/s    > 0.3 %     Dangerous

" + EddyVersion.toString(),
              EddyVersion.Name, @"3 | PostProcessing")
        {
        }

        public bool WeibullFit = true;

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Counting of Discrete Bins", Menu_DoClick, true, !WeibullFit);
        }

        private void Menu_DoClick(object sender, EventArgs e)
        {
            WeibullFit = !WeibullFit;
            ExpireSolution(true);
        }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetBoolean("Weibull Fit", WeibullFit);

            // Then call the base class implementation.
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            WeibullFit = reader.GetBoolean("Weibull Fit");

            // Then call the base class implementation.
            return base.Read(reader);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Wind Factors Spatial", "WFS", @"Wind Factors Spatial Object", GH_ParamAccess.item);

            pManager.AddGenericParameter("Wind Factors Annual", "WFA", @"Wind Factors Annual Object", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Wind Comfort Metric", "WCmftMetr", "Select a Wind Comfort Metric with a right click.", GH_ParamAccess.item, 0);

            //Using an enum to generate the dropdown items
            var types = Enum.GetNames(typeof(WindComfortHelper.PCMetric));
            Param_Integer param = pManager[2] as Param_Integer;

            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Wind Comfort Rank", "WCmft Rank", @"Wind Comfort Rank", GH_ParamAccess.list);
            pManager.AddTextParameter("Wind Comfort Class Letter", "WCmft Class Letter", @"Wind Comfort Class Letter", GH_ParamAccess.list);
            pManager.AddTextParameter("Wind Comfort Class", "WCmft Class", @"Wind Comfort Class", GH_ParamAccess.list);
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
            // mode to select environment
            if (!WeibullFit) { Message = "Counting of Discrete Bins"; }
            else { Message = "Weibull Fit"; }

            WindFactorsSpatial WFS = null;
            DA.GetData(0, ref WFS);

            WindFactorsTemporal WFA = null;
            DA.GetData(1, ref WFA);

            int cmftMetricGH = 0;
            DA.GetData("Wind Comfort Metric", ref cmftMetricGH);
            WindComfortHelper.PCMetric cmftMetric = (WindComfortHelper.PCMetric)cmftMetricGH;

            #region Wind Comfort

            if (WeibullFit == true)
            {
                var wc = new WindComfortWeibull(WFS, WFA, cmftMetric);

                if (GH_Document.IsEscapeKeyDown())
                {
                    GH_Document GHDocument = OnPingDocument();
                    GHDocument.RequestAbortSolution();
                }

                DA.SetDataList(0, wc.ValuesPedestrianWindComfortCat);
                DA.SetDataList(1, wc.ValuesPedestrianWindComfortClassLetter);
                DA.SetDataList(2, wc.ValuesPedestrianWindComfortClass);
            }
            else
            {
                var wc = new WindComfort(WFS, WFA, cmftMetric);

                if (GH_Document.IsEscapeKeyDown())
                {
                    GH_Document GHDocument = OnPingDocument();
                    GHDocument.RequestAbortSolution();
                }

                DA.SetDataList(0, wc.ValuesPedestrianWindComfortCat);
                DA.SetDataList(1, wc.ValuesPedestrianWindComfortClassLetter);
                DA.SetDataList(2, wc.ValuesPedestrianWindComfortClass);
            }

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