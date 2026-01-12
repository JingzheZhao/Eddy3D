using Eddy.Properties;
using EddyLib;
using EddyLib.Radiation;
using EddyLib.UI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy
{
    public class MRT_Simulation_Component : GH_Component
    {
        private MRT_Simulation_System MRTSystem;

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Initializes a new instance of the WorkerWithProgBarComponent class.
        /// </summary>
        public MRT_Simulation_Component()
          : base("MRT Simulation", "MRT", 
@"Mean Radiant Temperature (MRT) Solver

Calculates MRT, a key metric for thermal comfort, using ray-tracing and view factors.
Combines:
- Direct Solar Radiation
- Diffuse Sky Radiation
- Longwave Surface Emissions

" + EddyVersion.toString(), 
              EddyVersion.Name, "2 | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter(
                "Working Directory", "Dir", 
                "Folder for simulation files. Default: C:\\Temp\\Eddy3d", 
                GH_ParamAccess.item, @"C:\Temp\Eddy3d");

            pManager.AddTextParameter(
                "Weather File", "EPW", 
                "Path to EnergyPlus weather file (.epw) for climate data.", 
                GH_ParamAccess.item);

            pManager.AddGenericParameter(
                "Surfaces", "Srf", 
                "Radiation surfaces from Building/Ground/Tree Surface components.", 
                GH_ParamAccess.tree);

            pManager.AddGenericParameter(
                "Sensors", "Sen", 
                "Analysis locations as Mesh or RProbe objects.", 
                GH_ParamAccess.tree);

            pManager.AddTextParameter(
                "Settings", "Set", 
                "Optional: Simulation settings (Radiance parameters, timestep).", 
                GH_ParamAccess.item, "");
            pManager[4].Optional = true;

            pManager.AddTextParameter(
                "CFD Result", "CFD", 
                "Optional: Path to .wind.eddy file for wind-coupled MRT analysis.", 
                GH_ParamAccess.item, "");
            pManager[5].Optional = true;

            pManager.AddTextParameter(
                "Radiance Path", "RadPath", 
                @"Optional: Custom Radiance bin folder. Default: " + DefaultDirectoriesAndPaths.RadianceBinDir, 
                GH_ParamAccess.item, "");
            pManager[6].Optional = true;

            pManager.AddTextParameter(
                "EnergyPlus Path", "EPPath", 
                @"Optional: Custom EnergyPlus folder. Default: " + DefaultDirectoriesAndPaths.EnergyPlusDir , 
                GH_ParamAccess.item, "");
            pManager[7].Optional = true;

            pManager.AddBooleanParameter(
                "Run", "Run!", 
                "Set True to execute MRT simulation.", 
                GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("System", "Sys", "MRT simulation system object", GH_ParamAccess.item);
            pManager.AddTextParameter("Result", "Res", "Path to .mrt.eddy result file for post-processing", GH_ParamAccess.item);
            pManager.AddTextParameter("Settings", "Set", "Current simulation settings (for reference)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // ---------------------
            // Weather and work dir
            // ---------------------

            //string name = "";
            string workDir = "";
            string weatherPath = "";

            //DA.GetData(0, ref name);
            DA.GetData(0, ref workDir);
            DA.GetData(1, ref weatherPath);

            if (!File.Exists(weatherPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Weather file could not be found");
                return;
            }

            // Set custom engine paths if provided
            string radiancePath = "";
            string energyPlusPath = "";
            DA.GetData(6, ref radiancePath);
            DA.GetData(7, ref energyPlusPath);
            if (!string.IsNullOrWhiteSpace(radiancePath))
            {
                DefaultDirectoriesAndPaths.RadianceDir = radiancePath;
                DefaultDirectoriesAndPaths.RadianceLibDir = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(radiancePath), "lib");
            }
            if (!string.IsNullOrWhiteSpace(energyPlusPath))
            {
                DefaultDirectoriesAndPaths.EnergyPlusDir = energyPlusPath;
            }

            // Check EnergyPlus
            string epExe = Path.Combine(DefaultDirectoriesAndPaths.EnergyPlusDir, "energyplus.exe");
            if (!File.Exists(epExe))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "EnergyPlus v9.4.0 not found. Please install from: https://github.com/NREL/EnergyPlus/releases/tag/v9.4.0");
                return;
            }

            // Check Radiance
            string radExe = Path.Combine(DefaultDirectoriesAndPaths.RadianceDir, "rad.exe");
            if (!File.Exists(radExe))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                   "Radiance not found. Please install from: https://github.com/LBNL-ETA/Radiance/releases");
                return;
            }
            Weather weather = new Weather(weatherPath);

            // ---------------------
            // Get the RSurf objects
            // ---------------------

            List<RSurface> modelRSurfaces = new List<RSurface>();
            GH_Structure<IGH_Goo> GH_RSurfTree;
            if (!DA.GetDataTree(2, out GH_RSurfTree)) { }
            foreach (GH_Path p in GH_RSurfTree.Paths)
            {
                foreach (IGH_Goo o in GH_RSurfTree.get_Branch(p))
                {
                    if (o != null)
                    {
                        RSurface im;
                        if (!o.CastTo(out im)) continue;
                        modelRSurfaces.Add(im);
                    }
                }
            }

            // ----------------------
            // Get the probing points
            // ----------------------
            List<EddyProbe> probes = new List<EddyProbe>();
            List<Mesh> probeMeshes = new List<Mesh>();

            GH_Structure<IGH_Goo> GH_RProbeTree;
            if (!DA.GetDataTree(3, out GH_RProbeTree)) { }
            foreach (GH_Path p in GH_RProbeTree.Paths)
            {
                foreach (IGH_Goo o in GH_RProbeTree.get_Branch(p))
                {
                    if (o != null)
                    {
                        Mesh m;
                        EddyProbe pr;
                        if (o.CastTo(out m))
                        { probeMeshes.Add(m); }
                        else if (o.CastTo(out pr))
                        { probes.Add(pr); }
                    }
                }
            }

            string settingsInput = "";
            MRT_Simulation_Settings set = null;
            DA.GetData(4, ref settingsInput);

            if (!String.IsNullOrWhiteSpace(settingsInput))
            {
                try
                {
                    set = MRT_Simulation_Settings.fromJSON(settingsInput);
                }
                catch
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Don't understand your settings. Using defaults.");
                    set = new MRT_Simulation_Settings();
                }
            }

            // ----------------------
            // CFD results ?
            // ----------------------

            string CFDResultPath = "";
            DA.GetData(5, ref CFDResultPath);
            if (!String.IsNullOrWhiteSpace(CFDResultPath))
            {
                if (!File.Exists(CFDResultPath))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "CFD result file not found.");
                    return;
                }
            }

            // ---------------------
            // Run
            // ---------------------

            bool RUN = false;
            bool HidePopUp = false;
            DA.GetData(8, ref RUN);

            // ---------------------
            // Setup probes
            // ---------------------

            List<RProbe> RadProbes = new List<RProbe>();
            foreach (var m in probeMeshes)
            {
                RadProbes.AddRange(RProbe.Mesh2Probes(m));
            }
            foreach (var p in probes)
            {
                if (p.PreviewGeo == null)
                { RadProbes.Add(new RProbe(p.Point, p.Normal)); }
                else
                { RadProbes.Add(new RProbe(p.Point, p.Normal, p.PreviewGeo)); }
            }

            // ---------------------
            // Setup system
            // ---------------------

            MRTSystem = new MRT_Simulation_System(workDir, weather, modelRSurfaces, RadProbes, CFDResultPath, set);

            // redirect stderr
            var errors = new StringWriter();
            Console.SetError(errors);

            if (RUN)
            {
                if (HidePopUp)
                {
                    DoWork(new CancellationTokenSource());
                }
                else
                {
                    // show progress form
                    var progress = new ProgressDialog(DoWorkAsync);
                    progress.ShowModal();

                    // if user cancellation, abort solution
                    if (progress.Canceled)
                    {
                        OnPingDocument().RequestAbortSolution();
                    }
                }
            }

            DA.SetData(0, MRTSystem);

            if (MRTSystem != null)
            {
                string resultFilePath = MRTSystem.BaseWorkingDir + @"\UTCI.eddy";
                DA.SetData(1, resultFilePath);

                DA.SetData(2, MRTSystem.Settings.toJSON());
            }
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
                return Resources.Eddy_MRT_System;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{024B87D9-03ED-451C-B953-B50555EF7D96}"); }
        }

        private void DoWork(CancellationTokenSource cts)
        {
            var success = RunSlowSimulation(cts, 2);
        }

        private async Task DoWorkAsync(CancellationTokenSource cts)
        {
            await Task.Run(() =>
            {
                DoWork(cts);
            });
        }

        public bool RunSlowSimulation(CancellationTokenSource cts, int nthreads = 1)
        {
            if (!MRTSystem.Settings.ComputeReflectionsAndDiffuseRadiation) { MRTSystem.TOTAL += MRTSystem.Probes.Count - MRTSystem.RadiationSystem.methodsteps; }
            if (!MRTSystem.Settings.ComputeSurfaceTemperatureEnergyPlus) { MRTSystem.TOTAL -= MRTSystem.ThermalSystem.methodsteps; }

            if (MRTSystem == null) return false;
            if (MRTSystem.RadiationSystem == null) return false;
            if (MRTSystem.ThermalSystem == null) return false;

            Console.WriteLine("Starting ViewFactor Calculation");
            if (cts.IsCancellationRequested) return false;
            if (!MRTSystem.RunVF(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP)) { return false; }

            if (MRTSystem.Settings.ComputeReflectionsAndDiffuseRadiation)
            {
                if (cts.IsCancellationRequested) return false;
                if (!MRTSystem.RadiationSystem.RunDDS(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP)) { return false; }

                if (cts.IsCancellationRequested) return false;
                MRTSystem.RadiationSystem.LoadDDSData(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);
            }
            else
            {
                MRTSystem.RadiationSystem.RunDirectRayCast(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);
            }

            if (MRTSystem.ThermalSystem == null) return false;

            if (MRTSystem.Settings.ComputeSurfaceTemperatureEnergyPlus)
            {
                if (cts.IsCancellationRequested) return false;
                var data = MRTSystem.ThermalSystem.RunEP(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);
            }

            if (cts.IsCancellationRequested) return false;
            MRTSystem.ThermalSystem.ComputeMRT(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);

            if (cts.IsCancellationRequested) return false;
            MRTSystem.ThermalSystem.SaveResults(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);

            if (cts.IsCancellationRequested) return false;
            MRTSystem.ComfortSystem.LoadCFD_ComputeWindfactors(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);

            if (cts.IsCancellationRequested) return false;
            MRTSystem.ComfortSystem.ComputeUTCI(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);

            if (cts.IsCancellationRequested) return false;
            var proto = MRTSystem.ComfortSystem.SaveResults(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);

            return true;
        }
    }
}