using Eddy.Properties;
using EddyLib;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using System;

namespace Eddy.Components._2_Radiation
{
    public class SurfaceSettings_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Initializes a new instance of the _2_SurfaceMaterialSettings_Component class.
        /// </summary>
        public SurfaceSettings_Component()
          : base("Surface Settings", "SrfSet", 
@"Material Properties

Defines thermal and optical properties for building or ground surfaces (e.g., concrete, asphalt). Controls heat calculation parameters like conductivity, density, and emissivity.

" + EddyVersion.toString(), 
              EddyVersion.Name, "2 | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Material identifier (for reference).", GH_ParamAccess.item, "MySurface");
            pManager.AddNumberParameter("Thickness", "Thick", "Material thickness. Units: m. Default: 0.1", GH_ParamAccess.item, 0.1);
            pManager.AddNumberParameter("Conductivity", "k", "Thermal conductivity. Units: W/(m·K). Concrete: 2.3. Default: 2.3", GH_ParamAccess.item, 2.3);
            pManager.AddNumberParameter("Density", "ρ", "Material density. Units: kg/m³. Concrete: 2400. Default: 2400", GH_ParamAccess.item, 2400);
            pManager.AddNumberParameter("Specific Heat", "Cp", "Specific heat capacity. Units: J/(kg·K). Default: 840", GH_ParamAccess.item, 840);
            pManager.AddNumberParameter("Thermal Absorptance", "εT", "Longwave emissivity (0-1). High for most materials. Default: 0.9", GH_ParamAccess.item, 0.9);
            pManager.AddNumberParameter("Solar Absorptance", "αS", "Solar absorptance (0-1). Light surfaces ~0.3, dark ~0.9. Default: 0.7", GH_ParamAccess.item, 0.7);
            pManager.AddNumberParameter("Visible Absorptance", "αV", "Visible absorptance (0-1). Default: 0.7", GH_ParamAccess.item, 0.7);
            pManager.AddTextParameter("Radiance Material", "RadMat", "Optional: Custom Radiance material string", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("Settings", "Set", "Surface settings for Building/Ground Surface component");
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string Name = "";
            double Conductivity = 2.4;
            double Density = 2400;
            double SpecificHeat = 840;
            double ThermalAbsorbtance = 0.9;
            double SolarAbsorptance = 0.7;
            double VisibleAbsorptance = 0.7;

            //int roughselect = 0;

            double Thickness = 0.7;
            string RadianceMaterial = "";

            if (!DA.GetData(0, ref Name)) return;
            if (!DA.GetData(1, ref Thickness)) return;
            if (!DA.GetData(2, ref Conductivity)) return;
            if (!DA.GetData(3, ref Density)) return;
            if (!DA.GetData(4, ref SpecificHeat)) return;
            if (!DA.GetData(5, ref ThermalAbsorbtance)) return;
            if (!DA.GetData(6, ref SolarAbsorptance)) return;
            if (!DA.GetData(7, ref VisibleAbsorptance)) return;
            if (!DA.GetData(8, ref RadianceMaterial)) return;

            var surfSettings = new RSurface_Settings();

            surfSettings.Name = Name;
            surfSettings.Conductivity = Conductivity;
            surfSettings.SpecificHeat = SpecificHeat;
            surfSettings.ThermalAbsorptance = ThermalAbsorbtance;
            surfSettings.Density = Density;
            surfSettings.SolarAbsorptance = SolarAbsorptance;
            surfSettings.VisibleAbsorptance = VisibleAbsorptance;
            surfSettings.Thickness = Thickness;
            surfSettings.RadianceMaterial = RadianceMaterial;

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
                return Resources.Eddy_MRT_Surface_Settings;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{798709AF-F2CD-438E-84A8-1E6A9A346CA0}"); }
        }
    }
}