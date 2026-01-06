using System.IO;

namespace EddyLib
{
    public enum SnappySnapSettings
    {
        Blocks,

        BlocksSnapping,

        BlocksSnappingLayers
    }

    public enum SnappyMiscSettings
    {
        Default,

        //BIMHVAC
        Optimized
    }

    public class OFMeshSettings
    {
        public int accBuildings { get; set; } = 2;

        public int accBuildingsMax { get; set; } = 4;

        public int accFeatures { get; set; } = 2;

        public int accBoxRefinement { get; set; } = 2;

        public int accGround { get; set; } = 2;

        public int nLayers { get; set; } = 2;

        public SnappyMiscSettings miscSettings { get; set; }

        public SnappySnapSettings snappySetting { get; set; }

        public int nCellsBetweenLevels { get; set; } = 4;

        public string baseWorkingDir { get; set; }

        public string meshStlDir { get; set; }

        public string meshPolyMeshDir { get; set; }

        public string meshSystemDir { get; set; }

        public string meshConstantDir { get; set; }

        public string meshWorkingDir { get; set; }

        public string DockerbaseWorkingDir { get; set; }

        public string DockermeshStlDir { get; set; }

        public string DockermeshPolyMeshDir { get; set; }

        public string DockermeshSystemDir { get; set; }

        public string DockermeshConstantDir { get; set; }

        public string DockermeshWorkingDir { get; set; }

        public string meshStlFilenameBuildings { get; set; }

        public string meshStlFilenameGround { get; set; }

        public string meshStlFilenameGroundPerim { get; set; }

        public string meshBoundaryConditionsDirectory { get; set; }

        public void SetDirectories(string baseWorkingDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseWorkingDirectory))
            {
                baseWorkingDir = baseWorkingDirectory ?? string.Empty;
                meshWorkingDir = string.Empty;
                meshStlDir = string.Empty;
                meshPolyMeshDir = string.Empty;
                meshSystemDir = string.Empty;
                meshConstantDir = string.Empty;
                meshBoundaryConditionsDirectory = string.Empty;
                meshStlFilenameBuildings = string.Empty;
                meshStlFilenameGround = string.Empty;
                meshStlFilenameGroundPerim = string.Empty;
                DockerbaseWorkingDir = string.Empty;
                DockermeshWorkingDir = string.Empty;
                DockermeshStlDir = string.Empty;
                DockermeshPolyMeshDir = string.Empty;
                DockermeshSystemDir = string.Empty;
                DockermeshConstantDir = string.Empty;
                return;
            }

            baseWorkingDir = Utilities.Directories.FixDirectories(baseWorkingDirectory.Trim());

            meshWorkingDir = EnsureTrailingSeparator(Path.Combine(baseWorkingDir, "mesh"));
            meshStlDir = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "constant", "triSurface"));
            meshPolyMeshDir = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "constant", "polyMesh"));
            meshSystemDir = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "system"));
            meshConstantDir = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "constant"));
            meshBoundaryConditionsDirectory = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "0.org"));

            meshStlFilenameBuildings = Path.Combine(meshStlDir, "building.stl");
            meshStlFilenameGround = Path.Combine(meshStlDir, "ground.stl");
            meshStlFilenameGroundPerim = Path.Combine(meshStlDir, "ground_perim.stl");

            DockerbaseWorkingDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDir);
            DockermeshWorkingDir = Utilities.Directories.ReformatWorkingDir(meshWorkingDir);
            DockermeshStlDir = Utilities.Directories.ReformatWorkingDir(meshStlDir);
            DockermeshPolyMeshDir = Utilities.Directories.ReformatWorkingDir(meshPolyMeshDir);
            DockermeshSystemDir = Utilities.Directories.ReformatWorkingDir(meshSystemDir);
            DockermeshConstantDir = Utilities.Directories.ReformatWorkingDir(meshConstantDir);
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            return path.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        public override string ToString()
        {
            return string.Format(@"accBuilding = {0}
accBuildingMax = {1}
accFeatures = {2}
accRefinement = {3}
accGround = {4}
miscSettings = {5}
nLayers = {6}
nCellsBetweenLevels = {7}
Snap Settings = {8}", accBuildings, accBuildingsMax, accFeatures, accBoxRefinement, accGround, miscSettings, nLayers, nCellsBetweenLevels, snappySetting);
        }
    }
}
