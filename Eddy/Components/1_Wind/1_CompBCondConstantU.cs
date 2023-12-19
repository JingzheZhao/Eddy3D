using Eddy.Properties;
using EddyLib;
using EddyLib.BCs;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class BCondConstU : GH_Component
    {
        //readonly List<double> defaultDir = new List<double>(0);

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public BCondConstU()
          : base("Uniform Flow", "Uniform Flow", @"Uniform Flow Boundary Condition

        Property     | Description
        Uref         | Reference velocity [m/s]
        Zref         | Reference height [m]
        z0           | Surface roughness height [m]

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
            pManager.AddNumberParameter("Reference velocity [m/s]", "Uref", "Reference velocity [m/s]", GH_ParamAccess.list);

            pManager.AddNumberParameter("Surface roughness height [m]", "z0", "Surface roughness height [m]", GH_ParamAccess.item, 1);

            pManager.AddTextParameter("Epw", "Epw", "Weather data file path", GH_ParamAccess.item, "");

            pManager[0].Optional = true;
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

            List<int> windDir = new List<int>();
            List<double> Uref = Enumerable.Repeat(5.0, windDir.Count).ToList();
            List<double> z0 = Enumerable.Repeat(1.0, windDir.Count).ToList();

            string epwFilePath = "";

            DA.GetDataList(0, windDir);
            DA.GetDataList(1, Uref);
            DA.GetDataList(2, z0);
            DA.GetData(3, ref epwFilePath);

            int numberOfWindDirections = windDir.Count();

            if (numberOfWindDirections > 0)
            {
                if (Uref.Count == 1)
                {
                    Uref = Enumerable.Repeat(Uref[0], windDir.Count).ToList();
                }

                if (z0.Count == 1)
                {
                    z0 = Enumerable.Repeat(z0[0], windDir.Count).ToList();
                }
            }

            // Translate dirs > 359 into correct format
            windDir = Utilities.NormalizeWindDirs(windDir);

            BCCollection BCC = new BCCollection(windDir, epwFilePath);

            for (int w = 0; w < windDir.Count; w++)
            {
                BCC.BCs.Add(new ConstU(windDir[w], Uref[w], z0[w], epwFilePath));
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

            if (epwFilePath != "" && windDir.Count > 0)
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

            DA.SetData(0, BCC);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resources.Eddy_castU;

                // return null;
            }
        }

        // hidden parameter
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{C6C39723-8EA9-4478-B1A6-9F2ABD9109A8}"); }
        }
    }
}