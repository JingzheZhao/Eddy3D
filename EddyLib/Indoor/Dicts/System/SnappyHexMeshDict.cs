using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace EddyLib.Indoor.Dicts
{
    internal class SnappyHexMeshDict : GenericDict
    {
        private Point3d locationInMesh;

        public Dictionary<string, dynamic> GeometryDict { get; set; }

        //NEW IMPLEMENTATION

        public Dictionary<string, dynamic> GeometrySubDict { get; set; }

        public SnappyHexMeshDict(double refineMentLevel, Point3d pointInsideDomain, BoundingBox BBox, List<IndoorBC.Inlet> inlet, List<IndoorBC.Outlet> outlet, List<IndoorBC.Wall> wall)
        {
            this.DictionaryName = "snappyHexMeshDict";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);
            this.locationInMesh = pointInsideDomain;

            this.GeometrySubDict = new Dictionary<string, dynamic>();

            foreach (IndoorBC i in inlet) { this.GeometrySubDict.Add(i.Id + ".stl", GetGeometryDict(i)); }
            ;
            foreach (IndoorBC i in outlet) { this.GeometrySubDict.Add(i.Id + ".stl", GetGeometryDict(i)); }
            ;
            foreach (IndoorBC i in wall) { this.GeometrySubDict.Add(i.Id + ".stl", GetGeometryDict(i)); }
            ;

            this.GeometryDict = new Dictionary<string, dynamic>();
            GeometryDict.Add("geometry", GeometrySubDict);

            string[] parts = { this.Header, "\n",
               CppMapSerializerDyn.Serialize(GetSettingsDict()),"\n",
               CppMapSerializerDyn.Serialize(this.GeometryDict),"\n",
               CppMapSerializerDyn.Serialize(GetSnapControlsDict()),"\n",
               CppMapSerializerDyn.Serialize(GetCastellatedMeshControls(this.locationInMesh, wall, inlet, outlet)),"\n",
               CppMapSerializerDyn.Serialize(GetAddLayersControls()),"\n",
               CppMapSerializerDyn.Serialize(GetMeshQualityControls()),"\n" };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        public static Dictionary<string, dynamic> GetGeometryDict(IndoorBC input)
        {
            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            InternalDict.Add("type", input.OFGeometryType);
            InternalDict.Add("file", $"\"{input.Id}.stl\"");
            InternalDict.Add("name", input.Id);

            return InternalDict;
        }

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
            InternalDict.Add("explicitFeatureSnap   ", "true");
            InternalDict.Add("implicitFeatureSnap   ", "false");

            return Dict;
        }

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
            Dict2.Add("minTriangleTwist", "-1");
            Dict2.Add("nSmoothScale", "4");
            Dict2.Add("errorReduction", "0.75");

            Dict2.Add("relaxed", Dict3);

            return Dict1;
        }

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
            Dict2.Add("nCellsBetweenLevels", "4");
            Dict2.Add("resolveFeatureAngle", "60");
            Dict2.Add("maxLoadUnbalance", "1");
            Dict2.Add("allowFreeStandingZoneFaces", "false");

            //mesh features
            Dict2.Add("features", new MeshFeatureObject(inlet, outlet, wall).ToString());

            //refinement surfaces
            Dict2.Add("refinementSurfaces", new MeshRefinementObject(inlet, outlet, wall).ToString());

            //refinement regions
            Dict2.Add("refinementRegions", Dict3);

            return Dict1;
        }

        //Mesh Features Object
        private class MeshFeatureObject
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

                AppendFeatureEntries(sb, inlet);
                AppendFeatureEntries(sb, outlet);
                AppendFeatureEntries(sb, wall);

                return GenericDict.InParenthesis(sb.ToString());
            }

            private static void AppendFeatureEntries<T>(StringBuilder sb, IEnumerable<T> boundaries)
                where T : IndoorBC
            {
                foreach (IndoorBC boundary in boundaries)
                {
                    sb.AppendLine("{");
                    sb.AppendLine($@"    file ""{boundary.Id}.eMesh"";");
                    sb.AppendLine($"    level {boundary.refinementLevel};");
                    sb.AppendLine("}");
                }
            }
        }

        //Mesh Refinement Object
        private class MeshRefinementObject
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

                //return GenericDict.InParenthesis(sb.ToString());
                return sb.ToString();
            }
        }

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
        }}", bc.Id, bc.refinementLevel.ToString(), GetPolyPatchType(bc));
        }

        private static string GetPolyPatchType(IndoorBC bc)
        {
            return bc.bcType == IndoorBC.BCType.wall ? "wall" : "patch";
        }
    }
}
