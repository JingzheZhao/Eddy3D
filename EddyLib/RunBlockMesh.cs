using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib
{
    public class RunBlockMesh
    {



        public static void RunCyl(OFCylDomain DOMCYL, OFMeshSettings MeshSettings, OFRunSettings RunSettings, string workDir)
        {

           


            if (!Directory.Exists(DOMCYL.meshStlDir))
            {
                Directory.CreateDirectory(DOMCYL.meshStlDir);
            }


            STLExport.ExportBinary(MeshSettings.meshStlFilenameBuildings, DOMCYL.CombinedMesh);




            if (DOMCYL.TerrainMesh.Faces.Count > 0)
            {
                //No perim if we use a terrain                    
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMCYL.TerrainMesh);
            }
            else
            {
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMCYL.DomainMeshGround);
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGroundPerim, DOMCYL.DomainMeshGroundPerim);
            }




            if (!Directory.Exists(DOMCYL.meshSystemDir))
            {
                Directory.CreateDirectory(DOMCYL.meshSystemDir);
            }
            if (!Directory.Exists(DOMCYL.meshConstantDir))
            {
                Directory.CreateDirectory(DOMCYL.meshConstantDir);
            }
            if (!Directory.Exists(MeshSettings.meshBoundaryConditionsDirectory))
            {
                Directory.CreateDirectory(MeshSettings.meshBoundaryConditionsDirectory);
            }


            File.WriteAllText(DOMCYL.meshSystemDir + @"\blockMeshDict", DOMCYL.StringyfyDomain2());
            File.WriteAllText(DOMCYL.baseWorkingDir + @"\mesh\case.foam", "");
            File.WriteAllText(DOMCYL.meshSystemDir + @"\controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(RunSettings, DOMCYL, null, 0));

            if (!File.Exists(workDir + @"\mesh\log"))
            {
                File.WriteAllText(workDir + @"\mesh\log", "");
            }





            //TODO: Move Daysim related code into its own class


            //export RAD for DAYSIM
            if (!Directory.Exists(DOMCYL.baseWorkingDir + @"Rad\"))
            {
                Directory.CreateDirectory(DOMCYL.baseWorkingDir + @"Rad\");
            }



            string radMat = @"
void plastic Generic_20
0
0
5 0.2 0.2 0.2 0 0 
";
            Mesh daysimMesh = new Mesh();
            daysimMesh.Append(DOMCYL.CombinedMesh);
            // Todo: add ground plane to the above mesh

            File.WriteAllText(DOMCYL.baseWorkingDir + @"Rad\materials.rad", radMat);
            RadianceFiles.MeshProc(daysimMesh, DOMCYL.baseWorkingDir + @"Rad\scene.rad", "Generic_20");




        }

        public static void RunBox(OFBoxDomain DOMBOX, OFMeshSettings MeshSettings, OFRunSettings RunSettings, string workDir)
        {

            


            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }


            if (!Directory.Exists(DOMBOX.meshStlDir))
            {
                Directory.CreateDirectory(DOMBOX.meshStlDir);
            }


            // STL export

            STLExport.ExportBinary(meshStlFilenameBuildings, DOMBOX.CombinedMesh);




            if (DOMBOX.TerrainMesh.Faces.Count > 0)
            {
                //No perim if we use a terrain
                DOMBOX.newBoxGround.Translate(Vector3d.ZAxis * 0.001);
                STLExport.ExportBinary(meshStlFilenameGround, DOMBOX.newBoxGround);
            }
            else
            {
                STLExport.ExportBinary(meshStlFilenameGround, DOMBOX.newBoxGround);
                STLExport.ExportBinary(meshStlFilenameGroundPerim, DOMBOX.newBoxGroundPerim);
            }





            if (!Directory.Exists(DOMBOX.meshSystemDir))
            {
                Directory.CreateDirectory(DOMBOX.meshSystemDir);
            }
            if (!Directory.Exists(DOMBOX.meshConstantDir))
            {
                Directory.CreateDirectory(DOMBOX.meshConstantDir);
            }
            if (!Directory.Exists(meshBoundaryConditionsDirectory))
            {
                Directory.CreateDirectory(meshBoundaryConditionsDirectory);
            }





            File.WriteAllText(DOMBOX.meshSystemDir + @"\blockMeshDict", EddyLib.StrTemp.OFExecDicts.BlockMeshDict(DOMBOX));
            File.WriteAllText(DOMBOX.baseWorkingDir + @"\mesh\case.foam", "");
            File.WriteAllText(DOMBOX.meshSystemDir + @"\controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(RunSettings, DOMBOX, null, 0));

            if (!File.Exists(workDir + @"\mesh\log"))
            {
                File.WriteAllText(workDir + @"\mesh\log", "");
            }



            //export RAD for DAYSIM
            if (!Directory.Exists(DOMBOX.baseWorkingDir + @"Rad\"))
            {
                Directory.CreateDirectory(DOMBOX.baseWorkingDir + @"Rad\");
            }
            string radMat = @"
void plastic Generic_20
0
0
5 0.2 0.2 0.2 0 0 
";
            Mesh daysimMesh = new Mesh();
            daysimMesh.Append(DOMBOX.CombinedMesh);
            // Todo: add ground plane to the above mesh

            File.WriteAllText(DOMBOX.baseWorkingDir + @"Rad\materials.rad", radMat);
            RadianceFiles.MeshProc(daysimMesh, DOMBOX.baseWorkingDir + @"Rad\scene.rad", "Generic_20");


            string logFile = "";

            using (FileStream stream = File.Open(workDir + @"\mesh\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    logFile = reader.ReadToEnd();
                    //while (!reader.EndOfStream)
                    //{

                    //}

                }
            }
        }


    }

}
