using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;

namespace Eddy.Components.Indoor
{
    public class ViralEmitter_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Emitter class.
        /// </summary>
        public ViralEmitter_Component()
          : base("Viral Emitter", "Viral", 
@"Pathogen Source

Simulates the release of airborne pathogens (e.g., viruses) from a specific location to analyze infection risk and dispersion patterns.

" + EddyVersion.toString(), 
              EddyVersion.Name, "9 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Geo", "Source volume (Mesh).", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "Name", "Identifier for this emitter.", GH_ParamAccess.item, "");
            pManager.AddNumberParameter("Injection Rate", "Rate", "Viral particle emission rate.", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Rate Type", "Type", "0: Absolute [-], 1: Specific [1/m³]", GH_ParamAccess.item, 0);
            Param_Integer param = pManager[3] as Param_Integer;
            param.AddNamedValue("Absolute", 0);
            param.AddNamedValue("Specific", 1);

            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Function Object", "FObj", "Viral emitter for Indoor Simulation component", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh geo = null;
            if (!DA.GetData("Geometry", ref geo)) { }
            ;

            string Name = "";
            DA.GetData("Name", ref Name);

            double IR = 0;
            DA.GetData("Injection Rate", ref IR);

            int Type = 0;
            DA.GetData("Rate Type", ref Type);

            var em = new ViralEmitter(geo, Type, IR, Name);

            //var em = new CO2Emitters(  geo, Type, IR, Name);

            var goo = new FunctionObjectGoo(em);

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
                return Resources.Eddy_Indoor__Indoor_ViralEmitter;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            //get { return new Guid("9afa2ec3-39af-4454-9c43-8f31f521dab2"); }
            get { return new Guid("9afa2ec3-39af-4454-9c43-8f31f521dab3"); }
        }
    }
}