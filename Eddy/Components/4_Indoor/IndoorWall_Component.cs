using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using Eddy.Analytics;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Eddy.Components.Indoor
{
    public class IndoorWall_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the IndoorWall_Component class.
        /// </summary>
        public IndoorWall_Component()
          : base(
              "Indoor Wall", 
              "Wall",
              @"Indoor Wall/Boundary

Defines a solid boundary for indoor simulations, such as walls, floors, or ceilings. Allows specification of surface temperature for thermal analysis.

" + EddyVersion.toString(),
              EddyVersion.Name, 
              "9 | Indoor")
        {
            Analytics.Analytics.TrackComponentView("IndoorWall");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
            pManager.AddNumberParameter("Temp", "T", "Temperature [C]", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_IndoorBC_Wall(), "Geo", "Geo", "Geometry", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh m = null;
            if (!DA.GetData(0, ref m)) return;
            if (m == null) return;

            double temp = 20;
            DA.GetData(1, ref temp);

            int refinementLevel = 3;
            var wall = new IndoorBC.Wall(m, refinementLevel, temp);

            DA.SetData(0, new IndoorWallGoo(wall));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_Indoor_Geometry;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("41272573-11fa-4cf4-8275-6029f3fe2c5f"); }
        }
    }
}
