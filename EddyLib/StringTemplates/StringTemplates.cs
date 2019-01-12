using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Text;


namespace EddyLib
{
    public class StringTemplates
    {
        public static string BlockMeshDict(OFBoxDomain DOM)
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

vertices
(
(" + DOM.newBoxDomain.GetCorners()[0].X + " " + DOM.newBoxDomain.GetCorners()[0].Y + " " + DOM.newBoxDomain.GetCorners()[0].Z + @")
(" + DOM.newBoxDomain.GetCorners()[1].X + " " + DOM.newBoxDomain.GetCorners()[1].Y + " " + DOM.newBoxDomain.GetCorners()[1].Z + @")
(" + DOM.newBoxDomain.GetCorners()[2].X + " " + DOM.newBoxDomain.GetCorners()[2].Y + " " + DOM.newBoxDomain.GetCorners()[2].Z + @")
(" + DOM.newBoxDomain.GetCorners()[3].X + " " + DOM.newBoxDomain.GetCorners()[3].Y + " " + DOM.newBoxDomain.GetCorners()[3].Z + @")
(" + DOM.newBoxDomain.GetCorners()[4].X + " " + DOM.newBoxDomain.GetCorners()[4].Y + " " + DOM.newBoxDomain.GetCorners()[4].Z + @")
(" + DOM.newBoxDomain.GetCorners()[5].X + " " + DOM.newBoxDomain.GetCorners()[5].Y + " " + DOM.newBoxDomain.GetCorners()[5].Z + @")
(" + DOM.newBoxDomain.GetCorners()[6].X + " " + DOM.newBoxDomain.GetCorners()[6].Y + " " + DOM.newBoxDomain.GetCorners()[6].Z + @")
(" + DOM.newBoxDomain.GetCorners()[7].X + " " + DOM.newBoxDomain.GetCorners()[7].Y + " " + DOM.newBoxDomain.GetCorners()[7].Z + @")
);
blocks
(
        hex (0 1 2 3 4 5 6 7) (" + DOM.xCells + " " + DOM.yCells + " " + DOM.zCells + @") simpleGrading (1 1 1)
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

        public static string SnappyHexMeshDict(OFBaseDomain dom)
        {
            string refinementGeometry = "";
            string ground_perim = @"ground_perim.stl
            {
                type triSurfaceMesh;
                name ground_perim;
            }
            ";
            string Cylinder = @"refinementCylinder{
type searchableCylinder; 
point1 (" + dom.refinementCylinder.Center.ToString().Replace(',', ' ') + @");
point2 (" + (dom.refinementCylinder.Center + Vector3d.ZAxis * dom.refinementCylinder.Height2).ToString().Replace(',', ' ') + @");
radius " + dom.refinementCylinder.CircleAt(0.5).Radius + @";
}";

            string Box = @"refinementBox{
          type searchableBox;      
          min (" + dom.BBox.Min.X + " " + +dom.BBox.Min.Y + " " + dom.BBox.Min.Z + @");  
          max (" + dom.BBox.Max.X + " " + dom.BBox.Max.Y + " " + dom.BBox.Max.Z + @");  
}";
            refinementGeometry = Box;
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
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

    castellatedMesh true;");
            sb.Append("snap "); if (dom.meshingMode == 1 || dom.meshingMode == 2) { sb.AppendLine("true;"); } else { sb.AppendLine("false;"); }
            sb.Append("addLayers "); if (dom.meshingMode == 2) { sb.AppendLine("true;"); } else { sb.AppendLine("false;"); }
            sb.Append(@"geometry
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
            //Check for both Box and Cyl if there is a terrain. Unfortunately ground are called differently. TODO!!!
            if ((dom is OFBoxDomain && dom.DomainMeshGroundPerim != null) || (dom is OFCylDomain && dom.terrainMesh.Faces.Count == 0))
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
        (
            {file ""building.eMesh""; level " + (dom.accFeatures) + @" ;}
            {file ""ground.eMesh""; level " + (dom.accFeatures) + @" ;}
        );
        refinementSurfaces
        {
            building
            {
                level (" + (dom.accBuildings - 1) + @" " + dom.accBuildings + @");
                patchInfo
                {
                    type wall;
                }
            }

            ground
            {
                level (" + (dom.accGround) + @" " + (dom.accGround) + @");
                patchInfo
                {
                    type wall;
                }
            }
            ground_perim
            {
                level (" + (dom.accGround - 1) + @" " + (dom.accGround) + @");
                patchInfo
                {
                    type wall;
                }
            }
        }

        refinementRegions
        {

refinementBox {mode inside; levels ((" + dom.accRefinement + @" " + dom.accRefinement + @"));}
//refinementCylinder {mode inside; levels ((" + dom.accRefinement + " " + dom.accRefinement + @"));}


        }

        locationInMesh ( " + dom.locationInMesh.X + " " + dom.locationInMesh.Y + " " + dom.locationInMesh.Z + @" );
        //maxLocalCells 15000000;
        //maxGlobalCells 50000000;
        //minRefinementCells 5;
        //nCellsBetweenLevels 5;
        //resolveFeatureAngle 30;
        //allowFreeStandingZoneFaces true;
        //planarAngle 30;
        //maxLoadUnbalance 0.10;

maxLocalCells       100000;
    maxGlobalCells      100000000;
    minRefinementCells  10;
    maxLoadUnbalance    0.10;
    nCellsBetweenLevels 3;
    resolveFeatureAngle 30;
    allowFreeStandingZoneFaces true;
    }

    

//snapControls
//    {
//        nSolveIter 300;
//        nSmoothPatch 5;
//        tolerance 4.0;
//        nRelaxIter 8;
//        nFeatureSnapIter 10;
//        implicitFeatureSnap false;
//        explicitFeatureSnap true;
//        multiRegionFeatureSnap false;
//    }
snapControls
{
    nSmoothPatch    3;
    tolerance       2.0;
    nSolveIter      100;
    nRelaxIter      5;

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
        //relativeSizes true;

        // Per final patch (so not geometry!) the layer information
        layers
        {
            building
            {
                nSurfaceLayers " + dom.nLayers + @";
            }
            ground
            {
                nSurfaceLayers " + dom.nLayers + @";
            }
            ground_perim
            {
                nSurfaceLayers " + dom.nLayers + @";
            }
        }

   featureAngle              100;
    slipFeatureAngle          30;

    nLayerIter                50;
    nRelaxedIter              20;
    nRelaxIter                5;

    nGrow                     0;

    nSmoothSurfaceNormals     1;
    nSmoothNormals            3;
    nSmoothThickness          10;
    maxFaceThicknessRatio     0.5;
    maxThicknessToMedialRatio 0.3;

    minMedialAxisAngle        90;
    nMedialAxisIter           10;

    nBufferCellsNoExtrude     0;
    additionalReporting       false;

relativeSizes       true;
    expansionRatio      1.2;
    finalLayerThickness 0.5;
    minThickness        1e-3;

//    nSmoothDisplacement       0;
//    detectExtrusionIsland     false;

        //// Expansion factor for layer mesh
        //expansionRatio 1.2;

        //// Wanted thickness of final added cell layer. If multiple layers
        //// is the thickness of the layer furthest away from the wall.
        //// Relative to undistorted size of cell outside layer.
        //// See relativeSizes parameter.
        //finalLayerThickness 0.7;

        //// Minimum thickness of cell layer. If for any reason layer
        //// cannot be above minThickness do not add layer.
        //// Relative to undistorted size of cell outside layer.
        //// See relativeSizes parameter.
        //minThickness 0.1;

        //// If points get not extruded do nGrow layers of connected faces that are
        //// also not grown. This helps convergence of the layer addition process
        //// close to features.
        //// Note: changed(corrected) w.r.t 17x! (didn't do anything in 17x)
        //nGrow 0;

        //// Advanced settings

        //// When not to extrude surface. 0 is flat surface, 90 is when two faces
        //// are perpendicular
        //featureAngle 180;

        //// Maximum number of snapping relaxation iterations. Should stop
        //// before upon reaching a correct mesh.
        //nRelaxIter 5;

        //// Number of smoothing iterations of surface normals
        //nSmoothSurfaceNormals 1;

        //// Number of smoothing iterations of interior mesh movement direction
        //nSmoothNormals 3;

        //// Smooth layer thickness over surface patches
        //nSmoothThickness 10;

        //// Stop layer growth on highly warped cells
        //maxFaceThicknessRatio 0.5;

        //// Reduce layer growth where ratio thickness to medial
        //// distance is large
        //maxThicknessToMedialRatio 0.3;

        //// Angle used to pick up medial axis points
        //// Note: changed(corrected) w.r.t 16x! 90 degrees corresponds to 130 in 16x.
        //minMedianAxisAngle 90;

        //// Create buffer region for new layer terminations
        //nBufferCellsNoExtrude 0;


        //// Overall max number of layer addition iterations. The mesher will exit
        //// if it reaches this number of iterations; possibly with an illegal
        //// mesh.
        //nLayerIter 50;

        ////max number of iterations after which the controls in the relaxed sub dictionary of meshQuality are used (typically 20).
        //nRelaxedIter 20;
    }

