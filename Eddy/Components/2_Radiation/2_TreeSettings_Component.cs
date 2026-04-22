using Eddy.Properties;
using EddyLib;
using Eddy.Analytics;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using System;

namespace Eddy.Components._2_Radiation
{
    public class TreeSettings_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Initializes a new instance of the _2_SurfaceMaterialSettings_Component class.
        /// </summary>
        public TreeSettings_Component()
          : base("Tree Settings", "TreeSet",
@"Define material properties for tree surfaces.

Customize Radiance material for tree canopy ray-tracing.
Default uses standard deciduous tree reflectance.

" + EddyVersion.toString(),
              EddyVersion.Name, "2 | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Radiance Material", "RadMat", "Custom Radiance material string for tree canopy.", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("Settings", "Set", "Tree settings for Tree Surface component");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string RadianceMaterial = "";

            if (!DA.GetData(0, ref RadianceMaterial)) return;

            var surfSettings = new Tree_Settings();

            surfSettings.RadianceMaterial = RadianceMaterial;
            if (String.IsNullOrWhiteSpace(surfSettings.RadianceMaterial))
            {
                surfSettings.RadianceMaterial = RadianceMaterials.DefaultTree;
            }

            DA.SetData(0, surfSettings);
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
                return Resources.Eddy_MRT_Tree_Settings;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{F94CC435-D5CC-4F00-AD55-434138BCBB48}"); }
        }
    }
}
