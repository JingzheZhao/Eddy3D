using EddyLib.Indoor.Dicts;
using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;


#if DEBUG
[assembly: InternalsVisibleTo("UnitTest")]
#endif

namespace EddyLib.Indoor.Dicts
{
    internal class SnappyHexMeshDict : GenericDict
    {
        private Point3d locationInMesh;

        //  public Dictionary<string, List<Dictionary<string, Dictionary<string, string>>>> GeometryDict { get; set; } // old way of doing things

        public Dictionary<string, dynamic> GeometryDict { get; set; }

        //NEW IMPLEMENTATION

        public Dictionary<string, dynamic> GeometrySubDict { get; set; }
        // public List<Dictionary<string,dynamic>> GeometrySubDict { get; set; }

        //OLD IMPLEMENTATION
        //public List<Dictionary<string, Dictionary<string, string>>> GeometrySubDict { get; set; }

        public SnappyHexMeshDict(double refineMentLevel, Point3d pointInsideDomain, BoundingBox BBox, List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
        {
            this.DictionaryName = "snappyHexMeshDict";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);
            this.locationInMesh = pointInsideDomain;

            //NEW IMPLEMENTATION
            //this.GeometrySubDict = new List<Dictionary<string, dynamic>>();
            this.GeometrySubDict = new Dictionary<string, dynamic>();

            //OLD IMPLEMENTATION
            //this.GeometrySubDict = new List<Dictionary<string, Dictionary<string, string>>>();

            foreach (IndoorBC i in inlet) { this.GeometrySubDict.Add(i.Id, GetGeometryDict(i)); };
            foreach (IndoorBC i in outlet) { this.GeometrySubDict.Add(i.Id, GetGeometryDict(i)); };
            foreach (IndoorBC i in wall) { this.GeometrySubDict.Add(i.Id, GetGeometryDict(i)); };


            //foreach (IndoorBC i in inlet) { this.GeometrySubDict.Add("", GetGeometryDict(i)); };
            //foreach (IndoorBC i in outlet) { this.GeometrySubDict.Add("", GetGeometryDict(i)); };
            //foreach (IndoorBC i in wall) { this.GeometrySubDict.Add("", GetGeometryDict(i)); };

            //this.GeometrySubDict.Add("", "");

            this.GeometryDict = new Dictionary<string, dynamic>();
            GeometryDict.Add("geometry", GeometrySubDict);

            string[] parts = { this.Header, "\n",
               CppMapSerializerDyn.Serialize(GetSettingsDict()),"\n",
               CppMapSerializerDyn.Serialize(this.GeometryDict),"\n",
               CppMapSerializerDyn.Serialize(GetSnapControlsDict()),"\n",
               CppMapSerializerDyn.Serialize(GetCastellatedMeshControls(this.locationInMesh, wall, inlet, outlet)),"\n",
               //GetCastellatedMeshControls(this.locationInMesh, wall, inlet, outlet),"\n", OLD WAY
               CppMapSerializerDyn.Serialize(GetAddLayersControls()),"\n",
               CppMapSerializerDyn.Serialize(GetMeshQualityControls()),"\n" };

            //OLD IMPLEMENTATION
            //LayersAndMeshqualityControls() };


            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        public static Dictionary<string, dynamic> GetGeometryDict(IndoorBC input)
        {

            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add(input.Id + ".stl", InternalDict);

            InternalDict.Add("type", input.OFGeometryType);
            InternalDict.Add("name", input.Id);

            return Dict;
        }



        ////OLD IMPLEMENTATION

        //private static Dictionary<string, Dictionary<string, string>> GetGeometryDict(IndoorBC input)
        //{
        //    Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

        //    Dictionary<string, string> InternalDict = new Dictionary<string, string>();

        //    Dict.Add(input.Id + ".stl", InternalDict);

        //    InternalDict.Add("type", input.OFGeometryType);
        //    InternalDict.Add("name", input.Id);

        //    return Dict;
        //}

        private static Dictionary<string, dynamic> GetSettingsDict()
        {
            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            InternalDict.Add("castellatedMesh", "true");
            InternalDict.Add("snap", "true");
            InternalDict.Add("addLayers", "false");
            InternalDict.Add("singleRegionName", " true");
            InternalDict.Add("mergePatchFaces", "true");
            InternalDict.Add("keepPatches", "false");
            InternalDict.Add("mergeTolerance", "1e-8");
            InternalDict.Add("debug", "0");

            return InternalDict;
        }


        ////OLD IMPLEMENTATION
        //private static Dictionary<string, string> GetSettingsDict()
        //{
        //    Dictionary<string, string> InternalDict = new Dictionary<string, string>();

        //    InternalDict.Add("castellatedMesh", "true");
        //    InternalDict.Add("snap", "true");
        //    InternalDict.Add("addLayers", "false");
        //    InternalDict.Add("singleRegionName", " true");
        //    InternalDict.Add("mergePatchFaces", "true");
        //    InternalDict.Add("keepPatches", "false");
        //    InternalDict.Add("mergeTolerance", "1e-8");
        //    InternalDict.Add("debug", "0");

        //    return InternalDict;
        //}

        private static Dictionary<string, dynamic> GetSnapControlsDict()
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

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


        ////OLD IMPLEMENTATION
        //private static Dictionary<string, Dictionary<string, string>> GetSnapControlsDict()
        //{
        //    Dictionary<string, Dictionary<string, string>> Dict = new Dictionary<string, Dictionary<string, string>>();

        //    Dictionary<string, string> InternalDict = new Dictionary<string, string>();

        //    Dict.Add("snapControls", InternalDict);

        //    InternalDict.Add("nSmoothPatch          ", "3");
        //    InternalDict.Add("nSmoothInternal       ", "3");
        //    InternalDict.Add("tolerance             ", "4.0");
        //    InternalDict.Add("nSolveIter            ", "30");
        //    InternalDict.Add("nRelaxIter            ", "5");
        //    InternalDict.Add("nFeatureSnapIter      ", "10");
        //    InternalDict.Add("nFaceSplitInterval    ", "5");
        //    InternalDict.Add("detectBaffles         ", "true");
        //    InternalDict.Add("releasePoints         ", "false");
        //    InternalDict.Add("stringFeatures        ", "true");
        //    InternalDict.Add("avoidDiagonal         ", "false");
        //    InternalDict.Add("concaveAngle          ", "45");
        //    InternalDict.Add("minAreaRatio          ", "0.3");
        //    InternalDict.Add("detectNearSurfacesSnap", "true");
        //    InternalDict.Add("strictRegionSnap      ", "false");
        //    InternalDict.Add("ExplicitFeatureSnap   ", "true");
        //    InternalDict.Add("ImplicitFeatureSnap   ", "false");

        //    return Dict;
        //}

        private static Dictionary<string, dynamic> GetAddLayersControls()
        {
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict1.Add("addLayersControls", Dict2);

            Dict2.Add("relativeSizes", "true");
            Dict2.Add("firstLayerThickness", "0.3");
            Dict2.Add("expansionRatio", "1.3");
            Dict2.Add("minThickness", "0.3");
            Dict2.Add("nGrow", "0");
            Dict2.Add("featureAngle", "150");
            Dict2.Add("nSmoothSurfaceNormals", "10");
            Dict2.Add("nSmoothNormals", "15");
            Dict2.Add("nSmoothThickness", "10");
            Dict2.Add("maxFaceThicknessRatio", "0.5");
            Dict2.Add("minMedialAxisAngle", "90");
            Dict2.Add("maxThicknessToMedialRatio", "0.3");
            Dict2.Add("nRelaxIter", "5");
            Dict2.Add("nRelaxedIter", "25");
            Dict2.Add("nLayerIter", "50");
            Dict2.Add("nBufferCellsNoExtrude", "0");
            Dict2.Add("slipFeatureAngle", "30");
            Dict2.Add("mergePatchFacesAngle", "45");
            Dict2.Add("concaveAngle", "30");
            Dict2.Add("layerTerminationAngle", "30");
            Dict2.Add("nSmoothDisplacement", "0");

            Dict2.Add("layers", Dict3);

            return Dict1;
        }

        private static Dictionary<string, dynamic> GetMeshQualityControls()
        {
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict1.Add("meshQualityControls", Dict2);

            Dict2.Add("maxNonOrtho", "65");
            Dict2.Add("maxBoundarySkewness", "20");
            Dict2.Add("maxInternalSkewness", "4");
            Dict2.Add("maxConcave", "80");
            Dict2.Add("minFlatness", "0.5");
            Dict2.Add("minVol", "1e-13");
            Dict2.Add("minTetQuality", "1e-15");
            Dict2.Add("minArea", "1e-9");
            Dict2.Add("minTwist", "0.02");
            Dict2.Add("minDeterminant", "0.001");
            Dict2.Add("minFaceWeight", "0.05");
            Dict2.Add("minVolRatio", "0.01");
            Dict2.Add("minTriangleTwist", "- 1");
            Dict2.Add("nSmoothScale", "4");
            Dict2.Add("errorReduction", "0.75");

            Dict2.Add("relaxed", Dict3);

            return Dict1;
        }


        ////OLD IMPLEMENTATION
        ///
        //            private string LayersAndMeshqualityControls()
        //        {
        //            return @"addLayersControls
        //{
        //    relativeSizes   true;
        //    firstLayerThickness 0.3;
        //    expansionRatio  1.3;
        //    minThickness    0.3;
        //    nGrow           0;
        //    featureAngle    150;
        //    nSmoothSurfaceNormals 10;
        //    nSmoothNormals  15;
        //    nSmoothThickness 10;
        //    maxFaceThicknessRatio 0.5;
        //    minMedialAxisAngle 90;
        //    maxThicknessToMedialRatio 0.3;
        //    nRelaxIter      5;
        //    nRelaxedIter    25;
        //    nLayerIter      50;
        //    nBufferCellsNoExtrude 0;
        //    slipFeatureAngle 30;
        //    mergePatchFacesAngle 45;
        //    concaveAngle    30;
        //    layerTerminationAngle 30;
        //    nSmoothDisplacement 0;
        //    detectExtrusionIsland true;
        //    layers
        //    {
        //    }
        //}

        //meshQualityControls
        //{
        //    maxNonOrtho     65;
        //    maxBoundarySkewness 20;
        //    maxInternalSkewness 4;
        //    maxConcave      80;
        //    minFlatness     0.5;
        //    minVol          1e-13;
        //    minTetQuality   1e-15;
        //    minArea         1e-9;
        //    minTwist        0.02;
        //    minDeterminant  0.001;
        //    minFaceWeight   0.05;
        //    minVolRatio     0.01;
        //    minTriangleTwist -1;
        //    nSmoothScale    4;
        //    errorReduction  0.75;
        //    relaxed
        //    {
        //    }
        //}";
        //}

        //NEW IMPLEMENTATION - GetCastellatedMeshControls - beakdown

        private static Dictionary<string, dynamic> GetCastellatedMeshControls(Point3d locationInMesh, List<IndoorBC.Wall> wall, List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet)
        {

            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();


            Dict1.Add("castellatedMeshControls", Dict2);

            //mesh details
            Dict2.Add("locationInMesh", GenericDict.InParenthesis(locationInMesh.ToString().Replace(',', ' ')));
            Dict2.Add("maxLocalCells", "50000000");
            Dict2.Add("maxGlobalCells", "60000000");
            Dict2.Add("minRefinementCells", "50");
            Dict2.Add("nCellsBetweenLevels", "2");
            Dict2.Add("resolveFeatureAngle", "60");
            Dict2.Add("maxLoadUnbalance", "1");
            Dict2.Add("allowFreeStandingZoneFaces", "false");

            //mesh features
            Dict2.Add("features", new MeshFeatureObject(inlet, outlet, wall).ToString());

            //refinement surfaces
            Dict2.Add("refinementSurfaces", new MeshRefinementObject(inlet, outlet, wall).ToString());

            //refinement regions
            Dict2.Add("refinementRegions", Dict3);
            Dict3.Add(" ", " ");


            return Dict1;
        }

        //Mesh Features Object
        class MeshFeatureObject
        {

            private List<IndoorBC.Inlet> inlet { get; set; }
            private List<IndoorBC.Outlet> outlet { get; set; }
            private List<IndoorBC.Wall> wall { get; set; }


            public MeshFeatureObject(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.inlet = inlet;
                this.outlet = outlet;
                this.wall = wall;

            }

            public override string ToString()
            {

                StringBuilder sb = new StringBuilder();
                sb.Append(@"{");

                foreach (IndoorBC i in inlet) { sb.AppendLine(@"file """ + i.Id + @".eMesh"";
            level " + i.refinementLevel + @";"); }
                foreach (IndoorBC i in outlet) { sb.AppendLine(@"file """ + i.Id + @".eMesh"";
            level " + i.refinementLevel + @";"); }
                foreach (IndoorBC i in wall) { sb.AppendLine(@"file """ + i.Id + @".eMesh"";
            level " + i.refinementLevel + @";"); }

                sb.Append(@"}");

                return GenericDict.InParenthesis(sb.ToString());
            }
        }

        //Mesh Refinement Object
        class MeshRefinementObject
        {

            private List<IndoorBC.Inlet> inlet { get; set; }
            private List<IndoorBC.Outlet> outlet { get; set; }
            private List<IndoorBC.Wall> wall { get; set; }


            public MeshRefinementObject(List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
            {
                this.inlet = inlet;
                this.outlet = outlet;
                this.wall = wall;

            }

            public override string ToString()
            {

                StringBuilder sb = new StringBuilder();
                sb.Append(@"{");

                //DICTIONARY WAY
                //foreach (IndoorBC i in inlet) { CppMapSerializerDyn.Serialize(MakeDict_RefinementSurfaces(i)); }
                //foreach (IndoorBC i in outlet) { CppMapSerializerDyn.Serialize(MakeDict_RefinementSurfaces(i)); }
                //foreach (IndoorBC i in wall) { CppMapSerializerDyn.Serialize(MakeDict_RefinementSurfaces(i)); }

                //OTHER WAY - sting intead of Dict refinement surfaces
                foreach (IndoorBC i in inlet) { sb.AppendLine(GetRefinementSurfaces(i)); }
                foreach (IndoorBC i in outlet) { sb.AppendLine(GetRefinementSurfaces(i)); }
                foreach (IndoorBC i in wall) { sb.AppendLine(GetRefinementSurfaces(i)); }

                sb.Append(@"}");

                return GenericDict.InParenthesis(sb.ToString());
            }
        }





        //OLD WAY:
        //    private string GetCastellatedMeshControls(Point3d locationInMesh, List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets)
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        sb.Append(@"castellatedMeshControls
        //        {
        //locationInMesh  (" + locationInMesh.ToString().Replace(',', ' ') + @");
        //maxLocalCells   50000000;
        //maxGlobalCells  60000000;
        //minRefinementCells 50;
        //nCellsBetweenLevels 2;
        //resolveFeatureAngle 60;
        //maxLoadUnbalance 1;
        //allowFreeStandingZoneFaces false;
        //features
        //(

        //    {");

        //        foreach (IndoorBC i in Inlets) { sb.AppendLine(@"file """ + i.Id + @".eMesh"";
        //        level " + i.refinementLevel + @";"); }
        //        foreach (IndoorBC i in Outlets) { sb.AppendLine(@"file """ + i.Id + @".eMesh"";
        //        level " + i.refinementLevel + @";"); }
        //        foreach (IndoorBC i in RoomGeometry) { sb.AppendLine(@"file """ + i.Id + @".eMesh"";
        //        level " + i.refinementLevel + @";"); }

        //        sb.AppendLine(@"
        //    }

        //);");

        //        sb.AppendLine(@"    refinementSurfaces
        //{");

        //        foreach (IndoorBC i in Inlets) { sb.AppendLine(GetRefinementSurfaces(i)); }
        //        foreach (IndoorBC i in Outlets) { sb.AppendLine(GetRefinementSurfaces(i)); }
        //        foreach (IndoorBC i in RoomGeometry) { sb.AppendLine(GetRefinementSurfaces(i)); }

        //        sb.AppendLine(@"}");

        //        sb.AppendLine(@"    refinementRegions
        //{");

        //        //foreach (IndoorBC i in Inlets) { sb.Append(GetRefinementRegions(i)); }
        //        // foreach (IndoorBC i in Outlets) { sb.Append(GetRefinementRegions(i)); }
        //        //  foreach (IndoorBC i in RoomGeometry) { sb.Append(GetRefinementRegions(i)); }

        //        sb.AppendLine(@"    }");
        //        sb.AppendLine(@"}");
        //        return sb.ToString();
        //    }

        //private static Dictionary<string, dynamic> MakeDict_RefinementSurfaces(IndoorBC bc)
        //{
        //    Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();
        //    Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();
        //    Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

        //    Dict1.Add(bc.Id, Dict2);

        //    Dict2.Add("level", "(" + bc.refinementLevel.ToString() + " " + bc.refinementLevel.ToString() + ")");

        //    Dict2.Add("patchInfo", Dict3);

        //    Dict3.Add("type", bc.bcType.ToString());

        //    return Dict1;
        //}

        //OLD WAY:
        public static string GetRefinementSurfaces(IndoorBC bc)
        {
            return string.Format(@"{0}
        {{
			level           ({1} {1});
                    patchInfo
                    {{
                        type            {2};
                    }}
        }}", bc.Id, bc.refinementLevel.ToString(), bc.bcType.ToString());
        }



        //     //BEFORE OLD WAY

        //            private string GetRefinementRegions(IndoorBC bc)
        //          {
        //              return string.Format(@"{0}
        //          {
        //     level           ({1} {1});
        //                      patchInfo
        //                      {
        //                          type            {2};
        //                      }
        //          }", bc.Name, bc.refinementLevel, bc.bcType);
        //          }
    }
}

//FoamFile
//{
//    version     2.0;
//    format ascii;
//    class dictionary;
//location "system";
//object snappyHexMeshDict;
//} 
// castellatedMesh true;
//snap true;
//addLayers false;
//singleRegionName true;
//mergePatchFaces true;
//keepPatches false;
//mergeTolerance 1e-8;
//debug 0;

//geometry System.Collections.Generic.List`1[System.Collections.Generic.Dictionary`2[System.String, System.Collections.Generic.Dictionary`2[System.String, System.String]]];

//snapControls
//{
//    nSmoothPatch            3;
//    nSmoothInternal         3;
//    tolerance               4.0;
//    nSolveIter              30;
//    nRelaxIter              5;
//    nFeatureSnapIter        10;
//    nFaceSplitInterval      5;
//    detectBaffles           true;
//    releasePoints           false;
//    stringFeatures          true;
//    avoidDiagonal           false;
//    concaveAngle            45;
//    minAreaRatio            0.3;
//    detectNearSurfacesSnap  true;
//    strictRegionSnap        false;
//    ExplicitFeatureSnap     true;
//    ImplicitFeatureSnap     false;
//}

//castellatedMeshControls
//{
//    locationInMesh(3.1520913258375 - 9.49059505543707 0.431420096550907);
//    maxLocalCells   50000000;
//    maxGlobalCells  60000000;
//    minRefinementCells 50;
//    nCellsBetweenLevels 2;
//    resolveFeatureAngle 60;
//    maxLoadUnbalance 1;
//    allowFreeStandingZoneFaces false;
//    features
//    (

//        {
//        file "Inlet1.eMesh";
//        level 3;
//        file "Inlet2.eMesh";
//        level 3;
//        file "Outlet3.eMesh";
//        level 3;
//        file "Wall0.eMesh";
//        level 3;

//    }

//    );
//    refinementSurfaces
//    {
//        Inlet1
//        {
//            level(3 3);
//            patchInfo
//                    {
//                type inlet;
//            }
//        }
//        Inlet2
//        {
//            level(3 3);
//            patchInfo
//                    {
//                type inlet;
//            }
//        }
//        Outlet3
//        {
//            level(3 3);
//            patchInfo
//                    {
//                type outlet;
//            }
//        }
//        Wall0
//        {
//            level(3 3);
//            patchInfo
//                    {
//                type wall;
//            }
//        }
//    }
//    refinementRegions
//    {
//    }
//}

//addLayersControls
//{
//    relativeSizes   true;
//    firstLayerThickness 0.3;
//    expansionRatio  1.3;
//    minThickness    0.3;
//    nGrow           0;
//    featureAngle    150;
//    nSmoothSurfaceNormals 10;
//    nSmoothNormals  15;
//    nSmoothThickness 10;
//    maxFaceThicknessRatio 0.5;
//    minMedialAxisAngle 90;
//    maxThicknessToMedialRatio 0.3;
//    nRelaxIter      5;
//    nRelaxedIter    25;
//    nLayerIter      50;
//    nBufferCellsNoExtrude 0;
//    slipFeatureAngle 30;
//    mergePatchFacesAngle 45;
//    concaveAngle    30;
//    layerTerminationAngle 30;
//    nSmoothDisplacement 0;
//    detectExtrusionIsland true;
//    layers
//    {
//    }
//}

//meshQualityControls
//{
//    maxNonOrtho     65;
//    maxBoundarySkewness 20;
//    maxInternalSkewness 4;
//    maxConcave      80;
//    minFlatness     0.5;
//    minVol          1e-13;
//    minTetQuality   1e-15;
//    minArea         1e-9;
//    minTwist        0.02;
//    minDeterminant  0.001;
//    minFaceWeight   0.05;
//    minVolRatio     0.01;
//    minTriangleTwist - 1;
//    nSmoothScale    4;
//    errorReduction  0.75;
//    relaxed
//    {
//    }
//}