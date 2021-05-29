using Eddy.Properties;
using EddyLib;
using EddyLib.OutdoorComfort;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Eddy.Components.Radiation
{
    public class WindFactorsTemportal_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public WindFactorsTemportal_Component()
          : base("WindFactorsTemporal", "WindFactorsTemporal", "WindFactorsTemporal" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Res", "Res", "Res", GH_ParamAccess.item);

            pManager.AddTextParameter("EPW", "EPW", "EPW", GH_ParamAccess.item, "");

            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Result object containing probes, polygons and result data", GH_ParamAccess.item);

            pManager.AddGenericParameter("Probes", "Prb", "Analysis probes", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string EPW = "";
            bool run = false;

            DA.GetData(1, ref EPW);
            DA.GetData(2, ref run);

            if (!run) return;

            IGH_Goo system = null;
            if (!DA.GetData(0, ref system)) { }

            WProbeResultProto res;
            if (system == null) return;
            if (!system.CastTo<WProbeResultProto>(out res)) return;

            // WindFactorSpatial

            Weather w = new Weather(EPW);

            WindSystem WS = new WindSystem(w, res.Probes[0].WindDirections);

            foreach (var p in res.Probes)
            {
                p.WindFactorsTemporal = EddyLib.OutdoorComfort.WindFactorsTemporal.CalcWindFactorsTemporalSP(w, p, WS, true);
            }

            ///////////////////

            if (res != null)
            {
                DA.SetData(0, res);

                DA.SetDataList(1, res.Probes);
            }
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
                return Resources.Eddy_CFD_LoadResults;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{7BF96608-D018-4F30-A5B3-F7F6D459D26D}"); }
        }
    }
}