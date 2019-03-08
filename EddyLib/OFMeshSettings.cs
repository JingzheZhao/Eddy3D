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
        



        public void SetDirectories(string baseWorkingDirectory)
        {


            this.baseWorkingDir = baseWorkingDirectory;
            this.meshStlDir = baseWorkingDirectory + @"\mesh\constant\triSurface\";
            this.meshPolyMeshDir = baseWorkingDirectory + @"\mesh\constant\polyMesh\";
            this.meshSystemDir = baseWorkingDirectory + @"\mesh\system\";
            this.meshConstantDir = baseWorkingDirectory + @"\mesh\constant\";
            this.meshWorkingDir = baseWorkingDirectory + @"\mesh\";
            this.OFbaseWorkingDir = Utilities.ReformatWorkingDir(baseWorkingDirectory);
            this.OFmeshStlDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\triSurface\");
            this.OFmeshPolyMeshDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\polyMesh\");
            this.OFmeshSystemDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\system\");
            this.OFmeshConstantDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\constant\");
            this.OFmeshWorkingDir = Utilities.ReformatWorkingDir(baseWorkingDirectory + @"\mesh\");
            this.meshStlFilenameBuildings = baseWorkingDirectory + @"\mesh\constant\triSurface\building.stl";
            this.meshStlFilenameGround = baseWorkingDirectory + @"\mesh\constant\triSurface\ground.stl";
            this.meshStlFilenameGroundPerim = baseWorkingDirectory + @"\mesh\constant\triSurface\ground_perim.stl";
            this.meshBoundaryConditionsDirectory = baseWorkingDirectory + @"\mesh\0.org\";
            


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