using Eddy.Properties;
using EddyLib;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DateTimeExtensions;

namespace Eddy.Components.Radiation
{
    public class HOY_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public HOY_Component()
          : base("HOY", "HOY", "HOY" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("M", "M", "M", GH_ParamAccess.item);
            pManager.AddIntegerParameter("D", "D", "D", GH_ParamAccess.item);
            pManager.AddIntegerParameter("H", "H", "H", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("HOY", "HOY", "HOY", GH_ParamAccess.item);

            //pManager.AddMeshParameter("Meshes", "M", "Analysis meshes", GH_ParamAccess.list);
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

            var dt = new DateTime(m, d, h);

            DA.SetData(0, dt.HOY());
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
                return Resources.Eddy_stability;
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