  // Generic mesh quality settings. At any undoable phase these determine
  // where to undo.
  meshQualityControls
{
   
maxNonOrtho 65;

maxBoundarySkewness 20;

maxInternalSkewness 4;

maxConcave 80;

// Minimum cell pyramid volume; case dependent
minVol 1e-13;

//  1e-15 (small positive) to enable tracking
// -1e+30 (large negative) for best layer insertion
minTetQuality 1e-15;

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




////- Maximum non-orthogonality allowed. Set to 180 to disable.
//    maxNonOrtho 65;

//    //- Max skewness allowed. Set to <0 to disable.
//    maxBoundarySkewness 20;
//    maxInternalSkewness 4;

//    //- Max concaveness allowed. Is angle (in degrees) below which concavity
//    //  is allowed. 0 is straight face, <0 would be convex face.
//    //  Set to 180 to disable.
//    maxConcave 80;

//    //- Minimum pyramid volume. Is absolute volume of cell pyramid.
//    //  Set to a sensible fraction of the smallest cell volume expected.
//    //  Set to very negative number (e.g. -1E30) to disable.
//    minVol 1e-16;

//    //- Minimum quality of the tet formed by the face-centre
//    //  and variable base point minimum decomposition triangles and
//    //  the cell centre. This has to be a positive number for tracking
//    //  to work. Set to very negative number (e.g. -1E30) to
//    //  disable.
//    //     <0 = inside out tet,
//    //      0 = flat tet
//    //      1 = regular tet
//    minTetQuality -1e+30; // 1e-30;

//    //- Minimum face area. Set to <0 to disable.
//    minArea 1e-13;

//    //- Minimum face twist. Set to <-1 to disable. dot product of face normal
//    //  and face centre triangles normal
//    minTwist 0.02;

//    //- Minimum normalised cell determinant
//    //  1 = hex, <= 0 = folded or flattened illegal cell
//    minDeterminant 0.001;

//    //- minFaceWeight (0 -> 0.5)
//    minFaceWeight 0.02;

//    //- minVolRatio (0 -> 1)
//    minVolRatio 0.01;

//    //must be >0 for Fluent compatibility
//    minTriangleTwist -1;


//    // Advanced

//    //- Number of error distribution iterations
//    nSmoothScale 4;
//    //- Amount to scale back displacement at error points
//    errorReduction 0.75;

//    // Optional : some meshing phases allow usage of relaxed rules.
//    // See e.g. addLayersControls::nRelaxedIter.
//    relaxed
//    {
//        //- Maximum non-orthogonality allowed. Set to 180 to disable.
//        maxNonOrtho 75;
//    }

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
        public static string ControlDict(OFBaseDomain DOM, List<Mesh> topologies, int numberOfTopologies)
        {
            var sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
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
            endTime         " + DOM.iter + @";
            deltaT          1;
            writeControl timeStep;
            writeInterval   " + DOM.writeInterval + @";
            purgeWrite      " + DOM.keepTimeSteps + @";
            writeFormat binary;
            writePrecision  6;
            writeCompression uncompressed;
            timeFormat general;
            timePrecision   6;
            runTimeModifiable true;
            functions
{
#includeFunc residuals
");
            //if (topologies != null) {
            sb.Append(StringTemplates.FunctionObjCP(DOM, topologies, numberOfTopologies).ToString());
            //}
            //else { sb.Append(@"};"); }

            return sb.ToString();
        }
        public static string FunctionObjCP(OFBaseDomain DOM, List<Mesh> evaluationTopology, int d)
        {
            var sb = new StringBuilder();
            sb.Append(@"cp2
{
                    type pressure;
                    libs (""libfieldFunctionObjects.so"");
                    enabled yes;
                    writeControl timeStep;
                    writeInterval " + DOM.writeInterval + @";
                    UInf (" + DOM.BCInflow.Uinf[d].X + " " + DOM.BCInflow.Uinf[d].Y + " " + DOM.BCInflow.Uinf[d].Z + @");     // the undistrubed velocity at building height
                    pInf " + DOM.BCInflow.pinf + @";        // the dynamic undisturbed pressure at building height
                    pRef " + DOM.BCInflow.pref + @";        // the dynamic pressure at reference height (usually 10 m)
                    rhoInf              1.2;
                    calcTotal yes;
                    calcCoeff yes;
                }");



            if (evaluationTopology != null)
            {
                for (int i = 0; i < evaluationTopology.Count; i++)
                {
                    sb.Append(@"
patch" + i + @"
{
    type                    swakExpression;
    valueType               faceSet;
    outputControlMode       timeStep;
    outputInterval          1;
    setName                 patch" + i + @";
    aliases
    {
        c_p     total(p)_coeff;
    }
    expression              ""c_p"";
    accumulations (weightedAverage)
    ;
            verbose                 true;
            autoInterpolate         true;
            warnAutoInterpolate     false;
}
");
                }
            }
            else { sb.Append(@"};"); }



            return sb.ToString();

        }
        public static string TopoSetDict(List<Mesh> evaluationTopology)
        {
            var sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      / F ield | OpenFOAM: The Open Source CFD Toolbox |
|  \\    / O peration | Version:  5 |
|   \\  / A nd | Web:      www.OpenFOAM.org |
|    \\/ M anipulation |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
            version     2.0;
            format ascii;
    class dictionary;
    location    ""system"";
    object topoSetDict;
}
        // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        actions
        (");

            for (int i = 0; i < evaluationTopology.Count; i++)
            {
                sb.Append(@"
{
            name patch" + i + @";
            type faceZoneSet;
            action new;
            source searchableSurfaceToFaceZone;
            sourceInfo
        {
                surface triSurfaceMesh;
                name patch" + i + @".stl;    
            }
        }

        {
            name surfaceSlaveCells; 
            type cellSet;
            action new;
            source faceZoneToCell;
            sourceInfo
                {
                name patch" + i + @"; 
                option slave;
            }
        }
            ");
            }
            sb.Append(@");

        // ************************************************************************* //");

            return sb.ToString();

        }


        public static string SampleProbes(List<Point3d> listOfPoints, string enumeratedProbeName, string cleanedOFField)
        {
            var sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
  | =========                 |                                                 |
  | \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
  |  \\    /   O peration     | Version:  5                                     |
  |   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
  |    \\/     M anipulation  |                                                 |
  \*---------------------------------------------------------------------------*/

" + enumeratedProbeName + @"
{

                type probes;
                libs (""libsampling.so"");
                writeControl writeTime;

                interpolationScheme cellPoint;

                setFormat csv;

                fields (" + cleanedOFField + @");

                probeLocations
                  (");
            sb.Append(Environment.NewLine);
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                sb.Append(@"(" + listOfPoints[i].X + @" " + listOfPoints[i].Y + @" " + listOfPoints[i].Z + @")");
                sb.Append(Environment.NewLine);
            }

            sb.Append(@");

        }


            // ************************************************************************* //");

            return sb.ToString();


        }
        //        public static string circularDomainM4(OFBoxDomain DOM)
        //        {
        //            return @"/*--------------------------------*- C++ -*----------------------------------*\
        //| =========                 |                                                 |
        //| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
        //|  \\    /   O peration     | Version:  2.1.0                                  |
        //|   \\  /    A nd           | Web:      http://www.OpenFOAM.com               |
        //|    \\/     M anipulation  |                                                 |
        //\*---------------------------------------------------------------------------*/

        //FoamFile
        //{
        //    version     2.0;
        //    format      ascii;
        //    class       dictionary;
        //    object      blockMeshDict;
        //}
        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //convertToMeters 1;

        //dnl changecom(//)
        //changequote([,])
        //define(LPAREN,[(])dnl
        //define(RPAREN,[)])dnl
        //dnl
        //define(calc, [esyscmd(perl -e 'printf ($1)')])dnl
        //dnl
        //define(pip180, 0.017453)
        //define(cos45, 0.70711)
        //dnl *********USER***********
        //dnl ===      POINTS      ===
        //define(zLength, " + (6 * DOM.dimZ) + @")dnl
        //define(coreWidth, " + (6 * DOM.dim) + @")dnl 
        //define(diameter, " + (16.5 * DOM.dim) + @")dnl 
        ////define(rectangleWidth, 80)dnl //50
        //define(cornerStretch, 1)dnl 
        //define(arcStretch, 1)dnl
        //dnl ===    CELL COUNT    ===
        //define(coreCount, " + Math.Round((DOM.dim / DOM.blockDimension)) + @")dnl 
        //define(rectangleCount, " + Math.Round(((16.5 * DOM.dim) / DOM.blockDimension)) + @")dnl 
        //define(zCount, " + Math.Round((DOM.dimZ / DOM.blockDimension)) + @")dnl
        //dnl ===BOUNDING RECTANGLE?===
        //define(boundRect, 1)dnl
        //dnl =========================
        //dnl *******CALCULATED********
        //define(radius, calc(0.5*diameter))dnl
        //define(halfCoreWidth, calc(0.5*coreWidth))dnl
        //define(halfRectangleWidth, calc(0.5*rectangleWidth))dnl
        //define(negHalfCoreWidth, calc(-1*halfCoreWidth))dnl
        //define(halfCoreCorner, calc(cornerStretch*halfCoreWidth*2))dnl
        //define(negHalfCoreCorner, calc(-1*halfCoreCorner))dnl
        //define(negRadius, calc(-1*radius))dnl
        //define(cornerRadius, calc(sqrt(2)*radius/2))dnl
        //define(negCornerRadius, calc(-1*cornerRadius))dnl
        //define(negHalfRectangleWidth, calc(-1*halfRectangleWidth))dnl
        //dnl =========================
        //dnl ===     ARC POINTS    ===
        //dnl define(coreArchLong, calc(0.67*halfCoreWidth))dnl
        //define(coreArchLong, calc(1.001*arcStretch*(halfCoreWidth+halfCoreCorner*0.5-halfCoreWidth*0.5)))dnl
        //define(negCoreArchLong, calc(-1*coreArchLong))dnl
        //define(coreArchShort, calc(halfCoreWidth/2))dnl
        //define(negCoreArchShort, calc(-1*coreArchShort))dnl
        //define(radiusArchLong, calc(cos(22.5*pip180)*radius))dnl
        //define(negRadiusArchLong, calc(-1*radiusArchLong))dnl
        //define(radiusArchShort, calc(sin(22.5*pip180)*radius))dnl
        //define(negRadiusArchShort, calc(-1*radiusArchShort))dnl
        //dnl =========================
        //dnl
        //define(zCount, 1)dnl

        //vertices        
        //(
        //    (  0  0  0 )          //0
        //    (  0  0  zLength )          //1
        //    (  halfCoreCorner  0  0 )          //2
        //    (  halfCoreCorner  0  zLength )          //3
        //    (  0  halfCoreCorner  0 )          //4
        //    (  0  halfCoreCorner  zLength )        //5
        //    ( negHalfCoreCorner  0  0 )               //6
        //    ( negHalfCoreCorner  0  zLength )        //7
        //    (  0  negHalfCoreCorner  0 )               //8
        //    (  0  negHalfCoreCorner  zLength )        //9
        //    (  halfCoreWidth  halfCoreWidth  0 )               //10
        //    (  halfCoreWidth  halfCoreWidth  zLength )        //11
        //    ( negHalfCoreWidth  halfCoreWidth  0 )               //12
        //    ( negHalfCoreWidth  halfCoreWidth  zLength )        //13
        //    ( negHalfCoreWidth negHalfCoreWidth  0 )               //14
        //    ( negHalfCoreWidth negHalfCoreWidth  zLength )        //15
        //    (  halfCoreWidth negHalfCoreWidth  0 )               //16
        //    (  halfCoreWidth negHalfCoreWidth  zLength )        //17
        //    (  radius  0  0 )               //18
        //    (  radius  0  zLength )        //19
        //    (  0  radius  0 )               //20
        //    (  0  radius  zLength )        //21
        //    ( negRadius  0  0 )               //22
        //    ( negRadius  0  zLength )        //23
        //    (  0  negRadius  0 )               //24
        //    (  0  negRadius  zLength )        //25
        //    (  cornerRadius  cornerRadius  0 )           //26
        //    (  cornerRadius  cornerRadius  zLength )    //27
        //    ( negCornerRadius  cornerRadius  0 )           //28
        //    ( negCornerRadius  cornerRadius  zLength )    //29
        //    ( negCornerRadius negCornerRadius  0 )           //30
        //    ( negCornerRadius negCornerRadius  zLength )    //31
        //    (  cornerRadius negCornerRadius  0 )           //32
        //    (  cornerRadius negCornerRadius  zLength )    //33

        //); 

        //blocks          
        //LPAREN
        //    hex (2 10 0 16 3 11 1 17) (coreCount coreCount zCount) simpleGrading (1 1 1)          //1
        //    hex (10 4 12 0 11 5 13 1) (coreCount coreCount zCount) simpleGrading (1 1 1)          //2
        //    hex (12 6 14 0 13 7 15 1) (coreCount coreCount zCount) simpleGrading (1 1 1)          //3
        //    hex (14 8 16 0 15 9 17 1) (coreCount coreCount zCount) simpleGrading (1 1 1)          //4
        //    hex (18 26 10 2 19 27 11 3) (coreCount rectangleCount zCount) simpleGrading (1 1 1)        //5
        //    hex (26 20 4 10 27 21 5 11) (coreCount rectangleCount zCount) simpleGrading (1 1 1)        //6
        //    hex (20 28 12 4 21 29 13 5) (coreCount rectangleCount zCount) simpleGrading (1 1 1)        //7
        //    hex (28 22 6 12 29 23 7 13) (coreCount rectangleCount zCount) simpleGrading (1 1 1)        //8
        //    hex (22 30 14 6 23 31 15 7) (coreCount rectangleCount zCount) simpleGrading (1 1 1)        //9
        //    hex (30 24 8 14 31 25 9 15) (coreCount rectangleCount zCount) simpleGrading (1 1 1)        //10
        //    hex (24 32 16 8 25 33 17 9) (coreCount rectangleCount zCount) simpleGrading (1 1 1)        //11
        //    hex (32 18 2 16 33 19 3 17) (coreCount rectangleCount zCount) simpleGrading (1 1 1)        //12


        //RPAREN;

        // edges           
        // (
        //     arc  2 10 ( coreArchLong   coreArchShort   0)
        //     arc  3 11 ( coreArchLong   coreArchShort   zLength)
        //     arc 16  2 ( coreArchLong  negCoreArchShort   0)
        //     arc 17  3 ( coreArchLong  negCoreArchShort   zLength)
        //     arc 10  4 (  coreArchShort  coreArchLong   0)
        //     arc 11  5 (  coreArchShort  coreArchLong   zLength)
        //     arc  4 12 ( negCoreArchShort  coreArchLong   0)
        //     arc  5 13 ( negCoreArchShort  coreArchLong   zLength)
        //     arc 12  6 (negCoreArchLong   coreArchShort   0)
        //     arc 13  7 (negCoreArchLong   coreArchShort   zLength)
        //     arc  6 14 (negCoreArchLong  negCoreArchShort   0)
        //     arc  7 15 (negCoreArchLong  negCoreArchShort   zLength)
        //     arc 14  8 ( negCoreArchShort negCoreArchLong   0)
        //     arc 15  9 ( negCoreArchShort negCoreArchLong   zLength)
        //     arc  8 16 (  coreArchShort negCoreArchLong   0)
        //     arc  9 17 (  coreArchShort negCoreArchLong   zLength)
        //     arc 18 26 ( radiusArchLong  radiusArchShort  0)
        //     arc 19 27 ( radiusArchLong  radiusArchShort  zLength)
        //     arc 26 20 ( radiusArchShort  radiusArchLong  0)
        //     arc 27 21 ( radiusArchShort  radiusArchLong  zLength)
        //     arc 20 28 (negRadiusArchShort  radiusArchLong  0)
        //     arc 21 29 (negRadiusArchShort  radiusArchLong  zLength)
        //     arc 28 22 (negRadiusArchLong  radiusArchShort  0)
        //     arc 29 23 (negRadiusArchLong  radiusArchShort  zLength)
        //     arc 22 30 (negRadiusArchLong -radiusArchShort  0)
        //     arc 23 31 (negRadiusArchLong -radiusArchShort  zLength)
        //     arc 30 24 (negRadiusArchShort negRadiusArchLong  0)
        //     arc 31 25 (negRadiusArchShort negRadiusArchLong  zLength)
        //     arc 24 32 ( radiusArchShort negRadiusArchLong  0)
        //     arc 25 33 ( radiusArchShort negRadiusArchLong  zLength)
        //     arc 32 18 ( radiusArchLong negRadiusArchShort  0)
        //     arc 33 19 ( radiusArchLong negRadiusArchShort  zLength)
        // );
        //boundary
        //LPAREN
        //    ground
        //    {
        //    type wall;
        //    faces
        //	LPAREN
        //    (2 10 0 16)
        //    (10 4 12 0)
        //    (12 6 14 0)
        //    (14 8 16 0)
        //    (18 26 10 2)
        //    (10 26 20 4)
        //    (4 20 28 12)
        //    (12 28 22 6)
        //    (6 22 30 14)
        //    (14 30 24 8)
        //    (8 24 32 16)
        //    (16 32 18 2)
        //    RPAREN;
        //    }
        //    top
        //    {
        //    type symmetry;
        //    faces
        //	LPAREN
        //    (3 11 1 17)
        //    (11 5 13 1)
        //    (13 7 15 1)
        //    (15 9 17 1)
        //    (3 19 27 11)
        //    (11 27 21 5)
        //    (5 21 29 13)
        //    (13 29 23 7)
        //    (7 23 31 15)
        //    (15 31 25 9)
        //    (9 25 33 17)
        //    (17 33 19 3)
        //    RPAREN;
        //    }
        //    one
        //    {
        //    type patch;
        //    faces
        //	LPAREN
        //	(18 19 27 26)
        //	RPAREN;
        //	}
        //	two
        //    {
        //    type patch;
        //    faces
        //	LPAREN
        //	(26 27 21 20)
        //	RPAREN;
        //	}

        //three
        //    {
        //    type patch;
        //    faces
        //	LPAREN
        //	(20 21 29 28)
        //	RPAREN;
        //	}
        //four
        //    {
        //    type patch;
        //    faces
        //	LPAREN
        //	(28 29 23 22)
        //	RPAREN;
        //	}
        //five
        //    {
        //    type patch;
        //    faces
        //	LPAREN
        //	(22 23 31 30)
        //	RPAREN;
        //	}
        //six
        //    {
        //    type patch;
        //    faces
        //	LPAREN
        //	(30 31 25 24)
        //	RPAREN;
        //	}
        //seven
        //    {
        //    type patch;
        //    faces
        //	LPAREN
        //	(24 25 33 32)
        //	RPAREN;
        //	}
        //eight
        //    {
        //    type patch;
        //    faces
        //	LPAREN
        //	(32 33 19 18)
        //	RPAREN;
        //	}

        // RPAREN;


        //mergePatchPairs 
        //(
        //);

        //// ************************************************************************* //
        //            ";
        //        }



        public static string MeshQualityDict()
        {
            return
               @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  3.0.1                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      meshQualityDict;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

//- Maximum non-orthogonality allowed. Set to 180 to disable.
maxNonOrtho 65;

//- Max skewness allowed. Set to <0 to disable.
maxBoundarySkewness 20;
maxInternalSkewness 4;

//- Max concaveness allowed. Is angle (in degrees) below which concavity
//  is allowed. 0 is straight face, <0 would be convex face.
//  Set to 180 to disable.
maxConcave 80;

//- Minimum pyramid volume. Is absolute volume of cell pyramid.
//  Set to a sensible fraction of the smallest cell volume expected.
//  Set to very negative number (e.g. -1E30) to disable.
minVol 1e-13;

//- Minimum quality of the tet formed by the face-centre
//  and variable base point minimum decomposition triangles and
//  the cell centre. This has to be a positive number for tracking
//  to work. Set to very negative number (e.g. -1E30) to
//  disable.
//     <0 = inside out tet,
//      0 = flat tet
//      1 = regular tet
minTetQuality 1e-15;

//- Minimum face area. Set to <0 to disable.
minArea -1;

//- Minimum face twist. Set to <-1 to disable. dot product of face normal
// and face centre triangles normal
minTwist 0.02;

//- Minimum normalised cell determinant. This is the determinant of all
//  the areas of internal faces. It is a measure of how much of the
//  outside area of the cell is to other cells. The idea is that if all
//  outside faces of the cell are 'floating' (zeroGradient) the
//  'fixedness' of the cell is determined by the area of the internal faces.
//  1 = hex, <= 0 = folded or flattened illegal cell
minDeterminant 0.001;

//- Relative position of face in relation to cell centres (0.5 for orthogonal
//  mesh) (0 -> 0.5)
minFaceWeight 0.05;

//- Volume ratio of neighbouring cells (0 -> 1)
minVolRatio 0.01;

//- Per triangle normal compared to average normal. Like face twist
//  but now per (face-centre decomposition) triangle. Must be >0 for Fluent
//  compatibility
minTriangleTwist -1;


//- If >0 : preserve cells with all points on the surface if the
//  resulting volume after snapping (by approximation) is larger than
//  minVolCollapseRatio times old volume (i.e. not collapsed to flat cell).
//  If <0 : delete always.
//minVolCollapseRatio 0.1;


// ************************************************************************* //
";
        }
        public static string FvSchemesAccurate()
        {// An accurate and stable numerical scheme
            return
        @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      fvSchemes;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

ddtSchemes
{
    default         steadyState;
}

gradSchemes
{
    default cellMDLimited Gauss linear 0.5;
}

divSchemes
{
    default         bounded Gauss upwind grad(U);
    div(phi,U)      bounded Gauss linearUpwindV grad(U);
    div(phi,k)      bounded Gauss upwind grad(U);
    //div(phi,epsilon)  bounded Gauss upwind grad(U);
    div(phi,omega)  bounded Gauss upwind grad(U);
    div((nuEff*dev2(T(grad(U))))) Gauss linear;
    div(phi,time)   bounded Gauss upwind grad(U);
}

laplacianSchemes
{
    default         Gauss linear corrected;
    laplacian(nuEff,time) Gauss linear corrected;
}

interpolationSchemes
{
    default         linear;
}

snGradSchemes
{
    default         corrected;
}

fluxRequired
{
    default         no;
    p;
}
wallDist
{
	method meshWave;
}

// ************************************************************************* //
";
        }

        public static string FvSchemesRobust1()
        {//A robust numerical scheme but diffusive
            return
        @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      fvSchemes;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

ddtSchemes
{
    default         steadyState;
}



gradSchemes
{
    default cellMDLimited Gauss linear 1.0;
}

divSchemes
{
    default          Gauss upwind;
    div(phi,U)       Gauss upwind;
    //div(phi,k)       Gauss upwind;
    //div(phi,epsilon) Gauss upwind;
    div(phi,k)       Gauss linear;
    div(phi,epsilon) Gauss linear;
    div(phi,omega)   Gauss upwind;
    div((nuEff*dev2(T(grad(U))))) Gauss linear;
    div(phi,time)   Gauss upwind;
}

laplacianSchemes
{
    default         Gauss linear corrected;
    laplacian(nuEff,time) Gauss linear corrected;
}

interpolationSchemes
{
    default         linear;
}

snGradSchemes
{
    default         corrected;
}

fluxRequired
{
    default         no;
    p;
}
wallDist
{
	method meshWave;
}

// ************************************************************************* //
";
        }
        public static string FvSchemesAccurateOscillatory()
        {// An even more accurate but oscillatory scheme
            return
        @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      fvSchemes;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

ddtSchemes
{
    default         steadyState;
}



gradSchemes
{
    default Gauss linear;
}

divSchemes
{
    default         Gauss linearUpwind grad(U);
    div(phi,U)       Gauss linear;
    //div(phi,k)       Gauss linearUpwind grad(U);
    //div(phi,epsilon) Gauss linearUpwind grad(U);
    div(phi,k)       Gauss linear;
    div(phi,epsilon) Gauss linear;
    div(phi,omega)   Gauss linearUpwind grad(U);
    div((nuEff*dev2(T(grad(U))))) Gauss linear;
    div(phi,time)   Gauss linearUpwind grad(U);
}

laplacianSchemes
{
    default         Gauss linear corrected;
    laplacian(nuEff,time) Gauss linear corrected;
}

interpolationSchemes
{
    default         linear;
}

snGradSchemes
{
    default         corrected;
}

fluxRequired
{
    default         no;
    p;
}
wallDist
{
	method meshWave;
}

// ************************************************************************* //
";
        }
        public static string FvSchemesOrtho70_80()
        {
            // An accurate numerical scheme on orthogonal (70-80) meshes
            return
        @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      fvSchemes;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

ddtSchemes
{
    default         steadyState;
}



gradSchemes
{
    default cellMDLimited leastSquares 1.0;
}

divSchemes
{
    default          Gauss linearUpwind;
    div(phi,U)       Gauss linearUpwind grad(U);
    //div(phi,k)       Gauss linearUpwind;
    //div(phi,epsilon) Gauss linearUpwind;
    div(phi,k)       Gauss linear;
    div(phi,epsilon) Gauss linear;
    div(phi,omega)   Gauss linearUpwind;
    div((nuEff*dev2(T(grad(U))))) Gauss linear;
    div(phi,time)   Gauss linearUpwind grad(U);
}

laplacianSchemes
{
    default         Gauss linear limited 0.5;
    laplacian(nuEff,time) Gauss linear limited 0.5;
}

interpolationSchemes
{
    default         linear;
}

snGradSchemes
{
    default         limited 0.5;
}

fluxRequired
{
    default         no;
    p;
}
wallDist
{
	method meshWave;
}

// ************************************************************************* //
";


        }


        public static string FvSchemesOrtho60_70()
        {
            // An accurate numerical scheme on orthogonal (60-70) meshes
            return
        @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      fvSchemes;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

ddtSchemes
{
    default         steadyState;
}



gradSchemes
{
    default cellMDLimited Gauss linear 0.5;
}

divSchemes
{
    
    div(phi,U)       Gauss linearUpwind grad(U);
    //div(phi,k)       Gauss linearUpwind;
    //div(phi,epsilon) Gauss linearUpwind;
    div(phi,k)       Gauss linear;
    div(phi,epsilon) Gauss linear;
    div(phi,omega)   Gauss linearUpwind;
    div((nuEff*dev2(T(grad(U))))) Gauss linear;
    div(phi,time)   Gauss linearUpwind grad(U);
}

laplacianSchemes
{
    default         Gauss linear limited 0.77;
    laplacian(nuEff,time) Gauss linear limited 0.77;
}

interpolationSchemes
{
    default         linear;
}

snGradSchemes
{
    default         limited 0.77;
}

fluxRequired
{
    default         no;
    p;
}
wallDist
{
	method meshWave;
}

// ************************************************************************* //
";
        }
        public static string FvSchemesOrtho40_60()
        {
            // An accurate numerical scheme on orthogonal (40-60) meshes
            return
        @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      fvSchemes;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

ddtSchemes
{
    default         steadyState;
}



gradSchemes
{
    default cellMDLimited Gauss linear 0.5;
}

divSchemes
{
    default          Gauss linearUpwind;
    div(phi,U)       Gauss linearUpwind grad(U);
    //div(phi,k)       Gauss linearUpwind;
    //div(phi,epsilon) Gauss linearUpwind;
    div(phi,k)       Gauss linear;
    div(phi,epsilon) Gauss linear;
    div(phi,omega)   Gauss linearUpwind;
    div((nuEff*dev2(T(grad(U))))) Gauss linear;
    div(phi,time)   Gauss linearUpwind grad(U);
}

laplacianSchemes
{
    default         Gauss linear limited 1.0;
    laplacian(nuEff,time) Gauss linear limited 1.0;
}

interpolationSchemes
{
    default         linear;
}

snGradSchemes
{
    default         limited 1.0;
}

fluxRequired
{
    default         no;
    p;
}
wallDist
{
	method meshWave;
}

// ************************************************************************* //
";
        }
        //        public static string fvSolution(int mode)
        //        {
        //            StringBuilder sb = new StringBuilder(); sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
        //| =========                 |                                                 |
        //| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
        //|  \\    /   O peration     | Version:  2.2.2                                 |
        //|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
        //|    \\/     M anipulation  |                                                 |
        //\*---------------------------------------------------------------------------*/
        //FoamFile
        //{
        //    version     2.0;
        //    format      ascii;
        //    class       dictionary;
        //    object      fvSolution;
        //}
        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //solvers
        //{
        //p
        //    {
        //        solver           GAMG;
        //        tolerance        1e-9;
        //        relTol           0.001;
        //        smoother         GaussSeidel;
        //        nPreSweeps       0;
        //        nPostSweeps      2;
        //        cacheAgglomeration on;
        //        agglomerator     faceAreaPair;
        //        nCellsInCoarsestLevel 10;
        //        mergeLevels      1;
        //    }

        //U
        //    {
        //        solver           smoothSolver;
        //        smoother         GaussSeidel;
        //        tolerance        1e-8;
        //        relTol           0.01;
        //        nSweeps          1;
        //    }

        //k
        //    {
        //        solver           smoothSolver;
        //        smoother         GaussSeidel;
        //        tolerance        1e-8;
        //        relTol           0.1;
        //        nSweeps          1;
        //    }

        //epsilon
        //    {
        //        solver           smoothSolver;
        //        smoother         GaussSeidel;
        //        tolerance        1e-8;
        //        relTol           0.1;
        //        nSweeps          1;
        //    }
        //omega
        //    {
        //        solver           smoothSolver;
        //        smoother         GaussSeidel;
        //        tolerance        1e-8;
        //        relTol           0.1;
        //        nSweeps          1;
        //    }
        //}

        //SIMPLE
        //{
        //    nNonOrthogonalCorrectors 3;
        //    residualControl
        //    {
        //    p       1e-4;
        //    U       1e-5;
        //    k       1e-5;
        //    epsilon 1e-5;
        //    }
        //    pRefCell    0;
        //    pRefValue    0;
        //}

        //potentialFlow
        //{
        //    nNonOrthogonalCorrectors 3;
        //}");
        //            if (mode == 0)
        //            {
        //                sb.Append(@"relaxationFactors
        //{
        //    fields
        //    {
        //        p               0.7;
        //    }
        //    equations
        //    {
        //        U               0.3;
        //        k               0.3;
        //       epsilon          0.3;
        //	   omega			0.3;
        //    }
        //}"
        //);
        //            }
        //            else { sb.Append(@"relaxationFactors
        //{
        //    fields
        //    {
        //        p               0.3;
        //    }
        //    equations
        //    {
        //        U               0.7;
        //        k               0.7;
        //       epsilon          0.7;
        //	   omega			0.7;
        //    }
        //}"); }

        //            sb.Append(@"
        //cache
        //{
        //    grad(U);
        //}

        //// ************************************************************************* //

        //;");
        //            return sb.ToString();
        //        }
        public static string FvSolution(int mode)
        {
            StringBuilder sb = new StringBuilder(); sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      fvSolution;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

solvers
{
    p
    {
        solver GAMG;
        tolerance 1e-6;
        relTol 0.1;
        smoother GaussSeidel;
        nPreSweeps 0;
        nPostSweeps 2;
        cacheAgglomeration on;
        agglomerator faceAreaPair;
        nCellsInCoarsestLevel 100;
        mergeLevels 1;
    } 

    ""(k|omega|epsilon)""
    {
        solver          smoothSolver;
        smoother        symGaussSeidel;
        tolerance       1e-6;
        relTol          0.1;
    }   
    U
    {
        solver PBiCG;
        preconditioner DILU;
        tolerance 1e-8;
        relTol 0.0;
    } 
	Phi
    {
        solver          GAMG;
        smoother        GaussSeidel;
        tolerance       1e-6;
        relTol          0.1;
    }
}

SIMPLE
{");
            if (mode == 0) { sb.Append(@"nNonOrthogonalCorrectors 1;"); }
            else { sb.Append(@"nNonOrthogonalCorrectors 4;"); }
            sb.AppendLine(@"
    residualControl
    {
    p       1e-4;
    U       1e-5;
    k       1e-5;
    epsilon 1e-5;
    }
    pRefCell    0;
    pRefValue    0;
}

potentialFlow
{
    nNonOrthogonalCorrectors 40;
}
");
            if (mode == 0)
            {
                sb.Append(@"relaxationFactors
{
    fields
    {
        p               0.7;
    }
    equations
    {
        U               0.3;
        k               0.3;
       epsilon          0.3;
	   omega			0.3;
    }
}"
);
            }
            else { sb.Append(@"relaxationFactors
{
    fields
    {
        p               0.3;
    }
    equations
    {
        U               0.7;
        k               0.7;
       epsilon          0.7;
	   omega			0.7;
    }
}"); }

            sb.Append(@"
cache
{
    grad(U);
}

// ************************************************************************* //

;");
            return sb.ToString();
        }
        public static string SurfaceFeatureExtractDict()
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      surfaceFeatureExtractDict;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

building.stl
{
    // How to obtain raw features (extractFromFile || extractFromSurface)
    extractionMethod    extractFromSurface;

    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less
        // than includedAngle as features
        // - 0  : selects no edges
        // - 180: selects all edges
        includedAngle   180;
    }

    subsetFeatures
    {
        // Keep nonManifold edges (edges with >2 connected faces)
        nonManifoldEdges       no;

        // Keep open edges (edges with 1 connected face)
        openEdges       yes;
    }


    // Write options

        // Write features to obj format for PostProcessing
        writeObj                yes;
}

ground.stl
{
    // How to obtain raw features (extractFromFile || extractFromSurface)
    extractionMethod    extractFromSurface;

    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less
        // than includedAngle as features
        // - 0  : selects no edges
        // - 180: selects all edges
        includedAngle   180;
    }

    subsetFeatures
    {
        // Keep nonManifold edges (edges with >2 connected faces)
        nonManifoldEdges       no;

        // Keep open edges (edges with 1 connected face)
        openEdges       yes;
    }


    // Write options

        // Write features to obj format for PostProcessing
        writeObj                yes;
}

// ************************************************************************* //
";
        }
        public static string TransportProperties()
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      transportProperties;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

transportModel  Newtonian;

nu              nu [0 2 -1 0 0 0 0] 1.5e-05;

// ************************************************************************* //
";
        }
        public static string TurbulenceProperties(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  3.0.1                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      RASProperties;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

simulationType RAS;

RAS
{
    RASModel         ");
            if (DOM.turbulenceModel == 2) { sb.Append("kOmegaSST;"); } else if (DOM.turbulenceModel == 1) { sb.Append("RNGkEpsilon;"); } else { sb.Append("kEpsilon;"); }
            sb.AppendLine(@"
    turbulence on;

    printCoeffs on;

}

// ************************************************************************* //
");

            return sb.ToString();
        }

        public static string Run_Mesh_Cyl(OFBaseDomain DOM)
        {


            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; blockMesh | tee -a  log""");
            if (DOM.CPUs > 1)
            {

                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; surfaceFeatureExtract | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; pyFoamDecompose.py --clear . " + DOM.CPUs + @" | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; foamJob -parallel -screen snappyHexMesh -overwrite | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; reconstructParMesh -constant | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; renumberMesh -overwrite | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh | tee -a  log""");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }
            else
            {
                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; surfaceFeatureExtract | tee -a log; snappyHexMesh -overwrite  | tee -a log; checkMesh | tee -a log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; renumberMesh -overwrite | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFmeshWorkingDirectory + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh | tee -a  log""");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }





            return sb.ToString();
        }


        public static string Run_Mesh_Box(OFBaseDomain DOM, int windDir)
        {


            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; blockMesh | tee -a  log""");
            if (DOM.CPUs > 1)
            {

                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; surfaceFeatureExtract | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; pyFoamDecompose.py --clear . " + DOM.CPUs + @" | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; foamJob -parallel -screen snappyHexMesh -overwrite | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; reconstructParMesh -constant | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; renumberMesh -overwrite | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh | tee -a  log""");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }
            else
            {
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; surfaceFeatureExtract | tee  -a log; snappyHexMesh -overwrite  | tee  -a log; checkMesh | tee -a log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; renumberMesh -overwrite | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + "\\" + DOM.BCInflow.windDirs[windDir] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh | tee -a  log""");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }





            return sb.ToString();
        }

        public static string Run_sim(OFBaseDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it  hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; pyFoamPrepareCase.py . --no-mesh-create | tee -a log""");
            if (DOM.CPUs > 1)
            {

                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it  hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; pyFoamDecompose.py --clear . " + DOM.CPUs + @" | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it  hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; foamJob -s -p renumberMesh -overwrite | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it  hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; foamJob -s -p potentialFoam | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it  hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; mpirun -np " + DOM.CPUs + @" simpleFoam -parallel | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it  hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; reconstructPar -latestTime | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it  hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh | tee -a log""");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }
            else
            {
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; potentialFoam | tee -a log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; simpleFoam | tee -a  log""");
                sb.AppendLine(@"docker run -v """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + @":/home/openfoam/"" --entrypoint="""" -it hfdresearch/swak4foamandpyfoam:latest-v4.1 bash -c ""source /opt/openfoam4/etc/bashrc; cd /home/openfoam; checkMesh | tee -a  log""");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }
            return sb.ToString();
        }

        public static string Run_mesh_docker(OFBaseDomain DOM)
        {


            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""blockMesh | tee -a log"" -f """ + DOM.OFmeshWorkingDirectory + " \"");
            if (DOM.CPUs > 1)
            {
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""surfaceFeatureExtract | tee -a  log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""pyFoamDecompose.py --clear . " + DOM.CPUs + @" | tee -a  log"" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamJob -parallel -screen snappyHexMesh -overwrite | tee -a  log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""reconstructParMesh -constant | tee -a  log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""renumberMesh -overwrite | tee -a  log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }
            else
            {
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""surfaceFeatureExtract | tee -a log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""snappyHexMesh -overwrite  | tee -a  log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a  log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""renumberMesh -overwrite | tee -a  log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a log "" -f """ + DOM.OFmeshWorkingDirectory + " \"");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }



            return sb.ToString();
        }
        public static string Run_sim_docker(OFBaseDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""pyFoamPrepareCase.py . --no-mesh-create | tee -a log"" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
            if (DOM.CPUs > 1)
            {

                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""pyFoamDecompose.py --clear . " + DOM.CPUs + @"| tee -a  log "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamJob -s -p renumberMesh -overwrite | tee -a  log "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamJob -s -p potentialFoam | tee -a  log "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""mpirun -np " + DOM.CPUs + @" simpleFoam -parallel | tee -a  log "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""reconstructPar -latestTime | tee -a  log; "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a  log "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }

            else
            {
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""potentialFoam | tee -a log "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""simpleFoam | tee -a  log "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
                sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""checkMesh | tee -a  log "" -f """ + DOM.OFbaseWorkingDirectory + DOM.BCInflow.windDirs[d] + " \"");
#if DEBUG

                sb.AppendLine("PAUSE");

#endif 
            }
            return sb.ToString();
        }

        public static string BlockMesh(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""blockMesh | tee -a log"" -f """ + DOM.OFmeshWorkingDirectory + " \"");
#if DEBUG

            sb.AppendLine("PAUSE");

#endif
            return sb.ToString();
        }

