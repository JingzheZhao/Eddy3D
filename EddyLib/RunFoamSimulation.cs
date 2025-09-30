using EddyLib.BCs;
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
                    RunSettings.CPUs = Utilities.CalcOptimCPU(MeshSettings.meshWorkingDir, RunSettings.CPUs);   // TODO: take out all work dirs from dom!!!
                }

                for (int i = 0; i < DOM.BCond.WindDirections.Count; i++)
                {
                    string simStlDir = WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\constant\triSurface\";
                    string simStlFilenameBuildings = WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\constant\triSurface\building.stl";
                    string simStlFilenameGround = WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\constant\triSurface\ground.stl";
                    string simConstantDir = WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\constant\";
                    string simBoundaryConditionsDir = WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\0.org\";
                    string simBoundaryConditionsDirTemp = WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\0\";

                    if (!Directory.Exists(simStlDir))
                    {
                        Directory.CreateDirectory(simStlDir);
                    }

                    if (!Directory.Exists(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\"))
                    {
                        Directory.CreateDirectory(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\");
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

                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\" + "controlDict"), EddyLib.Strings.OFExecDicts.ControlDict(RunSettings, DOM, null, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\" + DOM.BCond.WindDirections[i] + ".foam"), "");

                    string symbolicPath = WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\constant\polyMesh";
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
                        if (DOM.BCond.BCs[i] is ABL)
                        {
                            var bcond = (ABL)DOM.BCond.BCs[i];

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.Strings.BCDicts.UBoxABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.Strings.BCDicts.UBoxABL(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                        }
                        if (DOM.BCond.BCs[i] is ConstU)
                        {
                            // We need the ABL file regardless
                            ABL bcond = new ABL(DOM.BCond.WindDirections[i], DOM.BCond.BCs[i].URef, 10, DOM.BCond.BCs[i].z0, 0);

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.Strings.BCDicts.UBoxConstU(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.Strings.BCDicts.UBoxConstU(DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                        }

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.Strings.BCDicts.P(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.Strings.BCDicts.Omega(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.Strings.BCDicts.K(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.Strings.BCDicts.Epsilon(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.Strings.BCDicts.Nut(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "aoa"), EddyLib.Strings.BCDicts.AOA());

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "p"), EddyLib.Strings.BCDicts.P(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "omega"), EddyLib.Strings.BCDicts.Omega(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "k"), EddyLib.Strings.BCDicts.K(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "epsilon"), EddyLib.Strings.BCDicts.Epsilon(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "nut"), EddyLib.Strings.BCDicts.Nut(DOM));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "aoa"), EddyLib.Strings.BCDicts.AOA());

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.Strings.BCDicts.InitialConditions(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "initialConditions"), EddyLib.Strings.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\snappyHexMeshDict"), EddyLib.Strings.OFExecDicts.SnappyHexMeshDict(MeshSettings, DOM));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\surfaceFeaturesDict"), EddyLib.Strings.OFExecDicts.surfaceFeaturesDict());

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\fvSchemes"), EddyLib.Strings.OFExecDicts.FvSchemes(RunSettings));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\fvSolution"), EddyLib.Strings.OFExecDicts.FvSolution(RunSettings));

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\residuals"), EddyLib.Strings.OFExecDicts.ResidualsDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\decomposeParDict"), EddyLib.Strings.OFExecDicts.DecomposeParDict(RunSettings));
                    }
                    else if (DOM is OFCylDomain)
                    {
                        if (DOM.BCond.BCs[i] is ABL)
                        {
                            var bcond = (ABL)DOM.BCond.BCs[i];

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.Strings.BCDicts.U_CylABL((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.Strings.BCDicts.U_CylABL((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                        }
                        if (DOM.BCond.BCs[i] is ConstU)
                        {
                            // We need the ABL file regardless
                            ABL bcond = new ABL(DOM.BCond.WindDirections[i], DOM.BCond.BCs[i].URef, 10, DOM.BCond.BCs[i].z0, 0);

                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "U"), EddyLib.Strings.BCDicts.UCylConstU((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "U"), EddyLib.Strings.BCDicts.UCylConstU((OFCylDomain)DOM, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                            File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "ABLConditions"), EddyLib.Strings.BCDicts.ABL(bcond, i));
                        }

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "p"), EddyLib.Strings.BCDicts.P_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "omega"), EddyLib.Strings.BCDicts.Omega_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "k"), EddyLib.Strings.BCDicts.K_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "epsilon"), EddyLib.Strings.BCDicts.Epsilon_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "nut"), EddyLib.Strings.BCDicts.Nut_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "aoa"), EddyLib.Strings.BCDicts.AOA_Cyl((OFCylDomain)DOM, i));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "p"), EddyLib.Strings.BCDicts.P_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "omega"), EddyLib.Strings.BCDicts.Omega_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "k"), EddyLib.Strings.BCDicts.K_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "epsilon"), EddyLib.Strings.BCDicts.Epsilon_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "nut"), EddyLib.Strings.BCDicts.Nut_Cyl((OFCylDomain)DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "aoa"), EddyLib.Strings.BCDicts.AOA_Cyl((OFCylDomain)DOM, i));

                        File.WriteAllText(Path.Combine(simBoundaryConditionsDir + "initialConditions"), EddyLib.Strings.BCDicts.InitialConditions(DOM, i));
                        File.WriteAllText(Path.Combine(simBoundaryConditionsDirTemp + "initialConditions"), EddyLib.Strings.BCDicts.InitialConditions(DOM, i));

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\snappyHexMeshDict"), EddyLib.Strings.OFExecDicts.SnappyHexMeshDict(MeshSettings, DOM));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\surfaceFeaturesDict"), EddyLib.Strings.OFExecDicts.surfaceFeaturesDict());

                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\fvSchemes"), EddyLib.Strings.OFExecDicts.FvSchemes(RunSettings));
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\fvSolution"), EddyLib.Strings.OFExecDicts.FvSolution(RunSettings));

                        //File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + @"\system\meshQualityDict"), EddyLib.Strings.OFExecDicts.MeshQualityDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\residuals"), EddyLib.Strings.OFExecDicts.ResidualsDict());
                        File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + @"\system\decomposeParDict"), EddyLib.Strings.OFExecDicts.DecomposeParDict(RunSettings));
                    }
                }

                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_mesh.bat"), EddyLib.Strings.BatFiles.Run_Mesh(RunSettings, MeshSettings, DOM, Strings.OFExecutionMode.Meshing));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run.bat"), EddyLib.Strings.BatFiles.Run(DOM, MeshSettings));
                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_sim_all.bat"), EddyLib.Strings.BatFiles.RunSimOnly(DOM, MeshSettings));

                //File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_ray.bat"), EddyLib.Strings.BatFiles.Run_RayTrace(DOM, MeshSettings));

                File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_make_trees.bat"), EddyLib.Strings.BatFiles.Run_Make_Trees(RunSettings, MeshSettings, DOM, Strings.OFExecutionMode.Meshing));

                //File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_probes.bat"), EddyLib.Strings.BatFiles.Run_Probes(DOM, MeshSettings));
                //File.WriteAllText(Path.Combine(WorkDir + "\\" + "run_utci.bat"), EddyLib.Strings.BatFiles.Run_UTCI(DOM, MeshSettings));


                for (int i = 0; i < DOM.BCond.WindDirections.Count; i++)
                {
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + "_run_sim.bat"), EddyLib.Strings.BatFiles.Run_sim(MeshSettings, RunSettings, DOM, Strings.OFExecutionMode.Simulation, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + "_run_sim_continue.bat"), EddyLib.Strings.BatFiles.Run_sim_continue(MeshSettings, RunSettings, DOM, Strings.OFExecutionMode.Simulation, i));
                    File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.WindDirections[i] + "_run_divU.bat"), EddyLib.Strings.BatFiles.Run_divU(MeshSettings, RunSettings, DOM, Strings.OFExecutionMode.Simulation, i));

                    //File.WriteAllText(Path.Combine(WorkDir + "\\" + DOM.BCond.windDirs[i] + "run_reconstructSim.bat"), EddyLib.StrTemp.BatFiles.Run_reconstructSim(RunSettings, MeshSettings, DOM, StrTemp.Mode.Simulation, i));
                }
            }
        }
    }
}