using EddyLib;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using static EddyLib.Radiation.RSurface;

namespace Eddy.Components.Radiation
{
    public class MakeRadiationSurface_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MakeRadiationMesh_Component class.
        /// </summary>
        public MakeRadiationSurface_Component()
          : base("Radiation Surface", "RadSurf", "Radiation Simulation Surface" + EddyVersion.toString(), EddyVersion.Name, "X | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddBrepParameter("Brep", "B", "Radiation surface", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Type", "T", "Type", GH_ParamAccess.item, 0);

            var types = Enum.GetNames(typeof(RSurface.RadiationSurfaceType));
            Param_Integer param = pManager[1] as Param_Integer;
            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }

            pManager.AddNumberParameter("Patch", "Ps", "Patch size", GH_ParamAccess.item, 3);
            pManager.AddTextParameter("Material", "M", "Optional Radiance Material", GH_ParamAccess.item, "");

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("RSurf", "RS", "Radiation Model Surfaces", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            var breps = new List<Brep>();
            int typeInt = 0;
            double patchSize = 2;
            string mat = "";

            if(!DA.GetDataList(0, breps)) return;
            if (!DA.GetData (1,ref typeInt)) return;
            if (!DA.GetData (2,ref patchSize)) return;
            if (!DA.GetData (3,ref mat)) return;

            RadiationSurfaceType thetype = (RadiationSurfaceType) typeInt;


            if (String.IsNullOrWhiteSpace(mat)) {

                if (thetype == RadiationSurfaceType.Ground) { mat = RadianceMaterial.DefaultGround; }
                else if (thetype == RadiationSurfaceType.Building) { mat = RadianceMaterial.DefaultFacade; }
                else if (thetype == RadiationSurfaceType.Vegetation) { mat = RadianceMaterial.DefaultGrass; }
                else if (thetype == RadiationSurfaceType.Tree) { mat = RadianceMaterial.DefaultTree; }

            }


            var RSurfs = new List<RSurface>();

            foreach (var b in breps) {

                RSurfs.Add(new RSurface("surf", b, thetype, mat, patchSize));
            
            }


            DA.SetDataList(0, RSurfs);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("52358012-b580-4a80-8d61-7d02bf600e76"); }
        }
    }
}