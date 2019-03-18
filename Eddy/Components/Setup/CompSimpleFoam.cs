using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.IO;
using System.Windows.Forms;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class SimpleFoam : GH_Component
    {


        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SimpleFoam()
          : base("Simulation", "Simulation",
              "Simulation",
              "Eddy", "1 | Setup")
        {
        }


        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Use Docker OpenFOAM", Menu_DoClick, true, !runWithBlueCFD);
        }

        private void Menu_DoClick(object sender, EventArgs e)
        {
            runWithBlueCFD = !runWithBlueCFD;
            ExpireSolution(true);

        }
        //public bool runWithBlueCFD = true;
        public bool runWithBlueCFD;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Dom", "Domain", GH_ParamAccess.item);

            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Eddy"));


            pManager.AddGenericParameter("Mesh Settings", "MSet", "Mesh Settings", GH_ParamAccess.item);
            pManager[1].Optional = true;

            pManager.AddGenericParameter("Run Settings", "RSet", "Run Settings", GH_ParamAccess.item);
            pManager[2].Optional = true;


            pManager.AddBooleanParameter("Run Meshing", "RunMsh", "RunMsh", GH_ParamAccess.item, false);

            pManager.AddBooleanParameter("Run Simulation", "RunSim", "RunSim", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Result", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // mode to select simulation environment
            if (runWithBlueCFD) { Message = "BlueCFD"; }
            else { Message = "Docker"; }




            // read inputs
            //------------

            // domain
            //-------
            OFCylDomain CylDom;
            OFBoxDomain BoxDom;
            OFBaseDomain DOM;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData("Domain", ref gobj)) { }

            if ((gobj.Value is OFCylDomain))
            {
                CylDom = (OFCylDomain)gobj.Value;
                DOM = (OFBaseDomain)gobj.Value;
            }
            else if ((gobj.Value is OFBoxDomain))
            {
                BoxDom = (OFBoxDomain)gobj.Value;
                DOM = (OFBaseDomain)gobj.Value;
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid domain object"); return;
            }



            // run settings
            //-----------------

            OFRunSettings RunSettings = new OFRunSettings(); // sets default mesh settings
            GH_ObjectWrapper gobjRunSet = null;
            if (DA.GetData("Run Settings", ref gobjRunSet))
            {
                if (gobjRunSet.Value is OFRunSettings)
                {
                    RunSettings = (OFRunSettings)gobjRunSet.Value;

                }
            }

            //crashes rhino
            if (!runWithBlueCFD)
            {
                RunSettings.simEngine = SimEngine.Docker;
            }



            // working directory
            //------------------

            string baseWorkingDirectory = "";
            DA.GetData("Directory", ref baseWorkingDirectory);
            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }

            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (RunSettings.ostype == OSType.Windows7 && (RunSettings.simEngine == SimEngine.Docker))
            {
                //if (!baseWorkingDirectory.StartsWith(userFolder, StringComparison.InvariantCultureIgnoreCase))
                //{
                //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "For Windows 7 and 8, the working directory must be in the user folder because of constraint with a deprecated Docker version.."); return;
                //}

            }
            baseWorkingDirectory = Utilities.FixDirectories(baseWorkingDirectory);



            // meshing settings
            //-----------------

            OFMeshSettings MeshSettings = new OFMeshSettings(); // sets default mesh settings            

            GH_ObjectWrapper gobjMeshSet = null;
            if (DA.GetData("Mesh Settings", ref gobjMeshSet)) { }
            if (gobjMeshSet.Value is OFMeshSettings)
            {
                MeshSettings = (OFMeshSettings)gobjMeshSet.Value;
            }
            MeshSettings.SetDirectories(baseWorkingDirectory);





            // @ Patrick: Make all of these regions static functions that live in the EddyLib DLL

            #region RUN BLOCKMESH

            if (DOM is OFBoxDomain)
            {
                if (DOM.BCond.windDirs.Count > 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "For box-shaped domains you can only pass one wind direction per simulation setup."); return;
                }


                RunBlockMesh.RunBox((OFBoxDomain)DOM, MeshSettings, RunSettings, baseWorkingDirectory);


            }
            else
            {


                RunBlockMesh.RunCyl((OFCylDomain)DOM, MeshSettings, RunSettings, baseWorkingDirectory);



            }

            //string logFile = "";

            //using (FileStream stream = File.Open(baseWorkingDirectory + @"\mesh\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //{
            //    using (StreamReader reader = new StreamReader(stream))
            //    {
            //        logFile = reader.ReadToEnd();

            //    }
            //}



            #endregion





            #region RUN SNAPPY HEX

            if (MeshSettings.accBuildings >= 5 || MeshSettings.accFeatures >= 5 || MeshSettings.accRefinement >= 5 || MeshSettings.accGround >= 5 || MeshSettings.nLayers >= 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A high number of refinment stages might significantly slow down mesh creation. Try to create a reasonable fine mesh with the Domain component.");
            }

            // Check for killed processes
            if (Utilities.DidProcessGetKilled(MeshSettings.meshWorkingDir) == true)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some processes got killed probably because to little RAM was available. Try to increase the RAM acclocated for the Docker virtual machine.");
                return;
            }


            //TODO: output the logs somewhere!

            RunSnappy.Run(DOM, MeshSettings, RunSettings, out string logfileOutput);


            #endregion




            #region RUN SIMULATION

            // Check if Docker is running if Docker is the sim engine

            if (RunSettings.simEngine == SimEngine.Docker)
            {

                Utilities.WriteDockerInfo(baseWorkingDirectory);

                if (!Utilities.IsDockerRunning(baseWorkingDirectory, RunSettings.ostype))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Blank, @"It seems that Docker is not running. Please start the application ""Docker for Windows"".");
                }

                // Check for killed processes


                for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
                {
                    if (Utilities.DidProcessGetKilled(baseWorkingDirectory + "\\" + DOM.BCond.windDirs[i]) == true)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some processes got killed probably because to little RAM was available. Try to increase the RAM acclocated for the Docker virtual machine.");
                    }

                }

            }



            if (RunSettings.iter == 0 || RunSettings.keepTimeSteps == 0 || RunSettings.writeInterval == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid inputs.");
            }


            RunFoamSimulation.Run(DOM, MeshSettings, RunSettings, baseWorkingDirectory);

            #endregion



            #region START PROCESSES

            bool runSimulation = false;
            bool runMeshing = false;

            DA.GetData("Run Simulation", ref runSimulation);
            DA.GetData("Run Meshing", ref runMeshing);

            if (runMeshing == true)
            {
                Utilities.DeletePhi(MeshSettings, DOM);
                Utilities.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\run_mesh.bat");
            }

            if (runSimulation == true)
            {
                Utilities.DeletePhi(MeshSettings, DOM);
                Utilities.StartProcessCMDNT("", false, true, false, true, baseWorkingDirectory + @"\run_sim_all.bat");
            }



            #endregion


            OFResult RES = new OFResult(DOM, RunSettings, MeshSettings, baseWorkingDirectory);
            DA.SetData(0, RES);


        }



        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_simulation;//return null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{7FF4A70C-DB4E-473C-BDC0-606CE58A979A}");
    }
}
