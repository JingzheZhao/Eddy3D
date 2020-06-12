using Eddy.Properties;
using EddyLib;
using EddyLib.BCs;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

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
          : base("ABL Flow", "ABL Flow", @"Atmospheric Boundary Layer Flow Boundary Condition

        Property     | Description
        Uref         | Reference velocity [m/s]
        Zref         | Reference height [m]
        z0           | Surface roughness height [m]
        zGround      | Minimum z-coordinate [m]

" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Setup")
        {
            //dirs.Add(0);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Wind Directions", "wDir", "Wind directions to be simulated", GH_ParamAccess.list);
            pManager.AddNumberParameter("Reference velocity at Zref [m/s]", "Uref", "Reference velocity at Zref [m/s]", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("Reference height [m]", "zref", "Reference height[m]", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Surface roughness height [m]", "z0", "Surface roughness height [m]", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("Minimum z-coordinate [m]", "zGround", "Minimum z - coordinate[m]", GH_ParamAccess.item, 0);
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
            //windDir.Add(0);

            List<int> windDir = new List<int>();
            List<Vector3d> flowDir = new List<Vector3d>();
            double Uref = 0;
            double zref = 0;
            double z0 = 0;
            double zGround = 0;

            DA.GetDataList(0, windDir);
            DA.GetData(1, ref Uref);
            DA.GetData(2, ref zref);
            DA.GetData(3, ref z0);
            DA.GetData(4, ref zGround);
            string epwFilePath = "";
            DA.GetData(5, ref epwFilePath);

            if (epwFilePath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Without a weather file (.epw) connected to your BC you will not be able to perform outdoor comfort calculations.");
            }

            // Translate dirs > 359 into correct format
            windDir = Utilities.NormalizeWindDirs(windDir);

            BoundaryCondition BCInflow = new ABL(windDir, Uref, zref, z0, zGround, epwFilePath);

            // Check if anything causes a 0 BC

            if (BCInflow.epsilon == 0 || BCInflow.k == 0 || BCInflow.omega == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Something is causing a turbulence boundary condition to be 0, please change the setup of the simulation domain."); return;
            }

            if (epwFilePath != "" && windDir.Count > 0)
            {
                if (BCInflow.WindDirOffSetAverage >= 13)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The average angle offset between simulated wind directions and the weather file is " + Math.Round(BCInflow.WindDirOffSetAverage, 2) + "°.\n You might want to consider changing the input wind directions to better fit the weather file.");
                }
                else
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "The average angle offset between simulated wind directions and the weather file is " + Math.Round(BCInflow.WindDirOffSetAverage, 2) + "°.");
                }
            }

            DA.SetData(0, BCInflow);
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