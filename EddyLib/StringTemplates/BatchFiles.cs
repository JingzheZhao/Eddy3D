using System.Collections.Generic;
using System.Text;

namespace EddyLib.StrTemp
{
    public class BatFiles
    {
        //Run commands as list



        private static readonly List<string> RCCheckMeshSingleCPU = new List<string> {
        "foamToVTK -faceSet highAspectRatioCells -ascii",
        "foamToVTK -faceSet nonOrthoFaces -ascii",
        "foamToVTK -faceSet skewFaces -ascii",
        "foamToVTK -faceSet wrongOrientedFaces -ascii",
        "foamToVTK -faceSet zeroVolumeCells -ascii"};

        private static readonly List<string> RCBlockMeshSingleCPU = new List<string> {
        "blockMesh"};

        private static List<string> RCSimMultiCPU(OFBaseDomain DOM)
        {
            List<string> lst = new List<string>();
            lst.Add("decomposePar");
            lst.Add("mpirun -np " + DOM.CPUs + @" renumberMesh -overwrite -parallel");
            lst.Add("mpirun -np " + DOM.CPUs + @" potentialFoam");
            lst.Add("mpirun -np " + DOM.CPUs + @" simpleFoam");
            lst.Add("reconstructPar -latestTime");
            lst.Add("checkMesh");
            return lst;
        }

        private static readonly List<string> RCSimSingleCPU = new List<string> {
        "renumberMesh -overwrite",
        "potentialFoam",
        "simpleFoam",
        "checkMesh"};

        private static readonly List<string> RCSimContinueSingleCPU = new List<string> {
        "simpleFoam",
        "checkMesh"};

        private static List<string> RCSimContinueMultiCPU(OFBaseDomain DOM)
        {
            List<string> lst = new List<string>();
            lst.Add("mpirun -np " + DOM.CPUs + @" simpleFoam");
            lst.Add("reconstructPar -latestTime");
            lst.Add("checkMesh");
            return lst;
        };

        private static List<string> RCMeshMultiCPU(OFBaseDomain DOM)
        {
            List<string> lst = new List<string>();
            lst.Add("blockMesh");
            lst.Add("surfaceFeatureExtract");
            lst.Add("mpirun -np " + DOM.CPUs + @" snappyHexMesh -overwrite");
            lst.Add("reconstructParMesh -constant");
            lst.Add("renumberMesh -overwrite");
            lst.Add("checkMesh");
            return lst;
        }

        private static readonly List<string> RCMeshSingleCPU = new List<string> {
        "blockMesh",
        "surfaceFeatureExtract",
        "snappyHexMesh -overwrite",
        "checkMesh" };

        private static string DockerPrefixPath(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"docker run - v """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[0] + @":/ home / openfoam / ""--entrypoint = """" - it hfdresearch / swak4foamandpyfoam:latest - v4.1 bash - c ""source / opt / openfoam4 / etc / bashrc; cd / home / openfoam;");
            return sb.ToString();
        }
        private static string DockerPrefixPath(OFBaseDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"docker run - v """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + @":/ home / openfoam / ""--entrypoint = """" - it hfdresearch / swak4foamandpyfoam:latest - v4.1 bash - c ""source / opt / openfoam4 / etc / bashrc; cd / home / openfoam;");
            return sb.ToString();
        }
        private static string AppendSuffix()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("| tee - a  log");
            return sb.ToString();
        }

        public static string Run_Mesh_Cyl(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();

            if (DOM.simEngine == 0)//Docker
            {
                if (DOM.CPUs > 1)
                {
                    foreach (string str in RCMeshMultiCPU(DOM))
                    {
                        sb.Append(DockerPrefixPath(DOM) + str + AppendSuffix());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }

                else
                {

                    foreach (string str in RCMeshSingleCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM) + str + AppendSuffix());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif

                }

            }
            else
            {
                if (DOM.CPUs > 1)
                {
                    sb.Append(TempBlueCFD(RCMeshMultiCPU(DOM), DOM.meshWorkingDir));
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    sb.Append(TempBlueCFD(RCMeshSingleCPU, DOM.meshWorkingDir));
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
        //            sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; blockMesh " + AppendSuffix());
        //            if (DOM.CPUs > 1)
        //            {

        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; surfaceFeatureExtract " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; pyFoamDecompose.py --clear . " + DOM.CPUs + @" " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; foamJob -parallel -screen snappyHexMesh -overwrite " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; reconstructParMesh -constant " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; renumberMesh -overwrite " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh " + AppendSuffix());
        //#if DEBUG

        //                sb.AppendLine("PAUSE");

        //#endif
        //            }
        //            else
        //            {
        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; surfaceFeatureExtract | tee  -a log; snappyHexMesh -overwrite  | tee  -a log; checkMesh | tee -a log""");
        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; renumberMesh -overwrite " + AppendSuffix());
        //                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDir + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh " + AppendSuffix());
        //#if DEBUG

