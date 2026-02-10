using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using Eddy.Analytics;
using EddyLib;
using EddyLib.Indoor.FunctionObjects;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;

namespace Eddy.Components.Indoor
{
    public class VolumetricHeatSource_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Emitter class.
        /// </summary>
        public VolumetricHeatSource_Component()
          : base("Heat Source", "HeatSrc", 
@"Heat Source

Models a heat-generating object within the indoor space, such as equipment, electronics, or a cluster of people. Can be defined by total power (W) or power density (W/m³).

" + EddyVersion.toString(), 
              EddyVersion.Name, "9 | Indoor")
        {
            Analytics.Analytics.TrackComponentView("IndoorHeatSource");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Geo", "Heat source volume (Mesh).", GH_ParamAccess.item);
            pManager.AddNumberParameter("Power", "P", "Heat output. Units: W (absolute) or W/m³ (specific).", GH_ParamAccess.item);
            pManager.AddTextParameter("Name", "Name", "Identifier for this heat source.", GH_ParamAccess.item, "");

            pManager.AddIntegerParameter("Power Type", "Type", "0: Absolute [W], 1: Specific [W/m³]", GH_ParamAccess.item);
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
            pManager.AddGenericParameter("Function Object", "FObj", "Heat source for Indoor Simulation component", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh m = null;
            DA.GetData(0, ref m);

            double Power = 1;
            DA.GetData(1, ref Power);

            string Name = "";
            DA.GetData(2, ref Name);

            int Type = 0;
            DA.GetData(3, ref Type);

            var heatSource = new VolumetricHeatSource(m, Type, Power, Name);

            var goo = new FunctionObjectGoo(heatSource);

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
                return Resources.Eddy_Indoor__Indoor_VHS;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{128F2233-5532-441A-BE3B-F0D9AED33C27}"); }
        }
    }
}
