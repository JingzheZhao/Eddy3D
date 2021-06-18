using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace Eddy.Components.Radiation
{
    public class UTCI_ConditionOfPerson_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.obscure | GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public UTCI_ConditionOfPerson_Component()
          : base("UTCI Rating", "UTCI", "UTCI condition of person rating" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("UTCI", "UTCI", "UTCI Temperature [°C]", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Rating", "Cond", @"Condition of person rating:
< -40 = -5(extreme cold stress)
- 40 to - 27 = -4(very strong cold stress)
- 27 to - 13 = -3(strong cold stress)
- 13 to 0 = -2(moderate cold stress)
0 to 9 = -1(slight cold stress)
9 to 26 = 0(no thermal stress)
26 to 28 = 1(slight heat stress)
28 to 32 = 2(moderate heat stress)
32 to 38 = 3(strong heat stress)
38 to 46 = 4(very strong heat stress)
> 46 = 5(extreme heat stress)", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<int> rating = new List<int>();

            List<double> data = new List<double>();
            DA.GetDataList(0, data);

            foreach (var d in data)
            {
                rating.Add(UTCI.CalcConditionOfPerson(d));
            }

            DA.SetDataList(0, rating);
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
                return Resources.Eddy_UTCI_Rating;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{5353A1D7-8D77-4028-811A-D89AC1150B48}"); }
        }
    }
}