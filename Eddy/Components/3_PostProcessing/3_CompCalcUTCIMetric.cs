using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompUTCI : GH_Component
    {
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public CompUTCI()
          : base("UTCI", "UTCI",
@"UTCI Calculation

Computes the Universal Thermal Climate Index (UTCI), a measure of how the weather ""feels"" to the human body.

Combines:
- Air Temperature (-50°C to +50°C)
- Mean Radiant Temperature (MRT)
- Wind Speed (0.5-17 m/s)
- Relative Humidity

Inputs Tair, Wind, and RH each accept a list of N hourly values. MRT accepts a list of M probe values.
Output: flat list of M values — per-probe UTCI averaged across all N hours.

" + EddyVersion.toString(),
              EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter(
                "Air Temperature", "Tair",
                "Ambient air temperature per hour. Units: °C. Valid: -50 to +50°C",
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                "Mean Radiant Temp", "MRT",
                "Mean radiant temperature per probe. Units: °C",
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                "Wind Speed", "Wind",
                "Wind velocity per hour at pedestrian height (1.5m). Units: m/s. Valid: 0.5-17 m/s",
                GH_ParamAccess.list);

            pManager.AddNumberParameter(
                "Relative Humidity", "RH",
                "Relative humidity per hour. Units: % (0-100)",
                GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("UTCI", "UTCI", "Per-probe average UTCI across all input hours. Units: °C equivalent temperature", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var tairList = new List<double>();
            var mrtList  = new List<double>();
            var windList = new List<double>();
            var rhList   = new List<double>();

            if (!DA.GetDataList("Air Temperature", tairList)) return;
            if (!DA.GetDataList("Mean Radiant Temp", mrtList)) return;
            if (!DA.GetDataList("Wind Speed", windList)) return;
            if (!DA.GetDataList("Relative Humidity", rhList)) return;

            if (tairList == null || tairList.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tair list is null or empty."); return; }
            if (windList == null || windList.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wind list is null or empty."); return; }
            if (rhList   == null || rhList.Count   == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "RH list is null or empty.");   return; }
            if (mrtList  == null || mrtList.Count  == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "MRT list is null or empty.");  return; }

            int n = tairList.Count;
            int m = mrtList.Count;

            if (windList.Count != n || rhList.Count != n)
            {
                var mismatches = new System.Text.StringBuilder("Tair, Wind, and RH must all have the same number of values (N hours). Lengths received: ");
                mismatches.Append($"Tair={n}, Wind={windList.Count}, RH={rhList.Count}.");
                if (windList.Count != n) mismatches.Append(" Wind length does not match Tair.");
                if (rhList.Count   != n) mismatches.Append(" RH length does not match Tair.");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, mismatches.ToString());
                return;
            }

            // Accumulate UTCI sum per probe across all hours, then average.
            var utciSums = new double[m];
            bool anyOutOfBounds = false;

            for (int h = 0; h < n; h++)
            {
                double tair = tairList[h];
                double wind = windList[h];
                double rh   = rhList[h];

                for (int p = 0; p < m; p++)
                {
                    double utci = EddyLib.UTCI.CalcUTCICorrectBounds(tair, rh, wind, mrtList[p], out bool outOfBounds);
                    if (outOfBounds) anyOutOfBounds = true;
                    utciSums[p] += utci;
                }
            }

            if (anyOutOfBounds)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some input values were outside UTCI accepted bounds and were clamped.");

            var result = new List<double>(m);
            for (int p = 0; p < m; p++)
                result.Add(utciSums[p] / n);

            DA.SetDataList(0, result);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_calUTCI;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{7D6AE002-2B70-4A79-BD4F-4405A0BD66A4}");
    }
}