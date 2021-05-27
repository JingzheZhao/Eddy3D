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
          : base("MRT System", "MRT", "MRT and Radiation simulation system " + EddyVersion.toString(), EddyVersion.Name, "2 | Radiation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddTextParameter("Name", "N", "Project name", GH_ParamAccess.item, "MyStudy");
            pManager.AddTextParameter("Dir", "D", "Working directory name", GH_ParamAccess.item, @"C:\Temp\Eddy3d");
            pManager.AddTextParameter("Weather", "W", "Weather filepath", GH_ParamAccess.item, DefaultDirectoriesAndPaths.DefaultWeather);

            pManager.AddGenericParameter("RSurf", "RS", "Radiation Model Surfaces", GH_ParamAccess.tree);

            pManager.AddGenericParameter("Sensors", "Sen", "Radiation sensors. Provide as [Mesh] or [RProbe]", GH_ParamAccess.tree);

            pManager.AddTextParameter("Settings", "Set", "MRT System Settings", GH_ParamAccess.item, "");
            pManager[4].Optional = true;

            pManager.AddTextParameter("CFD result", "CFD", "File path to *.wind.eddy file. If provided wind velocities are loaded from CFD result.", GH_ParamAccess.item, "");
            pManager[5].Optional = true;

            pManager.AddBooleanParameter("Run", "R", "Run simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("System", "SYS", "MRT Simulation System", GH_ParamAccess.item);

            pManager.AddTextParameter("Result", "RES", "MRT Result file path", GH_ParamAccess.item);

            pManager.AddTextParameter("Settings", "SET", "MRT System Settings", GH_ParamAccess.item);
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
            DA.GetData(6, ref RUN);

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