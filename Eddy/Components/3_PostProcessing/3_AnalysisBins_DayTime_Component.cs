using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;

namespace Eddy.Components.Radiation
{
    public class AnalysisBins_DayTime_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public AnalysisBins_DayTime_Component()
         : base("Analysis Bins Day Time", "ABDayTime", @"Analysis Bins Day Time

Morning,    // 12pm - 6am
Breakfast,  // 7am - 11am
Lunch,      // 12am - 3pm
Afternoon,  // 4pm - 5pm
Dinner,     // 6pm - 9pm
Nightlife   // 10pm - 12pm

" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("ABSeason", "AB", "ABSeason", GH_ParamAccess.item);

            pManager[0].Optional = true;

            pManager.AddIntegerParameter("DayTime", "DT", "DayTime", GH_ParamAccess.item, 0);

            //Using an enum to generate the dropdown items
            var types = Enum.GetNames(typeof(DayTime.DayTimeE));
            Param_Integer param = pManager[1] as Param_Integer;

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
            AnalysisBins ABPrevious = null;
            DA.GetData(0, ref ABPrevious);

            int DTE = 0;
            DA.GetData(1, ref DTE);

            var UserDayTime = new DayTime((DayTime.DayTimeE)DTE);

            DateTime dt1 = new DateTime(2021, 1, 1, UserDayTime.HourBegin, 0, 0);
            DateTime dt2 = new DateTime(2021, 12, 31, UserDayTime.HourEnd, 0, 0);

            AnalysisBins ABNew = new AnalysisBins(dt2, dt1, UserDayTime.dayTimeE);

            if (ABPrevious != null)
            {
                AnalysisBins ABResult = new AnalysisBins(ABPrevious, ABNew);
                DA.SetData(0, ABResult);
            }
            else
            {
                DA.SetData(0, ABNew);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                 // You can add image files to your project resources and access them like this:
                 Resources.Eddy_AnalysisBins_DayTime;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{6EE1CAC2-856F-4E28-A298-28985884A523}"); }
        }
    }
}