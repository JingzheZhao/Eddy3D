using System.IO;
using EddyLib.BCs;

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
                    RunSettings.CPUs = Utilities.CalcOptimCPU(MeshSettings.meshWorkingDir, RunSettings.CPUs);   // TODO: take out all work dirs from dom!!!
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

                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\" + "controlDict"), EddyLib.Strings.OFExecDicts.ControlDict(RunSettings, DOM, null, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\" + DOM.BCond.windDirs[i] + ".foam"), "");

                    // Not working currently: Symbolic dir junctions only for cylindrical domain,
                    // and, if they exist, delete them for boxDomain

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
                    File.WriteAllText(Path.Combine(simConstantDir + "turbulenceProperties"), EddyLib.Strings.OFExecDicts.TurbulenceProperties(RunSettings));
                    File.WriteAllText(Path.Combine(simConstantDir + "transportProperties"), EddyLib.Strings.OFExecDicts.TransportProperties());

                    Utilities.DeletePhi(MeshSettings, DOM);

                    if (DOM is OFBoxDomain)
                    {
                        if (DOM.BCond is ABL)
                        {
                            var bcond = (ABL)DOM.BCond;

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.Strings.BCDicts.UBoxABL(DOM, i));

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.Strings.BCDicts.UBoxABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                        }
                        if (DOM.BCond is ConstU)
                        {
                            // We need the ABL file regardless
                            ABL bcond = new ABL(DOM.BCond.windDirs, DOM.BCond.URef, 10, DOM.BCond.z0, 0, DOM.BCond.epwFilePath);

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.Strings.BCDicts.UBoxConstU(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.Strings.BCDicts.UBoxConstU(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                        }

                        ////REmove this later
                        //File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(DOM, i));
                        //File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(DOM, i));

                        ////REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.Strings.BCDicts.P(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.Strings.BCDicts.Omega(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.Strings.BCDicts.K(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.Strings.BCDicts.Epsilon(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.Strings.BCDicts.Nut(DOM));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "p"), EddyLib.Strings.BCDicts.P(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "omega"), EddyLib.Strings.BCDicts.Omega(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "k"), EddyLib.Strings.BCDicts.K(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "epsilon"), EddyLib.Strings.BCDicts.Epsilon(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "nut"), EddyLib.Strings.BCDicts.Nut(DOM));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.Strings.BCDicts.InitialConditions(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "initialConditions"), EddyLib.Strings.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\snappyHexMeshDict"), EddyLib.Strings.OFExecDicts.SnappyHexMeshDict(MeshSettings, DOM));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\surfaceFeatureExtractDict"), EddyLib.Strings.OFExecDicts.SurfaceFeatureExtractDict());

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.Strings.OFExecDicts.FvSchemes(RunSettings));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSolution"), EddyLib.Strings.OFExecDicts.FvSolution(RunSettings));

                        //File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\meshQualityDict"), EddyLib.Strings.OFExecDicts.MeshQualityDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\residuals"), EddyLib.Strings.OFExecDicts.ResidualsDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\decomposeParDict"), EddyLib.Strings.OFExecDicts.DecomposeParDict(RunSettings));
                    }
                    else if (DOM is OFCylDomain)
                    {
                        if (DOM.BCond is ABL)
                        {
                            var bcond = (ABL)DOM.BCond;

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.Strings.BCDicts.U_CylABL((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.Strings.BCDicts.U_CylABL((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                        }
                        if (DOM.BCond is ConstU)
                        {
                            // We need the ABL file regardless
                            ABL bcond = new ABL(DOM.BCond.windDirs, DOM.BCond.URef, 10, DOM.BCond.z0, 0, DOM.BCond.epwFilePath);

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.Strings.BCDicts.UCylConstU((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.Strings.BCDicts.UCylConstU((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                        }

                        ////REmove this later
                        //File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(DOM, i));
                        //File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(DOM, i));

                        ////REmove this later

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.Strings.BCDicts.P_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.Strings.BCDicts.Omega_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.Strings.BCDicts.K_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.Strings.BCDicts.Epsilon_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.Strings.BCDicts.Nut_Cyl((OFCylDomain)DOM, i));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "p"), EddyLib.Strings.BCDicts.P_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "omega"), EddyLib.Strings.BCDicts.Omega_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "k"), EddyLib.Strings.BCDicts.K_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "epsilon"), EddyLib.Strings.BCDicts.Epsilon_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "nut"), EddyLib.Strings.BCDicts.Nut_Cyl((OFCylDomain)DOM, i));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.Strings.BCDicts.InitialConditions(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "initialConditions"), EddyLib.Strings.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\snappyHexMeshDict"), EddyLib.Strings.OFExecDicts.SnappyHexMeshDict(MeshSettings, DOM));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\surfaceFeatureExtractDict"), EddyLib.Strings.OFExecDicts.SurfaceFeatureExtractDict());

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSchemes"), EddyLib.Strings.OFExecDicts.FvSchemes(RunSettings));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\fvSolution"), EddyLib.Strings.OFExecDicts.FvSolution(RunSettings));

                        //File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\meshQualityDict"), EddyLib.Strings.OFExecDicts.MeshQualityDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\residuals"), EddyLib.Strings.OFExecDicts.ResidualsDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\decomposeParDict"), EddyLib.Strings.OFExecDicts.DecomposeParDict(RunSettings));
                    }
                }

                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_mesh.bat"), EddyLib.Strings.BatFiles.Run_Mesh_Cyl(RunSettings, MeshSettings, DOM, Strings.Mode.Meshing));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run.bat"), EddyLib.Strings.BatFiles.Run(DOM, MeshSettings));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_sim_all.bat"), EddyLib.Strings.BatFiles.RunSimOnly(DOM, MeshSettings));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_ray.bat"), EddyLib.Strings.BatFiles.Run_RayTrace(DOM, MeshSettings));

                //File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_probes.bat"), EddyLib.Strings.BatFiles.Run_Probes(DOM, MeshSettings));
                //File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_utci.bat"), EddyLib.Strings.BatFiles.Run_UTCI(DOM, MeshSettings));

#if DEBUG
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_blockMesh.bat"), EddyLib.Strings.BatFiles.Run_blockMesh(RunSettings, DOM, MeshSettings, Strings.Mode.Meshing));
#endif

                for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
                {
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + "_run_sim.bat"), EddyLib.Strings.BatFiles.Run_sim(MeshSettings, RunSettings, DOM, Strings.Mode.Simulation, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + "_run_sim_continue.bat"), EddyLib.Strings.BatFiles.Run_sim_continue(MeshSettings, RunSettings, DOM, Strings.Mode.Simulation, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + "_run_divU.bat"), EddyLib.Strings.BatFiles.Run_divU(MeshSettings, RunSettings, DOM, Strings.Mode.Simulation, i));

                    //File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + "run_reconstructSim.bat"), EddyLib.StrTemp.BatFiles.Run_reconstructSim(RunSettings, MeshSettings, DOM, StrTemp.Mode.Simulation, i));
                }

                // This doesnt work atm because tee.exe puts write lock on log file

                // Calculate runtimes of all simulation based on log file
                //for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
                //{
                //    DOM.Runtimes.Add(Utilities.CalculateRunTimeFromLog(WorkDir + "\\" + DOM.BCond.windDirs[i], RunSettings.iter));
                //}
            }
        }
    }
}