using System;
using System.Collections.Generic;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Eddy.Components.Indoor
{
    public class Emitter : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Emitter class.
        /// </summary>
        public Emitter()
          : base("Emitter", "Em",
              "Emitter" + EddyVersion.toString(),
              EddyVersion.Name, "7 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Emitter", "E", "Emitter", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                 return Resources.Eddy_Indoor_Emitter;
                
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9afa2ec3-39af-4454-9c43-8f31f521dab2"); }
        }
    }
}