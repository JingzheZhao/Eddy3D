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
        public int accBuildings = 2;

        public int accBuildingsMax = 4;

        public int accFeatures = 2;

        public int accBoxRefinement = 2;

        public int accGround = 2;

        public int nLayers = 2;

        public SnappyMiscSettings miscSettings;

        public SnappySnapSettings snappySetting;

        public string baseWorkingDir;

        public string meshStlDir;

        public string meshPolyMeshDir;

        public string meshSystemDir;

        public string meshConstantDir;

        public string meshWorkingDir;

        public string DockerbaseWorkingDir;

        public string DockermeshStlDir;

        public string DockermeshPolyMeshDir;

        public string DockermeshSystemDir;

        public string DockermeshConstantDir;

        public string DockermeshWorkingDir;

        public string meshStlFilenameBuildings;

        public string meshStlFilenameGround;

        public string meshStlFilenameGroundPerim;

        public string meshBoundaryConditionsDirectory;

        // BlockMesh

        //private readonly double gradingPerim;

        //public double GradingPerim { get => gradingPerim; set => gradingPerim = value; }

        public void SetDirectories(string baseWorkingDirectory)
        {
            baseWorkingDir = baseWorkingDirectory;
            meshStlDir = baseWorkingDirectory + @"\mesh\constant\triSurface\";
            meshPolyMeshDir = baseWorkingDirectory + @"\mesh\constant\polyMesh\";
            meshSystemDir = baseWorkingDirectory + @"\mesh\system\";
            meshConstantDir = baseWorkingDirectory + @"\mesh\constant\";
            meshWorkingDir = baseWorkingDirectory + @"\mesh\";

            DockerbaseWorkingDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory);
            DockermeshStlDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\triSurface\");
            DockermeshPolyMeshDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\polyMesh\");
            DockermeshSystemDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\system\");
            DockermeshConstantDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\");
            DockermeshWorkingDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\");

            meshStlFilenameBuildings = baseWorkingDirectory + @"\mesh\constant\triSurface\building.stl";
            meshStlFilenameGround = baseWorkingDirectory + @"\mesh\constant\triSurface\ground.stl";
            meshStlFilenameGroundPerim = baseWorkingDirectory + @"\mesh\constant\triSurface\ground_perim.stl";
            meshBoundaryConditionsDirectory = baseWorkingDirectory + @"\mesh\0.org\";
        }

        public override string ToString()
        {
            return string.Format(@"accBuilding = {0}
accBuildingMax = {1}
accFeatures = {2}
accRefinement = {3}
accGround = {4}
accGround = {5}
nLayers = {6}
Snap Settings = {7}", accBuildings, accBuildingsMax, accFeatures, accBoxRefinement, accGround, miscSettings, nLayers, snappySetting);
        }
    }
}