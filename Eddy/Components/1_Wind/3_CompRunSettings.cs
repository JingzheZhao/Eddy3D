using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class RunSettings_Component : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.tertiary;

        /// <summary>
        /// Initializes a new instance of the RunSettings_Component class.
        /// </summary>
        public RunSettings_Component()
          : base(
              "Run Settings", 
              "RSet", 
              @"Configure CFD solver settings including iterations, turbulence model, and parallelization.

These settings control the OpenFOAM simpleFoam solver behavior.
Use higher iterations for complex geometries. Enable parallel for faster runs.

" + EddyVersion.toString(),
              EddyVersion.Name, 
              "1 | Wind")
        {
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{5898D6B7-6BDB-4A36-A0E8-FD0278D54A25}");

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.Eddy_run_settings;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter(
                "Iterations", "Iter", 
                "Maximum solver iterations. Higher = more accurate but slower. Typical: 500-2000. Default: 1000", 
                GH_ParamAccess.item, 1000);

            pManager.AddIntegerParameter(
                "Write Interval", "Write", 
                "Save results every N iterations. Lower = more disk space. Typical: 10-50. Default: 20", 
                GH_ParamAccess.item, 20);

            pManager.AddIntegerParameter(
                "Timesteps to Keep", "Keep", 
                "Number of saved timesteps to retain on disk. Older saves are deleted. Default: 3", 
                GH_ParamAccess.item, 3);

            pManager.AddIntegerParameter(
                "Turbulence Model", "Turb", 
                "RANS turbulence model. k-epsilon is fast and robust for urban flows. k-omega SST is more accurate near walls.", 
                GH_ParamAccess.item, 1);
            if (pManager[3] is Param_Integer turb)
            {
                turb.AddNamedValue("Laminar (no turbulence)", 0);
                turb.AddNamedValue("k-epsilon (fast, robust)", 1);
                turb.AddNamedValue("RNG k-epsilon (improved)", 2);
                turb.AddNamedValue("Realizable k-epsilon (accurate)", 3);
                turb.AddNamedValue("k-omega SST (best near walls)", 4);
            }

            pManager.AddIntegerParameter(
                "Relaxation Factors", "Relax", 
                "Under-relaxation for solver stability. Robust is safer for complex geometry. Default: Optimized", 
                GH_ParamAccess.item, 3);
            if (pManager[4] is Param_Integer relaxationFactors)
            {
                relaxationFactors.AddNamedValue("Fast (may diverge)", 0);
                relaxationFactors.AddNamedValue("Fluent-style", 1);
                relaxationFactors.AddNamedValue("Robust (stable)", 2);
                relaxationFactors.AddNamedValue("Optimized (recommended)", 3);
            }

            pManager.AddIntegerParameter(
                "Numerical Schemes", "Schemes", 
                "Discretization schemes for equations. Optimized balances accuracy and stability.", 
                GH_ParamAccess.item, 1);
            if (pManager[5] is Param_Integer simulationMode)
            {
                simulationMode.AddNamedValue("Default (OpenFOAM standard)", 0);
                simulationMode.AddNamedValue("Optimized (recommended)", 1);
            }

            pManager.AddBooleanParameter(
                "Potential Flow Init", "PotInit", 
                "Initialize with potentialFoam for faster convergence. Recommended for new simulations. Default: false", 
                GH_ParamAccess.item, false);

            pManager.AddBooleanParameter(
                "Age of Air", "AoA", 
                "Calculate mean age of air (ventilation effectiveness). Must be enabled before running simulation.", 
                GH_ParamAccess.item, false);

            pManager.AddIntegerParameter(
                "CPU Cores", "CPUs", 
                "Parallel processing cores. -1 = auto-detect. More cores = faster but needs more RAM. Default: 1", 
                GH_ParamAccess.item, 1);

            pManager.AddIntegerParameter(
                "Operating System", "OS", 
                "Target OS for simulation scripts. Auto-detect works in most cases.", 
                GH_ParamAccess.item, 0);
            if (pManager[9] is Param_Integer os)
            {
                os.AddNamedValue("Auto-detect", 0);
                os.AddNamedValue("Windows 7/8 (legacy)", 1);
                os.AddNamedValue("Windows 10/11", 2);
            }

            pManager[9].Optional = true;

            //10
            pManager.AddTextParameter("BlueCFD Path", "CFD", "Custom BlueCFD installation path. Default: C:\\Program Files\\blueCFD-Core-2020", GH_ParamAccess.item, "");
            pManager[10].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Run Settings", "RSet", 
                "Solver configuration object to connect to Wind Simulation component", 
                GH_ParamAccess.item);
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
            int iterations = 1000;
            int writeInterval = 10;
            int keepTimeSteps = 3;
            int turb = 0;
            int schemesIdx = 0;
            int cpus = 0;
            int osIdx = -1;
            int relaxIdx = 1;
            bool potentialFoamInit = false;
            bool aoa = false;
            string blueCfdPath = "";

            DA.GetData(0, ref iterations);
            DA.GetData(1, ref writeInterval);
            DA.GetData(2, ref keepTimeSteps);
            DA.GetData(3, ref turb);
            DA.GetData(4, ref relaxIdx);
            DA.GetData(5, ref schemesIdx);
            DA.GetData(6, ref potentialFoamInit);
            DA.GetData(7, ref aoa);
            DA.GetData(8, ref cpus);
            DA.GetData(9, ref osIdx);
            DA.GetData(10, ref blueCfdPath);

            if (!string.IsNullOrWhiteSpace(blueCfdPath))
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = blueCfdPath;
            }

            if (iterations < writeInterval) writeInterval = iterations;

            if (cpus > Environment.ProcessorCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system has only " + Environment.ProcessorCount + " CPUs, please lower the CPU count.");
            }

            RelaxationFactors relaxationFactors;
            switch (relaxIdx)
            {
                case 0: relaxationFactors = RelaxationFactors.Fast; break;
                case 1: relaxationFactors = RelaxationFactors.Fluent; break;
                case 2: relaxationFactors = RelaxationFactors.Robust; break;
                default: relaxationFactors = RelaxationFactors.Optimized; break;
            }

            fvSchemes schemes = schemesIdx == 0 ? fvSchemes.Default : fvSchemes.Optimized;

            string osName = Utilities.GetOSInfo();
            OSType osType;
            switch (osIdx)
            {
                case 1: osType = OSType.Windows7; break;
                case 2: osType = OSType.Windows10; break;
                case 3: osType = OSType.Linux; break;
                case 4: osType = OSType.MacOS; break;
                default: osType = (osName.Contains("Windows 7") || osName.Contains("Windows 8")) ? OSType.Windows7 : OSType.Windows10; break;
            }

            TurbModel turbModel;
            switch (turb)
            {
                case 0: turbModel = TurbModel.laminar; break;
                case 1: turbModel = TurbModel.kEpsilon; break;
                case 2: turbModel = TurbModel.RNGkEpsilon; break;
                case 3: turbModel = TurbModel.realizableKE; break;
                default: turbModel = TurbModel.kOmegaSST; break;
            }

            var runSet = new OFRunSettings()
            {
                iter = iterations,
                writeInterval = writeInterval,
                keepTimeSteps = keepTimeSteps,
                schemes = schemes,
                CPUs = cpus,
                ostype = osType,
                turbModel = turbModel,
                relaxationFactors = relaxationFactors,
                potentialFoamInit = potentialFoamInit,
                aoa_domain = aoa
            };

            DA.SetData("Run Settings", runSet);
        }
    }
}