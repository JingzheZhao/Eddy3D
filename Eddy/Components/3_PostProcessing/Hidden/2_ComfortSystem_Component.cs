using Eddy.Properties;
using EddyLib;
using EddyLib.OutdoorComfort;
using EddyLib.Radiation;
using EddyLib.UI;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy.Components.Radiation
{
    public class ComfortSystem_Component : GH_Component
    {

        string filePath_CFD = "";
        string filePath_MRT = "";



        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.hidden; }
        }

        private ComfortSystem ComfortSystem;

        /// <summary>
        /// Initializes a new instance of the ThermalSystem_Component class.
        /// </summary>
        public ComfortSystem_Component()
          : base("Comfort", "Comf", "Comfort System " + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("MRTRes", "MRT", "MRT Simulation Result", GH_ParamAccess.item);
            pManager.AddTextParameter("CFDRes", "CFD", "Wind Velocity Simulation Result", GH_ParamAccess.item);
            pManager[1].Optional = true;

            pManager.AddBooleanParameter("Run", "R", "Run simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("ComfSys", "CS", "Comf System", GH_ParamAccess.item);
            pManager.AddTextParameter("Result", "RES", "Result file path", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            bool run = false;
            bool HidePopUp = false;
            DA.GetData(2, ref run);




            //// ---------------------
            //// Load MRT Result
            //// ---------------------


            //  filePath_MRT = "";
            //DA.GetData(0, ref filePath_MRT);


            //if (!File.Exists(filePath_MRT))
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Result file not found.");
            //}

            //var prep = PrepareProtoBufSingleton.Instance;

            //MRT_Simulation_ResultProto resultProto_MRT = null;

            //try
            //{
            //    Stopwatch sp = new Stopwatch();
            //    sp.Restart();
            //    resultProto_MRT = MRT_Simulation_ResultProto.ReadFromFile(filePath_MRT);
            //    sp.Stop();
            //    Debug.WriteLine("Loading RadiationSimulationResultProto: " + sp.ElapsedMilliseconds);
            //}
            //catch (Exception e)
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Result file could not be deserialized. Are you loading a wrong file type? " + Environment.NewLine + e.Message);
            //    return;
            //}


            //if (resultProto_MRT == null) return;





            //// ---------------------
            //// Load CFD Result
            //// ---------------------
            //string filePath_CFD = "";

            //DA.GetData(1, ref filePath_CFD);

            //WProbeResultProto resultProto_CFD = null;

            //if (!String.IsNullOrWhiteSpace(filePath_CFD))
            //{

            //    if (!File.Exists(filePath_CFD))
            //    {
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Result file not found.");
            //    }
            //    try
            //    {
            //        Stopwatch sp = new Stopwatch();
            //        sp.Restart();
            //        resultProto_CFD = WProbeResultProto.ReadFromFile(filePath_CFD);
            //        sp.Stop();
            //        Debug.WriteLine("Loading WProbeResultProto: " + sp.ElapsedMilliseconds);
            //    }
            //    catch (Exception e)
            //    {
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Result file could not be deserialized. Are you loading a wrong file type? " + Environment.NewLine + e.Message);
            //        return;
            //    }

            //    // WindFactorSpatial

            //    foreach (var p in resultProto_CFD.Probes)
            //    {
            //        p.WindFactorsSpatial = EddyLib.OutdoorComfort.WindFactorsSpatial.CalcWindFactorsSpatialSP(p);
            //    }


            //    WindSystem WS = new WindSystem(resultProto_MRT.Weather, resultProto_CFD.Probes[0].WindDirections.ToList());

            //    foreach (var p in resultProto_CFD.Probes)
            //    {
            //        p.WindFactorsTemporal = EddyLib.OutdoorComfort.WindFactorsTemporal.CalcWindFactorsTemporalSP(resultProto_MRT.Weather, p, WS, true);
            //    }


            //}

            //ComfortSystem = new ComfortSystem(resultProto_MRT.ProjectName, resultProto_MRT.BaseWorkingDir, resultProto_MRT.Weather, resultProto_MRT.Probes, resultProto_MRT.Polys);











            var prep = PrepareProtoBufSingleton.Instance;


            // ---------------------
            // Load MRT Result
            // ---------------------


            filePath_MRT = "";
            DA.GetData(0, ref filePath_MRT);


            if (!File.Exists(filePath_MRT))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Result file not found.");
            }








            // ---------------------
            // Load CFD Result
            // ---------------------
            filePath_CFD = "";

            DA.GetData(1, ref filePath_CFD);



            if (!String.IsNullOrWhiteSpace(filePath_CFD))
            {
                if (!File.Exists(filePath_CFD))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Result file not found.");
                }
            }













            // redirect stderr
            var errors = new StringWriter();
            Console.SetError(errors);

            if (run)
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


            if (ComfortSystem == null) return;
            DA.SetData(0, ComfortSystem);
            DA.SetData(1, ComfortSystem.BaseWorkingDir + @"\UTCI.eddy");
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
                return Resources.Eddy_calUTCI;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{50868516-DF58-43FC-8A66-9F2B18C62864}"); }
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

            // ---------------------
            // Load MRT Result
            // ---------------------



            var prep = PrepareProtoBufSingleton.Instance;

            MRT_Simulation_ResultProto resultProto_MRT = null;

            try
            {
                Stopwatch sp = new Stopwatch();
                sp.Restart();
                resultProto_MRT = MRT_Simulation_ResultProto.ReadFromFile(filePath_MRT);
                sp.Stop();
                Console.WriteLine("Loading RadiationSimulationResultProto: " + sp.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Result file could not be deserialized. Are you loading a wrong file type? " + Environment.NewLine + e.Message);
                return false;
            }


            if (resultProto_MRT == null) return false;





            // ---------------------
            // Load CFD Result
            // ---------------------


            WProbeResultProto resultProto_CFD = null;

            if (!String.IsNullOrWhiteSpace(filePath_CFD))
            {

                if (!File.Exists(filePath_CFD))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Result file not found.");
                }
                try
                {
                    Stopwatch sp = new Stopwatch();
                    sp.Restart();
                    resultProto_CFD = WProbeResultProto.ReadFromFile(filePath_CFD);
                    sp.Stop();
                    Console.WriteLine("Loading WProbeResultProto: " + sp.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Result file could not be deserialized. Are you loading a wrong file type? " + Environment.NewLine + e.Message);
                    return false;
                }



                Console.WriteLine("MRT Probe Count: " + resultProto_MRT.Probes.Count);
                Console.WriteLine("CFD Probe Count: " + resultProto_CFD.Probes.Count);


                // WindFactorSpatial
                Console.WriteLine("Computing Spatial Wind Factors...");

                foreach (var p in resultProto_CFD.Probes)
                {
                    p.WindFactorsSpatial = EddyLib.OutdoorComfort.WindFactorsSpatial.CalcWindFactorsSpatialSP(p);
                }
                Console.WriteLine("Computing Spatial Wind Factors...");


                WindSystem WS = new WindSystem(resultProto_MRT.Weather, resultProto_CFD.Probes[0].WindDirections.ToList());
                Console.WriteLine("Computing Temporal Wind Factors...");

                int pcnt = 0;
                foreach (var p in resultProto_CFD.Probes)
                {
                    p.WindFactorsTemporal = EddyLib.OutdoorComfort.WindFactorsTemporal.CalcWindFactorsTemporalSP(resultProto_MRT.Weather, p, WS, true);
                    //Console.WriteLine("Wind factors for Probe: " + pcnt); pcnt++;

                }




                for (int i = 0; i < resultProto_MRT.Probes.Count; i++)
                {
                    if (i < resultProto_CFD.Probes.Count)
                    {
                        resultProto_MRT.Probes[i].WindSpeed = resultProto_CFD.Probes[i].WindFactorsTemporal;
                    }
                }



            }






            ComfortSystem = new ComfortSystem(  resultProto_MRT.BaseWorkingDir, resultProto_MRT.Weather, resultProto_MRT.Probes, resultProto_MRT.Polys, "");



            int xxx = 0;


            if (ComfortSystem == null) return false;
            if (cts.IsCancellationRequested) return false;
            ComfortSystem.ComputeUTCI(true, cts.Token, 0, ref xxx);

            if (cts.IsCancellationRequested) return false;
            var proto = ComfortSystem.SaveResults(true, cts.Token, 0, ref xxx);

            return true;
        }
    }
}