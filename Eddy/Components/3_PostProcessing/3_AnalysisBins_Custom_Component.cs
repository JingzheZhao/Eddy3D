using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;

namespace Eddy.Components.Radiation
{
    public class AnalysisBinsCustom_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public AnalysisBinsCustom_Component()
          : base("Custom Time Filter", "CustomTime",
@"Filter analysis results by custom date/time range.

Specify start and end DateTime objects for precise temporal filtering.

" + EddyVersion.toString(),
              EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Start", "From", "Start DateTime for filtering.", GH_ParamAccess.item);
            pManager.AddGenericParameter("End", "To", "End DateTime for filtering.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Time Filter", "Filter", "Custom time filter for Inspect components", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DateTime dt1 = new DateTime();
            DateTime dt2 = new DateTime();

            DA.GetData(0, ref dt1);
            DA.GetData(1, ref dt2);

            AnalysisBins TD = new AnalysisBins(dt2, dt1);

            if (dt2 < dt1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "'To' Date is before 'From' Date.");
                return;
            }

            DA.SetData(0, TD);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

            // You can add image files to your project resources and access them like this:
            Resources.Eddy_AnalysisBins_Custom;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{3EBF6D3D-8FD3-4614-B78B-CE2E819F36DF}"); }
        }
    }
}