using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Diagnostics;
using System.IO;

namespace Eddy.Components.Radiation
{
    public class LoadWProbeData_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public LoadWProbeData_Component()
          : base("Load Wind Results", "LoadWind", 
@"Wind Results Loader

Import raw results from a completed wind simulation for further analysis and visualization.

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
                "Path to .wind.eddy result file from Wind Simulation.", 
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
            pManager.AddGenericParameter("Result", "Res", "Wind result object for Inspect components", GH_ParamAccess.item);
            pManager.AddGenericParameter("Probes", "Prb", "Analysis probe locations with data", GH_ParamAccess.list);
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

            WProbeResultProto resultProto = null;

            try
            {
                Stopwatch sp = new Stopwatch();
                sp.Restart();
                resultProto = WProbeResultProto.ReadFromFile(filePath);
                sp.Stop();
                Debug.WriteLine("Loading WProbeResultProto: " + sp.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Result file could not be deserialized. Are you loading a wrong file type? " + Environment.NewLine + e.Message);
                return;
            }

            // WindFactorSpatial

            foreach (var p in resultProto.Probes)
            {
                p.WindFactorsSpatial = EddyLib.OutdoorComfort.WindFactorsSpatial.CalcWindFactorsSpatialSP(p);
            }

            ///////////////////

            if (resultProto != null)
            {
                DA.SetData(0, resultProto);

                DA.SetDataList(1, resultProto.Probes);
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
            get { return new Guid("{48B164A8-4DD4-4BED-8C8A-EB30861E59EF}"); }
        }
    }
}