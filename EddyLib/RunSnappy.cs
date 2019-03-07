using Rhino.Geometry;
using System.IO;

namespace EddyLib
{
    public class RunSnappy
    {

        public static void Run(OFBaseDomain DOM, OFMeshSettings MeshSettings, OFRunSettings RunSettings, out string logFile)
        {

            string SingleCPU = @"""surfaceFeatureExtract;snappyHexMesh -overwrite """; //-overwrite
            string MultipleCPU = @"""surfaceFeatureExtract;pyFoamDecompose.py --clear . " + RunSettings.CPUs + @"; foamJob -parallel -screen snappyHexMesh -overwrite""";

            // @ Patrick: Make cleaning function and handle behavior.. set to always true now
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


                if (RunSettings.CPUs > 1)
                {


                    for (int i = 0; i < RunSettings.CPUs; i++)
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

                    for (int i = 0; i < RunSettings.CPUs; i++)
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

                        for (int l = 0; l < DOM.BCond.windDirs.Count; l++)
                        {

                            var cpuPath = DOM.baseWorkingDir + DOM.BCond.windDirs[l] + @"\processor" + i;

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


            

            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "snappyHexMeshDict"), EddyLib.StrTemp.OFExecDicts.SnappyHexMeshDict(MeshSettings, DOM));
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "surfaceFeatureExtractDict"), EddyLib.StrTemp.OFExecDicts.SurfaceFeatureExtractDict());
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "fvSchemes"), EddyLib.StrTemp.OFExecDicts.FvSchemesRobust1());
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "fvSolution"), EddyLib.StrTemp.OFExecDicts.FvSolution(0));
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "meshQualityDict"), EddyLib.StrTemp.OFExecDicts.MeshQualityDict());
            File.WriteAllText(Path.Combine(DOM.meshSystemDir + "decomposeParDict"), EddyLib.StrTemp.OFExecDicts.DecomposeParDict(RunSettings));

            File.WriteAllText(Path.Combine(DOM.baseWorkingDir + "run_checkBadMesh.bat"), EddyLib.StrTemp.BatFiles.Run_checkBadMesh(RunSettings, DOM));



            //Autocalc number of CPUs
            if (RunSettings.CPUs == -1)
            {
                RunSettings.CPUs = Utilities.CPUAutoCalc(DOM.meshWorkingDir, RunSettings.CPUs);
            }




            string command = RunSettings.CPUs > 1 ? MultipleCPU : SingleCPU;


            logFile = "";

            using (FileStream stream = File.Open(DOM.meshWorkingDir + @"\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    logFile = reader.ReadToEnd();
                }
            }








        }

    }
}
