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
          : base("Simulation", "Simulation",
              "Simulation" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation domain", "Dom", "Eddy simulation domain", GH_ParamAccess.item);
            pManager.AddTextParameter("Working directory", "Dir", "Folder for simulation files. Use a simple name (e.g., 'MyProject') to create under %AppData%\\Eddy3D\\Cases, or provide a full path.", GH_ParamAccess.item, "DefaultCase");
            pManager[1].Optional = true;
            pManager.AddGenericParameter("Mesh Settings", "MSet", "Mesh Settings", GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddGenericParameter("Run Settings", "RSet", "Run Settings", GH_ParamAccess.item);
            pManager[3].Optional = true;

            pManager.AddBooleanParameter("Run Meshing", "RunMsh", "Run Meshing", GH_ParamAccess.item, false);

            pManager.AddBooleanParameter("Make Trees", "MakeTrees", "Create Tree Topologies", GH_ParamAccess.item, false);

            pManager.AddBooleanParameter("Run Simulation", "RunSim", "Run Simulation", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation result", "Res", "Eddy simulation result", GH_ParamAccess.item);
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
            // mode to select simulation environment
            Message = "BlueCFD";

            // read inputs
            //------------

            // domain
            //-------

            OFBaseDomain DOM;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData("Simulation domain", ref gobj)) { }

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
            if (DA.GetData("Run Settings", ref gobjRunSet))
            {
                if (gobjRunSet.Value is OFRunSettings)
                {
                    RunSettings = (OFRunSettings)gobjRunSet.Value;
                }
            }

            // Error Handling

            if (!RunSettings.IdenticalMPI && RunSettings.CPUs > 1 && RunSettings.BlueCFDIsInstalled)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "In order to use multiple CPUs, you need to ensure to use the same msmpi.dll for both Windows and BlueCFD. This is a BlueCFD issue and will hopefully be fixed in a future version."); return;
            }



            // working directory
            //------------------

            string baseWorkingDirectory = "";
            DA.GetData("Working directory", ref baseWorkingDirectory);
            
            // Resolve simple case names to full paths under AppData\Eddy3D\Cases
            baseWorkingDirectory = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(baseWorkingDirectory);
            
            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }

            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (RunSettings.ostype == OSType.Windows7 && (RunSettings.simEngine == SimEngine.Docker))
            {
                //if (!baseWorkingDirectory.StartsWith(userFolder, StringComparison.InvariantCultureIgnoreCase))
                //{
                //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "For Windows 7 and 8, the working directory must be in the user folder because of constraint with a deprecated Docker version.."); return;
                //}
            }
            baseWorkingDirectory = Utilities.Directories.FixDirectories(baseWorkingDirectory);

            // meshing settings
            //-----------------

            OFMeshSettings MeshSettings = new OFMeshSettings(); // sets default mesh settings

            GH_ObjectWrapper gobjMeshSet = null;
            if (DA.GetData("Mesh Settings", ref gobjMeshSet))
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

            // Export Frontage PNGs

            //var directory = baseWorkingDirectory + @"FrontageImages\";
            //foreach (int dir in DOM.BCond.windDirs)
            //{
            //    RunBlockMesh.SaveFrontagePNGs(directory, dir, DOM.FrontagePNGs);
            //}

            //string logFile = "";

            //using (FileStream stream = File.Open(baseWorkingDirectory + @"\mesh\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //{
            //    using (StreamReader reader = new StreamReader(stream))
            //    {
            //        logFile = reader.ReadToEnd();

            //    }
            //}

            #endregion RUN BLOCKMESH

            #region RUN SNAPPY HEX

            // Check for killed processes
            if (Utilities.DidProcessGetKilled(MeshSettings.meshWorkingDir) == true)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Some processes got killed probably because to little RAM was available. Try to increase the RAM acclocated for the Docker virtual machine.");
                return;
            }

            //TODO: output the logs somewhere!

            RunSnappy.Run(DOM, MeshSettings, RunSettings, out string logfileOutput);

            #endregion RUN SNAPPY HEX

            #region RUN SIMULATION

            // Check if Docker is running if Docker is the sim engine



            if (RunSettings.iter == 0 || RunSettings.keepTimeSteps == 0 || RunSettings.writeInterval == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid inputs.");
                return;
            }

            #region Trees

            var treeDict = new List<TopoSetSubDict>();
            var fvOptionsDict = new FunctionObjectDict(treeDict,"fvOptions");
            //var topoSetDict = new TopoSetDict(treeDict, DOM.LocationInMesh);

            if (DOM.Trees.Count > 0)

            {
                //foreach (var tree in DOM.Trees)
                //{
                //    var tDict = new MomentumSinkInternalDictOutdoor(tree, DOM.LocationInMesh);
                //    treeDict.Add(tDict);
                //}

                //fvOptionsDict = new FunctionObjectDict(treeDict, "fvOptions");
                //topoSetDict = new TopoSetDict(treeDict, DOM.LocationInMesh);
            }
            else
            {
                fvOptionsDict.RemoveDict(baseWorkingDirectory);
                //topoSetDict.RemoveDict(baseWorkingDirectory);
            }

            #endregion Trees

            RunFoamSimulation.Run(DOM, MeshSettings, RunSettings, baseWorkingDirectory);

            #endregion RUN SIMULATION

            #region START PROCESSES

            bool makeTrees = false;
            bool runSimulation = false;
            bool runMeshing = false;

            DA.GetData("Make Trees", ref makeTrees);
            DA.GetData("Run Simulation", ref runSimulation);
            DA.GetData("Run Meshing", ref runMeshing);

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
            DA.SetData(0, RES);

            canRun = true;
        }

        // hidden parameter
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.tertiary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                Resources.Eddy_simulation;//return null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{7FF4A70C-DB4E-473C-BDC0-606CE58A979A}");
    }
}