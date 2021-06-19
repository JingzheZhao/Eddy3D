using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;

namespace Eddy.Components.Radiation
{
    public class AnalysisBins_Season_Component : GH_Component
    {
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public AnalysisBins_Season_Component()
         : base("Analysis Bins Season", "ABSeason", @"Analysis Bins Season

Winter, // 12,1,2
Spring, // 3,4,5
Summer, // 6,7,8
Fall //  9,10,11

" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Season", "S", "Season", GH_ParamAccess.item, 0);

            //Using an enum to generate the dropdown items
            var types = Enum.GetNames(typeof(Season.SeasonE));
            Param_Integer param = pManager[0] as Param_Integer;

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
            pManager.AddGenericParameter("Analysis Bins", "AB", "Analysis Bins", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int S = 0;
            DA.GetData(0, ref S);

            var UserSeason = new Season((Season.SeasonE)S);

            DateTime dt1 = new DateTime(UserSeason.YearBegin, UserSeason.MonthBegin, UserSeason.DayBegin);
            DateTime dt2 = new DateTime(UserSeason.YearEnd, UserSeason.MonthEnd, UserSeason.DayEnd);

            AnalysisBins TS = new AnalysisBins(dt2, dt1);

            DA.SetData(0, TS);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        //protected override System.Drawing.Bitmap Icon
        //{
        //    get
        //    {
        //        //You can add image files to your project resources and access them like this:
        //        // return Resources.IconForThisComponent;
        //        return Resources.Eddy_stability;
        //    }
        //}

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{202B2EAD-3D2D-490C-B122-D0D4CD503371}"); }
        }
    }
}