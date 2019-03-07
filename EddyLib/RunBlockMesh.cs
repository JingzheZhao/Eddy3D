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



        public static void RunCyl(OFCylDomain DOMCYL, OFMeshSettings MeshSettings, string workDir) {

            var meshStlFilenameBuildings = DOMCYL.baseWorkingDir + @"\mesh\constant\triSurface\building.stl";
            var meshStlFilenameGround = DOMCYL.baseWorkingDir + @"\mesh\constant\triSurface\ground.stl";
            var meshStlFilenameGroundPerim = DOMCYL.baseWorkingDir + @"\mesh\constant\triSurface\ground_perim.stl";
            var meshBoundaryConditionsDirectory = DOMCYL.baseWorkingDir + @"\mesh\0.org\";


            if (!Directory.Exists(DOMCYL.meshStlDir))
            {
                Directory.CreateDirectory(DOMCYL.meshStlDir);
            }


            STLExport.ExportBinary(meshStlFilenameBuildings, DOMCYL.CombinedMesh);




            if (DOMCYL.TerrainMesh.Faces.Count > 0)
            {
                //No perim if we use a terrain                    
                STLExport.ExportBinary(meshStlFilenameGround, DOMCYL.TerrainMesh);
            }
            else
            {
                STLExport.ExportBinary(meshStlFilenameGround, DOMCYL.DomainMeshGround);
                STLExport.ExportBinary(meshStlFilenameGroundPerim, DOMCYL.DomainMeshGroundPerim);
            }




            if (!Directory.Exists(DOMCYL.meshSystemDir))
            {
                Directory.CreateDirectory(DOMCYL.meshSystemDir);
            }
            if (!Directory.Exists(DOMCYL.meshConstantDir))
            {
                Directory.CreateDirectory(DOMCYL.meshConstantDir);
            }
            if (!Directory.Exists(meshBoundaryConditionsDirectory))
            {
                Directory.CreateDirectory(meshBoundaryConditionsDirectory);
            }


            File.WriteAllText(DOMCYL.meshSystemDir + @"\blockMeshDict", DOMCYL.StringyfyDomain2());
            File.WriteAllText(DOMCYL.baseWorkingDir + @"\mesh\case.foam", "");
            File.WriteAllText(DOMCYL.meshSystemDir + @"\controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(DOMCYL, null, 0));

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


    }




}
