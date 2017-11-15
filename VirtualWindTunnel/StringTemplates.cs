using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;


namespace WindTunnel
{
    public class StringTemplates
    {
        public static string blockMeshDict(OFDomainBuilder DOM)
        {
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
backgroundMesh
{
        xMin " + DOM.newBoxDomain.BoundingBox.Min.X + @";
        xMax " + DOM.newBoxDomain.BoundingBox.Max.X + @";
        yMin " + DOM.newBoxDomain.BoundingBox.Min.Y + @";
        yMax " + DOM.newBoxDomain.BoundingBox.Max.Y + @";
        zMin " + DOM.newBoxDomain.BoundingBox.Min.Z + @";
        zMax " + DOM.newBoxDomain.BoundingBox.Max.Z + @";
        xCells " + DOM.xCells + @";
        yCells " + DOM.yCells + @";
        zCells " + DOM.zCells + @";
}
vertices
(
                ($:backgroundMesh.xMin $:backgroundMesh.yMin $:backgroundMesh.zMin)
                ($:backgroundMesh.xMax $:backgroundMesh.yMin $:backgroundMesh.zMin)
                ($:backgroundMesh.xMax $:backgroundMesh.yMax $:backgroundMesh.zMin)
                ($:backgroundMesh.xMin $:backgroundMesh.yMax $:backgroundMesh.zMin)
                ($:backgroundMesh.xMin $:backgroundMesh.yMin $:backgroundMesh.zMax)
                ($:backgroundMesh.xMax $:backgroundMesh.yMin $:backgroundMesh.zMax)
                ($:backgroundMesh.xMax $:backgroundMesh.yMax $:backgroundMesh.zMax)
                ($:backgroundMesh.xMin $:backgroundMesh.yMax $:backgroundMesh.zMax)
);
blocks
(
        hex (0 1 2 3 4 5 6 7)
        (
                        $:backgroundMesh.xCells
                        $:backgroundMesh.yCells
                        $:backgroundMesh.zCells
        )
        simpleGrading (1 1 1)
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
        type symmetry;
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

        public static string snappyHexMeshDict(int acc, Point3d locationInMesh)
        {
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
    object snappyHexMeshDict;
}

    castellatedMesh true;
    snap true;
    addLayers true;
    geometry
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
        }
	
    }

    castellatedMeshControls
    {
        features
        (
            {file ""building.eMesh""; level " + acc + @";}
        );
        refinementSurfaces
        {
            building
            {
                level (" + acc + " " + acc + @");
                patchInfo
                {
                    type wall;
                }
            }

            ground
            {
                level (" + acc + " " + acc + @");
                patchInfo
                {
                    type wall;
                }
            }
        }

        refinementRegions
        {

        }

        locationInMesh ( " + locationInMesh.X + " " + locationInMesh.Y + " "+ locationInMesh.Z + @" );
        maxLocalCells 5000000;
        maxGlobalCells 15000000;
        minRefinementCells 5;
        nCellsBetweenLevels 5;
        resolveFeatureAngle 30;
        allowFreeStandingZoneFaces true;
        planarAngle 30;
        maxLoadUnbalance 0.10;
    }

    snapControls
    {
        nSolveIter 300;
        nSmoothPatch 5;
        tolerance 4.0;
        nRelaxIter 8;
        nFeatureSnapIter 10;
        implicitFeatureSnap false;
        explicitFeatureSnap true;
        multiRegionFeatureSnap false;
    }

    // Settings for the layer addition.
    addLayersControls
    {
        // Are the thickness parameters below relative to the undistorted
        // size of the refined cell outside layer (true) or absolute sizes (false).
        relativeSizes true;

        // Per final patch (so not geometry!) the layer information
        layers
        {
            building
            {
                nSurfaceLayers 3;
            }
            ground
            {
                nSurfaceLayers 3;
            }
        }

        // Expansion factor for layer mesh
        expansionRatio 1.2;

        // Wanted thickness of final added cell layer. If multiple layers
        // is the thickness of the layer furthest away from the wall.
        // Relative to undistorted size of cell outside layer.
        // See relativeSizes parameter.
        finalLayerThickness 0.7;

        // Minimum thickness of cell layer. If for any reason layer
        // cannot be above minThickness do not add layer.
        // Relative to undistorted size of cell outside layer.
        // See relativeSizes parameter.
        minThickness 0.1;

        // If points get not extruded do nGrow layers of connected faces that are
        // also not grown. This helps convergence of the layer addition process
        // close to features.
        // Note: changed(corrected) w.r.t 17x! (didn't do anything in 17x)
        nGrow 0;

        // Advanced settings

        // When not to extrude surface. 0 is flat surface, 90 is when two faces
        // are perpendicular
        featureAngle 180;

        // Maximum number of snapping relaxation iterations. Should stop
        // before upon reaching a correct mesh.
        nRelaxIter 5;

        // Number of smoothing iterations of surface normals
        nSmoothSurfaceNormals 1;

        // Number of smoothing iterations of interior mesh movement direction
        nSmoothNormals 3;

        // Smooth layer thickness over surface patches
        nSmoothThickness 10;

        // Stop layer growth on highly warped cells
        maxFaceThicknessRatio 0.5;

        // Reduce layer growth where ratio thickness to medial
        // distance is large
        maxThicknessToMedialRatio 0.3;

        // Angle used to pick up medial axis points
        // Note: changed(corrected) w.r.t 16x! 90 degrees corresponds to 130 in 16x.
        minMedianAxisAngle 90;

        // Create buffer region for new layer terminations
        nBufferCellsNoExtrude 0;


        // Overall max number of layer addition iterations. The mesher will exit
        // if it reaches this number of iterations; possibly with an illegal
        // mesh.
        nLayerIter 50;
    }

  // Generic mesh quality settings. At any undoable phase these determine
  // where to undo.
  meshQualityControls
  {
      #include ""meshQualityDict""


      // Advanced

      //- Number of error distribution iterations
      nSmoothScale 4;
      //- Amount to scale back displacement at error points
      errorReduction 0.75;
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
";
        }
        public static string controlDict(int iter, int keepTimeSteps, int writeInterval)
        {
            //int iter;
            return @"
/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
        version     2.0;
        format ascii;
        class dictionary;
        object controlDict;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
libs
(
        ""libOpenFOAM.so""
        ""libsimpleSwakFunctionObjects.so""
        ""libswakFunctionObjects.so""
        ""libgroovyBC.so""
        ""libutilityFunctionObjects.so""
        ""libsolverFunctionObjects.so""
);
            application simpleFoam;
            startFrom latestTime;
            startTime       1;
            stopAt endTime;
            endTime         " + iter + @";
            deltaT          1;
            writeControl timeStep;
            writeInterval   " + writeInterval + @";
            purgeWrite      " + keepTimeSteps + @";
            writeFormat binary;
            writePrecision  6;
            writeCompression uncompressed;
            timeFormat general;
            timePrecision   6;
            runTimeModifiable true;
            functions
{
}
            ";
        }
    }
}
