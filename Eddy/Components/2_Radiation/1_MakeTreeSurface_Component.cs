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
    public class MakeTreeSurface_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Initializes a new instance of the MakeRadiationMesh_Component class.
        /// </summary>
        public MakeTreeSurface_Component()
          : base("Tree Surface", "TreeSrf", 
@"Tree Canopies

Converts tree geometries for radiation analysis. Simulates shading and evapotranspiration cooling effects.

" + EddyVersion.toString(), 
              EddyVersion.Name, "2 | Radiation")
        {
            Analytics.Analytics.TrackComponentView("MRTTreeSurface");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "Brep", "Tree canopy geometry as Brep(s).", GH_ParamAccess.list);
            pManager.AddNumberParameter("Patch Size", "Patch", "Mesh subdivision size. Units: m. Default: 3", GH_ParamAccess.item, 3);
            pManager.AddGenericParameter("Settings", "Set", "Optional: Material settings from Tree Settings component.", GH_ParamAccess.item);
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
            pManager.AddGenericParameter("Surfaces", "Srf", "Tree surfaces for MRT Simulation component", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var breps = new List<Brep>();
            double patchSize = 2;
            //string mat = "";

            if (!DA.GetDataList(0, breps)) return;
            if (!DA.GetData(1, ref patchSize)) return;
            //if (!DA.GetData(2, ref mat)) return;

            //RadiationSurfaceType thetype = RadiationSurfaceType.Tree;
            //if (String.IsNullOrWhiteSpace(mat))
            //{
            //    if (thetype == RadiationSurfaceType.Ground) { mat = RadianceMaterials.DefaultGround; }
            //    else if (thetype == RadiationSurfaceType.Building) { mat = RadianceMaterials.DefaultFacade; }
            //    else if (thetype == RadiationSurfaceType.Vegetation) { mat = RadianceMaterials.DefaultGrass; }
            //    else if (thetype == RadiationSurfaceType.Tree) { mat = RadianceMaterials.DefaultTree; }
            //}

            IGH_Goo goo_settings = null;
            if (!DA.GetData(2, ref goo_settings)) { }
            Tree_Settings settings = null;
            if (goo_settings != null)
            {
                if (!goo_settings.CastTo<Tree_Settings>(out settings))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Settings provided cannot be cast into the correct format. Are you sure you are passing the correct input?");
                    return;
                }
            }
            if (settings == null)
            {
                settings = Tree_Settings.GenerateTree();
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
                var rs = new RSurface("tree", b, RadiationSurfaceType.Tree, simsim, settings, patchSize);
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
                return Resources.Eddy_MRT_Tree;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{EE662406-5CF2-40E3-B128-A751C66247EF}"); }
        }
    }
}
