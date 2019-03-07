using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
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
              "Eddy", "Simulation")
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
        public bool runWithBlueCFD = true;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Dom", "Domain", GH_ParamAccess.item);

            pManager.AddGenericParameter("Mesh Settings", "MSet", "Mesh Settings", GH_ParamAccess.item);
            pManager[1].Optional = true;

            pManager.AddGenericParameter("Run Settings", "RSet", "Run Settings", GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Eddy"));


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
            if (runWithBlueCFD) { this.Message = "BlueCFD"; }
            else { this.Message = "Docker"; }



            // read inputs
            //------------

            // domain
            //-------
            OFCylDomain CylDom;
            OFBoxDomain BoxDom;
            OFBaseDomain DOM;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

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

            // meshing settings
            //-----------------

            OFMeshSettings MeshSettings = new OFMeshSettings(); // sets default mesh settings
            GH_ObjectWrapper gobjMeshSet = null;
            if (DA.GetData(1, ref gobjMeshSet)) { }
            if ((gobj.Value is OFMeshSettings))
            {
                MeshSettings = (OFMeshSettings)gobjMeshSet.Value;
            }

            // run settings
            //-----------------

            OFRunSettings RunSettings = new OFRunSettings(); // sets default mesh settings
            GH_ObjectWrapper gobjRunSet = null;
            if (DA.GetData(2, ref gobjRunSet)) { }
            if ((gobj.Value is OFRunSettings))
            {
                RunSettings = (OFRunSettings)gobjRunSet.Value;
            }


            // working directory
            //------------------

            string baseWorkingDirectory = "";
            DA.GetData("Directory", ref baseWorkingDirectory);
            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }

            // @ Patrick:... is this system check only relevant for BoxDomain?
            bool isWindows7 = Utilities.IsWindows7;
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);       
            if (isWindows7)
            {
                if (!baseWorkingDirectory.StartsWith(userFolder, StringComparison.InvariantCultureIgnoreCase))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "For Windows 7 and 8, the working directory must be in the user folder because of constraint with a deprecated Docker version.."); return;
                }
            }

            //Fix paths
            // @ Patrick:... what does this function do? Is this only relevant for CylDOM?
            baseWorkingDirectory = Utilities.FixDirectories(baseWorkingDirectory);











            // @ Patrick: This is really bad practice. Never set fields from outside of a class constructor
            DOM.iter = RunSettings.iter;
            DOM.writeInterval = RunSettings.writeInterval;
            DOM.keepTimeSteps = RunSettings.keepTimeSteps;
            DOM.turbulenceModel = RunSettings.turb;


 




            // @ Patrick: Make all of these regions static functions that live in the EddyLib DLL

            #region RUN BLOCKMESH

            #endregion





            #region RUN SNAPPY HEX


            string SingleCPU = @"""surfaceFeatureExtract;snappyHexMesh -overwrite """; //-overwrite
            string MultipleCPU = @"""surfaceFeatureExtract;pyFoamDecompose.py --clear . " + DOM.CPUs + @"; foamJob -parallel -screen snappyHexMesh -overwrite""";




            // @ Patrick: This is really bad practice. Never set fields from outside of a class constructor. This probaly now also breaks the code 
            //needed for meshing purposes at this point in time
            //DOM.iter = 1000;
            //DOM.writeInterval = 10;
            //DOM.keepTimeSteps = 2;

            DOM.meshingMode = MeshSettings.mode;




            if (MeshSettings.accBuilding >= 5 || MeshSettings.accFeatures >= 5 || MeshSettings.accRefinement >= 5 || MeshSettings.accGround >= 5 || MeshSettings.nLayers >= 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A high number of refinment stages might significantly slow down mesh creation. Try to create a reasonable fine mesh with the Domain component.");
            }

            // Check for killed processes
            if (Utilities.DidProcessGetKilled(DOM.meshWorkingDir) == true)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some processes got killed probably because to little RAM was available. Try to increase the RAM acclocated for the Docker virtual machine.");
            }


            // @ Patrick: Make cleaning function and handle behavior.. set to always true now
            #region cleanup
            //CLEAN UP THE MESS
            bool Clean = true;
            if (Clean == true)
            {

                if (Directory.Exists(DOM.meshPolyMeshDir))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(DOM.meshPolyMeshDir);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }

                if (Directory.Exists(DOM.meshConstantDir + @"extendedFeatureEdgeMesh"))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(DOM.meshConstantDir + @"extendedFeatureEdgeMesh");
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }


                if (DOM.CPUs > 1)
                {


                    for (int i = 0; i < DOM.CPUs; i++)
                    {
                        var path = DOM.meshWorkingDir + @"\processor" + i;
                        if (Directory.Exists(path))
                        {
                            System.IO.DirectoryInfo di = new DirectoryInfo(path);
                            foreach (FileInfo file in di.GetFiles())
                            {
                                file.Delete();
                            }
                            foreach (DirectoryInfo dir in di.GetDirectories())
                            {
                                dir.Delete(true);
                            }
                            di.Delete();

                        }
                    }

                    // Delete proc folders

                    for (int i = 0; i < DOM.CPUs; i++)
                    {
                        var meshPath = DOM.meshWorkingDir + @"\processor" + i;
                        if (Directory.Exists(meshPath))
                        {
                            System.IO.DirectoryInfo di = new DirectoryInfo(meshPath);
                            foreach (FileInfo file in di.GetFiles())
                            {
                                file.Delete();
                            }
                            foreach (DirectoryInfo dir in di.GetDirectories())
                            {
                                dir.Delete(true);
                            }
                            di.Delete();
                        }

                        for (int l = 0; l < DOM.BCInflow.windDirs.Count; l++)
                        {

                            var cpuPath = DOM.baseWorkingDir + DOM.BCInflow.windDirs[l] + @"\processor" + i;

                            if (Directory.Exists(cpuPath))
                            {
                                System.IO.DirectoryInfo di = new DirectoryInfo(cpuPath);
                                foreach (FileInfo file in di.GetFiles())
                                {
                                    file.Delete();
                                }
                                foreach (DirectoryInfo dir in di.GetDirectories())
                                {
                                    dir.Delete(true);
                                }
                                di.Delete();

                            }

                        }
                    }
                }

            }
