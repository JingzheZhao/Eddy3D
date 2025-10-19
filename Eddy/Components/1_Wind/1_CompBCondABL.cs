using Eddy.Properties;
using EddyLib;
using EddyLib.BCs;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class BCondABLComp : GH_Component
    {
        //List<double> defaultDir = new List<double>(0);
        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public BCondABLComp()
          : base("ABL Flow", "ABL Flow", @"Atmospheric Boundary Layer Flow Boundary Condition.

        Property     | Description
        wDir         | Wind directions to be simulated
        Uref         | Reference velocity at Zref [m/s]
        zref         | Reference height [m]
        z0           | Surface roughness height [m]
        zGround      | Minimum z-coordinate [m]
        EPW          | Weather data file path

" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
            //dirs.Add(0);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Wind Directions", "wDir", "Wind directions to be simulated", GH_ParamAccess.list);
            pManager.AddNumberParameter("Reference velocity at Zref [m/s]", "Uref", "Reference velocity at Zref [m/s]", GH_ParamAccess.list);
            pManager.AddNumberParameter("Reference height [m]", "zref", "Reference height[m]", GH_ParamAccess.list);
            pManager.AddNumberParameter("Surface roughness height [m]", "z0", "Surface roughness height [m]", GH_ParamAccess.list);
            pManager.AddNumberParameter("Minimum z-coordinate [m]", "zGround", "Minimum z - coordinate[m]", GH_ParamAccess.list);
            pManager.AddTextParameter("EPW", "EPW", "Weather data file path", GH_ParamAccess.item, "");
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Bcond", "Bcond", "Bcond", GH_ParamAccess.item);
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
            bool repeatedInputs = false;

            // Retrieve wind directions or set default
            List<int> windDirs = new List<int>();
            if (!DA.GetDataList(0, windDirs) || !windDirs.Any())
            {
                windDirs.Add(0); // Default wind direction
                repeatedInputs = true;
            }
            // Translate dirs > 359 into correct format
            windDirs = Utilities.NormalizeWindDirs(windDirs);

            // Initialize other parameters with defaults
            List<double> Uref = new List<double>(Enumerable.Repeat(5.0, windDirs.Count));
            List<double> zref = new List<double>(Enumerable.Repeat(10.0, windDirs.Count));
            List<double> z0 = new List<double>(Enumerable.Repeat(1.0, windDirs.Count));
            List<double> zGround = new List<double>(Enumerable.Repeat(0.0, windDirs.Count));
            string epwFilePath = "";

            // Retrieve other inputs and adjust if necessary

            EddyLib.BCs.BCHelpers.AdjustInputList(DA, 1, Uref, windDirs.Count, 5.0, ref repeatedInputs);
            EddyLib.BCs.BCHelpers.AdjustInputList(DA, 2, zref, windDirs.Count, 10, ref repeatedInputs);
            EddyLib.BCs.BCHelpers.AdjustInputList(DA, 3, z0, windDirs.Count, 1, ref repeatedInputs);
            EddyLib.BCs.BCHelpers.AdjustInputList(DA, 4, zGround, windDirs.Count, 0, ref repeatedInputs);

            DA.GetData(5, ref epwFilePath);

            BCCollection BCC = new BCCollection(epwFilePath);

            for (int w = 0; w < windDirs.Count; w++)
            {
                ABL newABL = new ABL(windDirs[w], Uref[w], zref[w], z0[w], zGround[w]);
                BCC.AddBoundaryCondition(newABL);
            }

            if (repeatedInputs)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "We assumed missing input values for at least one input based on the number of wind directions provided.");
            }

            if (epwFilePath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Without a weather file (.epw) connected to your BC you will not be able to perform outdoor comfort calculations.");
            }

            // Check if anything causes a 0 BC

            if (BCC.BCs.Where(v => v.epsilon == 0).Any() || BCC.BCs.Where(v => v.k == 0).Any() || BCC.BCs.Where(v => v.omega == 0).Any())
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something is causing a turbulence boundary condition to be 0, please change the setup of the simulation domain."); return;
            }

            if (epwFilePath != "" && windDirs.Count > 0)
            {
                if (BCC.WindDirOffSetAverage >= 13)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The average angle offset between simulated wind directions and the weather file is " + Math.Round(BCC.WindDirOffSetAverage, 2) + "°.\n You might want to consider changing the input wind directions to better fit the weather file.");
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The average angle offset between simulated wind directions and the weather file is " + Math.Round(BCC.WindDirOffSetAverage, 2) + "°.");
                }
            }

            // Create a dataframe-like structure
            var formattedSummary = BCHelpers.BuildTable(windDirs, Uref, zref, z0, zGround, epwFilePath);

            // Set the description of the output parameter
            Params.Output[0].Description = formattedSummary.ToString();

            DA.SetData(0, BCC);
        }

        // hidden parameter
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_abl;// return null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{820494F3-2858-4CB3-8E26-E11256EDAE35}");
    }
}