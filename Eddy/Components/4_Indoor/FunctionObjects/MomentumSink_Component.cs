using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Eddy.Components.Indoor
{
    public class MomentumSink_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Emitter class.
        /// </summary>
        public MomentumSink_Component()
          : base("Momentum Sink", "MSink", "Momentum Sink" + EddyVersion.toString(), EddyVersion.Name, "9 | Indoor")
        {
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
            //pManager.AddNumberParameter("Injection Rate", "IR", "Injection Rate imposed on object", GH_ParamAccess.item);

            //3
            //pManager.AddIntegerParameter("Type", "Typ", "Type: Absolute [W] or specific [W/m³]", GH_ParamAccess.item, 0);
            //Param_Integer param = pManager[3] as Param_Integer;
            //param.AddNamedValue("Absolute", 0);
            //param.AddNamedValue("Specific", 1);

            //pManager[3].Optional = true;
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
            if (!DA.GetData("Geo", ref geo)) { };

            string Name = "";
            DA.GetData("Name", ref Name);

            //double IR = 0;
            //DA.GetData("Injection Rate", ref IR);

            //int Type = 0;
            //DA.GetData("Type", ref Type);

            var m = new MomentumSinkIndoor(geo, Name);

            var goo = new FunctionObjectGoo(m);

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
                return Resources.Eddy_Indoor__Indoor_MomentumSink;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0163E2D4-01F4-4535-8255-461483A28ACF"); }
        }
    }

    //public class MomentumSink_Component : GH_Component
    //{
    //    /// <summary>
    //    /// Initializes a new instance of the Emitter class.
    //    /// </summary>
    //    public MomentumSink_Component()
    //      : base("Momentum Sink", "MSink", "Momentum Sink" + EddyVersion.toString(), EddyVersion.Name, "9 | Indoor")
    //    {
    //    }

    //    /// <summary>
    //    /// Registers all the input parameters for this component.
    //    /// </summary>
    //    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    //    {
    //        pManager.AddGeometryParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);

    //        pManager.AddTextParameter("Name", "N", "Name", GH_ParamAccess.item, "");

    //        pManager[1].Optional = true;
    //        pManager[2].Optional = true;
    //    }

    //    /// <summary>
    //    /// Registers all the output parameters for this component.
    //    /// </summary>
    //    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    //    {
    //        pManager.AddGenericParameter("Function Object", "FO", "Momentum Sink Function Object", GH_ParamAccess.item);
    //    }

    //    /// <summary>
    //    /// This is the method that actually does the work.
    //    /// </summary>
    //    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    //    protected override void SolveInstance(IGH_DataAccess DA)
    //    {
    //        Mesh geo = null;
    //        if (!DA.GetData("Geo", ref geo)) { };

    //        string Name = "";
    //        DA.GetData(2, ref Name);

    //        int Type = 0;
    //        DA.GetData("Type", ref Type);

    //        var momsink = new EddyLib.Indoor.MomentumSink(geo, Name);

    //        var goo = new FunctionObjectGoo(momsink);

    //        DA.SetData(0, goo);
    //        }
    //    }

    //    /// <summary>
    //    /// Provides an Icon for the component.
    //    /// </summary>
    //    protected override System.Drawing.Bitmap Icon
    //    {
    //        get
    //        {
    //            //You can add image files to your project resources and access them like this:
    //            return Resources.Eddy_Indoor_Emitter;
    //        }
    //    }

    //    /// <summary>
    //    /// Gets the unique ID for this component. Do not change this ID after release.
    //    /// </summary>
    //    public override Guid ComponentGuid
    //    {
    //        get { return new Guid("0163E2D4-01F4-4535-8255-461483A28ACF"); }
    //    }
    //}
}