        public static string Run_checkBadMesh(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("\"" + Utilities.AssemblyDirectory + @"\CallOF.exe""  -e ""foamToVTK -faceSet highAspectRatioCells -ascii;foamToVTK -faceSet nonOrthoFaces -ascii;foamToVTK -faceSet skewFaces -ascii;foamToVTK -faceSet wrongOrientedFaces -ascii; foamToVTK -faceSet zeroVolumeCells -ascii | tee -a  log"" -f """ + DOM.baseWorkingDirectory + @"\mesh\ ");
#if DEBUG

            sb.AppendLine("PAUSE");

#endif
            return sb.ToString();
        }


        public static string Run(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"call """ + DOM.baseWorkingDirectory + @"run_mesh.bat""");
            foreach (int i in DOM.BCInflow.windDirs)
            {

                sb.AppendLine(@"call """ + DOM.baseWorkingDirectory + i + @"_run_sim.bat""");
            }
            sb.AppendLine(@"call """ + DOM.baseWorkingDirectory + @"run_ray.bat""");
            sb.AppendLine(@"call """ + DOM.baseWorkingDirectory + @"run_probes.bat""");
            sb.AppendLine(@"call """ + DOM.baseWorkingDirectory + @"run_utci.bat""");
#if DEBUG

            sb.AppendLine("PAUSE");

#endif 
            return sb.ToString();
        }

