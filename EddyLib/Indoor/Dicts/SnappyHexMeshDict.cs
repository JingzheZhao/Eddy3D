using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor
{
    internal class SnappyHexMeshDict : GenericDict
    {
        public string boundaryFieldString;

        private Point3d locationInMesh;

        public SnappyHexMeshDict(double cellSize, BoundingBox BBox)
        {
            this.Name = "snappyHexMeshDict";
            this.location = @"\system\" + this.Name;
            this.header = GetHeader(this);

            this.locationInMesh = BBox.Center;

            string[] parts = { Settings(), SnapControls() };

            this.boundaryFieldString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private string Settings()
        {
            return @"castellatedMesh true;

snap            true;

addLayers       false;

singleRegionName true;

mergePatchFaces true;

keepPatches     false;

mergeTolerance  1e-8;

debug           0;";
        }

        private string SnapControls()
        {
            return @"snapControls
{
    nSmoothPatch    3;
    nSmoothInternal 3;
    tolerance       4.0;
    nSolveIter      30;
    nRelaxIter      5;
    nFeatureSnapIter 10;
    nFaceSplitInterval 5;
    detectBaffles   true;
    releasePoints   false;
    stringFeatures  true;
    avoidDiagonal   false;
    concaveAngle    45;
    minAreaRatio    0.3;
    detectNearSurfacesSnap true;
    strictRegionSnap false;
    ExplicitFeatureSnap true;
    ImplicitFeatureSnap false;
}";
        }

        private string Serialize(SnappyHexMeshDict dict)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(dict.header);

            sb.Append(dict.boundaryFieldString);

            return sb.ToString();
        }

        public void Export(string baseWorkingDir)
        {
            File.WriteAllText(baseWorkingDir + this.location, this.Serialize(this));
        }

        private string LayersAndMeshqualityControls()
        {
            return @"addLayersControls
{
    relativeSizes   true;
    firstLayerThickness 0.3;
    expansionRatio  1.3;
    minThickness    0.3;
    nGrow           0;
    featureAngle    150;
    nSmoothSurfaceNormals 10;
    nSmoothNormals  15;
    nSmoothThickness 10;
    maxFaceThicknessRatio 0.5;
    minMedialAxisAngle 90;
    maxThicknessToMedialRatio 0.3;
    nRelaxIter      5;
    nRelaxedIter    25;
    nLayerIter      50;
    nBufferCellsNoExtrude 0;
    slipFeatureAngle 30;
    mergePatchFacesAngle 45;
    concaveAngle    30;
    layerTerminationAngle 30;
    nSmoothDisplacement 0;
    detectExtrusionIsland true;
    layers
    {
    }
}

meshQualityControls
{
    maxNonOrtho     65;
    maxBoundarySkewness 20;
    maxInternalSkewness 4;
    maxConcave      80;
    minFlatness     0.5;
    minVol          1e-13;
    minTetQuality   1e-15;
    minArea         1e-9;
    minTwist        0.02;
    minDeterminant  0.001;
    minFaceWeight   0.05;
    minVolRatio     0.01;
    minTriangleTwist -1;
    nSmoothScale    4;
    errorReduction  0.75;
    relaxed
    {
    }
}";
        }

        private string CastellatedMeshControls(Point3d locationInMesh, List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"
            {
    locationInMesh  (" + locationInMesh.ToString().Replace(',', ' ') + @");
    maxLocalCells   50000000;
    maxGlobalCells  60000000;
    minRefinementCells 50;
    nCellsBetweenLevels 1;
    resolveFeatureAngle 60;
    maxLoadUnbalance 1;
    allowFreeStandingZoneFaces false;
    features
    (

        {");

            foreach (IndoorBC i in Inlets) { sb.Append(@"file            " + i.Name + @".eMesh"";
            level " + i.refinementLevel + @";"); }
            foreach (IndoorBC i in Outlets) { sb.Append(@"file            " + i.Name + @".eMesh"";
            level " + i.refinementLevel + @";"); }
            foreach (IndoorBC i in RoomGeometry) { sb.Append(@"file            " + i.Name + @".eMesh"";
            level " + i.refinementLevel + @";"); }

            sb.AppendLine(@"}

    );");

            sb.AppendLine(@"    refinementSurfaces
    {");

            sb.AppendLine(@"}");
            return sb.ToString();
        }

        private string GetRefinementSurfaces(IndoorBC bc)
        {
            return string.Format(@"{0}
        {
			level           ({1} {1});
                    patchInfo
                    {
                        type            {2};
                    }
        }", bc.Name, bc.refinementLevel, bc.bcType);
        }
    }
}