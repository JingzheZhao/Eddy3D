using Eddy.Properties;
using EddyLib;
using Eddy.Analytics;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using System;

namespace Eddy.Components._2_Radiation
{
    public class VegetationSurfaceSettings_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Initializes a new instance of the _2_SurfaceMaterialSettings_Component class.
        /// </summary>
        public VegetationSurfaceSettings_Component()
          : base("Vegetation Settings", "VegSet", "Vegetation surface settings " + EddyVersion.toString(), EddyVersion.Name, "2 | Radiation")
        {
            Analytics.Analytics.TrackComponentView("MRTVegetationSettings");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Material Name", GH_ParamAccess.item, "MyVegetationSurface");

            pManager.AddNumberParameter("Height Plants", "HP", "Height of Plants [m]", GH_ParamAccess.item, 0.5);
            pManager.AddNumberParameter("LeafAreaIndex", "LAI", "LeafAreaIndex [dimensionless]", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("LeafReflectivity", "LR", "LeafReflectivity [0-1]", GH_ParamAccess.item, 0.2);
            pManager.AddNumberParameter("LeafEmissivity", "LE", "LeafEmissivity [0-1]", GH_ParamAccess.item, 0.95);
            pManager.AddNumberParameter("MinimumStomatalResistance", "MSR", "MinimumStomatalResistance [s/m]", GH_ParamAccess.item, 180);

            pManager.AddTextParameter("Surface", "SMat", "Optional Radiance Surface Material", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("VegSet", "Set", "Vegetation settings");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string Name = "";
            double HeightOfPlants = 2.4;
            double LeafAreaIndex = 2400;
            double LeafReflectivity = 840;
            double LeafEmissivity = 0.9;
            double MinimumStomatalResistance = 0.7;

            string RadianceMaterial = "";

            if (!DA.GetData(0, ref Name)) return;
            if (!DA.GetData(1, ref HeightOfPlants)) return;
            if (!DA.GetData(2, ref LeafAreaIndex)) return;
            if (!DA.GetData(3, ref LeafReflectivity)) return;
            if (!DA.GetData(4, ref LeafEmissivity)) return;
            if (!DA.GetData(5, ref MinimumStomatalResistance)) return;

            if (!DA.GetData(6, ref RadianceMaterial)) return;

            var surfSettings = new VegetationSurface_Settings();

            surfSettings.Name = Name;
            surfSettings.HeightOfPlants = HeightOfPlants;
            surfSettings.LeafAreaIndex = LeafAreaIndex;
            surfSettings.LeafReflectivity = LeafReflectivity;
            surfSettings.LeafEmissivity = LeafEmissivity;
            surfSettings.MinimumStomatalResistance = MinimumStomatalResistance;
            surfSettings.RadianceMaterial = RadianceMaterial;

            surfSettings.RadianceMaterial = RadianceMaterial;
            if (String.IsNullOrWhiteSpace(surfSettings.RadianceMaterial))
            {
                surfSettings.RadianceMaterial = RadianceMaterials.DefaultGrass;
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
                return Resources.Eddy_MRT_Vegetation_Settings;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{AA753DAD-FDAD-46B2-A623-0E7494350083}"); }
        }
    }
}
