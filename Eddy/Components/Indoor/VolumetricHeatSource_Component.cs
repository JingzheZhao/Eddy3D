using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using EddyLib.Indoor.Dicts;
using Grasshopper.Kernel.Parameters;

namespace Eddy.Components.Indoor
{
    public class VolumetricHeatSource_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Emitter class.
        /// </summary>
        public VolumetricHeatSource_Component()
          : base("VolumetricHeatSource", "VHS", "VolumetricHeatSource" + EddyVersion.toString(), EddyVersion.Name, "9 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
            pManager.AddNumberParameter("Power", "P", "Power", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Type", "Typ", "Type: Absolute [W] or specific [W/m³]", GH_ParamAccess.item);
            Param_Integer param = pManager[2] as Param_Integer;
            param.AddNamedValue("Absolute", 0);
            param.AddNamedValue("Specific", 1);

            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Source", "S", "Source", GH_ParamAccess.item);
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

            int Type = 0;
            DA.GetData(2, ref Type);

            var heatSource = new VolumetricHeatSource(m, Type, Power);

            var goo = new VolumetricHeatSourceGoo(heatSource);

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
            get { return new Guid("0163E2D4-01F4-4535-8255-461483A28ACE"); }
        }
    }
}