using Eddy.Analytics;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.IO;
using System.Windows.Forms;
using EddyLib.Indoor;
using EddyLib.Indoor.Dicts;
using System.Collections.Generic;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Simulation : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public Simulation()
          : base(GH_Strings.Simulation.Name, GH_Strings.Simulation.Nick,
              GH_Strings.Simulation.Desc + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
            Analytics.Analytics.TrackComponentView("Simulation");
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(GH_Strings.Common.Domain, GH_Strings.Common.DomainNick, GH_Strings.Common.DomainDesc, GH_ParamAccess.item);
            pManager.AddTextParameter(GH_Strings.Common.WorkingDir, GH_Strings.Common.WorkingDirNick, GH_Strings.Common.WorkingDirDesc, GH_ParamAccess.item, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Eddy"));
            pManager[1].Optional = true;
            
            pManager.AddGenericParameter(GH_Strings.Common.MeshSettings, GH_Strings.Common.MeshSettingsNick, GH_Strings.Common.MeshSettingsDesc, GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddGenericParameter(GH_Strings.Common.RunSettings, GH_Strings.Common.RunSettingsNick, GH_Strings.Common.RunSettingsDesc, GH_ParamAccess.item);
            pManager[3].Optional = true;

            pManager.AddBooleanParameter(GH_Strings.Common.RunMeshing, GH_Strings.Common.RunMeshingNick, GH_Strings.Common.RunMeshingDesc, GH_ParamAccess.item, false);
            pManager.AddBooleanParameter(GH_Strings.Common.MakeTrees, GH_Strings.Common.MakeTreesNick, GH_Strings.Common.MakeTreesDesc, GH_ParamAccess.item, false);
            pManager.AddBooleanParameter(GH_Strings.Common.RunSimulation, GH_Strings.Common.RunSimulationNick, GH_Strings.Common.RunSimulationDesc, GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(GH_Strings.Common.Result, GH_Strings.Common.ResultNick, GH_Strings.Common.ResultDesc, GH_ParamAccess.item);
        }

        private bool canRun = true;

        public void taskComplete(object sender, System.EventArgs e)
        {
            canRun = false;
            this.ExpireSolution(true);
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
            Message = "Docker";

            // read inputs
            //------------

            OFBaseDomain DOM;
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(GH_Strings.Common.Domain, ref gobj)) { return; }

            if ((gobj.Value is OFCylDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            else if ((gobj.Value is OFBoxDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid domain object"); return;
            }

            OFRunSettings RunSettings = new OFRunSettings();
            GH_ObjectWrapper gobjRunSet = null;
            if (DA.GetData(GH_Strings.Common.RunSettings, ref gobjRunSet))
            {
                if (gobjRunSet.Value is OFRunSettings)
                {
                    RunSettings = (OFRunSettings)gobjRunSet.Value;
                }
            }

            string baseWorkingDirectory = "";
            DA.GetData(GH_Strings.Common.WorkingDir, ref baseWorkingDirectory);
            baseWorkingDirectory = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(baseWorkingDirectory);
            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }
            baseWorkingDirectory = Utilities.Directories.FixDirectories(baseWorkingDirectory);

            OFMeshSettings MeshSettings = new OFMeshSettings();
            GH_ObjectWrapper gobjMeshSet = null;
            if (DA.GetData(GH_Strings.Common.MeshSettings, ref gobjMeshSet))
            {
                if (gobjMeshSet.Value is OFMeshSettings)
                {
                    MeshSettings = (OFMeshSettings)gobjMeshSet.Value;
                }
            }
            MeshSettings.SetDirectories(baseWorkingDirectory);

            #region RUN BLOCKMESH

            if (DOM is OFBoxDomain)
            {
                RunBlockMesh.RunBox((OFBoxDomain)DOM, MeshSettings, RunSettings, baseWorkingDirectory);
            }
            else
            {
                RunBlockMesh.RunCyl((OFCylDomain)DOM, MeshSettings, RunSettings, baseWorkingDirectory);
            }

            #endregion RUN BLOCKMESH

            #region RUN SNAPPY HEX

            if (Utilities.DidProcessGetKilled(MeshSettings.meshWorkingDir) == true)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some processes got killed. Check RAM.");
                return;
            }

            RunSnappy.Run(DOM, MeshSettings, RunSettings, out string logfileOutput);

            #endregion RUN SNAPPY HEX

            #region RUN SIMULATION

            if (RunSettings.iter == 0 || RunSettings.keepTimeSteps == 0 || RunSettings.writeInterval == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid inputs.");
                return;
            }

            #region Trees

            var treeDict = new List<TopoSetSubDict>();
            var fvOptionsDict = new FunctionObjectDict(treeDict,"fvOptions");

            if (DOM.Trees.Count > 0)
            {
                // Legacy tree handling
            }
            else
            {
                fvOptionsDict.RemoveDict(baseWorkingDirectory);
            }

            #endregion Trees

            RunFoamSimulation.Run(DOM, MeshSettings, RunSettings, baseWorkingDirectory);

            #endregion RUN SIMULATION

            #region START PROCESSES

            bool makeTrees = false;
            bool runSimulation = false;
            bool runMeshing = false;

            DA.GetData(GH_Strings.Common.MakeTrees, ref makeTrees);
            DA.GetData(GH_Strings.Common.RunSimulation, ref runSimulation);
            DA.GetData(GH_Strings.Common.RunMeshing, ref runMeshing);

            if (makeTrees == true && canRun)
            {
                Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\Scripts\run_make_trees.bat", taskComplete);
            }

            if (runMeshing == true && runSimulation == true && canRun)
            {
                Analytics.Analytics.TrackMeshCase("BlueCFD");
                Analytics.Analytics.TrackSimulateCase("BlueCFD");
                Utilities.DeletePhi(MeshSettings, DOM);
                Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\Scripts\run.bat", taskComplete);
            }
            else if (runMeshing == true && runSimulation == false && canRun)
            {
                Analytics.Analytics.TrackMeshCase("BlueCFD");
                Utilities.DeletePhi(MeshSettings, DOM);
                Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\Scripts\run_mesh.bat", taskComplete);
            }
            else if (runMeshing == false && runSimulation == true && canRun)
            {
                Analytics.Analytics.TrackSimulateCase("BlueCFD");
                Utilities.DeletePhi(MeshSettings, DOM);
                Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\Scripts\run_sim_all.bat", taskComplete);
            }

            #endregion START PROCESSES

            OFResult RES = new OFResult(DOM, RunSettings, MeshSettings, baseWorkingDirectory);
            DA.SetData(GH_Strings.Common.Result, RES);

            canRun = true;
        }

        /// <summary>
        /// Provides an Icon for every component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.Eddy_simulation;

        /// <summary>
        /// Unique ID for this component.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{C24346BD-42B9-4113-A656-373307FF4A70}");
    }
}