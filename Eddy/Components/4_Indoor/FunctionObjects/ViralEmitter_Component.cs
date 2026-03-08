using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using Eddy.Analytics;
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
            Analytics.Analytics.TrackComponentView("IndoorViralEmitter");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //0
            pManager.AddGeometryParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
            //1
            pManager.AddTextParameter("Name", "N", "Name", GH_ParamAccess.item, "");
            //2
            pManager.AddNumberParameter("Injection Rate", "IR", "Injection Rate imposed on object", GH_ParamAccess.item);

            //3
            pManager.AddIntegerParameter("Type", "Typ", "Type: Absolute [-] or specific [1/m³]", GH_ParamAccess.item, 0);
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
            pManager.AddGenericParameter("Function Object", "FO", "Function Object", GH_ParamAccess.item);
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

            string Name = "";
            DA.GetData(1, ref Name);

            double IR = 0;
            DA.GetData(2, ref IR);

            int Type = 0;
            DA.GetData(3, ref Type);

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
