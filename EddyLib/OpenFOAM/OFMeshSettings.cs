using EddyLib.Helpers;
using System.IO;

namespace EddyLib
{
    public enum SnappySnapSettings
    {
        Blocks,

        BlocksSnapping,

        BlocksSnappingLayers
    }

    public enum MeshPreset
    {
        Default = 0,
        GPT53Codex = 1
    }

    public class OFMeshSettings
    {
        public int accBuildings { get; set; } = 2;

        public int accBuildingsMax { get; set; } = 4;

        public int accFeatures { get; set; } = 2;

        public int accBoxRefinement { get; set; } = 2;

        public int accGround { get; set; } = 4;

        public int nLayers { get; set; } = 4;

        public SnappySnapSettings snappySetting { get; set; } = SnappySnapSettings.BlocksSnapping;

        public int nCellsBetweenLevels { get; set; } = 5;

        public MeshPreset preset { get; set; } = MeshPreset.Default;

        public string baseWorkingDir { get; set; }

        public string meshStlDir { get; set; }

        public string meshPolyMeshDir { get; set; }

        public string meshSystemDir { get; set; }

        public string meshConstantDir { get; set; }

        public string meshWorkingDir { get; set; }

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
                return;
            }

            baseWorkingDir = DirectoryHelpers.EnsureTrailingBackslash(baseWorkingDirectory.Trim());

            meshWorkingDir = EnsureTrailingSeparator(Path.Combine(baseWorkingDir, "mesh"));
            meshStlDir = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "constant", "triSurface"));
            meshPolyMeshDir = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "constant", "polyMesh"));
            meshSystemDir = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "system"));
            meshConstantDir = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "constant"));
            meshBoundaryConditionsDirectory = EnsureTrailingSeparator(Path.Combine(meshWorkingDir, "0.org"));

            meshStlFilenameBuildings = Path.Combine(meshStlDir, "building.stl");
            meshStlFilenameGround = Path.Combine(meshStlDir, "ground.stl");
            meshStlFilenameGroundPerim = Path.Combine(meshStlDir, "ground_perim.stl");

        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            return path.EndsWith("/") || path.EndsWith("\\")
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
nLayers = {5}
nCellsBetweenLevels = {6}
Snap Settings = {7}
Preset = {8}", accBuildings, accBuildingsMax, accFeatures, accBoxRefinement, accGround, nLayers, nCellsBetweenLevels, snappySetting, preset);
        }
    }
}