        public static string RunSimOnly(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            //  sb.AppendLine(@"call " + DOM.baseWorkingDirectory + "run_mesh.bat");
            foreach (int i in DOM.BCInflow.windDirs)
            {
                //sb.AppendLine("start " + DOM.baseWorkingDirectory +i + "_run_sim.bat");
                sb.AppendLine("call " + DOM.OFbaseWorkingDirectory + i + "_run_sim.bat");
            }
#if DEBUG

            sb.AppendLine("PAUSE");

#endif 
            return sb.ToString();
        }

        public static string Run_RayTrace(OFBaseDomain DOM)
        {
            string workDir = DOM.baseWorkingDirectory.Trim('\\');
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallRay.exe\" " + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCInflow.weather + "\"");
#if DEBUG

            sb.AppendLine("PAUSE");

#endif 
            return sb.ToString();
        }

        public static string Run_Probes(OFBaseDomain DOM)
        {

            //@ Patrick WIP

            string dirs = "";
            foreach (var d in DOM.BCInflow.windDirs)
            {
                dirs += (((int)d).ToString() + ',');
            }

            dirs = dirs.TrimEnd(',');

            string workDir = DOM.baseWorkingDirectory.Trim('\\');
            string uref = DOM.BCInflow.URef.ToString();
            string z0 = DOM.BCInflow.z0.ToString();
            string zref = DOM.BCInflow.zref.ToString();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallProbes.exe\" " + "-d " + "\"" + workDir + "\" " + "-p " + "\"" + workDir + @"\Rad\sensors.pts" + "\"" + " -w " + dirs + " -m 1" + " -u " + uref + " -r " + z0+ " -z " + zref);

