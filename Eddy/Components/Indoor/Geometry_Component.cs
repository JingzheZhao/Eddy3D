using System;
using System.Collections.Generic;
using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Eddy.Components.Indoor
{
    public class Geometry_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Geometry class.
        /// </summary>
        public Geometry_Component()
          : base("Geometry", "Geo",
              "Geometry" + EddyVersion.toString(),
              EddyVersion.Name, "7 | Indoor")
        {
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
            pManager.AddGenericParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Mesh m = null;
            DA.GetData(0, ref m);

            double temp = 0;
            DA.GetData(1, ref temp);

            var wall = new IndoorBCs.Wall(m, temp, temp);

            DA.SetData(0, wall);

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