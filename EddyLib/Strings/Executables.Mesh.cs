using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace EddyLib.Strings
{
    public partial class OFExecDicts
    {
        #region SnappyHexMesh Helpers

        /// <summary>
        /// Gets the OpenFOAM file header for snappyHexMeshDict.
        /// </summary>
        private static string GetSnappyHeader() => @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.3.0                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.com                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version 2.0;
    format ascii;
    class dictionary;
    location system;
    object snappyHexMeshDict;
}

";

        /// <summary>
        /// Builds the refinement box geometry string.
        /// </summary>
        private static string GetRefinementBox(OFBaseDomain dom) => $@"refinementBox{{
          type searchableBox;
          min ({Utilities.FormatDouble(dom.BBox.X.Min)} {Utilities.FormatDouble(dom.BBox.Y.Min)} {Utilities.FormatDouble(dom.BBox.Z.Min)});
          max ({Utilities.FormatDouble(dom.BBox.X.Max)} {Utilities.FormatDouble(dom.BBox.Y.Max)} {Utilities.FormatDouble(dom.BBox.Z.Max)});
}}";

        /// <summary>
        /// Builds the geometry section.
        /// </summary>
        private static void AppendGeometrySection(StringBuilder sb, OFBaseDomain dom, string refinementGeometry)
        {
            sb.AppendLine(@"geometry
    {
        building.stl
        {
            type triSurfaceMesh;
            name building;
        }

        ground.stl
        {
            type triSurfaceMesh;
            name ground;
        }");

            if (!dom.HasTerrain)
            {
                sb.Append(@"
        ground_perim.stl
        {
            type triSurfaceMesh;
            name ground_perim;
        }");
            }

            sb.Append($@"
        {refinementGeometry}
    }}
");
        }

        /// <summary>
        /// Appends ground_perim section if no terrain.
        /// </summary>
        private static void AppendGroundPerimIfNeeded(StringBuilder sb, OFBaseDomain dom, OFMeshSettings settings, string sectionType)
        {
            if (!dom.HasTerrain)
            {
                if (sectionType == "surface")
                {
                    sb.Append($@"ground_perim
            {{
                level ({settings.accGround} {settings.accGround});
                patchInfo
                {{
                    type wall;
                }}
            }}");
                }
                else if (sectionType == "layer")
                {
                    sb.Append($@"ground_perim
            {{
                nSurfaceLayers {settings.nLayers};
            }}");
                }
            }
        }

        #endregion SnappyHexMesh Helpers

        public static string BlockMeshDict(OFBoxDomain DOM)
        {
            var corners = DOM.SBox.GetCorners();

            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.3.0                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.com                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
        version 2.0;
        format ascii;
        class dictionary;
        location system;
        object blockMeshDict;
}
convertToMeters 1;

vertices
(
(" + Utilities.FormatPV(corners[0]) + @")
(" + Utilities.FormatPV(corners[1]) + @")
(" + Utilities.FormatPV(corners[2]) + @")
(" + Utilities.FormatPV(corners[3]) + @")
(" + Utilities.FormatPV(corners[4]) + @")
(" + Utilities.FormatPV(corners[5]) + @")
(" + Utilities.FormatPV(corners[6]) + @")
(" + Utilities.FormatPV(corners[7]) + @")
);
blocks
(
        hex (0 1 2 3 4 5 6 7) (" + DOM.CellsAlongWidth + " " + DOM.CellsAlongLength + " " + DOM.CellsAlongHeight + @") simpleGrading (1 1 1)
);
edges
(
);
boundary
(
        inlet
{
        type patch;
        faces
        (
                (0 1 4 5)
        );
}
        outlet
{
        type patch;
        faces
        (
                (2 3 6 7)
        );
}
        ground
{
        type wall;
        faces
        (
                (0 1 2 3)
        );
}
        frontAndBack
{
        type patch;
        faces
        (
                        (1 2 5 6)
                        (0 3 4 7)
                        (4 5 6 7)
        );
}
);
        ";
        }

        public static string SnappyHexMeshDict(OFMeshSettings MeshSettings, OFBaseDomain dom)
        {
                string refinementGeometry = "";

                string Cylinder = @"refinementCylinder{
type searchableCylinder;
point1 (" + Utilities.FormatPV(dom.RefinementCylinder.Center).Replace(',', ' ') + @");
point2 (" + Utilities.FormatPV(dom.RefinementCylinder.Center + Vector3d.ZAxis * dom.RefinementCylinder.Height2).ToString().Replace(',', ' ') + @");
radius " + Utilities.FormatDouble(dom.RefinementCylinder.CircleAt(0.5).Radius) + @";
}";

                string Box = @"refinementBox{
          type searchableBox;
          min (" + Utilities.FormatDouble(dom.BBox.X.Min) + " " + Utilities.FormatDouble(dom.BBox.Y.Min) + " " + Utilities.FormatDouble(dom.BBox.Z.Min) + @");
          max (" + Utilities.FormatDouble(dom.BBox.X.Max) + " " + Utilities.FormatDouble(dom.BBox.Y.Max) + " " + Utilities.FormatDouble(dom.BBox.Z.Max) + @");
}";
                refinementGeometry = Box;

                bool useGpt53CodexPreset = MeshSettings.preset == MeshPreset.GPT53Codex;
                bool enableSnap = useGpt53CodexPreset
                    || MeshSettings.snappySetting == SnappySnapSettings.BlocksSnapping
                    || MeshSettings.snappySetting == SnappySnapSettings.BlocksSnappingLayers;
                bool enableLayers = !useGpt53CodexPreset
                    && MeshSettings.snappySetting == SnappySnapSettings.BlocksSnappingLayers;
                bool includeFeatureExtraction = useGpt53CodexPreset
                    || MeshSettings.snappySetting != SnappySnapSettings.Blocks;

                int nCellsBetweenLevels = useGpt53CodexPreset
                    ? Math.Max(5, MeshSettings.nCellsBetweenLevels)
                    : MeshSettings.nCellsBetweenLevels;

                int snapSmoothPatch = useGpt53CodexPreset ? 5 : 3;
                string snapTolerance = useGpt53CodexPreset ? "2.0" : "4.0";
                int snapSolveIter = useGpt53CodexPreset ? 50 : 30;
                int snapRelaxIter = useGpt53CodexPreset ? 8 : 5;
                int snapFeatureIter = useGpt53CodexPreset ? 15 : 10;

                string maxConcave = useGpt53CodexPreset ? "70" : "80";
                string minFaceWeight = useGpt53CodexPreset ? "0.08" : "0.05";
                string minVolRatio = useGpt53CodexPreset ? "0.02" : "0.01";

                StringBuilder sb = new StringBuilder();
                sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.3.0                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.com                      |
|   \\  /    A nd           | Web:      www.OpenFOAM.com                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version 2.0;
    format ascii;
    class dictionary;
    location system;
    object snappyHexMeshDict;
}

    castellatedMesh true;");
                sb.AppendLine("snap "); if (enableSnap) { sb.Append("true;"); } else { sb.Append("false;"); }
                sb.AppendLine("addLayers "); if (enableLayers) { sb.Append("true;"); } else { sb.Append("false;"); }
                sb.AppendLine(@"geometry
    {
        building.stl
        {
            type triSurfaceMesh;
            name building;
        }

        ground.stl
        {
            type triSurfaceMesh;
            name ground;
        }");

                if (!dom.HasTerrain)
                {
                    sb.Append(@"
        ground_perim.stl
        {
            type triSurfaceMesh;
            name ground_perim;
        }");
                }

                //if (dom.terrainMesh.Faces.Count == 0) { sb.Append(ground_perim); }
                sb.Append(@"
        " + refinementGeometry + @"
    }

    castellatedMeshControls
    {
        features
        (");
                if (includeFeatureExtraction)
                {
                    sb.Append(@"
            {file ""building.eMesh""; levels ((0.3 " + (MeshSettings.accFeatures) + @")) ;}
            {file ""ground.eMesh""; levels ((0.3 " + (MeshSettings.accFeatures) + @")) ;}");
                }

                sb.Append(@"
        );
        refinementSurfaces
        {
            building
            {
                level (" + (MeshSettings.accBuildings) + @" " + MeshSettings.accBuildingsMax + @");
                patchInfo
                {
                    type wall;
                }
            }

            ground
            {
                level (" + (MeshSettings.accGround) + @" " + (MeshSettings.accGround) + @");
                patchInfo
                {
                    type wall;
                }
            }");
                if (!dom.HasTerrain)
                {
                    sb.Append(@"ground_perim
            {
                level (" + (MeshSettings.accGround) + @" " + (MeshSettings.accGround) + @");
                patchInfo
                {
                    type wall;
                }
            }");
                }
                sb.Append(@"}
refinementRegions
        {
refinementBox {mode inside; levels ((" + MeshSettings.accBoxRefinement + @" " + MeshSettings.accBoxRefinement + @"));}
        }

        locationInMesh ( " + Utilities.FormatPV(dom.LocationInMesh) + @" );

    maxLocalCells       50000000;
    maxGlobalCells      60000000;
    minRefinementCells  50;
    maxLoadUnbalance    1;
    nCellsBetweenLevels " + nCellsBetweenLevels + @";
    resolveFeatureAngle 60;
    allowFreeStandingZoneFaces false;
    }

snapControls
{
    nSmoothPatch    " + snapSmoothPatch + @";
    tolerance       " + snapTolerance + @";
    nSolveIter      " + snapSolveIter + @";
    nRelaxIter      " + snapRelaxIter + @";

    nFeatureSnapIter " + snapFeatureIter + @";

    explicitFeatureSnap    true;
    multiRegionFeatureSnap false;
    implicitFeatureSnap    false;
}

    // Settings for the layer addition.
    addLayersControls
    {
      layers
        {
            building
            {
                nSurfaceLayers " + MeshSettings.nLayers + @";
            }
            ground
            {
                nSurfaceLayers " + MeshSettings.nLayers + @";
            }
");
                if (!dom.HasTerrain)
                {
                    sb.Append(@"ground_perim
            {
                nSurfaceLayers " + MeshSettings.nLayers + @";
            }");
                }
                sb.Append(@"
        }

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
    }

  // Generic mesh quality settings. At any undoable phase these determine where to undo.
  meshQualityControls
{
maxNonOrtho 65;

maxBoundarySkewness 20;

maxInternalSkewness 4;

maxConcave " + maxConcave + @";

minFlatness 0.5;

// Minimum cell pyramid volume; case dependent
minVol 1e-13;

// 1e-15 (small positive) to enable tracking
// -1e+30 (large negative) for best layer insertion
minTetQuality 1.00000E-015;

// if >0 : preserve single cells with all points on the surface if the
// resulting volume after snapping (by approximation) is larger than
// minVolCollapseRatio times old volume (i.e. not collapsed to flat cell).
//  If <0 : delete always.
//minVolCollapseRatio 0.5;

minArea          -1;

minTwist          0.02;

minDeterminant    0.001;

minFaceWeight     " + minFaceWeight + @";

minVolRatio       " + minVolRatio + @";

minTriangleTwist -1;

nSmoothScale   4;

errorReduction 0.75;

relaxed
{
    maxNonOrtho   75;
}
}

  // Write flags
  writeFlags
  (
      scalarLevels
      layerSets
      layerFields     // write volScalarField for layer coverage
  );

debug 0;
mergeTolerance 1E-6;

");
                return sb.ToString();
        }

    }
}
