using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eddy.Components.Radiation
{
    public class AnalysisSystem_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public AnalysisSystem_Component()
          : base("AnalysisSystem", "AS", "AS" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")

        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("TimeDifference", "TD", "TD", GH_ParamAccess.list);

            //pManager.AddIntegerParameter("Hour", "H", "Hour", GH_ParamAccess.item, 12);
            //pManager.AddGenericParameter("T", "T", "T", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("AnalysisSystem", "AS", "AS", GH_ParamAccess.tree);

            //pManager.AddMeshParameter("Meshes", "M", "Analysis meshes", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var TS = new List<TimeDiff>();

            DA.GetDataList(0, TS);

            AnalysisSystem AS = new AnalysisSystem(TS);

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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.Eddy_stability;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{C42B171D-96CA-4275-BE2C-F11AD25226B7}"); }
        }
    }
}