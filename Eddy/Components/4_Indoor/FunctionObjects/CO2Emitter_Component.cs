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
    public class CO2Emitter_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Emitter class.
        /// </summary>
        public CO2Emitter_Component()
          : base("CO2 Emitter", "CO2", 
@"CO2 Source

Simulates carbon dioxide generation, typically from occupants. Use this to assess ventilation effectiveness and air quality.

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
            pManager.AddNumberParameter("Injection Rate", "Rate", "CO2 injection rate.", GH_ParamAccess.item);

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
            pManager.AddGenericParameter("Function Object", "FObj", "CO2 emitter for Indoor Simulation component", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //0
            Mesh geo = null;
            if (!DA.GetData("Geo", ref geo)) { }
            ;

            //1
            string Name = "";
            DA.GetData("Name", ref Name);

            //2
            double IR = 0;
            DA.GetData("Injection Rate", ref IR);

            //3
            int Type = 0;
            DA.GetData("Type", ref Type);

            var em = new CO2Emitter(geo, Type, IR, Name);

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
                return Resources.Eddy_Indoor__Indoor_CO2Emitter;
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