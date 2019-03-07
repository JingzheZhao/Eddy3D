using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using Eddy.Properties;
using EddyLib;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class RunSettings : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RunSettings()
          : base("Run Settings", "RSet",
              "Run Settings",
              "Eddy", "Settings")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Iterations", "Iter", "Specify the number of iterations to be simulated.", GH_ParamAccess.item, 1000);
            pManager.AddIntegerParameter("WriteInterval", "WriteInt", "Simulation write interval.", GH_ParamAccess.item, 20);
            pManager.AddIntegerParameter("KeepTimeSteps", "TimeSteps", "Number of time steps to keep in simulation folder..", GH_ParamAccess.item, 2);

            pManager.AddIntegerParameter("Turbulence", "Turb", "Turbulence model.", GH_ParamAccess.item, 0);
            Param_Integer turb = pManager[3] as Param_Integer;
            turb.AddNamedValue("kEpsilon (quick)", 0);
            turb.AddNamedValue("RNGkEpsilon (more accurate)", 1);
            turb.AddNamedValue("kOmegaSST (most accurate)", 2);

            pManager.AddIntegerParameter("Mode", "Mode", "Robustness of the solver", GH_ParamAccess.item, 0);
            Param_Integer simulationMode = pManager[4] as Param_Integer;
            simulationMode.AddNamedValue("quick", 0);
            simulationMode.AddNamedValue("robust", 1);
            simulationMode.AddNamedValue("orthogonal (70-80)", 2);
            simulationMode.AddNamedValue("orthogonal (60-70)", 3);
            simulationMode.AddNamedValue("orthogonal (40-60)", 4);
            simulationMode.AddNamedValue("accurate and stable", 5);
            simulationMode.AddNamedValue("more accurate but oscillatory", 6);
            simulationMode.AddNamedValue("robust but diffusive", 7);

            pManager.AddIntegerParameter("CPUs", "CPUs", "Number of CPUs. Set to -1 to set the number of CPUs for the simulation automatically.", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Settings", "Set", "Mesh Settings", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

    int _iter = 1000;
    int _writeInterval = 10;
    int _keepTimeSteps = 2;
    int _mode = 0;
    int _turb = 0;
    int _CPUs = 1;



        DA.GetData(0, ref _iter);
            DA.GetData(1, ref _writeInterval);
            DA.GetData(2, ref _keepTimeSteps);
            DA.GetData(3, ref _mode);
            DA.GetData(4, ref _turb);
            DA.GetData(5, ref _CPUs);


            //TODO: Handle SimEngine




            //Make sure that all fields are always written
            if (_iter < _writeInterval)
            {
                _writeInterval = _iter;
            }


            if (_CPUs > Environment.ProcessorCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that many CPUs.");
            }


            var os = OSType.Windows10;
            if (Utilities.IsWindows7) os = OSType.Windows7;


            DA.SetData(0, new OFRunSettings() {

            iter = _iter,
            writeInterval = _writeInterval,
            keepTimeSteps = _keepTimeSteps,
            mode = _mode,
            turb = _turb,
            CPUs = _CPUs,
            ostype = os
        });

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{5898D6B7-6BDB-4A36-A0E8-FD0278D54A25}"); }
        }
    }
}
