using System;
using System.Drawing;
using System.IO;
using Rhino.Geometry;

namespace EddyLib
{
    public class RunBlockMesh
    {
        public static void RunCyl(OFCylDomain DOMCYL, OFMeshSettings MeshSettings, OFRunSettings RunSettings, string workDir)
        {
            if (!Directory.Exists(MeshSettings.meshStlDir))
            {
                Directory.CreateDirectory(MeshSettings.meshStlDir);
            }

            STLExport.ExportBinary(MeshSettings.meshStlFilenameBuildings, DOMCYL.BuildingGeometry);

            if (DOMCYL.hasTerrain)
            {
                //No perim if we use a terrain

                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMCYL.TerrainMesh);
            }
            else
            {
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMCYL.CylDomainMeshGround);
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGroundPerim, DOMCYL.CylDomainMeshGroundPerim);
            }

            if (!Directory.Exists(MeshSettings.meshSystemDir))
            {
                Directory.CreateDirectory(MeshSettings.meshSystemDir);
            }
            if (!Directory.Exists(MeshSettings.meshConstantDir))
            {
                Directory.CreateDirectory(MeshSettings.meshConstantDir);
            }
            if (!Directory.Exists(MeshSettings.meshBoundaryConditionsDirectory))
            {
                Directory.CreateDirectory(MeshSettings.meshBoundaryConditionsDirectory);
            }

            File.WriteAllText(MeshSettings.meshSystemDir + @"\blockMeshDict", DOMCYL.StringyfyDomain2());
            File.WriteAllText(MeshSettings.baseWorkingDir + @"\mesh\mesh.foam", "");
            File.WriteAllText(MeshSettings.meshSystemDir + @"\controlDict", EddyLib.Strings.OFExecDicts.ControlDict(RunSettings, DOMCYL, null, 0));

            if (!File.Exists(workDir + @"\mesh\log"))
            {
                File.WriteAllText(workDir + @"\mesh\log", "");
            }
        }

        public static void RunBox(OFBoxDomain DOMBOX, OFMeshSettings MeshSettings, OFRunSettings RunSettings, string workDir)
        {
            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }

            if (!Directory.Exists(MeshSettings.meshStlDir))
            {
                Directory.CreateDirectory(MeshSettings.meshStlDir);
            }

            // STL export

            STLExport.ExportBinary(MeshSettings.meshStlFilenameBuildings, DOMBOX.BuildingGeometry);

            if (DOMBOX.hasTerrain)
            {
                //No perim if we use a terrain
                DOMBOX.DomainMeshGround.Translate(Vector3d.ZAxis * 0.001);
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMBOX.DomainMeshGround);
            }
            else
            {
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMBOX.DomainMeshGround);
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGroundPerim, DOMBOX.DomainMeshGroundPerim);
            }

            if (!Directory.Exists(MeshSettings.meshSystemDir))
            {
                Directory.CreateDirectory(MeshSettings.meshSystemDir);
            }
            if (!Directory.Exists(MeshSettings.meshConstantDir))
            {
                Directory.CreateDirectory(MeshSettings.meshConstantDir);
            }
            if (!Directory.Exists(MeshSettings.meshBoundaryConditionsDirectory))
            {
                Directory.CreateDirectory(MeshSettings.meshBoundaryConditionsDirectory);
            }

            File.WriteAllText(MeshSettings.meshSystemDir + @"\blockMeshDict", EddyLib.Strings.OFExecDicts.BlockMeshDict(DOMBOX));
            File.WriteAllText(MeshSettings.baseWorkingDir + @"\mesh\mesh.foam", "");
            File.WriteAllText(MeshSettings.meshSystemDir + @"\controlDict", EddyLib.Strings.OFExecDicts.ControlDict(RunSettings, DOMBOX, null, 0));

            if (!File.Exists(workDir + @"\mesh\log"))
            {
                File.WriteAllText(workDir + @"\mesh\log", "");
            }

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

        public static void SaveFrontagePNGs(String dirToSavePNGs, int windDir, Bitmap[] bitmapArray)
        {
            string folder = Path.GetDirectoryName(dirToSavePNGs);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            foreach (Bitmap bm in bitmapArray)
            {
                if (bm != null)
                {
                    var filePath = dirToSavePNGs + "FA_" + windDir + ".png";

                    bm.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }
    }
}