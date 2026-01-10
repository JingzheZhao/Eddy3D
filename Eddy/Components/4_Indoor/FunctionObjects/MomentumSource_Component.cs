using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Eddy.Components.Indoor
{
    public class MomentumSource_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Emitter class.
        /// </summary>
        public MomentumSource_Component()
          : base("Momentum Source", "MomSrc", 
@"Fan / Jet Source

Creates a volume that actively pushes air in a specific direction. Use this to model fans, blowers, HVAC supply jets, or other active airflow devices.

" + EddyVersion.toString(), 
              EddyVersion.Name, "9 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
            pManager.AddVectorParameter("Ubar", "Ubar", @"Ubar.

Desired mean velocity.", GH_ParamAccess.item);

            pManager.AddTextParameter("Name", "N", "Name", GH_ParamAccess.item, "");

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Function Object", "FO", "Momentum Source Function Object", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh geo = null;
            if (!DA.GetData(0, ref geo)) return;
            if (geo == null) return;

            Vector3d Ubar = new Vector3d(0, 0, 0);
            DA.GetData(1, ref Ubar);

            string Name = "";
            DA.GetData(2, ref Name);

            var fan = new MomentumSource(geo, Ubar, Name);

            var goo = new FunctionObjectGoo(fan);

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
                return Resources.Eddy_Indoor_Emitter;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("11DE2A3B-5AFE-447F-A822-5A949153DDCF"); }
        }
    }
}