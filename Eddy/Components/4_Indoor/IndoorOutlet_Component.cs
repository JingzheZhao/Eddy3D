using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Eddy.Components.Indoor
{
    public class IndoorOutlet_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the IndoorOutlet_Component class.
        /// </summary>
        public IndoorOutlet_Component()
          : base(
              "Indoor Outlet", 
              "Outlet",
              @"Define an air exhaust outlet for indoor CFD simulation.

Specify outlet geometry and optional extraction velocity.

" + EddyVersion.toString(),
              EddyVersion.Name, 
              "9 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter(
                "Geometry", "Geo", 
                "Outlet surface mesh.", 
                GH_ParamAccess.item);

            pManager.AddVectorParameter(
                "Velocity", "Vel", 
                "Optional extraction velocity. (0,0,0) = passive outlet. Units: m/s", 
                GH_ParamAccess.item, new Vector3d(0, 0, 0));
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_IndoorBC_Outlet(), "Outlet", "Out", "Outlet boundary condition for Indoor Domain", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh m = null;
            DA.GetData("Geometry", ref m);

            Vector3d vec = Vector3d.Zero;
            DA.GetData("Velocity", ref vec);

            int refinementLevel = 3;
            var outlet = new IndoorBC.Outlet(m, refinementLevel);

            DA.SetData("Outlet", new IndoorOutletGoo(outlet));
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