using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;

namespace Eddy.Components.Radiation
{
    public class AnalysisBins_System_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public AnalysisBins_System_Component()
          : base("Analysis System", "AS", "Analysis System" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("AnalysisBins", "AB", "AnalysisBins", GH_ParamAccess.list);

            //pManager.AddIntegerParameter("Hour", "H", "Hour", GH_ParamAccess.item, 12);
            //pManager.AddGenericParameter("T", "T", "T", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("AnalysisSystem", "AS", "AnalysisSystem", GH_ParamAccess.tree);

            //pManager.AddMeshParameter("Meshes", "M", "Analysis meshes", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var ABs = new List<AnalysisBins>();

            DA.GetDataList(0, ABs);

            AnalysisSystem AS = new AnalysisSystem(ABs);

            var hours = AS.AnalysisHourBins;

            GH_Structure<GH_Number> AS_Tree = new GH_Structure<GH_Number>();

            for (int i = 0; i < hours.GetLength(0); i++)
            {
                for (int j = 0; j < hours[i].GetLength(0); j++)
                {
                    AS_Tree.Append(new GH_Number(hours[i][j]), new GH_Path(i));
                }
            }

            DA.SetDataTree(0, AS_Tree);
        }

        protected override System.Drawing.Bitmap Icon =>

               // You can add image files to your project resources and access them like this:
               Resources.Eddy_AnalysisBins_System;

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{C42B171D-96CA-4275-BE2C-F11AD25226B7}"); }
        }
    }
}