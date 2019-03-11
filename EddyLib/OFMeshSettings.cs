using System;

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

        private readonly double gradingPerim;

        //public double GradingPerim { get => gradingPerim; set => gradingPerim = value; }

        public void SetDirectories(string baseWorkingDirectory)
        {


            baseWorkingDir = baseWorkingDirectory;
            meshStlDir = baseWorkingDirectory + @"\mesh\constant\triSurface\";
            meshPolyMeshDir = baseWorkingDirectory + @"\mesh\constant\polyMesh\";
            meshSystemDir = baseWorkingDirectory + @"\mesh\system\";
            meshConstantDir = baseWorkingDirectory + @"\mesh\constant\";
            meshWorkingDir = baseWorkingDirectory + @"\mesh\";

            OFbaseWorkingDir = Utilities.ReformatWorkingDir(baseWorkingDirectory);
            OFmeshStlDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\triSurface\");
            OFmeshPolyMeshDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\polyMesh\");
            OFmeshSystemDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\system\");
            OFmeshConstantDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\");
            OFmeshWorkingDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\");

            meshStlFilenameBuildings = baseWorkingDirectory + @"\mesh\constant\triSurface\building.stl";
            meshStlFilenameGround = baseWorkingDirectory + @"\mesh\constant\triSurface\ground.stl";
            meshStlFilenameGroundPerim = baseWorkingDirectory + @"\mesh\constant\triSurface\ground_perim.stl";
            meshBoundaryConditionsDirectory = baseWorkingDirectory + @"\mesh\0.org\";



        }

        public override string ToString()
        {
            return String.Format(@"accBuilding = {0}
accFeatures = {1}
accRefinement = {2}
accGround = {3}
nLayers = {4}
Snappy Settings = {5}", accBuildings, accFeatures, accRefinement, accGround, nLayers, snappySetting);

        }
    }
}