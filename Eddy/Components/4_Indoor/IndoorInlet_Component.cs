using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Eddy.Components.Indoor
{
    public class IndoorInlet_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the IndoorInlet_Component class.
        /// </summary>
        public IndoorInlet_Component()
          : base(
              "Indoor Inlet", 
              "Inlet",
              @"Ventilation Inlet

Defines where air enters the room, such as diffusers, windows, or doors.

" + EddyVersion.toString(),
              EddyVersion.Name, 
              "9 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Geo", "Geo", "Geometry", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vel", "V", "Velocity [m/s]", GH_ParamAccess.item);
            pManager.AddNumberParameter("Temp", "T", "Temperature [C]", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_IndoorBC_Inlet(), "Inlet", "In", "Inlet", GH_ParamAccess.item);
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

            Vector3d vec = Vector3d.ZAxis;
            DA.GetData(1, ref vec);

            double T = 20;
            DA.GetData(2, ref T);

            int refinement = 3;
            var inlet = new IndoorBC.Inlet(m, T, refinement, vec);

            DA.SetData(0, new IndoorInletGoo(inlet));
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_Indoor_Inlet;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("cb1b044e-b0bf-4fb3-b844-9f9469931244"); }
        }
    }
}