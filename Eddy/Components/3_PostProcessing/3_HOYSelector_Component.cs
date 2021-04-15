using DateTimeExtensions;
using Eddy.Properties;
using EddyLib;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Eddy.Components.Radiation
{
    public class HOYSelector_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.obscure | GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public HOYSelector_Component()
          : base("Analysis Bins", "ABins", "Define analysis time frames for seasonal and diurnal analysis" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("From", "F", "From Day [1-365]", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("To", "T", "To Day [1-365]", GH_ParamAccess.item, 365);

            pManager.AddIntegerParameter("Start", "S", "Start Hour [1-24]", GH_ParamAccess.item , 8);
            pManager.AddIntegerParameter("End", "E", "End Hour [1-24]", GH_ParamAccess.item, 18);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("HOY", "HOY", "Hours in the year", GH_ParamAccess.list);
            pManager.AddGenericParameter("DateTime", "Date", "DateTime objects", GH_ParamAccess.list);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int dt1 = 0;
            int dt2 = 0;

            DA.GetData(0, ref dt1);
            DA.GetData(1, ref dt2);



            if (dt1 < 1 || dt1 > 365 || dt2 < 1 || dt2 > 365) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'To' or 'From' day of year input is invalid. Must be an integer from 1-365.");
                return;
            }


            int days = Math.Abs(dt1 - dt2);
            if (dt1 > dt2) {
                int _d = Math.Abs(dt1 - 365);
                days = Math.Abs(_d + dt2);
            }


                int h1 = 0;
            int h2 = 0;

            DA.GetData(2, ref h1);
            DA.GetData(3, ref h2);
            if (h1 < 1 || h1 > 24 || h2 <1 || h2 > 24)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'Start' or 'End' hour of day input is invalid. Must be an integer from 0-24.");
                return;
            }

            int hours = Math.Abs(h1 - h2);

            DateTime fromDay = new DateTime(2021, 1, 1).AddDays(dt1 - 1);
            List<int> HOYS = new List<int>();
            List<DateTime> HOYSDateTime = new List<DateTime>();

            for (int i = 0; i < days+1; i++)
            {
                var currentDay = fromDay.AddDays(i).AddHours(h1-1);
                for (int j = 0; j < hours+1; j++)
                {
                    var currentHour = currentDay.AddHours(j);
                    HOYS.Add(currentHour.HOY());
                    HOYSDateTime.Add(currentHour);
                }
            }

            DA.SetDataList(0, HOYS);
            DA.SetDataList(1, HOYSDateTime);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.Eddy_analysisPeriod;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{4D0C6124-12EE-4592-B6F7-D02B1E3A4233}"); }
        }
    }
}