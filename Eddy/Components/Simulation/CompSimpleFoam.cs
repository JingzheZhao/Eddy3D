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
            string MultipleCPU = @"""renumberMesh -overwrite;pyFoamPrepareCase.py . --no-mesh-create;pyFoamDecompose.py --clear . """ + DOM.CPU + @""";pyFoamRunner.py --autosense-parallel simpleFoam""";


            int iter = 1000;
            int writeInterval = 10;
            int keepTimeSteps = 2;
            int mode = 0;
            int turb = 0;




            DA.GetData(1, ref iter);
            DA.GetData(3, ref keepTimeSteps);
            DA.GetData(2, ref writeInterval);
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

                Utilities.WriteDockerInfo(DOM.baseWorkingDirectory);
                bool dockerRunning = false;
                if (Utilities.IsDockerRunning(DOM.baseWorkingDirectory))
                {
                    dockerRunning = true;
                }
                if (dockerRunning == false)
                {

                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Docker is not running. Please start the application ""Docker for Windows"".");

                }





                if (iter == 0 || keepTimeSteps == 0 || writeInterval == 0)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide valid inputs.");
                }


                // Check for killed processes
                for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                {
                    if (Utilities.DidProcessGetKilled(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i]) == true)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some processes got killed probably because to little RAM was available. Try to increase the RAM acclocated for the Docker virtual machine.");
                    }

                }

                //Autocalc number of CPUs
                if (DOM.autoCPUCalc == true)
                {
                    DOM.CPU = Utilities.CPUAutoCalc(DOM.meshWorkingDirectory, DOM.CPU);
                }



                for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                {

                    var simStlDir = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\constant\triSurface\";
                    var simStlFilenameBuildings = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\constant\triSurface\building.stl";
                    var simStlFilenameGround = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\constant\triSurface\ground.stl";

                    if (!Directory.Exists(simStlDir))
                    {
                        Directory.CreateDirectory(simStlDir);
                    }


                    string simConstantDir = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\constant\";
                    string simBoundaryConditionsDir = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\0.org\";

                    if (!Directory.Exists(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\"))
                    {
                        Directory.CreateDirectory(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\");
                    }
                    if (!Directory.Exists(simConstantDir))
                    {
                        Directory.CreateDirectory(simConstantDir);
                    }
                    if (!Directory.Exists(simBoundaryConditionsDir))
                    {
                        Directory.CreateDirectory(simBoundaryConditionsDir);
                    }

                    File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\" + "controlDict"), StringTemplates.ControlDict(DOM, null, i));
                    File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\case.foam"), "");

                    // Not working currently: Symbolic dir junctions only for cylindrical domain, and, if they exist, delete them for boxDomain

                    //if (DOM is OFCylDomain)
                    //{
                    string symbolicPath = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\constant\polyMesh";
                    if (Directory.Exists(symbolicPath) && !SymlinkCreator.IsSymbolic(symbolicPath))
                    {
                        SymlinkCreator.Delete(symbolicPath);
                        SymlinkCreator.Create(symbolicPath, DOM.meshConstantDirectory + @"\polyMesh");
                    }
                    else
                    {
                        SymlinkCreator.Create(symbolicPath, DOM.meshConstantDirectory + @"\polyMesh");
                    }
                    //}
                    //else
                    //{
                    //    SymlinkCreator.Delete(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\constant\polyMesh");
                    //}





                    if (DOM is OFBoxDomain)
                    {
                        if (DOM.BCInflow.btype is BoundaryType.abl)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), BoundaryConditionTemplates.UBoxABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), BoundaryConditionTemplates.ABLConditions(DOM, i));
                        }
                        if (DOM.BCInflow.btype is BoundaryType.constant) { File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), BoundaryConditionTemplates.UBoxConstU(DOM, i)); }

                        //REmove this later
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), BoundaryConditionTemplates.ABLConditions_Cyl(DOM, i));
                        //REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), BoundaryConditionTemplates.P(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), BoundaryConditionTemplates.Omega(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), BoundaryConditionTemplates.K(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), BoundaryConditionTemplates.Epsilon(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), BoundaryConditionTemplates.Nut(DOM));


                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), BoundaryConditionTemplates.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\snappyHexMeshDict"), StringTemplates.SnappyHexMeshDict(DOM));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\surfaceFeatureExtractDict"), StringTemplates.SurfaceFeatureExtractDict());

                        if (mode == 0)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesAccurate());
                        }
                        else if (mode == 1)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesRobust1());
                        }
                        else if (mode == 2)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesOrtho70_80());
                        }
                        else if (mode == 3)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesOrtho60_70());
                        }
                        else if (mode == 4)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesOrtho40_60());
                        }

                        else if (mode == 5)

                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesAccurate());
                        }
                        else if (mode == 6)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesAccurateOscillatory());
                        }
                        else if (mode == 7)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesRobust1());
                        }

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSolution"), StringTemplates.FvSolution(0));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\meshQualityDict"), StringTemplates.MeshQualityDict());
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\residuals"), StringTemplates.Residuals());


                    }
                    else
                    {
                        if (DOM.BCInflow.btype is BoundaryType.abl)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), BoundaryConditionTemplates.U_CylABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), BoundaryConditionTemplates.ABLConditions_Cyl(DOM, i));
                        }
                        if (DOM.BCInflow.btype is BoundaryType.constant) { File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), BoundaryConditionTemplates.UCylConstU(DOM, i)); }

                        //REmove this later
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), BoundaryConditionTemplates.ABLConditions_Cyl(DOM, i));
                        //REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), BoundaryConditionTemplates.P_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), BoundaryConditionTemplates.Omega_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), BoundaryConditionTemplates.K_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), BoundaryConditionTemplates.Epsilon_Cyl(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), BoundaryConditionTemplates.Nut_Cyl(DOM, i));


                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), BoundaryConditionTemplates.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\snappyHexMeshDict"), StringTemplates.SnappyHexMeshDict(DOM));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\surfaceFeatureExtractDict"), StringTemplates.SurfaceFeatureExtractDict());
                        if (mode == 0)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesAccurate());
                        }
                        else if (mode == 1)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesRobust1());
                        }
                        else if (mode == 2)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesOrtho70_80());
                        }
                        else if (mode == 3)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesOrtho60_70());
                        }
                        else if (mode == 4)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesOrtho40_60());
                        }

                        else if (mode == 5)

                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesAccurate());
                        }
                        else if (mode == 6)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesAccurateOscillatory());
                        }
                        else if (mode == 7)
                        {
                            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSchemes"), StringTemplates.FvSchemesRobust1());
                        }


                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\fvSolution"), StringTemplates.FvSolution(mode));
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\meshQualityDict"), StringTemplates.MeshQualityDict());
                        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\system\residuals"), StringTemplates.Residuals());



                    }

                    //Constant folder
                    File.WriteAllText(Path.Combine(simConstantDir + "turbulenceProperties"), StringTemplates.TurbulenceProperties(DOM));
                    File.WriteAllText(Path.Combine(simConstantDir + "transportProperties"), StringTemplates.TransportProperties());



                }


                ////Batch files depending on type


                //if (DOM is OFBoxDomain)
                //{

                //    for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                //    {
                //        File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_mesh_" + DOM.BCInflow.windDir[i] + @".bat"), StringTemplates.Run_Mesh_Box(DOM, i));
                //    }
                //    File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run.bat"), StringTemplates.Run(DOM));
                //    File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_sim_all.bat"), StringTemplates.RunSimOnly(DOM));
                //    File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_ray.bat"), StringTemplates.Run_RayTrace(DOM));
                //    File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_probes.bat"), StringTemplates.Run_Probes(DOM));
                //    File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_utci.bat"), StringTemplates.Run_UTCI(DOM));
                //}
                //else
                //{
                File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_mesh.bat"), StringTemplates.Run_Mesh_Cyl(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run.bat"), StringTemplates.Run(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_sim_all.bat"), StringTemplates.RunSimOnly(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_ray.bat"), StringTemplates.Run_RayTrace(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_probes.bat"), StringTemplates.Run_Probes(DOM));
                File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_utci.bat"), StringTemplates.Run_UTCI(DOM));
                //}




#if DEBUG
                File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_blockMesh.bat"), StringTemplates.BlockMesh(DOM));
#endif

                for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
                {
                    File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + "_run_sim.bat"), StringTemplates.Run_sim(DOM, i));
                }







                //if (Run == true)
                //{
                //}

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
