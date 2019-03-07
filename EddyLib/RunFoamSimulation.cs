using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib
{
    public class RunFoamSimulation
    { 
        public static void Run(OFBaseDomain DOM, OFRunSettings RunSettings, string WorkDir)
        {


            if (Utilities.CheckLicence() == true)
            {

                //Autocalc number of CPUs
                if (RunSettings.CPUs == -1)
                {
                    RunSettings.CPUs = Utilities.CPUAutoCalc(DOM.meshWorkingDir, RunSettings.CPUs);   // TODO: take out all work dirs from dom!!!
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






            }



        }
    }
}
