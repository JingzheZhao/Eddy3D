using EddyLib;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;

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

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Result object", GH_ParamAccess.item);
            pManager.AddMeshParameter("Meshes", "M", "Analysis meshes", GH_ParamAccess.list);
            pManager.AddNumberParameter("Hour", "H", "Data for each mesh vertex for the selected hour", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

             string workDir = "";
            int hour = 0;

            DA.GetData(0, ref workDir);
            DA.GetData(1, ref hour);

            if (!File.Exists(workDir)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Result file not found.");
            }

            RadiationSimulationDDSResult result = null;

            try
            {
                result = RadiationSimulationDDSResult.FromBson(File.ReadAllText(workDir));
            }
            catch(Exception e) { 
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Result file could not be deserialized. Are you loading a wrong file type? " +Environment.NewLine + e.Message);
                return;

            }

            if (result != null)
            {
                DA.SetData(0, result);
                DA.SetDataList(1, result.AnalysisMeshes);
                DA.SetDataList(2, result.TotalRad[hour]);

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