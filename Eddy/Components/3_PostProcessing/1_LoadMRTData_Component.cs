using Eddy.Properties;
using EddyLib;
using Eddy.Analytics;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using System;
using System.Diagnostics;
using System.IO;

namespace Eddy.Components.Radiation
{
    public class LoadMRTData_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public LoadMRTData_Component()
          : base("Load MRT Results", "LoadMRT",
@"MRT Results Loader

Import calculated Mean Radiant Temperature (MRT) data for thermal comfort analysis.

" + EddyVersion.toString(),
              EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                "File Path", "Path",
                "Path to .mrt.eddy result file from MRT Simulation.",
                GH_ParamAccess.item);

            pManager.AddBooleanParameter(
                "Load", "Load!",
                "Set True to load data from disk into memory.",
                GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "MRT result object for Inspect components", GH_ParamAccess.item);
            pManager.AddGenericParameter("Probes", "Prb", "Analysis probe locations with data", GH_ParamAccess.list);
            pManager.AddGenericParameter("Polygons", "Poly", "Analysis polygons with spatial data", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string filePath = "";
            //int hour = 0;
            bool run = false;

            DA.GetData(0, ref filePath);
            //DA.GetData(1, ref hour);
            DA.GetData(1, ref run);

            if (!run) return;

            if (!File.Exists(filePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Result file not found.");
            }

            var prep = PrepareProtoBufSingleton.Instance;

            MRT_Simulation_ResultProto resultProto = null;

            try
            {
                Stopwatch sp = new Stopwatch();
                sp.Restart();
                resultProto = MRT_Simulation_ResultProto.ReadFromFile(filePath);
                sp.Stop();
                Debug.WriteLine("Loading RadiationSimulationResultProto: " + sp.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Result file could not be deserialized. Are you loading a wrong file type? " + Environment.NewLine + e.Message);
                return;
            }

            if (resultProto != null)
            {
                DA.SetData(0, resultProto);

                DA.SetDataList(1, resultProto.Probes);
                DA.SetDataList(2, resultProto.Polys);

                //if (resultProto.Meshes != null)
                //{
                //    DA.SetDataList(3, resultProto.Meshes.Select(x => x.Value));
                //}
                //if (resultProto.Probes != null)
                //{
                //    DA.SetDataList(3, resultProto.Probes.Select(x => x.TotalRad[hour]));
                //}
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
                return Resources.Eddy_MRT_LoadResults;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a0eba267-ad9c-4b6b-9e9a-366b8e8a1c94"); }
        }
    }
}
