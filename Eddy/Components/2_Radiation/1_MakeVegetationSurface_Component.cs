using Eddy.Properties;
using EddyLib;
using Eddy.Analytics;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Eddy.Components.Radiation
{
    public class MakeVegetationSurface_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Initializes a new instance of the MakeRadiationMesh_Component class.
        /// </summary>
        public MakeVegetationSurface_Component()
          : base("Vegetation Surface", "VegSrf",
@"Create grass/lawn surfaces for MRT simulation.

Models low vegetation with evapotranspiration cooling.
Surface temperatures are typically lower than paved surfaces.

" + EddyVersion.toString(),
              EddyVersion.Name, "2 | Radiation")
        {
            Analytics.Analytics.TrackComponentView("MRTVegetationSurface");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "Brep", "Vegetation surface geometry as Brep(s).", GH_ParamAccess.list);
            pManager.AddNumberParameter("Patch Size", "Patch", "Mesh subdivision size. Units: m. Default: 3", GH_ParamAccess.item, 3);
            pManager.AddTextParameter("Settings", "Set", "Optional: Radiance material settings.", GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddIntegerParameter("Temperature Type", "Type", "Surface temperature calculation method.", GH_ParamAccess.item, 1);
            var types = Enum.GetNames(typeof(SimulationType));
            Param_Integer param = pManager[3] as Param_Integer;
            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }
            pManager.AddNumberParameter("Temperature", "Temp", "Override surface temperature. Units: °C", GH_ParamAccess.list);
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Surfaces", "Srf", "Vegetation surfaces for MRT Simulation component", GH_ParamAccess.list);
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
            //if (!DA.GetData(2, ref mat)) return;

            //RadiationSurfaceType thetype = RadiationSurfaceType.Vegetation;
            //if (String.IsNullOrWhiteSpace(mat))
            //{
            //    if (thetype == RadiationSurfaceType.Ground) { mat = RadianceMaterials.DefaultGround; }
            //    else if (thetype == RadiationSurfaceType.Building) { mat = RadianceMaterials.DefaultFacade; }
            //    else if (thetype == RadiationSurfaceType.Vegetation) { mat = RadianceMaterials.DefaultGrass; }
            //    else if (thetype == RadiationSurfaceType.Tree) { mat = RadianceMaterials.DefaultTree; }
            //}

            string settingsInput = "";
            VegetationSurface_Settings settings = null;

            if (!DA.GetData(2, ref settingsInput)) { }

            if (!String.IsNullOrWhiteSpace(settingsInput))
            {
                try
                {
                    settings = VegetationSurface_Settings.fromJSON(settingsInput);
                }
                catch
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Settings provided cannot be cast into the correct format. Are you sure you are passing the correct input?");
                    settings = null;
                    return;
                }
            }
            if (settings == null)
            {
                settings = new VegetationSurface_Settings();
            }
            if (String.IsNullOrWhiteSpace(settings.RadianceMaterial))
            {
                settings.RadianceMaterial = RadianceMaterials.DefaultGrass;
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
                var rs = new RSurface("vegetation", b, RadiationSurfaceType.Vegetation, simsim, settings, patchSize);
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
                return Resources.Eddy_MRT_Vegetation;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{192A5188-B15F-473C-9B63-D73A6B91A91E}"); }
        }
    }
}
