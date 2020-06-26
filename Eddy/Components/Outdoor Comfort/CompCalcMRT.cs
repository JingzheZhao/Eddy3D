using Eddy.Properties;
using EddyLib;
using EddyLib.OutdoorComfort;
using EddyLib.Radiance;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompCalcMRT : GH_Component
    {
        // exposure
        //public override GH_Exposure Exposure
        //{
        //    get { return GH_Exposure.hidden; }
        //}

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        ///

        public CompCalcMRT()
          : base("Mean Radiant Temperature", "Mean Radiant Temperature", @"Mean Radiant Temperature.

This is based on a TwoPhaseDDS approach for which it is assumed that the building surface temperature equals the ambient temperature.

" + EddyVersion.toString(),
              EddyVersion.Name, "6 | Outdoor Comfort")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation Result", "Res", "Simulation Result", GH_ParamAccess.item);

            //pManager.AddIntegerParameter("windDirs", "windDirs", "windDirs", GH_ParamAccess.list);
            //pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            // pManager.AddIntegerParameter("Hours", "H", "Hours", GH_ParamAccess.list);
            pManager.AddPointParameter("Probing points", "Points", "List of probing points", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Simulation Mode", "Mode", "Pick a simulation mode", GH_ParamAccess.item, 0);

            //Using an enum to generate the dropdown items
            var types = Enum.GetNames(typeof(MRT.MRTType));
            Param_Integer param = pManager[2] as Param_Integer;

            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }

            pManager.AddBooleanParameter("Run", "Run", "Run the calculation", GH_ParamAccess.item, false);

            pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.list);
            pManager.AddGenericParameter("Mean Radiant Temperature [°C]", "MRT", "Mean Radiant Temperature [°C] Object", GH_ParamAccess.item);

            // pManager.AddGenericParameter("MRT_T", "MRT_T", "MRT_T", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        ///

        private bool canRun = true;

        public void MRTSimComplete(object sender, System.EventArgs e)
        {
            //RhinoApp.WriteLine("Proping complete");
            canRun = false;
            this.ExpireSolution(true);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            OFResult RES = null;
            DA.GetData(0, ref RES);

            List<Point3d> probes = new List<Point3d>();
            DA.GetDataList("Probing points", probes);
            var numberOfProbes = probes.Count;
            var probesArr = probes.ToArray();

            int mode = 0;
            DA.GetData("Simulation Mode", ref mode);

            MRT.MRTType SimMode = (MRT.MRTType)mode;

            bool run = false;
            DA.GetData("Run", ref run);

            #region Load prerequisites

            Console.WriteLine("Load weather data...");

            //Weather data...

            if (RES.Domain.BCond.epwFilePath == "")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Without a weather file (.epw) referenced you will not be able to run any pedestrian or outdoor thermal comfort post-processing.");
                return;
            }

            if (Utilities.HasWhiteSpace(RES.WorkingDirectory))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "FilePath cannot contain whitespaces to perform any outdoor comfort calculations at this time.");
                return;
            }

            // AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "This is an experimental component. Please refrain from using this in a production environment.");

            Weather weather = new Weather(RES.Domain.BCond.epwFilePath);

            #endregion Load prerequisites

            #region Create Ground and Building Mesh

            var BAG = new Mesh();
            if (RES.Domain is OFBoxDomain)
            {
                var dom = (OFBoxDomain)RES.Domain;
                BAG.Append(dom.BuildingGeometry);
                BAG.Append(dom.DomainMeshGround);
                BAG.Append(dom.DomainMeshGroundPerim);
            }
            else
            {
                var dom = (OFCylDomain)RES.Domain;
                BAG.Append(dom.BuildingGeometry);
                BAG.Append(dom.CylDomainMeshGround);
                BAG.Append(dom.CylDomainMeshGroundPerim);
            }

            #endregion Create Ground and Building Mesh

            // @ Timur: This was borrowed from EddyLib.Utilities.StartProcess.StartProcessCMD but it doesn't work

            MRTSimulation mrtsim = null;

            if (run == true && canRun)
            {
                EventHandler eh = MRTSimComplete;

                bool close = true;
                string executable = @"C:\Windows\System32\cmd.exe";
                bool createnowindow = false;
                string argument = @"echo Starting MRT simulation";

                System.Diagnostics.Process p = new System.Diagnostics.Process();

                // if(eh!=null) p.Exited += eh;
                p.StartInfo.FileName = executable;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;

                p.StartInfo.CreateNoWindow = createnowindow;

                string theArgument = argument + ((close) ? @"
exit
" : "");

                ThreadStart ths = new ThreadStart(() =>
                {
                    p.Start();
                    p.WaitForExit();

                    StreamWriter sw = p.StandardInput;
                    String strInputText = theArgument;
                    sw.WriteLine(strInputText);

                    // Window doesn't close with
                    //sw.Flush();

                    mrtsim = new MRTSimulation(RES, weather, BAG, probesArr, MRT.MRTType.RadianceTwoPhaseDDS, run);

                    if (close) { p.Close(); }
                    if (eh != null) { eh.Invoke(p, new EventArgs()); }
                });

                Thread th = new Thread(ths);
                th.Start();
            }
            else
            {
                mrtsim = new MRTSimulation(RES, weather, BAG, probesArr, MRT.MRTType.RadianceTwoPhaseDDS, run);
            }

            // Order important

            if (mrtsim.mrt != null)
            {
                if (mrtsim.mrt.wrongNumberOfProbes)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, EddyLib.Strings.ReturnMsg.WrongNumberOfProbes(RES, SimMode.ToString()));
                    return;
                }

                if (mrtsim.mrt.Values is null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, EddyLib.Strings.ReturnMsg.NoResults(RES, SimMode.ToString()));
                    return;
                }

                if (mrtsim.mrt.resultPrecalculated)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, EddyLib.Strings.ReturnMsg.PrecalResLoaded(RES, SimMode.ToString()));
                }
            }

            if (mrtsim != null)
            {
                DA.SetData(0, mrtsim.mrt);
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_calMRT;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{FE115CFE-B2B9-4DC6-8DBE-DDB43710090C}");
    }
}