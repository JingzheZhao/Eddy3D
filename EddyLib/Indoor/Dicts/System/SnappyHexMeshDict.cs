using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor
{
    internal class SnappyHexMeshDict : GenericDict
    {
        public string fullDict;

        private Point3d locationInMesh;

        public List<Dictionary<string, Dictionary<string, string>>> GeometryDict { get; set; }

        public SnappyHexMeshDict(double cellSize, BoundingBox BBox, List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
        {
            this.Name = "snappyHexMeshDict";
            this.location = @"\system\" + this.Name;
            this.header = GetHeader(this);

            this.locationInMesh = BBox.Center;

            foreach (IndoorBC i in inlet) { this.GeometryDict.Add(GetGeometryDict(i)); };

            string[] parts = { JsonConvert.SerializeObject(GetSettingsDict()), JsonConvert.SerializeObject(this.GeometryDict), JsonConvert.SerializeObject(GetSnapControlsDict()), JsonConvert.SerializeObject(GetCastellatedMeshControls(this.locationInMesh, wall, inlet, outlet)), LayersAndMeshqualityControls() };

            this.fullDict = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        public void Export(string baseWorkingDir)
        {
            File.WriteAllText(baseWorkingDir + this.location, this.fullDict);
        }

        private static Dictionary<string, Dictionary<string, string>> GetGeometryDict(IndoorBC input)
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add(input.Name + ".stl", InternalDict);

            InternalDict.Add("type", input.OFGeometryType);
            InternalDict.Add("name", input.Name);

            return Dict;
        }

        private static Dictionary<string, string> GetSettingsDict()
        {
            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            InternalDict.Add("castellatedMesh", "true;");
            InternalDict.Add("snap", "true;");
            InternalDict.Add("addLayers", "false;");
            InternalDict.Add("singleRegionName", " true;");
            InternalDict.Add("mergePatchFaces", "true;");
            InternalDict.Add("keepPatches", "false;");
            InternalDict.Add("mergeTolerance", "1e-8;");
            InternalDict.Add("debug", "0;");

            return InternalDict;
        }

        private static Dictionary<string, Dictionary<string, string>> GetSnapControlsDict()
        {
            Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

            Dictionary<string, string> InternalDict = new Dictionary<string, string>();

            Dict.Add("snapControls", InternalDict);

            InternalDict.Add("nSmoothPatch          ", "3");
            InternalDict.Add("nSmoothInternal       ", "3");
            InternalDict.Add("tolerance             ", "4.0");
            InternalDict.Add("nSolveIter            ", "30");
            InternalDict.Add("nRelaxIter            ", "5");
            InternalDict.Add("nFeatureSnapIter      ", "10");
            InternalDict.Add("nFaceSplitInterval    ", "5");
            InternalDict.Add("detectBaffles         ", "true");
            InternalDict.Add("releasePoints         ", "false");
            InternalDict.Add("stringFeatures        ", "true");
            InternalDict.Add("avoidDiagonal         ", "false");
            InternalDict.Add("concaveAngle          ", "45");
            InternalDict.Add("minAreaRatio          ", "0.3");
            InternalDict.Add("detectNearSurfacesSnap", "true");
            InternalDict.Add("strictRegionSnap      ", "false");
            InternalDict.Add("ExplicitFeatureSnap   ", "true");
            InternalDict.Add("ImplicitFeatureSnap   ", "false");

            return Dict;
        }

        private string Serialize(SnappyHexMeshDict dict)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(dict.header);

            sb.Append(dict.fullDict);

            return sb.ToString();
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

        private string GetCastellatedMeshControls(Point3d locationInMesh, List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets)
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

            foreach (IndoorBC i in Inlets) { sb.Append(GetRefinementSurfaces(i)); }
            foreach (IndoorBC i in Outlets) { sb.Append(GetRefinementSurfaces(i)); }
            foreach (IndoorBC i in RoomGeometry) { sb.Append(GetRefinementSurfaces(i)); }

            sb.AppendLine(@"}");

            sb.AppendLine(@"    refinementRegions
    {");

            //foreach (IndoorBC i in Inlets) { sb.Append(GetRefinementRegions(i)); }
            // foreach (IndoorBC i in Outlets) { sb.Append(GetRefinementRegions(i)); }
            //  foreach (IndoorBC i in RoomGeometry) { sb.Append(GetRefinementRegions(i)); }

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

        //       private string GetRefinementRegions(IndoorBC bc)
        //     {
        //         return string.Format(@"{0}
        //     {
        //level           ({1} {1});
        //                 patchInfo
        //                 {
        //                     type            {2};
        //                 }
        //     }", bc.Name, bc.refinementLevel, bc.bcType);
        //     }
    }
}