#endregion





            var meshStlDir = DOM.meshWorkingDir + @"\constant\triSurface\";
            var meshStlFilenameBuildings = DOM.meshWorkingDir + @"\constant\triSurface\building.stl";
            var meshStlFilenameGround = DOM.meshWorkingDir + @"\constant\triSurface\ground.stl";

            if (!Directory.Exists(meshStlDir))
            {
                Directory.CreateDirectory(meshStlDir);
            }


            if (!File.Exists(DOM.meshWorkingDir + @"\log"))
            {
                File.WriteAllText(DOM.meshWorkingDir + @"\log", "");
            }



            if (!Directory.Exists(DOM.meshSystemDir))
            {
                Directory.CreateDirectory(DOM.meshSystemDir);
            }

            Point3d locationInMesh = new Point3d();
            locationInMesh = DOM.locationInMesh;




            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "snappyHexMeshDict"), EddyLib.StrTemp.OFExecDicts.SnappyHexMeshDict(DOM));
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "surfaceFeatureExtractDict"), EddyLib.StrTemp.OFExecDicts.SurfaceFeatureExtractDict());
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "fvSolution"), EddyLib.StrTemp.OFExecDicts.FvSolution(0));
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "meshQualityDict"), EddyLib.StrTemp.OFExecDicts.MeshQualityDict());
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "decomposeParDict"), EddyLib.StrTemp.OFExecDicts.DecomposeParDict(DOM));

            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "run_checkBadMesh.bat"), EddyLib.StrTemp.BatFiles.Run_checkBadMesh(DOM));



            //Autocalc number of CPUs
            if (DOM.autoCPUCalc == true)
            {
                DOM.CPUs = Utilities.CPUAutoCalc(DOM.meshWorkingDir, DOM.CPUs);
            }




            string command = DOM.CPUs > 1 ? MultipleCPU : SingleCPU;


            string logFile = "";

            using (FileStream stream = File.Open(DOM.meshWorkingDir + @"\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    logFile = reader.ReadToEnd();
                }
            }

            //DA.SetData(0, logFile);
            //DA.SetData(1, DOM);

            #endregion



            #region RUN SIMULATION


            if (Utilities.CheckLicence() == true)
            {



                // Check if Docker is running if Docker is the sim engine

                if (DOM.simEngine == 0)
                {

                    Utilities.WriteDockerInfo(DOM.baseWorkingDir);
                    bool dockerRunning = false;
                    if (Utilities.IsDockerRunning(DOM.baseWorkingDir, DOM.IsWindows7))
                    {
                        dockerRunning = true;
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Blank, @"It seems that Docker is not running. Please start the application ""Docker for Windows"".");
                    }

                    // Check for killed processes


                    for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                    {
                        if (Utilities.DidProcessGetKilled(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i]) == true)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some processes got killed probably because to little RAM was available. Try to increase the RAM acclocated for the Docker virtual machine.");
                        }

                    }

                }



                if (RunSettings.iter == 0 || RunSettings.keepTimeSteps == 0 || RunSettings.writeInterval == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid inputs.");
                }




                //Autocalc number of CPUs
                if (DOM.autoCPUCalc == true)
                {
                    DOM.CPUs = Utilities.CPUAutoCalc(DOM.meshWorkingDir, DOM.CPUs);
                }



                for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                {

                    var simStlDir = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\constant\triSurface\";
                    var simStlFilenameBuildings = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\constant\triSurface\building.stl";
                    var simStlFilenameGround = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\constant\triSurface\ground.stl";
                    var simConstantDir = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\constant\";
                    var simBoundaryConditionsDir = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\0.org\";
                    var simBoundaryConditionsDirTemp = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\0\";

                    if (!Directory.Exists(simStlDir))
                    {
                        Directory.CreateDirectory(simStlDir);
                    }

                    if (!Directory.Exists(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\"))
                    {
                        Directory.CreateDirectory(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\");
                    }
                    if (!Directory.Exists(simConstantDir))
                    {
                        Directory.CreateDirectory(simConstantDir);
                    }
                    if (!Directory.Exists(simBoundaryConditionsDir))
                    {
                        Directory.CreateDirectory(simBoundaryConditionsDir);
                    }
                    if (!Directory.Exists(simBoundaryConditionsDirTemp))
                    {
                        Directory.CreateDirectory(simBoundaryConditionsDirTemp);
                    }

                    File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\" + "controlDict"), EddyLib.StrTemp.OFExecDicts.ControlDict(DOM, null, i));
                    File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\case.foam"), "");

                    // Not working currently: Symbolic dir junctions only for cylindrical domain, and, if they exist, delete them for boxDomain

                    //if (DOM is OFCylDomain)
                    //{
                    string symbolicPath = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\constant\polyMesh";
                    if (Directory.Exists(symbolicPath) && !SymlinkCreator.IsSymbolic(symbolicPath))
                    {
                        SymlinkCreator.Delete(symbolicPath);
                        SymlinkCreator.Create(symbolicPath, DOM.meshConstantDir + @"\polyMesh");
                    }
                    else
                    {
                        SymlinkCreator.Create(symbolicPath, DOM.meshConstantDir + @"\polyMesh");
                    }

                    //Constant folder
                    File.WriteAllText(Path.Combine(simConstantDir + "turbulenceProperties"), EddyLib.StrTemp.OFExecDicts.TurbulenceProperties(DOM));
                    File.WriteAllText(Path.Combine(simConstantDir + "transportProperties"), EddyLib.StrTemp.OFExecDicts.TransportProperties());



                    if (DOM is OFBoxDomain)
                    {
                        if (DOM.BCInflow.btype is BoundaryType.abl)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.UBoxABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLConditions(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.StrTemp.BCDicts.UBoxABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLConditions(DOM, i));
                        }
                        if (DOM.BCInflow.btype is BoundaryType.constant)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.UBoxConstU(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.StrTemp.BCDicts.UBoxConstU(DOM, i));
                        }

                        //REmove this later
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond_Cyl(DOM, i));
                        //REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.StrTemp.BCDicts.P(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.StrTemp.BCDicts.Omega(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.StrTemp.BCDicts.K(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.StrTemp.BCDicts.Epsilon(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.StrTemp.BCDicts.Nut(DOM));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "p"), EddyLib.StrTemp.BCDicts.P(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "omega"), EddyLib.StrTemp.BCDicts.Omega(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "k"), EddyLib.StrTemp.BCDicts.K(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "epsilon"), EddyLib.StrTemp.BCDicts.Epsilon(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "nut"), EddyLib.StrTemp.BCDicts.Nut(DOM));


                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.StrTemp.BCDicts.InitialConditions(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "initialConditions"), EddyLib.StrTemp.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\snappyHexMeshDict"), EddyLib.StrTemp.OFExecDicts.SnappyHexMeshDict(DOM));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\surfaceFeatureExtractDict"), EddyLib.StrTemp.OFExecDicts.SurfaceFeatureExtractDict());

                        if (RunSettings.mode == 0)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (RunSettings.mode == 1)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }
                        else if (RunSettings.mode == 2)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho70_80());
                        }
                        else if (RunSettings.mode == 3)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho60_70());
                        }
                        else if (RunSettings.mode == 4)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho40_60());
                        }

                        else if (RunSettings.mode == 5)

                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (RunSettings.mode == 6)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurateOscillatory());
                        }
                        else if (RunSettings.mode == 7)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSolution"), EddyLib.StrTemp.OFExecDicts.FvSolution(0));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\meshQualityDict"), EddyLib.StrTemp.OFExecDicts.MeshQualityDict());
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\residuals"), EddyLib.StrTemp.OFExecDicts.ResidualsDict());


                    }
                    else if (DOM is OFCylDomain)
                    {
                        if (DOM.BCInflow.btype is BoundaryType.abl)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.U_CylABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond_Cyl(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.StrTemp.BCDicts.U_CylABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond_Cyl(DOM, i));
                        }
                        if (DOM.BCInflow.btype is BoundaryType.constant)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.UCylConstU(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.StrTemp.BCDicts.UCylConstU(DOM, i));
                        }

                        //REmove this later
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond_Cyl(DOM, i));
                        //REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.StrTemp.BCDicts.P_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.StrTemp.BCDicts.Omega_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.StrTemp.BCDicts.K_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.StrTemp.BCDicts.Epsilon_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.StrTemp.BCDicts.Nut_Cyl(DOM, i));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "p"), EddyLib.StrTemp.BCDicts.P_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "omega"), EddyLib.StrTemp.BCDicts.Omega_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "k"), EddyLib.StrTemp.BCDicts.K_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "epsilon"), EddyLib.StrTemp.BCDicts.Epsilon_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "nut"), EddyLib.StrTemp.BCDicts.Nut_Cyl(DOM, i));


                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.StrTemp.BCDicts.InitialConditions(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "initialConditions"), EddyLib.StrTemp.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\snappyHexMeshDict"), EddyLib.StrTemp.OFExecDicts.SnappyHexMeshDict(DOM));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\surfaceFeatureExtractDict"), EddyLib.StrTemp.OFExecDicts.SurfaceFeatureExtractDict());
                        if (RunSettings.mode == 0)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (RunSettings.mode == 1)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }
                        else if (RunSettings.mode == 2)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho70_80());
                        }
                        else if (RunSettings.mode == 3)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho60_70());
                        }
                        else if (RunSettings.mode == 4)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho40_60());
                        }
                        else if (RunSettings.mode == 5)

                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (RunSettings.mode == 6)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurateOscillatory());
                        }
                        else if (RunSettings.mode == 7)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSolution"), EddyLib.StrTemp.OFExecDicts.FvSolution(RunSettings.mode));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\meshQualityDict"), EddyLib.StrTemp.OFExecDicts.MeshQualityDict());
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\residuals"), EddyLib.StrTemp.OFExecDicts.ResidualsDict());
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\decomposeParDict"), EddyLib.StrTemp.OFExecDicts.DecomposeParDict(DOM));


                    }



                }



                // Batch files depending on type


                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_mesh.bat"), EddyLib.StrTemp.BatFiles.Run_Mesh_Cyl(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run.bat"), EddyLib.StrTemp.BatFiles.Run(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_sim_all.bat"), EddyLib.StrTemp.BatFiles.RunSimOnly(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_ray.bat"), EddyLib.StrTemp.BatFiles.Run_RayTrace(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_probes.bat"), EddyLib.StrTemp.BatFiles.Run_Probes(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_utci.bat"), EddyLib.StrTemp.BatFiles.Run_UTCI(DOM));
#if DEBUG
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_blockMesh.bat"), EddyLib.StrTemp.BatFiles.run_blockMesh(DOM));
#endif

                for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                {
                    File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + "_run_sim.bat"), EddyLib.StrTemp.BatFiles.Run_sim(DOM, i));
                    File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + "_run_sim_continue.bat"), EddyLib.StrTemp.BatFiles.Run_sim_continue(DOM, i));

                }



                // Calculate runtimes of all simulation based on log file

                for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                {
                    DOM.runtimes.Add(Utilities.CalculateRunTimeFromLog(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i], DOM.iter));
                }



                #endregion



                // @ Patrick: This component should output a Result Class - not just the domain. the domain shoudld not know the working directory...
                OFResult RES = new OFResult(DOM, RunSettings, MeshSettings);
                DA.SetData(0, RES);


            }

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_foam;//return null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{7FF4A70C-DB4E-473C-BDC0-606CE58A979A}");
    }
}
