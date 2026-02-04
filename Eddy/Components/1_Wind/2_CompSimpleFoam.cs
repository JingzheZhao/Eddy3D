using Eddy.Properties;
using EddyLib;
using EddyLib.OpenFOAM;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.IO;
using System.Windows.Forms;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class SimpleFoam : GH_Component
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
        public SimpleFoam()
          : base(GH_Strings.SimpleFoam.Name, GH_Strings.SimpleFoam.Nick, 
GH_Strings.SimpleFoam.Desc + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                GH_Strings.Common.Domain, GH_Strings.Common.DomainNick, 
                GH_Strings.Common.DomainDesc, 
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                GH_Strings.Common.WorkingDir, GH_Strings.Common.WorkingDirNick, 
                GH_Strings.Common.WorkingDirDesc, 
                GH_ParamAccess.item, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Eddy"));
            pManager[1].Optional = true;

            pManager.AddGenericParameter(
                GH_Strings.Common.MeshSettings, GH_Strings.Common.MeshSettingsNick, 
                GH_Strings.Common.MeshSettingsDesc, 
                GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddGenericParameter(
                GH_Strings.Common.RunSettings, GH_Strings.Common.RunSettingsNick, 
                GH_Strings.Common.RunSettingsDesc, 
                GH_ParamAccess.item);
            pManager[3].Optional = true;

            pManager.AddBooleanParameter(
                GH_Strings.Common.RunMeshing, GH_Strings.Common.RunMeshingNick, 
                GH_Strings.Common.RunMeshingDesc, 
                GH_ParamAccess.item, false);

            pManager.AddBooleanParameter(
                GH_Strings.Common.MakeTrees, GH_Strings.Common.MakeTreesNick, 
                GH_Strings.Common.MakeTreesDesc, 
                GH_ParamAccess.item, false);

            pManager.AddBooleanParameter(
                GH_Strings.Common.RunSimulation, GH_Strings.Common.RunSimulationNick, 
                GH_Strings.Common.RunSimulationDesc, 
                GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter(GH_Strings.Common.Result, GH_Strings.Common.ResultNick, GH_Strings.Common.ResultDesc, GH_ParamAccess.item);
            pManager.AddBooleanParameter(GH_Strings.SimpleFoam.MeshDone, GH_Strings.SimpleFoam.MeshDoneNick, GH_Strings.SimpleFoam.MeshDone, GH_ParamAccess.item);
            pManager.AddBooleanParameter(GH_Strings.SimpleFoam.SimDone, GH_Strings.SimpleFoam.SimDoneNick, GH_Strings.SimpleFoam.SimDone, GH_ParamAccess.list);
            pManager.AddTextParameter(GH_Strings.SimpleFoam.MeshEta, GH_Strings.SimpleFoam.MeshEtaNick, GH_Strings.SimpleFoam.MeshEta, GH_ParamAccess.item);
            pManager.AddTextParameter(GH_Strings.SimpleFoam.SimEta, GH_Strings.SimpleFoam.SimEtaNick, GH_Strings.SimpleFoam.SimEta, GH_ParamAccess.list);
        }

        private bool canRun = true;

        public void taskComplete(object sender, System.EventArgs e)
        {
            //RhinoApp.WriteLine("Sim complete");
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
            // mode to select simulation environment
            Message = "BlueCFD";

            // read inputs
            //------------

            // domain
            //-------

            OFBaseDomain DOM;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(GH_Strings.Common.Domain, ref gobj)) { }

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

            // run settings
            //-----------------

            OFRunSettings RunSettings = new OFRunSettings();

            GH_ObjectWrapper gobjRunSet = null;
            if (DA.GetData(GH_Strings.Common.RunSettings, ref gobjRunSet))
            {
                if (gobjRunSet.Value is OFRunSettings)
                {
                    RunSettings = (OFRunSettings)gobjRunSet.Value;
                }
            }

            // Error Handling

            if (!RunSettings.IdenticalMPI && RunSettings.CPUs > 1 && RunSettings.BlueCFDIsInstalled)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "To use multiple CPUs, you need to ensure to use the same msmpi.dll for both Windows and BlueCFD. This is a BlueCFD issue and will hopefully be fixed in a future version."); return;
            }

            // working directory
            //------------------

            string baseWorkingDirectory = "";
            DA.GetData(GH_Strings.Common.WorkingDir, ref baseWorkingDirectory);
            
            // Resolve simple case names to full paths under AppData\Eddy3D\Cases
            baseWorkingDirectory = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(baseWorkingDirectory);
            
            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }

            DirectoryInfo parentDir = Directory.GetParent(baseWorkingDirectory.EndsWith("\\") ? baseWorkingDirectory : string.Concat(baseWorkingDirectory, "\\"));
            var myParentDir = parentDir.Parent.FullName;

            if (myParentDir == @"C:\")
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, @"Please use an additional subfolder for Eddy3D simulations e.g. ""C:\Eddy3D\3_SimpleWindAnalysis"""); return;
            }

            baseWorkingDirectory = Utilities.Directories.FixDirectories(baseWorkingDirectory);

            // meshing settings
            //-----------------

            OFMeshSettings MeshSettings = new OFMeshSettings(); // sets default mesh settings

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

            RunSnappy.Run(DOM, MeshSettings, RunSettings, out string logfileOutput);

            #endregion RUN SNAPPY HEX

            #region RUN SIMULATION

            if (RunSettings.iter == 0 || RunSettings.keepTimeSteps == 0 || RunSettings.writeInterval == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid inputs.");
                return;
            }

            #region Trees

            if (DOM.Trees.Count > 0)
            {
                var trees = new TreeObject(DOM, MeshSettings);
            }
            else
            {
                TreeObject.RemoveDicts(DOM, MeshSettings);
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
                Utilities.DeletePhi(MeshSettings, DOM);
                Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\Scripts\run.bat", taskComplete);
            }
            else if (runMeshing == true && runSimulation == false && canRun)
            {
                Utilities.DeletePhi(MeshSettings, DOM);
                Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\Scripts\run_mesh.bat", taskComplete);
            }
            else if (runMeshing == false && runSimulation == true && canRun)
            {
                Utilities.DeletePhi(MeshSettings, DOM);
                Utilities.StartProcess.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\Scripts\run_sim_all.bat", taskComplete);
            }

            #endregion START PROCESSES

            OFResult RES = new OFResult(DOM, RunSettings, MeshSettings, baseWorkingDirectory);
            DA.SetData(GH_Strings.Common.Result, RES);

            var windDirs = DOM?.BCond?.WindDirections ?? new System.Collections.Generic.List<int>();

            var parseOptions = new OpenFOAMLogParseOptions { RollingWindow = 5 };

            var meshLog = OpenFOAMLogLocator.FindLatestMeshingLog(MeshSettings.meshWorkingDir);
            var meshStatus = OpenFOAMLogParser.ParseMeshingLog(meshLog, parseOptions);

            var simDone = new System.Collections.Generic.List<bool>(windDirs.Count);
            var simEta = new System.Collections.Generic.List<string>(windDirs.Count);
            foreach (var dir in windDirs)
            {
                var caseDir = Path.Combine(baseWorkingDirectory, dir.ToString());
                var simLog = OpenFOAMLogLocator.FindLatestSimulationLog(caseDir);
                var simStatus = OpenFOAMLogParser.ParseSimulationLog(simLog,
                    new OpenFOAMLogParseOptions { RollingWindow = 5, TotalIterations = RunSettings.iter });

                simDone.Add(simStatus.IsFinished);
                simEta.Add(OpenFOAMStatusFormatter.FormatEta(simStatus));
            }

            DA.SetData(GH_Strings.SimpleFoam.MeshDone, meshStatus.IsFinished);
            DA.SetDataList(GH_Strings.SimpleFoam.SimDone, simDone);
            DA.SetData(GH_Strings.SimpleFoam.MeshEta, OpenFOAMStatusFormatter.FormatEta(meshStatus));
            DA.SetDataList(GH_Strings.SimpleFoam.SimEta, simEta);

            canRun = true;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// </summary>
        protected override System.Drawing.Bitmap Icon => Resources.Eddy_simulation;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{7FF4A70C-DB4E-473C-BDC0-606CE58A979A}");
    }
}
