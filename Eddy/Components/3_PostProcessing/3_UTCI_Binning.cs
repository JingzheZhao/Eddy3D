using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;

namespace Eddy.Components.Radiation
{
    public class UTCI_Binning_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.obscure | GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Initializes a new instance of the LoadRadiationData_Component class.
        /// </summary>
        public UTCI_Binning_Component()
          : base("UTCI Binning", "UTCI", @"Condition of person rating in bins. Order is as follows:
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
> 46 = 5(extreme heat stress)" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("UTCI Condition", "UC", "UTCI Condition [-]", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Bins", "Bins", @"Condition of person rating in bins.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Comfortable Hours", "CH", @"Comfortable Hours [%] (No Stress Condition).", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> Bins = new List<double>();
            List<double> ComfortableHours = new List<double>();

            List<double> data = new List<double>();
            DA.GetDataList(0, data);

            double ExtrCold = 0;
            double VryStrngCold = 0;
            double StrngCold = 0;
            double MdrtCold = 0;
            double SlgtCold = 0;
            double NoStress = 0;
            double SlgtHeat = 0;
            double MdrtHeat = 0;
            double StrngHeat = 0;
            double VryStrngHeat = 0;
            double ExtrHeat = 0;

            UTCI.Binning(data,
             ref ExtrCold,
             ref VryStrngCold,
             ref StrngCold,
             ref MdrtCold,
             ref SlgtCold,
             ref NoStress,
             ref SlgtHeat,
             ref MdrtHeat,
             ref StrngHeat,
             ref VryStrngHeat,
             ref ExtrHeat);

            Bins.Add(ExtrCold);
            Bins.Add(VryStrngCold);
            Bins.Add(StrngCold);
            Bins.Add(MdrtCold);
            Bins.Add(SlgtCold);
            Bins.Add(NoStress);
            Bins.Add(SlgtHeat);
            Bins.Add(MdrtHeat);
            Bins.Add(StrngHeat);
            Bins.Add(VryStrngHeat);
            Bins.Add(ExtrHeat);

            ComfortableHours.Add(NoStress);

            DA.SetDataList(0, Bins);
            DA.SetDataList(1, ComfortableHours);
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
            get { return new Guid("{F03BF184-DF83-443C-8440-A02D98A91653}"); }
        }
    }
}