using Eddy.Properties;
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
    public class MakeVegetationSurface_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Initializes a new instance of the MakeRadiationMesh_Component class.
        /// </summary>
        public MakeVegetationSurface_Component()
          : base("Vegetation Surface", "VegSurf", "Vegetation Simulation Surface" + EddyVersion.toString(), EddyVersion.Name, "2 | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Radiation surface", GH_ParamAccess.list);
            pManager.AddNumberParameter("Patch", "Ps", "Patch size", GH_ParamAccess.item, 3);
            pManager.AddTextParameter("Material", "M", "Optional Radiance Material", GH_ParamAccess.item, "");

            pManager.AddIntegerParameter("SimType", "Sts", "Surface Temparature Simulation Type", GH_ParamAccess.item, 1);
            var types = Enum.GetNames(typeof(SimulationType));
            Param_Integer param = pManager[3] as Param_Integer;
            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }
            pManager.AddNumberParameter("Temp", "Temp", "Surface Temparature Input", GH_ParamAccess.list);
            pManager[4].Optional = true;
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
            double patchSize = 2;
            string mat = "";

            if (!DA.GetDataList(0, breps)) return;
            if (!DA.GetData(1, ref patchSize)) return;
            if (!DA.GetData(2, ref mat)) return;

            RadiationSurfaceType thetype = RadiationSurfaceType.Vegetation;
            if (String.IsNullOrWhiteSpace(mat))
            {
                if (thetype == RadiationSurfaceType.Ground) { mat = RadianceMaterial.DefaultGround; }
                else if (thetype == RadiationSurfaceType.Building) { mat = RadianceMaterial.DefaultFacade; }
                else if (thetype == RadiationSurfaceType.Vegetation) { mat = RadianceMaterial.DefaultGrass; }
                else if (thetype == RadiationSurfaceType.Tree) { mat = RadianceMaterial.DefaultTree; }
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
                var rs = new RSurface("vegetation", b, thetype, simsim, mat, patchSize);
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