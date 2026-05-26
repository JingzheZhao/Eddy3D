using DateTimeExtensions;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;

namespace Eddy.Components.Radiation
{
    public class DateTime_To_HOY_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.obscure | GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public DateTime_To_HOY_Component()
          : base("Date to HOY", "HOY",
@"Date to HOY

Converts a specific Date and Time (Month, Day, Hour) into a single 'Hour of Year' integer (1-8760). Essential for querying specific timestamps in annual data.

" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Month", "M", "Month [1-12]", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Day", "D", "Day [1-31]", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Hour", "H", "Hour [0-23]", GH_ParamAccess.item, 0);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Hour Of Year", "HOY", "Hour Of Year", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int m = 1;
            int d = 1;
            int h = 1;

            DA.GetData(0, ref m);
            DA.GetData(1, ref d);
            DA.GetData(2, ref h);

            try
            {
                var dt = new DateTime(2021, m, d, h, 0, 0);
                int hoy = dt.HOY();
                DA.SetData(0, hoy);
                Message = $"HOY: {hoy}";
            }
            catch (ArgumentOutOfRangeException)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid date or time provided.");
                Message = "Invalid Date";
            }
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
                return Resources.Eddy_HOY;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{D5234126-347B-456F-8C9C-93B4CAB6ED1D}"); }
        }
    }
}