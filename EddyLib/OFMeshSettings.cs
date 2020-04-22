namespace EddyLib
{
    public enum SnappySetting
    {
        Blocks,

        BlocksSnapping,

        BlocksSnappingLayers
    }

    public class OFMeshSettings
    {
        public int accBuildings = 2;

        public int accFeatures = 2;

        public int accRefinement = 2;

        public int accGround = 2;

        public int nLayers = 2;

        public SnappySetting snappySetting = SnappySetting.Blocks;

        public string baseWorkingDir;

        public string meshStlDir;

        public string meshPolyMeshDir;

        public string meshSystemDir;

        public string meshConstantDir;

        public string meshWorkingDir;

        public string OFbaseWorkingDir;

        public string OFmeshStlDir;

        public string OFmeshPolyMeshDir;

        public string OFmeshSystemDir;

        public string OFmeshConstantDir;

        public string OFmeshWorkingDir;

        public string meshStlFilenameBuildings;

        public string meshStlFilenameGround;

        public string meshStlFilenameGroundPerim;

        public string meshBoundaryConditionsDirectory;

        // BlockMesh

        //private readonly double gradingPerim;

        //public double GradingPerim { get => gradingPerim; set => gradingPerim = value; }

        public void SetDirectories(string baseWorkingDirectory)
        {
            baseWorkingDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory);
            meshStlDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\triSurface\");
            meshPolyMeshDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\polyMesh\");
            meshSystemDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\system\");
            meshConstantDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\");
            meshWorkingDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\");

            OFbaseWorkingDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory);
            OFmeshStlDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\triSurface\");
            OFmeshPolyMeshDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\polyMesh\");
            OFmeshSystemDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\system\");
            OFmeshConstantDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\");
            OFmeshWorkingDir = Utilities.Directories.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\");

            meshStlFilenameBuildings = baseWorkingDirectory + @"\mesh\constant\triSurface\building.stl";
            meshStlFilenameGround = baseWorkingDirectory + @"\mesh\constant\triSurface\ground.stl";
            meshStlFilenameGroundPerim = baseWorkingDirectory + @"\mesh\constant\triSurface\ground_perim.stl";
            meshBoundaryConditionsDirectory = baseWorkingDirectory + @"\mesh\0.org\";
        }

        public override string ToString()
        {
            return string.Format(@"accBuilding = {0}
accFeatures = {1}
accRefinement = {2}
accGround = {3}
nLayers = {4}
Snappy Settings = {5}", accBuildings, accFeatures, accRefinement, accGround, nLayers, snappySetting);
        }
    }
}