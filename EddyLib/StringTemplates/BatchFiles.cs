using System.Collections.Generic;
using System.Text;

namespace EddyLib.StrTemp
{
    public enum Mode
    {
        Simulation,
        Meshing
    }

    public class BatFiles
    {
        //Run commands as list

        private static readonly List<string> RCCheckMeshSingleCPU = new List<string> {
        "checkMesh -allGeometry -allTopology -writeAllFields -writeSets vtk",
        "foamToVTK -faceSet highAspectRatioCells -ascii",
        "foamToVTK -faceSet nonOrthoFaces -ascii",
        "foamToVTK -faceSet skewFaces -ascii",
        "foamToVTK -faceSet wrongOrientedFaces -ascii",
        "foamToVTK -faceSet zeroVolumeCells -ascii"};

        private static readonly List<string> RCBlockMeshSingleCPU = new List<string> {
        "blockMesh"};

        private static List<string> RCSimMultiCPU(OFRunSettings RunSettings)
        {
            List<string> lst = new List<string>
            {
                "decomposePar -force",
                "mpiexec -np " + RunSettings.CPUs + @" renumberMesh -overwrite",
                "mpiexec -np " + RunSettings.CPUs + @" potentialFoam -parallel",
                "mpiexec -np " + RunSettings.CPUs + @" simpleFoam -parallel",
                "reconstructPar -latestTime"
            };
            return lst;
        }

        private static readonly List<string> RCSimSingleCPU = new List<string> {
        "potentialFoam",
        "simpleFoam"};

        private static readonly List<string> RCSimContinueSingleCPU = new List<string> {
        "simpleFoam"};

        private static List<string> RCSimContinueMultiCPU(OFRunSettings RunSettings)
        {
            List<string> lst = new List<string>
            {
                "mpiexec -np " + RunSettings.CPUs + @" simpleFoam -parallel",
                "reconstructPar -latestTime"
            };
            return lst;
        }

        private static List<string> RCMeshMultiCPU(OFRunSettings RunSettings)
        {
            List<string> lst = new List<string>
            {
                "blockMesh",
                "surfaceFeatureExtract",
                "decomposePar -force",
                "mpiexec -np " + RunSettings.CPUs + @" snappyHexMesh -overwrite -parallel",
                "reconstructParMesh -constant",
                "renumberMesh -overwrite"
            };
            return lst;
        }

        private static readonly List<string> RCMeshSingleCPU = new List<string> {
        "blockMesh",
        "surfaceFeatureExtract",
        "snappyHexMesh -overwrite",
        "renumberMesh -overwrite"};

        private static readonly List<string> divU = new List<string> { "postProcess -func div(U)" };

