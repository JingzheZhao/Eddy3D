using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System;
using System.IO;
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



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Iterations", "Iter", "Specify the number of iterations to be simulated.", GH_ParamAccess.item, 1000);
            pManager.AddIntegerParameter("WriteInterval", "WriteInt", "Simulation write interval.", GH_ParamAccess.item, 20);
            pManager.AddIntegerParameter("KeepTimeSteps", "TimeSteps", "Number of time steps to keep in simulation folder..", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("Turbulence", "Turb", "Turbulence model.", GH_ParamAccess.item, 0);
            Param_Integer turb = pManager[4] as Param_Integer;
            turb.AddNamedValue("kEpsilon (quick)", 0);
            turb.AddNamedValue("RNGkEpsilon (more accurate)", 1);
            turb.AddNamedValue("kOmegaSST (most accurate)", 2);
            pManager.AddIntegerParameter("Mode", "Mode", "Robustness of the solver", GH_ParamAccess.item, 0);
            Param_Integer simulationMode = pManager[5] as Param_Integer;
            simulationMode.AddNamedValue("quick", 0);
            simulationMode.AddNamedValue("robust", 1);
            simulationMode.AddNamedValue("orthogonal (70-80)", 2);
            simulationMode.AddNamedValue("orthogonal (60-70)", 3);
            simulationMode.AddNamedValue("orthogonal (40-60)", 4);
            simulationMode.AddNamedValue("accurate and stable", 5);
            simulationMode.AddNamedValue("more accurate but oscillatory", 6);
            simulationMode.AddNamedValue("robust but diffusive", 7);


            //pManager.AddGenericParameter("Type", "Bcond", "", GH_ParamAccess.item);


            //  pManager.AddBooleanParameter("Clean", "Clean", "Run the solver.", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //string filepath = @"C:\OF\";




            // OFBaseDomain DOM = null;
            // if (!DA.GetData(0, ref DOM)) { return; }


            // we can use class inheritance 

            OFBaseDomain DOM = null;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is OFBaseDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }





            // string SingleCPU = @"""pyFoamPrepareCase.py . --no-mesh-create;simpleFoam""";
            string MultipleCPU = @"""renumberMesh -overwrite;pyFoamPrepareCase.py . --no-mesh-create;pyFoamDecompose.py --clear . """ + DOM.CPUs + @""";pyFoamRunner.py --autosense-parallel simpleFoam""";


            int iter = 1000;
            int writeInterval = 10;
            int keepTimeSteps = 2;
            int mode = 0;
            int turb = 0;




            DA.GetData(1, ref iter);
            DA.GetData(2, ref writeInterval);
            DA.GetData(3, ref keepTimeSteps);
            DA.GetData(4, ref turb);
            DA.GetData(5, ref mode);

            // DA.GetData(6, ref Run);

            //Make sure that all fields are always written
            if (iter < writeInterval)
            {
                writeInterval = iter;
            }

            DOM.iter = iter;
            DOM.writeInterval = writeInterval;
            DOM.keepTimeSteps = keepTimeSteps;

            DOM.turbulenceModel = turb;





            if (Utilities.CheckLicence() == true)
            {



                // Check if Docker is running

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





                if (iter == 0 || keepTimeSteps == 0 || writeInterval == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid inputs.");
                }


                // Check for killed processes
                for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                {
                    if (Utilities.DidProcessGetKilled(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i]) == true)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some processes got killed probably because to little RAM was available. Try to increase the RAM acclocated for the Docker virtual machine.");
                    }

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
                        }
                        if (DOM.BCInflow.btype is BoundaryType.constant) { File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.UBoxConstU(DOM, i)); }

                        //REmove this later
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond_Cyl(DOM, i));
                        //REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.StrTemp.BCDicts.P(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.StrTemp.BCDicts.Omega(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.StrTemp.BCDicts.K(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.StrTemp.BCDicts.Epsilon(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.StrTemp.BCDicts.Nut(DOM));


                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.StrTemp.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\snappyHexMeshDict"), EddyLib.StrTemp.OFExecDicts.SnappyHexMeshDict(DOM));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\surfaceFeatureExtractDict"), EddyLib.StrTemp.OFExecDicts.SurfaceFeatureExtractDict());

                        if (mode == 0)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (mode == 1)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }
                        else if (mode == 2)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho70_80());
                        }
                        else if (mode == 3)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho60_70());
                        }
                        else if (mode == 4)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho40_60());
                        }

                        else if (mode == 5)

                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (mode == 6)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurateOscillatory());
                        }
                        else if (mode == 7)
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
                        }
                        if (DOM.BCInflow.btype is BoundaryType.constant) { File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.UCylConstU(DOM, i)); }

                        //REmove this later
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond_Cyl(DOM, i));
                        //REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.StrTemp.BCDicts.P_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.StrTemp.BCDicts.Omega_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.StrTemp.BCDicts.K_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.StrTemp.BCDicts.Epsilon_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.StrTemp.BCDicts.Nut_Cyl(DOM, i));


                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.StrTemp.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\snappyHexMeshDict"), EddyLib.StrTemp.OFExecDicts.SnappyHexMeshDict(DOM));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\surfaceFeatureExtractDict"), EddyLib.StrTemp.OFExecDicts.SurfaceFeatureExtractDict());
                        if (mode == 0)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (mode == 1)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }
                        else if (mode == 2)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho70_80());
                        }
                        else if (mode == 3)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho60_70());
                        }
                        else if (mode == 4)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho40_60());
                        }
                        else if (mode == 5)

                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (mode == 6)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurateOscillatory());
                        }
                        else if (mode == 7)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\fvSolution"), EddyLib.StrTemp.OFExecDicts.FvSolution(mode));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\meshQualityDict"), EddyLib.StrTemp.OFExecDicts.MeshQualityDict());
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + @"\system\residuals"), EddyLib.StrTemp.OFExecDicts.ResidualsDict());


                    }

                    

                }
              
              
                
                // Batch files depending on type
                

                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_mesh.bat"), EddyLib.StrTemp.DockerBatFiles.Run_Mesh_Cyl(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run.bat"), EddyLib.StrTemp.DockerBatFiles.Run(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_sim_all.bat"), EddyLib.StrTemp.DockerBatFiles.RunSimOnly(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_ray.bat"), EddyLib.StrTemp.DockerBatFiles.Run_RayTrace(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_probes.bat"), EddyLib.StrTemp.DockerBatFiles.Run_Probes(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_utci.bat"), EddyLib.StrTemp.DockerBatFiles.Run_UTCI(DOM));
#if DEBUG
                File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + "run_blockMesh.bat"), EddyLib.StrTemp.DockerBatFiles.run_blockMesh(DOM));
#endif

                for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                {
                    File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + "_run_sim.bat"), EddyLib.StrTemp.DockerBatFiles.Run_sim(DOM, i));
                    File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i] + "_run_sim_continue.bat"), EddyLib.StrTemp.DockerBatFiles.Run_sim_continue(DOM, i));

                }

                

                // Calculate runtimes of all simulation based on log file

                for (int i = 0; i < DOM.BCInflow.windDirs.Count; i++)
                {
                    DOM.runtimes.Add(Utilities.CalculateRunTimeFromLog(DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[i], DOM.iter));
                }

                DA.SetData(0, DOM);


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
