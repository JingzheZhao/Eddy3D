using EddyLib.OpenFOAM;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.IO;

namespace EddyLib
{
    /// <summary>
    /// Generates OpenFOAM blockMesh files for domain geometry.
    /// </summary>
    public static class RunBlockMesh
    {
        /// <summary>
        /// Generates blockMesh files for a cylindrical domain.
        /// </summary>
        /// <param name="domain">Cylindrical domain.</param>
        /// <param name="meshSettings">Mesh generation settings.</param>
        /// <param name="runSettings">Simulation run settings.</param>
        /// <param name="workDir">Working directory for output.</param>
        public static void RunCyl(OFCylDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings, string workDir)
        {
            OpenFOAMPaths.EnsureWorkDirectory(workDir);
            var paths = OpenFOAMPaths.CreateMeshPaths(meshSettings);

            // Export building geometry
            STLExport.ExportBinary(paths.BuildingsStl, domain.BuildingGeometry);

            // Export ground geometry
            if (domain.HasTerrain)
            {
                STLExport.ExportBinary(paths.GroundStl, domain.TerrainMesh);
            }
            else
            {
                STLExport.ExportBinary(paths.GroundStl, domain.CylDomainMeshGround);
                STLExport.ExportBinary(paths.GroundPerimStl, domain.CylDomainMeshGroundPerim);
            }

            // Write mesh dictionaries
            WriteMeshDicts(paths, domain.StringyfyDomain2(), runSettings, domain, 0);

            OpenFOAMPaths.EnsureMeshLog(workDir);
        }

        /// <summary>
        /// Generates blockMesh files for a box domain.
        /// </summary>
        /// <param name="domain">Box domain.</param>
        /// <param name="meshSettings">Mesh generation settings.</param>
        /// <param name="runSettings">Simulation run settings.</param>
        /// <param name="workDir">Working directory for output.</param>
        public static void RunBox(OFBoxDomain domain, OFMeshSettings meshSettings, OFRunSettings runSettings, string workDir)
        {
            OpenFOAMPaths.EnsureWorkDirectory(workDir);
            var paths = OpenFOAMPaths.CreateMeshPaths(meshSettings);

            // Export building geometry
            STLExport.ExportBinary(paths.BuildingsStl, domain.BuildingGeometry);

            // Export ground geometry
            if (domain.HasTerrain)
            {
                // Slight Z offset to avoid z-fighting
                domain.DomainMeshGround.Translate(Vector3d.ZAxis * 0.001);
                STLExport.ExportBinary(paths.GroundStl, domain.DomainMeshGround);
            }
            else
            {
                STLExport.ExportBinary(paths.GroundStl, domain.DomainMeshGround);
                STLExport.ExportBinary(paths.GroundPerimStl, domain.DomainMeshGroundPerim);
            }

            // Write mesh dictionaries
            WriteMeshDicts(paths, Strings.OFExecDicts.BlockMeshDict(domain), runSettings, domain, 0);

            OpenFOAMPaths.EnsureMeshLog(workDir);
        }

        /// <summary>
        /// Saves frontage area PNG images for visualization.
        /// </summary>
        /// <param name="dirToSavePNGs">Directory path for PNG files.</param>
        /// <param name="windDir">Wind direction in degrees.</param>
        /// <param name="bitmapArray">Array of frontage bitmaps.</param>
        public static void SaveFrontagePNGs(string dirToSavePNGs, int windDir, Bitmap[] bitmapArray)
        {
            var folder = Path.GetDirectoryName(dirToSavePNGs);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            foreach (var bm in bitmapArray)
            {
                if (bm != null)
                {
                    var filePath = Path.Combine(dirToSavePNGs, $"FA_{windDir}.png");
                    bm.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        private static void WriteMeshDicts(MeshPaths paths, string blockMeshDict, OFRunSettings runSettings, OFBaseDomain domain, int index)
        {
            DictFileWriter.WriteDictToDir(paths.SystemDir, "blockMeshDict", blockMeshDict);
            DictFileWriter.WriteDictToDir(paths.SystemDir, "controlDict", Strings.OFExecDicts.ControlDict(runSettings, domain, null, index));
            DictFileWriter.WriteFoamFile(paths.WorkingDir, "mesh");
        }
    }
}
