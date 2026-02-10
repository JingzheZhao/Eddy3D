using Eddy.Properties;
using EddyLib;
using Eddy.Analytics;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Eddy.Components.Radiation
{
    public class MakeGroundSurface_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Initializes a new instance of the MakeRadiationMesh_Component class.
        /// </summary>
        public MakeGroundSurface_Component()
          : base("Ground Surface", "GndSrf", 
@"Ground Material

Defines properties for ground surfaces like asphalt, concrete, or soil. Critical for analyzing the Urban Heat Island effect.

" + EddyVersion.toString(), 
              EddyVersion.Name, "2 | Radiation")
        {
            Analytics.Analytics.TrackComponentView("MRTGroundSurface");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter(
                "Geometry", "Geo", 
                "Ground surface geometry (Breps). Will be meshed into analysis patches.", 
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                "Patch Size", "Patch", 
                "Size of analysis mesh patches. Units: meters. Default: 3m", 
                GH_ParamAccess.item, 3);

            pManager.AddGenericParameter(
                "Settings", "Set", 
                "Optional: Material settings (albedo, emissivity) from Surface Settings component.", 
                GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddIntegerParameter(
                "Temp Source", "Src", 
                "Surface temperature data source (EnergyPlus, measured, or user-defined).", 
                GH_ParamAccess.item, 1);
            var types = Enum.GetNames(typeof(SimulationType));
            Param_Integer param = pManager[3] as Param_Integer;
            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }

            pManager.AddNumberParameter(
                "Temperature", "Temp", 
                "Optional: User-defined surface temperatures. Units: °C", 
                GH_ParamAccess.list);
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Surface", "Srf", "Ground surface for MRT Simulation component", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var breps = new List<Brep>();
            double patchSize = 2;
            string mat = "";

            if (!DA.GetDataList(0, breps)) return;
            if (!DA.GetData(1, ref patchSize)) return;

            IGH_Goo goo_settings = null;
            if (!DA.GetData(2, ref goo_settings)) { }
            RSurface_Settings settings = null;
            if (goo_settings != null)
            {
                if (!goo_settings.CastTo<RSurface_Settings>(out settings))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Settings provided cannot be cast into the correct format. Are you sure you are passing the correct input?");
                    return;
                }
            }
            if (settings == null)
            {
                settings = RSurface_Settings.GenerateGround();
            }
            if (String.IsNullOrWhiteSpace(settings.RadianceMaterial))
            {
                settings.RadianceMaterial = RadianceMaterials.DefaultGround;
            }

            int simType = 0;
            if (!DA.GetData(3, ref simType)) return;
            SimulationType simsim = (SimulationType)simType;

            float[] toverride = new float[8760];
            List<double> temperatureOverride = new List<double>();
            if (DA.GetDataList(4, temperatureOverride))
            {
                if (temperatureOverride.Count < 8760 && temperatureOverride.Count > 0)
                {
                    int cnt = 0;
                    while (cnt < 8760)
                    {
                        for (int i = 0; i < temperatureOverride.Count; i++)
                        {
                            if (cnt >= 8760) { break; }
                            toverride[cnt] = (float)temperatureOverride[i];
                            cnt++;
                        }
                    }
                }
            }

            var RSurfs = new List<RSurface>();
            foreach (var b in breps)
            {
                var rs = new RSurface("surf", b, RadiationSurfaceType.Ground, simsim, settings, patchSize);
                if (simsim == SimulationType.TemperatureInput)
                {
                    rs.TemperatureOverride = toverride;
                }
                RSurfs.Add(rs);
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
                return Resources.Eddy_MRT_Ground;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{C3180228-B5A4-4459-9F08-9256C28495AC}"); }
        }
    }
}
