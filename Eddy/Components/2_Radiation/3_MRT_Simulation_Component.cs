using Eddy.Properties;
using EddyLib;
using Eddy.Analytics;
using EddyLib.Radiation;
using EddyLib.UI;
using EddyLib.Web;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy
{
    public class MRT_Simulation_Component : GH_Component
    {
        private MRT_Simulation_System MRTSystem;
        private bool MRTSimulationSucceeded;

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
                "Folder for simulation files. Default: My_MRT_Project",
                GH_ParamAccess.item, @"My_MRT_Project");

            pManager.AddTextParameter(
                "Weather File", "EPW",
                "Path to EnergyPlus weather file (.epw) for climate data. " +
                "Also accepts an http/https URL — the file is downloaded once and cached in %AppData%\\Eddy3D\\Weather.",
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

            pManager.AddBooleanParameter("Run", "Run", "Run the calculation", GH_ParamAccess.item, false);

            pManager.AddTextParameter(
                "Radiance Folder", "RadFolder",
                @"Optional: Custom Radiance installation folder. Default: " + DefaultDirectoriesAndPaths.RadianceDir,
                GH_ParamAccess.item, "");
            pManager[7].Optional = true;

            pManager.AddTextParameter(
                "EnergyPlus Folder", "EPFolder",
                @"Optional: Custom EnergyPlus installation folder. Default: " + DefaultDirectoriesAndPaths.EnergyPlusDir,
                GH_ParamAccess.item, "");
            pManager[8].Optional = true;
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

            try
            {
                workDir = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(workDir);
                workDir = Path.GetFullPath(workDir);
                Directory.CreateDirectory(workDir);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Invalid working directory: {ex.Message}");
                return;
            }

            if (!string.IsNullOrEmpty(weatherPath) && weatherPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                this.Message = "Downloading...";
                Grasshopper.Instances.ActiveCanvas?.Refresh();
                try
                {
                    var (localPath, downloaded) = FileDownloader.ResolveEpwPath(weatherPath);
                    weatherPath = localPath;
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                        downloaded ? $"Downloaded weather file to: {localPath}"
                                   : $"Using cached weather file: {localPath}");
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Failed to download weather file: {ex.Message}");
                    return;
                }
                finally
                {
                    this.Message = null;
                    Grasshopper.Instances.ActiveCanvas?.Refresh();
                }
            }

            if (!File.Exists(weatherPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Weather file not found. Provide a local .epw path or an http/https URL.");
                return;
            }

            // Set custom engine paths if provided
            string radiancePath = "";
            string energyPlusPath = "";
            DA.GetData(7, ref radiancePath);
            DA.GetData(8, ref energyPlusPath);
            if (!string.IsNullOrWhiteSpace(radiancePath))
            {
                DefaultDirectoriesAndPaths.RadianceDir = radiancePath;

            }
            if (!string.IsNullOrWhiteSpace(energyPlusPath))
            {
                DefaultDirectoriesAndPaths.EnergyPlusDir = energyPlusPath;
            }

            try
            {
                // Check Engines
                DefaultDirectoriesAndPaths.CheckEnergyPlus();
                DefaultDirectoriesAndPaths.CheckRadiance();
            }
            catch (FileNotFoundException ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
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
            MRT_Simulation_Settings set = new MRT_Simulation_Settings();
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
            if (!DA.GetData(6, ref RUN)) return;

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

            // Tee stdout + stderr into a log buffer so we can persist it to the working dir
            var logBuffer = new StringWriter();
            var originalOut = Console.Out;
            var originalErr = Console.Error;
            Console.SetOut(new TeeWriter(originalOut, logBuffer));
            Console.SetError(new TeeWriter(originalErr, logBuffer));

            string logPath = Path.Combine(MRTSystem.BaseWorkingDir, "mrt.log");
            MRTSimulationSucceeded = false;
            try
            {
                Console.WriteLine($"=== MRT run started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                Console.WriteLine($"Working dir: {MRTSystem.BaseWorkingDir}");
                Console.WriteLine($"Weather:     {weatherPath}");
                Console.WriteLine($"Run:         {RUN}");

                if (RUN)
                {
                    Analytics.Analytics.TrackSimulationRun(
                        "mrt",
                        Analytics.Analytics.InferEngineFromPath(CFDResultPath, "native"));

                    if (HidePopUp)
                    {
                        DoWork(new CancellationTokenSource());
                    }
                    else
                    {
                        // show progress form
                        var progress = new ProgressDialog(DoWorkAsync, "MRT Simulation");
                        progress.ShowModal();

                        // if user cancellation, abort solution
                        if (progress.Canceled)
                        {
                            OnPingDocument().RequestAbortSolution();
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Run input is false - skipping simulation.");
                }
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                try
                {
                    File.WriteAllText(logPath, logBuffer.ToString());
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, $"Log: {logPath}");
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Could not write log: {ex.Message}");
                }
            }

            ReportSurfaceTemperatureStatus(RUN, MRTSimulationSucceeded);

            DA.SetData(0, MRTSystem);

            if (MRTSystem != null)
            {
                string resultFilePath = Path.Combine(MRTSystem.BaseWorkingDir, "UTCI.eddy");
                DA.SetData(1, resultFilePath);

                DA.SetData(2, MRTSystem.Settings.toJSON());
            }
        }

        private void ReportSurfaceTemperatureStatus(bool ran, bool succeeded)
        {
            if (MRTSystem?.Settings == null || !MRTSystem.Settings.ComputeSurfaceTemperatureEnergyPlus) return;

            var simulatedPolys = MRTSystem.Polys?.FindAll(p =>
                p.Type != RadiationSurfaceType.Sky &&
                p.SimulationType == SimulationType.Simulated);

            int simulatedCount = simulatedPolys?.Count ?? 0;
            if (simulatedCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "Surface temperatures are enabled, but no non-sky polygons use the Simulated temperature source.");
                return;
            }

            if (!ran)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Surface temperatures will be available after running the MRT simulation with EnergyPlus enabled.");
                return;
            }

            if (!succeeded)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "MRT simulation failed before EnergyPlus surface temperatures were produced. Check mrt.log and RadiationErrorLog.log in the case folder.");
                return;
            }

            int mappedCount = simulatedPolys.FindAll(p => p.SurfaceTemperature != null && p.SurfaceTemperature.Length > 0).Count;
            if (mappedCount == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "EnergyPlus finished, but no simulated polygons received surface temperature results. Check mrt.log and lower the VFC setting if all polygons were filtered.");
            }
            else if (mappedCount < simulatedCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    $"EnergyPlus surface temperatures were mapped to {mappedCount} of {simulatedCount} simulated polygons. Filtered polygons will fall back to ambient temperature in MRT.");
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"EnergyPlus surface temperatures mapped to all {mappedCount} simulated polygons.");
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
            MRTSimulationSucceeded = RunSlowSimulation(cts, 2);
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

            if (MRTSystem.Settings.ComputeReflectionsAndDiffuseRadiation)
            {
                // VF and DDS have no data dependencies — run in parallel.
                // VF writes to Probes[i].VFtoPolys and Polys[j].SeenByProbes.
                // DDS reads probe positions/normals (immutable) and writes .ill files to disk.
                Console.WriteLine("Starting ViewFactor + Radiance DDS in parallel");
                if (cts.IsCancellationRequested) return false;

                bool vfOk = false;
                bool ddsOk = false;

                var vfTask = Task.Run(() =>
                {
                    vfOk = MRTSystem.RunVF(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);
                });

                var ddsTask = Task.Run(() =>
                {
                    ddsOk = MRTSystem.RadiationSystem.RunDDS(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);
                });

                Task.WaitAll(vfTask, ddsTask);
                if (!vfOk || !ddsOk) return false;

                if (cts.IsCancellationRequested) return false;
                MRTSystem.RadiationSystem.LoadDDSData(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);
            }
            else
            {
                // Simple raycast path — must stay sequential.
                // RunDirectRayCast reads Probes[i].VFtoMaterial["Sky"] which is set by RunVF.
                Console.WriteLine("Starting ViewFactor Calculation");
                if (cts.IsCancellationRequested) return false;
                if (!MRTSystem.RunVF(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP)) { return false; }

                MRTSystem.RadiationSystem.RunDirectRayCast(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);
            }

            if (MRTSystem.ThermalSystem == null) return false;

            if (MRTSystem.Settings.ComputeSurfaceTemperatureEnergyPlus)
            {
                if (cts.IsCancellationRequested) return false;
                var data = MRTSystem.ThermalSystem.RunEP(true, cts.Token, MRTSystem.TOTAL, ref MRTSystem.STEP);
                if (data == null || data.Count == 0) return false;
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

        private sealed class TeeWriter : TextWriter
        {
            private readonly TextWriter a;
            private readonly TextWriter b;
            public TeeWriter(TextWriter a, TextWriter b) { this.a = a; this.b = b; }
            public override Encoding Encoding => a?.Encoding ?? Encoding.UTF8;
            public override void Write(string value) { a?.Write(value); b?.Write(value); }
            public override void WriteLine(string value) { a?.WriteLine(value); b?.WriteLine(value); }
            public override void Write(char value) { a?.Write(value); b?.Write(value); }
            public override void Flush() { a?.Flush(); b?.Flush(); }
        }
    }
}
