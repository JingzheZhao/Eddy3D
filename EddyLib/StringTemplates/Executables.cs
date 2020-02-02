using System;
using System.Collections.Generic;
using System.Text;
using Rhino.Geometry;

namespace EddyLib.StrTemp
{
    public class OFExecDicts
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
(" + corners[0].X + " " + corners[0].Y + " " + corners[0].Z + @")
(" + corners[1].X + " " + corners[1].Y + " " + corners[1].Z + @")
(" + corners[2].X + " " + corners[2].Y + " " + corners[2].Z + @")
(" + corners[3].X + " " + corners[3].Y + " " + corners[3].Z + @")
(" + corners[4].X + " " + corners[4].Y + " " + corners[4].Z + @")
(" + corners[5].X + " " + corners[5].Y + " " + corners[5].Z + @")
(" + corners[6].X + " " + corners[6].Y + " " + corners[6].Z + @")
(" + corners[7].X + " " + corners[7].Y + " " + corners[7].Z + @")
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
            //string ground_perim = @"ground_perim.stl
            //{
            //    type triSurfaceMesh;
            //    name ground_perim;
            //}
            //";
            string Cylinder = @"refinementCylinder{
type searchableCylinder;
point1 (" + dom.RefinementCylinder.Center.ToString().Replace(',', ' ') + @");
point2 (" + (dom.RefinementCylinder.Center + Vector3d.ZAxis * dom.RefinementCylinder.Height2).ToString().Replace(',', ' ') + @");
radius " + dom.RefinementCylinder.CircleAt(0.5).Radius + @";
}";

            string Box = @"refinementBox{
          type searchableBox;
          min (" + dom.BBox.X.Min + " " + +dom.BBox.Y.Min + " " + dom.BBox.Z.Min + @");
          max (" + dom.BBox.X.Max + " " + dom.BBox.Y.Max + " " + dom.BBox.Z.Max + @");
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
            sb.AppendLine("snap "); if (MeshSettings.snappySetting == SnappySetting.BlocksSnapping || MeshSettings.snappySetting == SnappySetting.BlocksSnappingLayers) { sb.Append("true;"); } else { sb.Append("false;"); }
            sb.AppendLine("addLayers "); if (MeshSettings.snappySetting == SnappySetting.BlocksSnappingLayers) { sb.Append("true;"); } else { sb.Append("false;"); }
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

            if (!dom.hasTerrain)
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
            if (MeshSettings.snappySetting != SnappySetting.Blocks)
            {
                sb.Append(@"
            {file ""building.eMesh""; level " + (MeshSettings.accFeatures) + @" ;}
            {file ""ground.eMesh""; level " + (MeshSettings.accFeatures) + @" ;}");
            }

            sb.Append(@"
        );
        refinementSurfaces
        {
            building
            {
                level (" + (MeshSettings.accBuildings - 1) + @" " + MeshSettings.accBuildings + @");
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
            if (!dom.hasTerrain)
            {
                sb.Append(@"ground_perim
            {
                level (" + (MeshSettings.accGround - 1) + @" " + (MeshSettings.accGround) + @");
                patchInfo
                {
                    type wall;
                }
            }");
            }
            sb.Append(@"}
refinementRegions
        {
refinementBox {mode inside; levels ((" + MeshSettings.accRefinement + @" " + MeshSettings.accRefinement + @"));}
//refinementCylinder {mode inside; levels ((" + MeshSettings.accRefinement + " " + MeshSettings.accRefinement + @"));}
        }

        locationInMesh ( " + dom.LocationInMesh.X + " " + dom.LocationInMesh.Y + " " + dom.LocationInMesh.Z + @" );
        //maxLocalCells 15000000;
        //maxGlobalCells 50000000;
        //minRefinementCells 5;
        //nCellsBetweenLevels 5;
        //resolveFeatureAngle 30;
        //allowFreeStandingZoneFaces true;
        //planarAngle 30;
        //maxLoadUnbalance 0.10;

    maxLocalCells       4000000;
    maxGlobalCells      100000000;
    minRefinementCells  1;
    maxLoadUnbalance    0.20;
    nCellsBetweenLevels 3;
    resolveFeatureAngle 30;
    allowFreeStandingZoneFaces false;
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
            if (!dom.hasTerrain)
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

////- Maximum non-orthogonality allowed. Set to 180 to disable.
// maxNonOrtho 65;

// //- Max skewness allowed. Set to <0 to disable. maxBoundarySkewness 20; maxInternalSkewness 4;

// //- Max concaveness allowed. Is angle (in degrees) below which concavity // is allowed. 0 is
// straight face, <0 would be convex face. // Set to 180 to disable. maxConcave 80;

// //- Minimum pyramid volume. Is absolute volume of cell pyramid. // Set to a sensible fraction of
// the smallest cell volume expected. // Set to very negative number (e.g. -1E30) to disable. minVol 1e-16;

// //- Minimum quality of the tet formed by the face-centre // and variable base point minimum
// decomposition triangles and // the cell centre. This has to be a positive number for tracking //
// to work. Set to very negative number (e.g. -1E30) to // disable. // <0 = inside out tet, // 0 =
// flat tet // 1 = regular tet minTetQuality -1e+30; // 1e-30;

// //- Minimum face area. Set to <0 to disable. minArea 1e-13;

// //- Minimum face twist. Set to <-1 to disable. dot product of face normal // and face centre
// triangles normal minTwist 0.02;

// //- Minimum normalised cell determinant // 1 = hex, <= 0 = folded or flattened illegal cell
// minDeterminant 0.001;

// //- minFaceWeight (0 -> 0.5) minFaceWeight 0.02;

// //- minVolRatio (0 -> 1) minVolRatio 0.01;

// //must be >0 for Fluent compatibility minTriangleTwist -1;

// // Advanced

// //- Number of error distribution iterations nSmoothScale 4; //- Amount to scale back displacement
// at error points errorReduction 0.75;

// // Optional : some meshing phases allow usage of relaxed rules. // See e.g.
// addLayersControls::nRelaxedIter. relaxed { //- Maximum non-orthogonality allowed. Set to 180 to
// disable. maxNonOrtho 75; }
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

        public static string ControlDict(OFRunSettings RunSettings, OFBaseDomain DOM, List<Mesh> topologies, int numberOfTopologies)
        {
            StringBuilder sb = new StringBuilder();
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
        ""libutilityFunctionObjects.so""
        ""libsolverFunctionObjects.so""");
            if (RunSettings.simEngine == SimEngine.Docker)
            {
                sb.Append(@"""libsimpleSwakFunctionObjects.so""
                ""libswakFunctionObjects.so""
                ""libgroovyBC.so""");
            }
            sb.Append(@"
);
            application simpleFoam;
            startFrom latestTime;
            startTime       1;
            stopAt endTime;
            endTime         " + RunSettings.iter + @";
            deltaT          1;
            writeControl timeStep;
            writeInterval   " + RunSettings.writeInterval + @";
            purgeWrite      " + RunSettings.keepTimeSteps + @";
            writeFormat binary;
            writePrecision  8;
            writeCompression uncompressed;
            timeFormat general;
            timePrecision   6;
            runTimeModifiable true;
            functions
{
#includeFunc residuals
");
            //if (topologies != null) {
            sb.Append(EddyLib.StrTemp.OFExecDicts.FunctionObjCP(DOM, RunSettings, topologies, numberOfTopologies).ToString());
            //}
            //else { sb.Append(@"};"); }

            return sb.ToString();
        }

        public static string FunctionObjCP(OFBaseDomain DOM, OFRunSettings RunSettings, List<Mesh> evaluationTopology, int d)
        {
            BoundaryConditionsCP BCondCP = new BoundaryConditionsCP(DOM.MaxHeightBuilding, DOM.BCond);

            StringBuilder sb = new StringBuilder();
            sb.Append(@"pressureCoefficients
{
                    type pressure;
                    libs (""libfieldFunctionObjects.so"");
                    enabled yes;
                    writeControl timeStep;
                    writeInterval " + RunSettings.writeInterval + @";
                    UInf (" + Math.Round(BCondCP.Uinf[d].X, 1) + " " + Math.Round(BCondCP.Uinf[d].Y, 1) + " " + Math.Round(BCondCP.Uinf[d].Z, 1) + @");     // the undistrubed velocity at building height
                    pInf " + Math.Round(BCondCP.pinf, 1) + @";        // the dynamic undisturbed pressure at building height
                    pRef " + Math.Round(BCondCP.pref, 1) + @";        // the dynamic pressure at reference height (usually 10 m)
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
            StringBuilder sb = new StringBuilder();
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

        public static string SampleProbes(List<Point3d> listOfPoints, OFField ofField)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
  | =========                 |                                                 |
  | \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
  |  \\    /   O peration     | Version:  5                                     |
  |   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
  |    \\/     M anipulation  |                                                 |
  \*---------------------------------------------------------------------------*/

" + ofField.ProbeName + @"
{
                type probes;
                libs (""libsampling.so"");
                writeControl writeTime;

                //interpolationScheme cellPointFace;
                interpolationScheme cellPoint;

                setFormat csv;

                fields (" + ofField.FieldName + @");

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
        // "; }

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
    div(U) Gauss linear;
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

        public static string FvSchemesDefault()
        {
            return
        @"/*--------------------------------*- C++ -*----------------------------------*\
  =========                 |
  \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox
   \\    /   O peration     | Website:  https://openfoam.org
    \\  /    A nd           | Version:  6
     \\/     M anipulation  |
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
    default         Gauss linear;

    limited         cellLimited Gauss linear 1;
    grad(U)         $limited;
    grad(k)         $limited;
    grad(epsilon)     $limited;
}

divSchemes
{
    default         none;

    div(phi,U)      bounded Gauss linearUpwind limited;

    turbulence      bounded Gauss limitedLinear 1;
    div(phi,k)      $turbulence;
    div(phi,epsilon) $turbulence;

    div((nuEff*dev2(T(grad(U))))) Gauss linear;
}

laplacianSchemes
{
    default         Gauss linear corrected;
}

interpolationSchemes
{
    default         linear;
}

snGradSchemes
{
    default         corrected;
}

wallDist
{
    method meshWave;
}

// ************************************************************************* //

";
        }

        public static string FvSchemesSimscale()
        {
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
    div(U) Gauss linear;
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
    div(U) Gauss linear;
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

        public static string FvSolutionDefault(OFRunSettings RunSettings)
        {
            StringBuilder sb = new StringBuilder(); sb.Append(@"
/*--------------------------------*- C++ -*----------------------------------*\
  =========                 |
  \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox
   \\    /   O peration     | Website:  https://openfoam.org
    \\  /    A nd           | Version:  6
     \\/     M anipulation  |
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
        solver          GAMG;
        smoother        GaussSeidel;
        tolerance       1e-6;
        relTol          0.1;
    }

	Phi
    {
        solver          GAMG;
        smoother        GaussSeidel;
        tolerance       1e-9;
        relTol          0.0001;
    }

    ""(U|k|omega|epsilon)""
    {
                solver smoothSolver;
                smoother symGaussSeidel;
                tolerance       1e-6;
                relTol          0.1;
            }
        }

        SIMPLE
{
    residualControl
    {
        p               1e-4;
        U               1e-4;
        ""(k|omega|epsilon)"" 1e-4;
    }
");
            if (RunSettings.relaxationFactors == RelaxationFactors.OpenFOAM) { sb.Append(@"nNonOrthogonalCorrectors 1;"); }
            else { sb.Append(@"nNonOrthogonalCorrectors 4;"); }
            sb.AppendLine(@"
    pRefCell        0;
    pRefValue       0;
}

potentialFlow
{
    nNonOrthogonalCorrectors 30;
}

");
            if (RunSettings.relaxationFactors == RelaxationFactors.Fluent)
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
            else if (RunSettings.relaxationFactors == RelaxationFactors.OpenFOAM) { sb.Append(@"relaxationFactors
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
            else if (RunSettings.relaxationFactors == RelaxationFactors.SimScale) { sb.Append(@"relaxationFactors
{
    fields
    {
        p               0.3;
    }
    equations
    {
        U               0.3;
        k               0.3;
       epsilon          0.3;
	   omega			0.3;
    }
}"); }

            sb.Append(@"

// ************************************************************************* //

");
            return sb.ToString();
        }

        public static string FvSolution(OFRunSettings RunSettings)
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
        tolerance 1e-9;
        relTol 0.0001;
        smoother GaussSeidel;
        nPreSweeps 2;
        nPostSweeps 1;
        cacheAgglomeration on;
        agglomerator faceAreaPair;
        nCellsInCoarsestLevel 10;
        mergeLevels 1;
    }

    ""(k|omega|epsilon)""
    {
        solver          smoothSolver;
        smoother        GaussSeidel;
        tolerance       1e-9;
        relTol          0.0001;
    }
    U
    {
        solver smoothSolver;
        smoother GaussSeidel;
        preconditioner DILU;
        tolerance 1e-9;
        relTol 0.0001;
    }
	Phi
    {
        solver          GAMG;
        smoother        GaussSeidel;
        tolerance       1e-9;
        relTol          0.0001;
    }
}

SIMPLE
{");
            if (RunSettings.relaxationFactors == RelaxationFactors.OpenFOAM) { sb.Append(@"nNonOrthogonalCorrectors 1;"); }
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
    nNonOrthogonalCorrectors 30;
}
");
            if (RunSettings.relaxationFactors == RelaxationFactors.Fluent)
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
            else if (RunSettings.relaxationFactors == RelaxationFactors.OpenFOAM) { sb.Append(@"relaxationFactors
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
            else if (RunSettings.relaxationFactors == RelaxationFactors.SimScale) { sb.Append(@"relaxationFactors
{
    fields
    {
        p               0.3;
    }
    equations
    {
        U               0.3;
        k               0.3;
       epsilon          0.3;
	   omega			0.3;
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
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
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
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
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

        public static string TurbulenceProperties(OFRunSettings RunSettings)
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
");
            if (RunSettings.turbModel == TurbModel.laminar) { sb.AppendLine("simulationType laminar; "); }
            else { sb.AppendLine("simulationType RAS;"); }
            sb.AppendLine(@"RAS
{
    RASModel         ");
            if (RunSettings.turbModel == TurbModel.kOmegaSST) { sb.Append("kOmegaSST;"); } else if (RunSettings.turbModel == TurbModel.RNGkEpsilon) { sb.Append("RNGkEpsilon;"); } else { sb.Append("kEpsilon;"); }
            sb.AppendLine(@"
    turbulence on;

    printCoeffs on;
}

// ************************************************************************* //
");

            return sb.ToString();
        }

        public static string ResidualsDict()
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

        public static string DecomposeParDict(OFRunSettings RunSettings)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"// * * * * * * * * * //
            FoamFile
{
                version 0.5;
                format ascii;
                root ""ROOT"";
	case ""CASE"";

    class dictionary;
        object nix;
    }
    method scotch;
    numberOfSubdomains " + RunSettings.CPUs + @";
scotchCoeffs
{
}");
            return sb.ToString();
        }
    }
}