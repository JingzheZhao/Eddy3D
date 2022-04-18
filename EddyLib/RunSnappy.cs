using Rhino.Geometry;
using System.IO;

namespace EddyLib
{
    public class RunSnappy
    {
        public static void Run(OFBaseDomain DOM, OFMeshSettings MeshSettings, OFRunSettings RunSettings, out string logFile)
        {
            //string SingleCPU = @"""surfaceFeatureExtract;snappyHexMesh -overwrite """; //-overwrite
            //string MultipleCPU = @"""surfaceFeatureExtract;pyFoamDecompose.py --clear . " + RunSettings.CPUs + @"; foamJob -parallel -screen snappyHexMesh -overwrite""";

            // @ Patrick: Make cleaning function and handle behavior.. set to always true now
            //CLEAN UP THE MESS
            bool Clean = false;
            if (Clean == true)
            {
                if (Directory.Exists(MeshSettings.meshPolyMeshDir))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(MeshSettings.meshPolyMeshDir);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }

                if (Directory.Exists(MeshSettings.meshConstantDir + @"extendedFeatureEdgeMesh"))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(MeshSettings.meshConstantDir + @"extendedFeatureEdgeMesh");
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
                        var path = MeshSettings.meshWorkingDir + @"\processor" + i;
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
                        for (int l = 0; l < DOM.BCond.windDirs.Count; l++)
                        {
                            var cpuPath = MeshSettings.baseWorkingDir + DOM.BCond.windDirs[l] + @"\processor" + i;

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

            if (!Directory.Exists(MeshSettings.meshStlDir))
            {
                Directory.CreateDirectory(MeshSettings.meshStlDir);
            }

            if (!File.Exists(MeshSettings.meshWorkingDir + @"\log"))
            {
                File.WriteAllText(MeshSettings.meshWorkingDir + @"\log", "");
            }

            if (!Directory.Exists(MeshSettings.meshSystemDir))
            {
                Directory.CreateDirectory(MeshSettings.meshSystemDir);
            }

            Point3d locationInMesh = new Point3d();
            locationInMesh = DOM.LocationInMesh;

            File.WriteAllText(Path.Combine(MeshSettings.meshSystemDir + "snappyHexMeshDict"), EddyLib.Strings.OFExecDicts.SnappyHexMeshDict(MeshSettings, DOM));
            File.WriteAllText(Path.Combine(MeshSettings.meshSystemDir + "surfaceFeaturesDict"), EddyLib.Strings.OFExecDicts.surfaceFeaturesDict());
            File.WriteAllText(Path.Combine(MeshSettings.meshSystemDir + "fvSchemes"), EddyLib.Strings.OFExecDicts.FvSchemesDefault());
            File.WriteAllText(Path.Combine(MeshSettings.meshSystemDir + "fvSolution"), EddyLib.Strings.OFExecDicts.FvSolution(RunSettings));

            //File.WriteAllText(Path.Combine(MeshSettings.meshSystemDir + "meshQualityDict"), EddyLib.Strings.OFExecDicts.MeshQualityDict());
            File.WriteAllText(Path.Combine(MeshSettings.meshSystemDir + "decomposeParDict"), EddyLib.Strings.OFExecDicts.DecomposeParDict(RunSettings));

            File.WriteAllText(Path.Combine(MeshSettings.baseWorkingDir + "run_checkMesh.bat"), EddyLib.Strings.BatFiles.Run_checkMesh(RunSettings, MeshSettings, DOM, Strings.OFExecutionMode.Meshing));
            File.WriteAllText(Path.Combine(MeshSettings.baseWorkingDir + "run_reconstructMesh.bat"), EddyLib.Strings.BatFiles.Run_reconstructMesh(RunSettings, MeshSettings, DOM, Strings.OFExecutionMode.Meshing));

            //Autocalc number of CPUs
            if (RunSettings.CPUs == -1)
            {
                RunSettings.CPUs = Utilities.CalcOptimCPU(MeshSettings.meshWorkingDir, RunSettings.CPUs);
            }

            logFile = "";

            //if (!File.Exists(MeshSettings.baseWorkingDir + @"\mesh\log"))
            //{
            //    File.WriteAllText(MeshSettings.baseWorkingDir + @"\mesh\log", "");
            //}

            //using (FileStream stream = File.Open(MeshSettings.meshWorkingDir + @"\log", FileMode.Open, FileAccess.Read, FileShare.Read))
            //{
            //    using (StreamReader reader = new StreamReader(stream))
            //    {
            //        logFile = reader.ReadToEnd();
            //    }
            //}
        }
    }
}