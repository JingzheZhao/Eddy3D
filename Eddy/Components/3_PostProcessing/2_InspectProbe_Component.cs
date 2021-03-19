using Eddy.Properties;
using EddyLib;
using EddyLib.Radiation;
using EddyLib.UI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy.Components.Radiation
{
    public class InspectProbe_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }


        /// <summary>
        /// Initializes a new instance of the ThermalSystem_Component class.
        /// </summary>
        public InspectProbe_Component()
          : base("InspectSensor", "InSen", "Inspect sensor " + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sensor", "Sen", "Radiation Simulation Sensor", GH_ParamAccess.item);

            pManager.AddIntegerParameter("Metric", "Met", "Metric", GH_ParamAccess.item, 0);
            var types = Enum.GetNames(typeof(RProbeMetric));
            Param_Integer param = pManager[1] as Param_Integer;
            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }
            pManager.AddIntegerParameter("Hour", "H", "Hour", GH_ParamAccess.item, 0);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "Pt", "Point", GH_ParamAccess.item);

            pManager.AddNumberParameter("Data", "Data", "Data", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int _metricI = 0;
            if (!DA.GetData(1, ref _metricI)) return;
            RProbeMetric metric = (RProbeMetric)_metricI;

            int h = 0;
            if (!DA.GetData(2, ref h)) return;



            IGH_Goo gooProbe = null;
            if (!DA.GetData(0, ref gooProbe)) { }
            RProbe rprobe = null;
            WProbe wprobe = null;
            if (gooProbe != null)
            {
                if (gooProbe.CastTo<RProbe>(out rprobe)) { }
                else if (gooProbe.CastTo<WProbe>(out wprobe)) { }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Probe provided cannot be cast into the correct format. Are you sure you are passing the correct input?");
                    return;
                }
            }
            if (rprobe != null)
            {
                DA.SetData(0, rprobe.Point.Value);

                if (metric == RProbeMetric.MRT)
                {
                    // var mrt = rprobe.LongWave_MRT.Zip(rprobe.SolarGain_dMRT, (a, b) => a + b);
                    DA.SetData(1, rprobe.LongWave_MRT[h] + rprobe.SolarGain_dMRT[h]);
                }
                else if (metric == RProbeMetric.UTCI)
                {
                    DA.SetData(1, rprobe.UTCI[h]);
                }
                else if (metric == RProbeMetric.DirRad)
                {
                    DA.SetData(1, rprobe.DirRad[h]);
                }
                else if (metric == RProbeMetric.TotalRad)
                {
                    DA.SetData(1, rprobe.TotalRad[h]);
                }
                else if (metric == RProbeMetric.dMRT)
                {
                    DA.SetData(1, rprobe.SolarGain_dMRT[h]);
                }
                else if (metric == RProbeMetric.lwMRT)
                {
                    DA.SetData(1, rprobe.LongWave_MRT[h]);
                }
                else if (metric == RProbeMetric.WindSpeed)
                {
                    DA.SetData(1, rprobe.WindSpeed[h]);
                }
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
                return Resources.Eddy_Sensor_Inspect;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{FF0BDCD5-D39E-4943-83E5-25C8B0D24034}"); }
        }

    }
}