        ////                sb.AppendLine("PAUSE");

        ////#endif
        ////            }



        //            return sb.ToString();
        //        }




        public static string Run_sim(OFBaseDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();

            string caseWorkingDir = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[d];

            if (DOM.simEngine == 0)//Docker
            {

                if (DOM.CPUs > 1)
                {

                    foreach (string str in RCSimMultiCPU(DOM))
                    {
                        sb.Append(DockerPrefixPath(DOM, d) + str + AppendSuffix());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in RCSimSingleCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM, d) + str + AppendSuffix());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }

            }
            else
            {
                if (DOM.CPUs > 1)
                {
                    sb.Append(TempBlueCFD(RCSimMultiCPU(DOM), caseWorkingDir));
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

        public static string Run_sim_continue(OFBaseDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();

            string caseWorkingDir = DOM.baseWorkingDir + "\\" + DOM.BCInflow.windDirs[d];


            if (DOM.simEngine == 0)//Docker
            {

                if (DOM.CPUs > 1)
                {

                    foreach (string str in RCSimContinueMultiCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM, d) + str + AppendSuffix());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }
                else
                {
                    foreach (string str in RCSimContinueSingleCPU)
                    {
                        sb.Append(DockerPrefixPath(DOM, d) + str + AppendSuffix());
                    }
#if DEBUG
                    sb.AppendLine("PAUSE");
#endif
                }

            }
            else
            {
                if (DOM.CPUs > 1)
                {
                    sb.Append(TempBlueCFD(RCSimContinueMultiCPU, caseWorkingDir));
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

        //                sb.AppendLine("PAUSE");

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

        //                sb.AppendLine("PAUSE");

        //#endif
        //            }



        //            return sb.ToString();
        //        }
        //        public static string Run_sim_docker(OFBaseDomain DOM, int d)
        //        {
        //            StringBuilder sb = new StringBuilder();

        //            sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""pyFoamPrepareCase.py . --no-mesh-create | tee -a log"" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //            if (DOM.CPUs > 1)
        //            {

        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""pyFoamDecompose.py --clear . " + DOM.CPUs + @"| tee -a  log "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamJob -s -p renumberMesh -overwrite | tee -a  log "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamJob -s -p potentialFoam | tee -a  log "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""mpirun -np " + DOM.CPUs + @" simpleFoam -parallel | tee -a  log "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""reconstructPar -latestTime | tee -a  log; "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a  log "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //#if DEBUG

        //                sb.AppendLine("PAUSE");

        //#endif
        //            }

        //            else
        //            {
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""potentialFoam | tee -a log "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""simpleFoam | tee -a  log "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a  log "" -f """ + DOM.OFbaseWorkingDir + DOM.BCInflow.windDirs[d] + " \"");
        //#if DEBUG

        //                sb.AppendLine("PAUSE");

        //#endif
        //            }
        //            return sb.ToString();
        //        }

        public static string run_blockMesh(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            if (DOM.simEngine == 0)//Docker
            {

                foreach (string str in RCBlockMeshSingleCPU)
                {
                    sb.Append(DockerPrefixPath(DOM) + str + AppendSuffix());
                }
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            else
            {

                foreach (string str in RCBlockMeshSingleCPU)
                {
                    sb.Append(TempBlueCFD(RCBlockMeshSingleCPU, DOM.meshWorkingDir));
                }
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            return sb.ToString();
        }

        public static string Run_checkBadMesh(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            if (DOM.simEngine == 0)//Docker
            {

                foreach (string str in RCCheckMeshSingleCPU)
                {
                    sb.Append(DockerPrefixPath(DOM) + str + AppendSuffix());
                }
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            else
            {

                foreach (string str in RCCheckMeshSingleCPU)
                {
                    sb.Append(TempBlueCFD(RCMeshSingleCPU, DOM.meshWorkingDir));
                }
#if DEBUG
                sb.AppendLine("PAUSE");
#endif
            }
            return sb.ToString();
        }


        public static string Run(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"call """ + DOM.baseWorkingDir + @"run_mesh.bat""");
            foreach (int i in DOM.BCInflow.windDirs)
            {

                sb.AppendLine(@"call """ + DOM.baseWorkingDir + i + @"_run_sim.bat""");
            }
            sb.AppendLine(@"call """ + DOM.baseWorkingDir + @"run_ray.bat""");
            sb.AppendLine(@"call """ + DOM.baseWorkingDir + @"run_probes.bat""");
            sb.AppendLine(@"call """ + DOM.baseWorkingDir + @"run_utci.bat""");
#if DEBUG

            sb.AppendLine("PAUSE");

#endif
            return sb.ToString();
        }

        public static string RunSimOnly(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            foreach (int i in DOM.BCInflow.windDirs)
            {
                //sb.AppendLine("start " + DOM.baseWorkingDirectory +i + "_run_sim.bat");
                sb.AppendLine("call " + DOM.baseWorkingDir + i + "_run_sim.bat");
            }
#if DEBUG

            sb.AppendLine("PAUSE");

#endif
            return sb.ToString();
        }

        public static string Run_RayTrace(OFBaseDomain DOM)
        {
            string workDir = DOM.baseWorkingDir.Trim('\\');
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallRay.exe\" " + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCInflow.weather + "\"");
#if DEBUG

            sb.AppendLine("PAUSE");

#endif
            return sb.ToString();
        }

        public static string Run_Probes(OFBaseDomain DOM)
        {

            //@ Patrick WIP

            string dirs = "";
            foreach (var d in DOM.BCInflow.windDirs)
            {
                dirs += (d.ToString() + ',');
            }

            dirs = dirs.TrimEnd(',');

            string workDir = DOM.baseWorkingDir.Trim('\\');
            string uref = DOM.BCInflow.URef.ToString();
            string z0 = DOM.BCInflow.z0.ToString();
            string zref = DOM.BCInflow.zref.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallProbes.exe\" " + "-d " + "\"" + workDir + "\" " + "-p " + "\"" + workDir + @"\Rad\sensors.pts" + "\"" + " -w " + dirs + " -m 1" + " -u " + uref + " -r " + z0 + " -z " + zref);

#if DEBUG
            sb.AppendLine("PAUSE");
#endif



            return sb.ToString();
        }

