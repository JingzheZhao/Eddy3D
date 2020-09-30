using System;
using System.Collections.Generic;
using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Eddy.Components.Indoor
{
    public class IndoorOutlet_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Outlet class.
        /// </summary>
        public IndoorOutlet_Component()
          : base("Outlet", "Ol",
              "Outlet" + EddyVersion.toString(),
              EddyVersion.Name, "7 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vel", "V", "Velocity [m/s]. The default is a zero length vector indicating that there is no force removing air from the space", GH_ParamAccess.item, new Vector3d(0,0,0) );
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_IndoorBC_Outlet(), "Outlet", "Ol", "Outlet", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh m = null;
            DA.GetData(0, ref m);
            Vector3d vec = new Vector3d(0, 0, 0);
            DA.GetData(1, ref vec);
           

            var inlet = new IndoorBCs.Outlet(m, vec);

            var goo = new IndoorOutletGoo(inlet);

            DA.SetData(0, goo);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_Indoor_Outlet;

            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c9c87691-db10-4778-a955-51c9335cdb31"); }
        }
    }
}