using System.IO;

namespace EddyLib
{
    public class RunFoamSimulation
    {
        public static void Run(OFBaseDomain DOM, OFMeshSettings MeshSettings, OFRunSettings RunSettings, string WorkDir)
        {


            if (Utilities.CheckLicence() == true)
            {

                //Autocalc number of CPUs
                if (RunSettings.CPUs == -1)
                {
                    RunSettings.CPUs = Utilities.CPUAutoCalc(MeshSettings.meshWorkingDir, RunSettings.CPUs);   // TODO: take out all work dirs from dom!!!
                }



                for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
                {

                    string simStlDir = WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\constant\triSurface\";
                    string simStlFilenameBuildings = WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\constant\triSurface\building.stl";
                    string simStlFilenameGround = WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\constant\triSurface\ground.stl";
                    string simConstantDir = WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\constant\";
                    string simBoundaryConditionsDir = WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\0.org\";
                    string simBoundaryConditionsDirTemp = WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\0\";

                    if (!Directory.Exists(simStlDir))
                    {
                        Directory.CreateDirectory(simStlDir);
                    }

                    if (!Directory.Exists(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\"))
                    {
                        Directory.CreateDirectory(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\");
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

                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\" + "controlDict"), EddyLib.StrTemp.OFExecDicts.ControlDict(RunSettings, DOM, null, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\"+ DOM.BCond.windDirs[i]+".foam"), "");

                    // Not working currently: Symbolic dir junctions only for cylindrical domain, and, if they exist, delete them for boxDomain

                    //if (DOM is OFCylDomain)
                    //{
                    string symbolicPath = WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\constant\polyMesh";
                    if (Directory.Exists(symbolicPath) && !SymlinkCreator.IsSymbolic(symbolicPath))
                    {
                        SymlinkCreator.Delete(symbolicPath);
                        SymlinkCreator.Create(symbolicPath, MeshSettings.meshConstantDir + @"\polyMesh");
                    }
                    else
                    {
                        SymlinkCreator.Create(symbolicPath, MeshSettings.meshConstantDir + @"\polyMesh");
                    }

                    //Constant folder
                    File.WriteAllText(Path.Combine(simConstantDir + "turbulenceProperties"), EddyLib.StrTemp.OFExecDicts.TurbulenceProperties(RunSettings));
                    File.WriteAllText(Path.Combine(simConstantDir + "transportProperties"), EddyLib.StrTemp.OFExecDicts.TransportProperties());

                    Utilities.DeletePhi(MeshSettings, DOM);


                 

                    if (DOM is OFBoxDomain)
                    {
                        if (DOM.BCond.btype is BoundaryType.abl)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.UBoxABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLConditions(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.StrTemp.BCDicts.UBoxABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLConditions(DOM, i));
                        }
                        if (DOM.BCond.btype is BoundaryType.constant)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.UBoxConstU(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.StrTemp.BCDicts.UBoxConstU(DOM, i));
                        }

                        //REmove this later
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond(DOM, i));
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

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\snappyHexMeshDict"), EddyLib.StrTemp.OFExecDicts.SnappyHexMeshDict(MeshSettings, DOM));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\surfaceFeatureExtractDict"), EddyLib.StrTemp.OFExecDicts.SurfaceFeatureExtractDict());

                        if (RunSettings.Schemes == 0)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (RunSettings.Schemes == 1)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }
                        else if (RunSettings.Schemes == 2)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho70_80());
                        }
                        else if (RunSettings.Schemes == 3)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho60_70());
                        }
                        else if (RunSettings.Schemes == 4)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho40_60());
                        }

                        else if (RunSettings.Schemes == 5)

                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (RunSettings.Schemes == 6)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurateOscillatory());
                        }
                        else if (RunSettings.Schemes == 7)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSolution"), EddyLib.StrTemp.OFExecDicts.FvSolution(0));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\meshQualityDict"), EddyLib.StrTemp.OFExecDicts.MeshQualityDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\residuals"), EddyLib.StrTemp.OFExecDicts.ResidualsDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\decomposeParDict"), EddyLib.StrTemp.OFExecDicts.DecomposeParDict(RunSettings));

                    }
                    else if (DOM is OFCylDomain)
                    {

                        if (DOM.BCond.btype is BoundaryType.abl)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.U_CylABL((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.StrTemp.BCDicts.U_CylABL((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond(DOM, i));
                        }
                        if (DOM.BCond.btype is BoundaryType.constant)
                        {
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.StrTemp.BCDicts.UCylConstU((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.StrTemp.BCDicts.UCylConstU((OFCylDomain)DOM, i));
                        }

                        //REmove this later
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.StrTemp.BCDicts.ABLCond(DOM, i));
                        //REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.StrTemp.BCDicts.P_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.StrTemp.BCDicts.Omega_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.StrTemp.BCDicts.K_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.StrTemp.BCDicts.Epsilon_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.StrTemp.BCDicts.Nut_Cyl((OFCylDomain)DOM, i));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "p"), EddyLib.StrTemp.BCDicts.P_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "omega"), EddyLib.StrTemp.BCDicts.Omega_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "k"), EddyLib.StrTemp.BCDicts.K_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "epsilon"), EddyLib.StrTemp.BCDicts.Epsilon_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "nut"), EddyLib.StrTemp.BCDicts.Nut_Cyl((OFCylDomain)DOM, i));


                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.StrTemp.BCDicts.InitialConditions(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "initialConditions"), EddyLib.StrTemp.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\snappyHexMeshDict"), EddyLib.StrTemp.OFExecDicts.SnappyHexMeshDict(MeshSettings, DOM));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\surfaceFeatureExtractDict"), EddyLib.StrTemp.OFExecDicts.SurfaceFeatureExtractDict());
                        if (RunSettings.Schemes == 0)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (RunSettings.Schemes == 1)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
                        }
                        else if (RunSettings.Schemes == 2)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho70_80());
                        }
                        else if (RunSettings.Schemes == 3)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho60_70());
                        }
                        else if (RunSettings.Schemes == 4)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesOrtho40_60());
                        }
                        else if (RunSettings.Schemes == 5)

                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurate());
                        }
                        else if (RunSettings.Schemes == 6)
                        {
                            File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesAccurateOscillatory());
                        }
                        else if (RunSettings.Schemes == 7)
                        { File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1()); }

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSolution"), EddyLib.StrTemp.OFExecDicts.FvSolution(RunSettings.Schemes));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\meshQualityDict"), EddyLib.StrTemp.OFExecDicts.MeshQualityDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\residuals"), EddyLib.StrTemp.OFExecDicts.ResidualsDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\decomposeParDict"), EddyLib.StrTemp.OFExecDicts.DecomposeParDict(RunSettings));


                    }



                }

                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_mesh.bat"), EddyLib.StrTemp.BatFiles.Run_Mesh_Cyl(RunSettings, MeshSettings, DOM, StrTemp.Mode.Meshing));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run.bat"), EddyLib.StrTemp.BatFiles.Run(DOM, MeshSettings));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_sim_all.bat"), EddyLib.StrTemp.BatFiles.RunSimOnly(DOM, MeshSettings));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_ray.bat"), EddyLib.StrTemp.BatFiles.Run_RayTrace(DOM, MeshSettings));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_probes.bat"), EddyLib.StrTemp.BatFiles.Run_Probes(DOM, MeshSettings));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_utci.bat"), EddyLib.StrTemp.BatFiles.Run_UTCI(DOM, MeshSettings));
                
#if DEBUG
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_blockMesh.bat"), EddyLib.StrTemp.BatFiles.Run_blockMesh(RunSettings, DOM, MeshSettings, StrTemp.Mode.Meshing));
#endif

                for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
                {
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + "_run_sim.bat"), EddyLib.StrTemp.BatFiles.Run_sim(MeshSettings, RunSettings, DOM, StrTemp.Mode.Simulation, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + "_run_sim_continue.bat"), EddyLib.StrTemp.BatFiles.Run_sim_continue(MeshSettings, RunSettings, DOM, StrTemp.Mode.Simulation, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + "_run_divU.bat"), EddyLib.StrTemp.BatFiles.Run_divU(MeshSettings, RunSettings, DOM, StrTemp.Mode.Simulation, i));


                }





                // Calculate runtimes of all simulation based on log file

                for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
                {
                    DOM.Runtimes.Add(Utilities.CalculateRunTimeFromLog(WorkDir + "\\" + DOM.BCond.windDirs[i], RunSettings.iter));
                }






            }



        }
    }
}
