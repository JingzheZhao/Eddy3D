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
          : base("Surface Settings", "SurfSet", "Surface settings " + EddyVersion.toString(), EddyVersion.Name, "2 | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Name", "Name", "Material Name", GH_ParamAccess.item, "MySurface");
            pManager.AddNumberParameter("Thickness", "Thick", "Material Thickness [m]", GH_ParamAccess.item, 0.1);

            //pManager.AddIntegerParameter("Roughness", "Rhn", "Roughness [0 VeryRough,1 Rough,2 MediumRough,3 MediumSmooth,4 Smooth,5 VerySmooth]", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Conductivity", "Con", "Material Conductivity [W/(m-K)]", GH_ParamAccess.item, 2.3);
            pManager.AddNumberParameter("Density", "Den", "Material Density [kg/m3]", GH_ParamAccess.item, 2400);
            pManager.AddNumberParameter("SpecificHeat", "Sph", "Material SpecificHeat [J/(kg-K)]", GH_ParamAccess.item, 840);
            pManager.AddNumberParameter("Thermal absorptance", "Tabs", "Material thermal absorptance", GH_ParamAccess.item, 0.9);
            pManager.AddNumberParameter("Solar absorptance", "Sabs", "Material solar absorptance", GH_ParamAccess.item, 0.7);
            pManager.AddNumberParameter("Visible absorptance", "Vabs", "Material visible absorptance", GH_ParamAccess.item, 0.7);

            pManager.AddTextParameter("Surface", "SMat", "Optional Radiance Surface Material", GH_ParamAccess.item, "");

            //Param_Integer param = pManager[1] as Param_Integer;
            //param.AddNamedValue("VeryRough", 0);
            //param.AddNamedValue("Rough", 1);
            //param.AddNamedValue("MediumRough", 2);
            //param.AddNamedValue("MediumSmooth", 3);
            //param.AddNamedValue("Smooth", 4);
            //param.AddNamedValue("VerySmooth", 5);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.Register_GenericParam("SurfSet", "Set", "Surface settings");
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