        public static string Run_UTCI(OFBaseDomain DOM)
        {

            //@ Patrick WIP

            string dirs = "";
            foreach (var d in DOM.BCInflow.windDirs)
            {
                dirs += (d.ToString() + ',');
            }

            dirs = dirs.TrimEnd(',');

            string workDir = DOM.baseWorkingDir.Trim('\\');

            string dif = "-f " + "\"" + workDir + @"\Rad\CallRay.dif.ill" + "\"";
            string dir = "-r " + "\"" + workDir + @"\Rad\CallRay.dir.ill" + "\"";
            string u = "-u " + "\"" + workDir + @"\WindReductionData.csv" + "\"";
            // windDirs
            string o = "-o " + dirs;

            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallProbes.exe\" "+ "-w " + "\"" +workDir + "\" " + "-p " + "\"" + workDir + @"\Rad\sensors.pts" + "\"" + " -d " + dirs + " -m 1");
            //sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallRay.exe\" "  + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCInflow.weather + "\"");
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallOC.exe\" " + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCInflow.weather + "\" " + dif + " " + dir + " " + o + " " + u);

#if DEBUG
            sb.Append(" -b 0,0;");
            sb.AppendLine("PAUSE");

#endif

            return sb.ToString();
        }



        private static string TempBlueCFD
            (List<string> commands, string caseDir, string installationPath = @"C:\OpenFOAM\")
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format(@"cd ""{0}""
call setvars.bat
set PATH =% HOME %\msys64\usr\bin;% PATH %
cd ""{1}""" + System.Environment.NewLine, installationPath, caseDir));

            foreach (string str in commands)
            {
                sb.AppendLine(str + " " +  AppendSuffix());
            }

            return sb.ToString();

        }



    }
}

