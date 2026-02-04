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
              GH_Strings.RunSettings.Name, 
              GH_Strings.RunSettings.Nick, 
              GH_Strings.RunSettings.Desc + EddyVersion.toString(),
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
                GH_Strings.RunSettings.Iterations, GH_Strings.RunSettings.IterationsNick, 
                GH_Strings.RunSettings.IterationsDesc, 
                GH_ParamAccess.item, 1000);

            pManager.AddIntegerParameter(
                GH_Strings.RunSettings.WriteInterval, GH_Strings.RunSettings.WriteIntervalNick, 
                GH_Strings.RunSettings.WriteIntervalDesc, 
                GH_ParamAccess.item, 20);

            pManager.AddIntegerParameter(
                GH_Strings.RunSettings.Keep, GH_Strings.RunSettings.KeepNick, 
                GH_Strings.RunSettings.KeepDesc, 
                GH_ParamAccess.item, 3);

            pManager.AddIntegerParameter(
                GH_Strings.RunSettings.Turb, GH_Strings.RunSettings.TurbNick, 
                GH_Strings.RunSettings.TurbDesc, 
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
                GH_Strings.RunSettings.Relax, GH_Strings.RunSettings.RelaxNick, 
                GH_Strings.RunSettings.RelaxDesc, 
                GH_ParamAccess.item, 3);
            if (pManager[4] is Param_Integer relaxationFactors)
            {
                relaxationFactors.AddNamedValue("Fast (may diverge)", 0);
                relaxationFactors.AddNamedValue("Fluent-style", 1);
                relaxationFactors.AddNamedValue("Robust (stable)", 2);
                relaxationFactors.AddNamedValue("Optimized (recommended)", 3);
            }

            pManager.AddIntegerParameter(
                GH_Strings.RunSettings.Schemes, GH_Strings.RunSettings.SchemesNick, 
                GH_Strings.RunSettings.SchemesDesc, 
                GH_ParamAccess.item, 1);
            if (pManager[5] is Param_Integer simulationMode)
            {
                simulationMode.AddNamedValue("Default (OpenFOAM standard)", 0);
                simulationMode.AddNamedValue("Optimized (recommended)", 1);
            }

            pManager.AddBooleanParameter(
                GH_Strings.RunSettings.PotInit, GH_Strings.RunSettings.PotInitNick, 
                GH_Strings.RunSettings.PotInitDesc, 
                GH_ParamAccess.item, false);

            pManager.AddBooleanParameter(
                GH_Strings.RunSettings.AoA, GH_Strings.RunSettings.AoANick, 
                GH_Strings.RunSettings.AoADesc, 
                GH_ParamAccess.item, false);

            pManager.AddIntegerParameter(
                GH_Strings.RunSettings.CPUs, GH_Strings.RunSettings.CPUsNick, 
                GH_Strings.RunSettings.CPUsDesc, 
                GH_ParamAccess.item, 1);

            pManager.AddIntegerParameter(
                GH_Strings.RunSettings.OS, GH_Strings.RunSettings.OSNick, 
                GH_Strings.RunSettings.OSDesc, 
                GH_ParamAccess.item, 0);
            if (pManager[9] is Param_Integer os)
            {
                os.AddNamedValue("Auto-detect", 0);
                os.AddNamedValue("Windows 7/8 (legacy)", 1);
                os.AddNamedValue("Windows 10/11", 2);
            }

            pManager[9].Optional = true;

            //10
            pManager.AddTextParameter(GH_Strings.RunSettings.BlueCFD, GH_Strings.RunSettings.BlueCFDNick, GH_Strings.RunSettings.BlueCFDDesc, GH_ParamAccess.item, "");
            pManager[10].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(
                GH_Strings.Common.RunSettings, GH_Strings.Common.RunSettingsNick, 
                GH_Strings.Common.RunSettingsDesc, 
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

            DA.GetData(GH_Strings.RunSettings.Iterations, ref iterations);
            DA.GetData(GH_Strings.RunSettings.WriteInterval, ref writeInterval);
            DA.GetData(GH_Strings.RunSettings.Keep, ref keepTimeSteps);
            DA.GetData(GH_Strings.RunSettings.Turb, ref turb);
            DA.GetData(GH_Strings.RunSettings.Relax, ref relaxIdx);
            DA.GetData(GH_Strings.RunSettings.Schemes, ref schemesIdx);
            DA.GetData(GH_Strings.RunSettings.PotInit, ref potentialFoamInit);
            DA.GetData(GH_Strings.RunSettings.AoA, ref aoa);
            DA.GetData(GH_Strings.RunSettings.CPUs, ref cpus);
            DA.GetData(GH_Strings.RunSettings.OS, ref osIdx);
            DA.GetData(GH_Strings.RunSettings.BlueCFD, ref blueCfdPath);

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

            DA.SetData(GH_Strings.Common.RunSettings, runSet);
        }
    }
}