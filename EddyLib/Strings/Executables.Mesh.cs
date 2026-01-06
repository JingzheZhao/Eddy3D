using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace EddyLib.Strings
{
    public partial class OFExecDicts
    {
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
            if (MeshSettings.miscSettings == SnappyMiscSettings.Default)
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

    castellatedMesh on;");
                sb.AppendLine("");
                sb.AppendLine("snap "); if (MeshSettings.snappySetting == SnappySnapSettings.BlocksSnapping || MeshSettings.snappySetting == SnappySnapSettings.BlocksSnappingLayers) { sb.Append("on;"); } else { sb.Append("off;"); }
                sb.AppendLine("addLayers "); if (MeshSettings.snappySetting == SnappySnapSettings.BlocksSnappingLayers) { sb.Append("on;"); } else { sb.Append("off;"); }
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
                if (MeshSettings.snappySetting != SnappySnapSettings.Blocks)
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

    maxLocalCells       4000000;
    maxGlobalCells      100000000;
    minRefinementCells  1;
    maxLoadUnbalance    0.20;
    nCellsBetweenLevels " + MeshSettings.nCellsBetweenLevels + @";
    resolveFeatureAngle 30;
    allowFreeStandingZoneFaces false;
    }

snapControls
{
    nSmoothPatch    5;
    tolerance       2.0;
    nSolveIter      150;
    nRelaxIter      8;

    nFeatureSnapIter 10;

    explicitFeatureSnap    false;
    multiRegionFeatureSnap false;
    implicitFeatureSnap    true;
}

    // Settings for the layer addition.
    addLayersControls
    {
        //// Are the thickness parameters below relative to the undistorted
        //// size of the refined cell outside layer (true) or absolute sizes (false).
        relativeSizes true;

        // Per final patch (so not geometry!) the layer information
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

// nSmoothDisplacement 0; detectExtrusionIsland false;

        //// Expansion factor for layer mesh
        expansionRatio 1.0;

        //// Wanted thickness of final added cell layer. If multiple layers
        //// is the thickness of the layer furthest away from the wall.
        //// Relative to undistorted size of cell outside layer.
        //// See relativeSizes parameter.
        finalLayerThickness 0.3;

        //// Minimum thickness of cell layer. If for any reason layer
        //// cannot be above minThickness do not add layer.
        //// Relative to undistorted size of cell outside layer.
        //// See relativeSizes parameter.
        minThickness 0.1;

        //// If points get not extruded do nGrow layers of connected faces that are
        //// also not grown. This helps convergence of the layer addition process
        //// close to features.
        //// Note: changed(corrected) w.r.t 17x! (didn't do anything in 17x)
        nGrow 0;

        // Advanced settings

    // When not to extrude surface. 0 is flat surface, 90 is when two faces are perpendicular
    featureAngle 180;

    // At non-patched sides allow mesh to slip if extrusion direction makes angle larger than slipFeatureAngle.
    slipFeatureAngle 75;

        //// Maximum number of snapping relaxation iterations. Should stop
        //// before upon reaching a correct mesh.
        nRelaxIter 8;

        //// Number of smoothing iterations of surface normals
        nSmoothSurfaceNormals 2;

        //// Number of smoothing iterations of interior mesh movement direction
        nSmoothNormals 5;

        //// Smooth layer thickness over surface patches
        nSmoothThickness 10;

        //// Stop layer growth on highly warped cells
        maxFaceThicknessRatio 0.5;

        //// Reduce layer growth where ratio thickness to medial
        //// distance is large
        maxThicknessToMedialRatio 0.3;

        //// Angle used to pick up medial axis points
        //// Note: changed(corrected) w.r.t 16x! 90 degrees corresponds to 130 in 16x.
        minMedianAxisAngle 90;

        //// Create buffer region for new layer terminations
        nBufferCellsNoExtrude 0;

        //// Overall max number of layer addition iterations. The mesher will exit
        //// if it reaches this number of iterations; possibly with an illegal
        //// mesh.
        nLayerIter 50;

        ////max number of iterations after which the controls in the relaxed sub dictionary of meshQuality are used (typically 20).
        nRelaxedIter 20;
    }

  // Generic mesh quality settings. At any undoable phase these determine where to undo.
  meshQualityControls
{
maxNonOrtho 65;

maxBoundarySkewness 20;

maxInternalSkewness 4;

maxConcave 40;

// Minimum cell pyramid volume; case dependent
minVol 1e-20;

// 1e-15 (small positive) to enable tracking
// -1e+30 (large negative) for best layer insertion
minTetQuality -1e+30;

// if >0 : preserve single cells with all points on the surface if the
// resulting volume after snapping (by approximation) is larger than
// minVolCollapseRatio times old volume (i.e. not collapsed to flat cell).
//  If <0 : delete always.
//minVolCollapseRatio 0.5;

minArea          -1;

minTwist          0.01;

minDeterminant    0.001;

minFaceWeight     0.02;

minVolRatio       0.01;

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

//autoBlockMesh true;
");
                return sb.ToString();
            }
            else
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
                sb.AppendLine("snap "); if (MeshSettings.snappySetting == SnappySnapSettings.BlocksSnapping || MeshSettings.snappySetting == SnappySnapSettings.BlocksSnappingLayers) { sb.Append("true;"); } else { sb.Append("false;"); }
                sb.AppendLine("addLayers "); if (MeshSettings.snappySetting == SnappySnapSettings.BlocksSnappingLayers) { sb.Append("true;"); } else { sb.Append("false;"); }
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
                if (MeshSettings.snappySetting != SnappySnapSettings.Blocks)
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
    nCellsBetweenLevels " + MeshSettings.nCellsBetweenLevels + @";
    resolveFeatureAngle 60;
    allowFreeStandingZoneFaces false;
    }

snapControls
{
    nSmoothPatch    3;
    tolerance       4.0;
    nSolveIter      30;
    nRelaxIter      5;

    nFeatureSnapIter 10;

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

maxConcave 80;

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

minFaceWeight     0.05;

minVolRatio       0.01;

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
}