#if DEBUG

            sb.AppendLine("PAUSE");

#endif 



            return sb.ToString();
        }

        public static string Run_UTCI(OFBaseDomain DOM)
        {

            //@ Patrick WIP

            string dirs = "";
            foreach (var d in DOM.BCInflow.windDirs)
            {
                dirs += (((int)d).ToString() + ',');
            }

            dirs = dirs.TrimEnd(',');

            string workDir = DOM.baseWorkingDirectory.Trim('\\');

            string dif = "-f " + "\"" + workDir + @"\Rad\CallRay.dif.ill" + "\"";
            string dir = "-r " + "\"" + workDir + @"\Rad\CallRay.dir.ill" + "\"";
            string u = "-u " + "\"" + workDir + @"\WindReductionData.csv" + "\"";
            // windDirs
            string o = "-o " + dirs;           


            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallProbes.exe\" "+ "-w " + "\"" +workDir + "\" " + "-p " + "\"" + workDir + @"\Rad\sensors.pts" + "\"" + " -d " + dirs + " -m 1");
            //sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallRay.exe\" "  + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCInflow.weather + "\"");
            sb.AppendLine("\"" + Utilities.AssemblyDirectory + "\\CallOC.exe\" " + "-d " + "\"" + workDir + "\" " + "-w " + "\"" + DOM.BCInflow.weather + "\" " + dif + " " + dir + " " + o + " " + u);

#if DEBUG
            sb.Append(" -b 0,0;");
            sb.AppendLine("PAUSE");

#endif



            return sb.ToString();
        }



        public static string Residuals()
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
  =========                 |
  \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox
   \\    /   O peration     |
    \\  /    A nd           | Web:      www.OpenFOAM.org
     \\/     M anipulation  |
-------------------------------------------------------------------------------
Description
    For specified fields, writes out the initial residuals for the first
    solution of each time step; for non-scalar fields (e.g. vectors), writes
    the largest of the residuals for each component (e.g. x, y, z).

\*---------------------------------------------------------------------------*/

type            residuals;
libs            (""libutilityFunctionObjects.so"");

writeControl timeStep;
writeInterval   1;

fields (U p epsilon omega  k);

// ************************************************************************* //
";
        }
    }
}