        public static string DockerPrefixPath(OFBaseDomain DOM, OFMeshSettings MeshSettings, OFRunSettings RunSettings, Mode mode)
        {
            StringBuilder sb = new StringBuilder();
            if (mode == Mode.Simulation && RunSettings.ostype == OSType.Windows7)
            {
                sb.Append(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + DOM.BCond.windDirs[0] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == Mode.Meshing && RunSettings.ostype == OSType.Windows7)
            {
                sb.Append(@"docker run -v """ + MeshSettings.OFmeshWorkingDir + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == Mode.Simulation)
            {
                sb.Append(@"docker run -v """ + MeshSettings.baseWorkingDir + DOM.BCond.windDirs[0] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == Mode.Meshing)
            {
                sb.Append(@"docker run -v """ + MeshSettings.meshWorkingDir + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }

            return sb.ToString();
        }

        public static string DockerPrefixPath(OFBaseDomain DOM, OFMeshSettings MeshSettings, OFRunSettings RunSettings, Mode mode, int d)
        {
            StringBuilder sb = new StringBuilder();

            if (mode == Mode.Simulation && RunSettings.ostype == OSType.Windows7)
            {
                sb.Append(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + +DOM.BCond.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == Mode.Meshing && RunSettings.ostype == OSType.Windows7)
            {
                sb.Append(@"docker run -v """ + MeshSettings.OFmeshWorkingDir + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == Mode.Simulation)
            {
                sb.Append(@"docker run -v """ + MeshSettings.baseWorkingDir + +DOM.BCond.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }
            else if (mode == Mode.Meshing)
            {
                sb.Append(@"docker run -v """ + MeshSettings.meshWorkingDir + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam;");
            }

            return sb.ToString();
        }

        private static string AppendSuffixDocker()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(" | tee -a log\"");
            sb.AppendLine("");
            return sb.ToString();
        }

        //private static string AppendSuffixWin()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.AppendLine("> log");
        //    return sb.ToString();
        //}

        private static string AppendSuffixWin()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("| tee -a log");
            return sb.ToString();
        }

        public static string Run_Mesh_Cyl(OFRunSettings RunSettings, OFMeshSettings MeshSettings, OFBaseDomain DOM, Mode mode)
        {
            StringBuilder sb = new StringBuilder();

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                if (RunSettings.CPUs > 1)
                {
                    foreach (string str in RCMeshMultiCPU(RunSettings))
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str + AppendSuffixDocker());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in RCMeshSingleCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str + AppendSuffixDocker());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                if (RunSettings.CPUs > 1)
                {
                    sb.Append(TempBlueCFD(RCMeshMultiCPU(RunSettings), MeshSettings.meshWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(TempBlueCFD(RCMeshSingleCPU, MeshSettings.meshWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }
            return sb.ToString();
        }

        //        public static string Run_Mesh_Box(OFBaseDomain DOM, int windDir)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; blockMesh " + AppendSuffix());
        //            if (DOM.CPUs > 1)
        //            {
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; surfaceFeatureExtract " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; pyFoamDecompose.py --clear . " + DOM.CPUs + @" " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; foamJob -parallel -screen snappyHexMesh -overwrite " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; reconstructParMesh -constant " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; renumberMesh -overwrite " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh " + AppendSuffix());
        //#if DEBUG

        // sb.AppendLine("PAUSE");

        //#endif
        //            }
        //            else
        //            {
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; surfaceFeatureExtract | tee  -a log; snappyHexMesh -overwrite  | tee  -a log; checkMesh | tee -a log""");
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; renumberMesh -overwrite " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + MeshSettings.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -i hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh " + AppendSuffix());
        //#if DEBUG

        ////                sb.AppendLine("PAUSE");

        ////#endif
        ////            }

        // return sb.ToString(); }

        public static string Run_sim(OFMeshSettings MeshSettings, OFRunSettings RunSettings, OFBaseDomain DOM, Mode mode, int d)
        {
            StringBuilder sb = new StringBuilder();

            string caseWorkingDir = MeshSettings.baseWorkingDir + "\\" + DOM.BCond.windDirs[d];

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                if (RunSettings.CPUs > 1)
                {
                    foreach (string str in RCSimMultiCPU(RunSettings))
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str + AppendSuffixDocker());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in RCSimSingleCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str + AppendSuffixDocker());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                if (RunSettings.CPUs > 1)
                {
                    sb.Append(TempBlueCFD(RCSimMultiCPU(RunSettings), caseWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(TempBlueCFD(RCSimSingleCPU, caseWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }

            return sb.ToString();
        }

        public static string Run_sim_continue(OFMeshSettings MeshSettings, OFRunSettings RunSettings, OFBaseDomain DOM, Mode mode, int d)
        {
            StringBuilder sb = new StringBuilder();

            string caseWorkingDir = MeshSettings.baseWorkingDir + "\\" + DOM.BCond.windDirs[d];

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                if (RunSettings.CPUs > 1)
                {
                    foreach (string str in RCSimContinueMultiCPU(RunSettings))
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str + AppendSuffixDocker());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in RCSimContinueSingleCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str + AppendSuffixDocker());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                if (RunSettings.CPUs > 1)
                {
                    sb.Append(TempBlueCFD(RCSimContinueMultiCPU(RunSettings), caseWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(TempBlueCFD(RCSimContinueSingleCPU, caseWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }

            return sb.ToString();
        }

        public static string Run_divU(OFMeshSettings MeshSettings, OFRunSettings RunSettings, OFBaseDomain DOM, Mode mode, int d)
        {
            StringBuilder sb = new StringBuilder();

            string caseWorkingDir = MeshSettings.baseWorkingDir + "\\" + DOM.BCond.windDirs[d];

            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                if (RunSettings.CPUs > 1)
                {
                    foreach (string str in divU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str + AppendSuffixDocker());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in divU)
                    {
                        sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode, d) + str + AppendSuffixDocker());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }
            else
            {
                if (RunSettings.CPUs > 1)
                {
                    sb.Append(TempBlueCFD(divU, caseWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(TempBlueCFD(divU, caseWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
            }

            return sb.ToString();
        }

        //        public static string Run_mesh_docker(OFBaseDomain DOM)
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""blockMesh | tee -a log"" -f """ + DOM.OFmeshWorkingDir + " \"");
        //            if (DOM.CPUs > 1)
        //            {
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""surfaceFeatureExtract | tee -a  log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""pyFoamDecompose.py --clear . " + DOM.CPUs + @" | tee -a  log"" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamJob -parallel -screen snappyHexMesh -overwrite | tee -a  log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""reconstructParMesh -constant | tee -a  log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""renumberMesh -overwrite | tee -a  log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //#if DEBUG

        // sb.AppendLine("PAUSE");

        //#endif
        //            }
        //            else
        //            {
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""surfaceFeatureExtract | tee -a log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""snappyHexMesh -overwrite  | tee -a  log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a  log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""renumberMesh -overwrite | tee -a  log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a log "" -f """ + DOM.OFmeshWorkingDir + " \"");
        //#if DEBUG

        // sb.AppendLine("PAUSE");

        //#endif
        //            }

        // return sb.ToString(); } public static string Run_sim_docker(OFBaseDomain DOM, int d) {
        // StringBuilder sb = new StringBuilder();

        //            sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""pyFoamPrepareCase.py . --no-mesh-create | tee -a log"" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //            if (DOM.CPUs > 1)
        //            {
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""pyFoamDecompose.py --clear . " + DOM.CPUs + @"| tee -a  log "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamJob -s -p renumberMesh -overwrite | tee -a  log "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamJob -s -p potentialFoam | tee -a  log "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""mpiexec -np " + DOM.CPUs + @" simpleFoam -parallel | tee -a  log "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""reconstructPar -latestTime | tee -a  log; "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a  log "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //#if DEBUG

        // sb.AppendLine("PAUSE");

        //#endif
        //            }

        //            else
        //            {
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""potentialFoam | tee -a log "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""simpleFoam | tee -a  log "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a  log "" -f """ + MeshSettings.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //#if DEBUG

        // sb.AppendLine("PAUSE");

        //#endif
        //            }
        //            return sb.ToString();
        //        }

        public static string Run_blockMesh(OFRunSettings RunSettings, OFBaseDomain DOM, OFMeshSettings MeshSettings, Mode mode)
        {
            StringBuilder sb = new StringBuilder();
            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                foreach (string str in RCBlockMeshSingleCPU)
                {
                    sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str + AppendSuffixDocker());
                }
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            else
            {
                foreach (string str in RCBlockMeshSingleCPU)
                {
                    sb.Append(TempBlueCFD(RCBlockMeshSingleCPU, MeshSettings.meshWorkingDir));
                }
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            return sb.ToString();
        }

        public static string Run_checkMesh(OFRunSettings RunSettings, OFMeshSettings MeshSettings, OFBaseDomain DOM, Mode mode)
        {
            StringBuilder sb = new StringBuilder();
            if (RunSettings.simEngine == SimEngine.Docker)//Docker
            {
                foreach (string str in RCCheckMeshSingleCPU)
                {
                    sb.Append(DockerPrefixPath(DOM, MeshSettings, RunSettings, mode) + str + AppendSuffixDocker());
                }
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            else
            {
                //foreach (string str in RCCheckMeshSingleCPU)
                //{
                sb.Append(TempBlueCFD(RCCheckMeshSingleCPU, MeshSettings.meshWorkingDir));
                //}
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            return sb.ToString();
        }

        public static string Run(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"call """ + MeshSettings.baseWorkingDir + @"run_mesh.bat""");
            foreach (int i in DOM.BCond.windDirs)
            {
                sb.AppendLine(@"call """ + MeshSettings.baseWorkingDir + i + @"_run_sim.bat""");
            }
            sb.AppendLine(@"call """ + MeshSettings.baseWorkingDir + @"run_ray.bat""");
            sb.AppendLine(@"call """ + MeshSettings.baseWorkingDir + @"run_probes.bat""");
            sb.AppendLine(@"call """ + MeshSettings.baseWorkingDir + @"run_utci.bat""");
#if DEBUG
            sb.AppendLine("PAUSE");
#endif
            return sb.ToString();
        }

        public static string RunSimOnly(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int i in DOM.BCond.windDirs)
            {
                //sb.AppendLine("start " + MeshSettings.baseWorkingDirectory +i + "_run_sim.bat");
                sb.AppendLine("call \"" + MeshSettings.baseWorkingDir + i + "_run_sim.bat\"");
            }
#if DEBUG
            sb.AppendLine("PAUSE");
#endif
            return sb.ToString();
        }

        public static string Run_RayTrace(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            string workDir = MeshSettings.baseWorkingDir.Trim('\\');
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"REM CallRay.exe
REM CallRay 1.0.0.0
REM Copyright c  2018
REM
REM ERROR(S):
REM   -d / --workingDir required option is missing.
REM   - w / --weather required option is missing.
REM
REM   - d, --workingDir    Required.Working directory.
REM
REM   - w, --weather       Required.EPW weather file path.
REM
REM   - l, --loud(Default: True) Prints all messages to standard output.
REM
REM   --help              Display this help screen.");
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallRay.exe\" " + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCond.epwFilePath + "\"");
#if DEBUG

            sb.AppendLine("PAUSE");

#endif
            return sb.ToString();
        }

        public static string Run_Probes(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            //@ Patrick WIP

            string dirs = "";
            foreach (int d in DOM.BCond.windDirs)
            {
                dirs += (d.ToString() + ',');
            }

            dirs = dirs.TrimEnd(',');

            string workDir = MeshSettings.baseWorkingDir.Trim('\\');
            string uref = DOM.BCond.URef.ToString();
            string z0 = DOM.BCond.z0.ToString();
            string zref = DOM.BCond.zref.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallProbes.exe\" " + "-d " + "\"" + workDir + "\" " + "-p " + "\"" + workDir + @"\Rad\sensors.pts" + "\"" + " -w " + dirs + " -m 1" + " -u " + uref + " -r " + z0 + " -z " + zref);

#if DEBUG
            sb.AppendLine("PAUSE");
#endif

            return sb.ToString();
        }

        public static string Run_UTCI(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            //@ Patrick WIP

            string dirs = "";
            foreach (int d in DOM.BCond.windDirs)
            {
                dirs += (d.ToString() + ',');
            }

            dirs = dirs.TrimEnd(',');

            string workDir = MeshSettings.baseWorkingDir.Trim('\\');

            string dif = "-f " + "\"" + workDir + @"\Rad\CallRay.dif.ill" + "\"";
            string dir = "-r " + "\"" + workDir + @"\Rad\CallRay.dir.ill" + "\"";
            string u = "-u " + "\"" + workDir + @"\WindReductionData.csv" + "\"";
            // windDirs
            string o = "-o " + dirs;

            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallProbes.exe\" "+ "-w " + "\"" +workDir + "\" " + "-p " + "\"" + workDir + @"\Rad\sensors.pts" + "\"" + " -d " + dirs + " -m 1");
            //sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallRay.exe\" "  + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCInflow.weather + "\"");
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallOC.exe\" " + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCond.epwFilePath + "\" " + dif + " " + dir + " " + o + " " + u);

#if DEBUG
            sb.Append(" -b 0,0;");
            sb.AppendLine("PAUSE");

#endif

            return sb.ToString();
        }

        public static string TempBlueCFD
            (List<string> commands, string caseDir, string installationPath = @"C:\Program Files\blueCFD-Core-2017\")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(@"call ""{0}setvars.bat""
set PATH=%HOME%\msys64\usr\bin;%PATH%
cd ""{1}""" + System.Environment.NewLine, installationPath, caseDir));

            foreach (string str in commands)
            {
                sb.AppendLine(str + " " + AppendSuffixWin());
                //sb.AppendLine(str);
            }

            return sb.ToString();
        }

        public static string BlueCFDEnvVars
            (List<string> commands, string caseDir, string installationPath = @"C:\Program Files\blueCFD-Core-2017\")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(@"call ""{0}setvars.bat""
set PATH=%HOME%\msys64\usr\bin;%PATH%
cd ""{1}""" + System.Environment.NewLine, installationPath, caseDir));

            foreach (string str in commands)
            {
                sb.AppendLine(str + " " + AppendSuffixWin());
                //sb.AppendLine(str);
            }

            return sb.ToString();
        }

        //        public static string TempBlueCFD
        //            (List<string> commands, string caseDir, string installationPath = @"C:\OpenFOAM\")
        //        {
        //            StringBuilder sb = new StringBuilder();
        //            sb.Append(string.Format(@"call ""{0}""setvars.bat
        //set PATH =%HOME%\msys64\usr\bin;%PATH%
        //cd ""{1}""" + System.Environment.NewLine, installationPath, caseDir));

        // foreach (string str in commands) { sb.AppendLine(str); }

        // return sb.ToString();

        // }
    }
}