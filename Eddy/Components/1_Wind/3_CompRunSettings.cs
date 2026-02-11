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
                GH_ParamAccess.item, 2);
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
                GH_ParamAccess.item, -1);
            if (pManager[8] is Param_Integer cpusParam)
            {
                cpusParam.AddNamedValue("Auto (75% physical cores)", -1);
            }

            // 9
            pManager.AddTextParameter(GH_Strings.RunSettings.BlueCFD, GH_Strings.RunSettings.BlueCFDNick, GH_Strings.RunSettings.BlueCFDDesc, GH_ParamAccess.item, "");
            pManager[9].Optional = true;

            // 10
            pManager.AddBooleanParameter(
                GH_Strings.RunSettings.StabilityLimiter,
                GH_Strings.RunSettings.StabilityLimiterNick,
                GH_Strings.RunSettings.StabilityLimiterDesc,
                GH_ParamAccess.item,
                true);
            pManager[10].Optional = true;

            // 11
            pManager.AddBooleanParameter(
                GH_Strings.RunSettings.Debug,
                GH_Strings.RunSettings.DebugNick,
                GH_Strings.RunSettings.DebugDesc,
                GH_ParamAccess.item,
                false);
            pManager[11].Optional = true;

            // 12
            pManager.AddBooleanParameter(
                GH_Strings.RunSettings.SimpleC,
                GH_Strings.RunSettings.SimpleCNick,
                GH_Strings.RunSettings.SimpleCDesc,
                GH_ParamAccess.item,
                false);
            pManager[12].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(
                GH_Strings.Common.RunSettings, "RSet",
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
            int endTime = 1000;
            int writeInterval = 10;
            int purgeWrite = 3;
            int turb = 2;
            int schemesIdx = 0;
            int cpus = -1;
            int relaxIdx = 1;
            bool potentialFoamInit = false;
            bool aoa = false;
            string blueCfdPath = "";
            bool stabilityLimiters = true;
            bool debugDiagnostics = false;
            bool simpleConsistent = false;

            DA.GetData(0, ref endTime);
            DA.GetData(1, ref writeInterval);
            DA.GetData(2, ref purgeWrite);
            DA.GetData(3, ref turb);
            DA.GetData(4, ref relaxIdx);
            DA.GetData(5, ref schemesIdx);
            DA.GetData(6, ref potentialFoamInit);
            DA.GetData(7, ref aoa);
            DA.GetData(8, ref cpus);
            DA.GetData(9, ref blueCfdPath);
            DA.GetData(10, ref stabilityLimiters);
            DA.GetData(11, ref debugDiagnostics);
            DA.GetData(12, ref simpleConsistent);

            if (!string.IsNullOrWhiteSpace(blueCfdPath))
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = blueCfdPath;
            }

            if (endTime < writeInterval) writeInterval = endTime;

            if (endTime <= 600)
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Warning,
                    "Iterations <= 600 are recommended for demo purposes only; the simulation will likely not converge within this iteration horizon.");
            }

            if (cpus < -1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "CPU cores below -1 are invalid. Using Auto (-1).");
                cpus = -1;
            }
            else if (cpus > Utilities.GetPhysicalCoreCount())
            {
                AddRuntimeMessage(
                    GH_RuntimeMessageLevel.Remark,
                    "Requested CPU cores exceed detected physical cores (" + Utilities.GetPhysicalCoreCount() + "). This might not lead to optimal speedup.");
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
                endTime = endTime,
                writeInterval = writeInterval,
                purgeWrite = purgeWrite,
                schemes = schemes,
                CPUs = cpus,
                turbModel = turbModel,
                relaxationFactors = relaxationFactors,
                potentialFoamInit = potentialFoamInit,
                aoa_domain = aoa,
                stabilityLimiters = stabilityLimiters,
                debugMode = debugDiagnostics,
                simpleConsistent = simpleConsistent
            };

            DA.SetData(0, runSet);
        }
    }
}
