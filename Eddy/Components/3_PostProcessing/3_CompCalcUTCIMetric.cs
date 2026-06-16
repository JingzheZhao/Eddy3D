using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
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

Inputs Tair, Wind, and RH each accept a list of N hourly values. MRT accepts a tree with M branches (one per probe), each containing N hourly values.
Outputs: Hourly UTCI (tree, same structure as MRT) and Averaged UTCI (flat list of M values, one per probe).

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
                "Mean radiant temperature as a tree: one branch per probe, N hourly values per branch. Units: °C",
                GH_ParamAccess.tree);

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
            pManager.AddNumberParameter("Hourly UTCI", "HourlyUTCI", "UTCI per hour per probe as a tree: one branch per probe, N hourly values per branch. Units: °C equivalent temperature", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Averaged UTCI", "AvgUTCI", "Per-probe average UTCI across all input hours. Units: °C equivalent temperature", GH_ParamAccess.list);
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
            var windList = new List<double>();
            var rhList   = new List<double>();
            var mrtTree  = new GH_Structure<GH_Number>();

            if (!DA.GetDataList("Air Temperature", tairList)) return;
            if (!DA.GetDataList("Wind Speed", windList)) return;
            if (!DA.GetDataList("Relative Humidity", rhList)) return;
            if (!DA.GetDataTree("Mean Radiant Temp", out mrtTree)) return;

            if (tairList == null || tairList.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Tair list is null or empty."); return; }
            if (windList == null || windList.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wind list is null or empty."); return; }
            if (rhList   == null || rhList.Count   == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "RH list is null or empty.");   return; }
            if (mrtTree  == null || mrtTree.PathCount == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "MRT tree is null or empty."); return; }

            int n = tairList.Count;
            int m = mrtTree.PathCount;

            if (windList.Count != n || rhList.Count != n)
            {
                var mismatches = new System.Text.StringBuilder("Tair, Wind, and RH must all have the same number of values (N hours). Lengths received: ");
                mismatches.Append($"Tair={n}, Wind={windList.Count}, RH={rhList.Count}.");
                if (windList.Count != n) mismatches.Append(" Wind length does not match Tair.");
                if (rhList.Count   != n) mismatches.Append(" RH length does not match Tair.");
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, mismatches.ToString());
                return;
            }

            var hourlyUtci = new GH_Structure<GH_Number>();
            var utciSums   = new double[m];
            bool anyOutOfBounds = false;

            for (int p = 0; p < m; p++)
            {
                var mrtBranch = mrtTree.Branches[p];
                if (mrtBranch.Count != n)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"MRT branch {p} has {mrtBranch.Count} values but expected {n} (one per hour).");
                    return;
                }

                var path = new GH_Path(p);
                for (int h = 0; h < n; h++)
                {
                    double utci = EddyLib.UTCI.CalcUTCICorrectBounds(tairList[h], rhList[h], windList[h], mrtBranch[h].Value, out bool outOfBounds);
                    if (outOfBounds) anyOutOfBounds = true;
                    hourlyUtci.Append(new GH_Number(utci), path);
                    utciSums[p] += utci;
                }
            }

            if (anyOutOfBounds)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Some input values were outside UTCI accepted bounds and were clamped.");

            var avgUtci = new List<double>(m);
            for (int p = 0; p < m; p++)
                avgUtci.Add(utciSums[p] / n);

            DA.SetDataTree(0, hourlyUtci);
            DA.SetDataList(1, avgUtci);
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