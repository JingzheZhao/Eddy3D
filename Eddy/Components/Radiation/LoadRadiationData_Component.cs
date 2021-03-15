using EddyLib;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Eddy.Components.Radiation
{
    public class LoadRadiationData_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public LoadRadiationData_Component()
          : base("Load Radiation", "LRad", "Load radiation data" + EddyVersion.toString(), EddyVersion.Name, "X | Radiation")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Path", "P", "Result path", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Hour", "H", "Hour", GH_ParamAccess.item, 12);
            pManager.AddBooleanParameter("Load", "L", "Load data from disk", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Result object", GH_ParamAccess.item);
            pManager.AddMeshParameter("Meshes", "M", "Analysis meshes", GH_ParamAccess.list);
            pManager.AddGenericParameter("Probes", "P", "Analysis probes", GH_ParamAccess.list);

            pManager.AddNumberParameter("Hour", "H", "Data for each mesh vertex for the selected hour", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string filePath = "";
            int hour = 0;
            bool run = false;

            DA.GetData(0, ref filePath);
            DA.GetData(1, ref hour);
            DA.GetData(2, ref run);


            if (!run) return;


            if (!File.Exists(filePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Result file not found.");
            }

            var prep = PrepareProtoBufSingleton.Instance;

            MRTSimulationResultProto resultProto = null;

            try
            {
                Stopwatch sp = new Stopwatch();
                sp.Restart();
                resultProto = MRTSimulationResultProto.ReadFromFile(filePath);
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
                if (resultProto.Meshes != null)
                {
                    DA.SetDataList(1, resultProto.Meshes.Select(x => x.Value));
                }
                DA.SetDataList(2, resultProto.Probes);
                if (resultProto.Probes != null)
                {
                    DA.SetDataList(3, resultProto.Probes.Select(x => x.TotalRad[hour]));
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
                return null;
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