using Eddy.Properties;
using EddyLib;
using EddyLib.FluidX3D;
using Grasshopper.Kernel;
using System;

namespace Eddy
{
    public class FluidX3DRunSettings_Component : GH_Component
    {
        public override GH_Exposure Exposure => GH_Exposure.tertiary | GH_Exposure.obscure;

        public FluidX3DRunSettings_Component()
            : base(
                  GH_Strings.FluidX3DRunSettings.Name,
                  GH_Strings.FluidX3DRunSettings.Nick,
                  GH_Strings.FluidX3DRunSettings.Desc + EddyVersion.toString(),
                  EddyVersion.Name,
                  "1 | Wind")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                GH_Strings.FluidX3DRunSettings.SourceDir,
                GH_Strings.FluidX3DRunSettings.SourceDirNick,
                GH_Strings.FluidX3DRunSettings.SourceDirDesc,
                GH_ParamAccess.item,
                string.Empty);
            pManager[0].Optional = true;

            pManager.AddIntegerParameter(
                GH_Strings.FluidX3DRunSettings.MemoryMb,
                GH_Strings.FluidX3DRunSettings.MemoryMbNick,
                GH_Strings.FluidX3DRunSettings.MemoryMbDesc,
                GH_ParamAccess.item,
                1000);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DRunSettings.SimTime,
                GH_Strings.FluidX3DRunSettings.SimTimeNick,
                GH_Strings.FluidX3DRunSettings.SimTimeDesc,
                GH_ParamAccess.item,
                360.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DRunSettings.ExportEvery,
                GH_Strings.FluidX3DRunSettings.ExportEveryNick,
                GH_Strings.FluidX3DRunSettings.ExportEveryDesc,
                GH_ParamAccess.item,
                30.0);

            pManager.AddNumberParameter(
                GH_Strings.FluidX3DRunSettings.GroundZ,
                GH_Strings.FluidX3DRunSettings.GroundZNick,
                GH_Strings.FluidX3DRunSettings.GroundZDesc,
                GH_ParamAccess.item,
                0.0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(
                GH_Strings.FluidX3DRunSettings.Output,
                GH_Strings.FluidX3DRunSettings.OutputNick,
                GH_Strings.FluidX3DRunSettings.OutputDesc,
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            try
            {
                SolveInstanceCore(DA);
            }
            catch (TypeLoadException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Could not load FluidX3D types. This usually means an outdated EddyLib.dll "
                    + "is installed. Please update Eddy3D or reinstall the plugin.\n"
                    + "Details: " + ex.Message);
            }
        }

        private void SolveInstanceCore(IGH_DataAccess DA)
        {
            string sourceDir = string.Empty;
            int memoryMb = 1000;
            double simSeconds = 360.0;
            double exportEverySeconds = 30.0;
            double groundZ = 0.0;

            DA.GetData(0, ref sourceDir);
            DA.GetData(1, ref memoryMb);
            DA.GetData(2, ref simSeconds);
            DA.GetData(3, ref exportEverySeconds);
            DA.GetData(4, ref groundZ);

            if (memoryMb < 256)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "VRAM budget below 256 MB is unsupported. Using 256 MB.");
                memoryMb = 256;
            }

            if (simSeconds <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Simulation time must be > 0 s. Using 360 s.");
                simSeconds = 360.0;
            }

            if (exportEverySeconds <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Export interval must be > 0 s. Using 30 s.");
                exportEverySeconds = 30.0;
            }

            FluidX3DRunSettings runSettings = new FluidX3DRunSettings
            {
                SourceDirectory = sourceDir?.Trim() ?? string.Empty,
                MemoryMb = memoryMb,
                SimSeconds = simSeconds,
                ExportIntervalSeconds = exportEverySeconds,
                GroundZ = groundZ
            };

            DA.SetData(0, runSettings);
        }

        protected override System.Drawing.Bitmap Icon => Resources.Eddy_run_settings;

        public override Guid ComponentGuid => new Guid("{1BB4C1A4-9F72-46D5-A4D8-89A87F496273}");
    }
}
