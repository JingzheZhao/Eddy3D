using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace EddyLib.Strings
{
    public partial class OFExecDicts
    {
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
        ""libsolverFunctionObjects.so""
        ""libatmosphericModels.so""");
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

            sb.AppendLine(FunctionObjCP(DOM, RunSettings, topologies, numberOfTopologies));
            sb.AppendLine(FunctionObjFieldMinMax());
            sb.AppendLine(FunctionObjFieldAverage());
            if (RunSettings.aoa_domain == true)
            {
                sb.AppendLine(EddyLib.Strings.OFExecDicts.FunctionObjAOA());
            }

            sb.AppendLine(@"};");

            return sb.ToString();
        }

        public static string FunctionObjAOA()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"aoa
    {
        type            scalarTransport;
        libs (""libfieldFunctionObjects.so"");

        writeControl    outputTime;
            D               1.0;
            field aoa;
            resetOnStartUp  false;
            schemesField aoa;
            bounded01       true;
            write           true;

            fvOptions
        {
                aoa_00
            {
                    type semiImplicitSource;
                    active          true;
					selectionMode all;
					volumeMode specific;
                    sources
                {
                        aoa
                    {
                            explicit 1;
							implicit 0;
                        }
                    }
                }
            }
        }");

            return sb.ToString();
        }

        private static string FunctionObjFieldMinMax()
        {
            return @"fieldMinMax
{
    type fieldMinMax;
    libs (""libfieldFunctionObjects.so"");
    writeToFile true;
    log true;
    mode magnitude;
    fields (U p k epsilon omega nut aoa);
}";
        }

        private static string FunctionObjFieldAverage()
        {
            return @"average
{
    type            volFieldValue;
    libs            (""libfieldFunctionObjects.so"");
    fields (U p);
    operation       weightedVolAverage;
    regionType      all;
    writeFields     false;
    log true;
}";
        }

        public static string FunctionObjCP(OFBaseDomain DOM, OFRunSettings RunSettings, List<Mesh> evaluationTopology, int d)
        {
            PressureCoeff BCondCP = new PressureCoeff(DOM.MaxHeightBuilding, DOM.BCond);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"pressureCoefficients
{
                    type pressure;
                    libs (""libfieldFunctionObjects.so"");
                    enabled true;
                    writeControl timeStep;
                    writeInterval " + RunSettings.writeInterval + @";
                    UInf (" + Utilities.FormatPV(BCondCP.Uinf[d]) + @");     // the undistrubed velocity at building height
                    pInf " + Utilities.FormatDouble(Math.Round(BCondCP.pinf[d], 1)) + @";        // the dynamic undisturbed pressure at building height
                    pRef " + Utilities.FormatDouble(Math.Round(BCondCP.pref[d], 1)) + @";        // the dynamic pressure at reference height (usually 10 m)
                    rhoInf              1.2;
                    calcTotal true;
                    calcCoeff true;
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

        public static string SampleProbesAllFields(List<Point3d> listOfPoints, string ProbeName, string InterpolationScheme)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
  | =========                 |                                                 |
  | \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
  |  \\    /   O peration     | Version:  5                                     |
  |   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
  |    \\/     M anipulation  |                                                 |
  \*---------------------------------------------------------------------------*/

" + ProbeName + @"
{
                type probes;
                libs (""libsampling.so"");
                writeControl writeTime;

                interpolationScheme " + InterpolationScheme + @";

                setFormat csv;

                fields (U p total(p)_coeff epsilon omega k nut phi aoa);

                probeLocations
                  (");
            sb.Append(Environment.NewLine);
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                sb.Append(@"(" + Utilities.FormatPV(listOfPoints[i]) + @")");
                sb.Append(Environment.NewLine);
            }

            sb.Append(@");
        }

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

                interpolationScheme " + ofField.InterpolationScheme + @";

                setFormat csv;

                fields (" + ofField.FieldName + @");

                probeLocations
                  (");
            sb.Append(Environment.NewLine);
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                sb.Append(@"(" + Utilities.FormatPV(listOfPoints[i]) + @")");
                sb.Append(Environment.NewLine);
            }

            sb.Append(@");
        }

            // ************************************************************************* //");

            return sb.ToString();
        }

        //        public static string MeshQualityDict()
        //        {
        //            return
        //               @"/*--------------------------------*- C++ -*----------------------------------*\
        //| =========                 |                                                 |
        //| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
        //|  \\    /   O peration     | Version:  3.0.1                                 |
        //|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
        //|    \\/     M anipulation  |                                                 |
        //\*---------------------------------------------------------------------------*/
        //FoamFile
        //{
        //    version     2.0;
        //    format      ascii;
        //    class       dictionary;
        //    object      meshQualityDict;
        //}

        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        ////- Maximum non-orthogonality allowed. Set to 180 to disable.
        //maxNonOrtho 65;

        ////- Max skewness allowed. Set to <0 to disable.
        //maxBoundarySkewness 20;
        //maxInternalSkewness 4;

        ////- Max concaveness allowed. Is angle (in degrees) below which concavity
        ////  is allowed. 0 is straight face, <0 would be convex face.
        ////  Set to 180 to disable.
        //maxConcave 80;

        ////- Minimum pyramid volume. Is absolute volume of cell pyramid.
        ////  Set to a sensible fraction of the smallest cell volume expected.
        ////  Set to very negative number (e.g. -1E30) to disable.
        //minVol 1e-13;

        ////- Minimum quality of the tet formed by the face-centre
        ////  and variable base point minimum decomposition triangles and
        ////  the cell centre. This has to be a positive number for tracking
        ////  to work. Set to very negative number (e.g. -1E30) to
        ////  disable.
        ////     <0 = inside out tet,
        ////      0 = flat tet
        ////      1 = regular tet
        //minTetQuality 1e-15;

        ////- Minimum face area. Set to <0 to disable.
        //minArea -1;

        ////- Minimum face twist. Set to <-1 to disable. dot product of face normal
        //// and face centre triangles normal
        //minTwist 0.02;

        ////- Minimum normalised cell determinant. This is the determinant of all
        ////  the areas of internal faces. It is a measure of how much of the
        ////  outside area of the cell is to other cells. The idea is that if all
        ////  outside faces of the cell are 'floating' (zeroGradient) the
        ////  'fixedness' of the cell is determined by the area of the internal faces.
        ////  1 = hex, <= 0 = folded or flattened illegal cell
        //minDeterminant 0.001;

        ////- Relative position of face in relation to cell centres (0.5 for orthogonal
        ////  mesh) (0 -> 0.5)
        //minFaceWeight 0.05;

        ////- Volume ratio of neighbouring cells (0 -> 1)
        //minVolRatio 0.01;

        ////- Per triangle normal compared to average normal. Like face twist
        ////  but now per (face-centre decomposition) triangle. Must be >0 for Fluent
        ////  compatibility
        //minTriangleTwist -1;

        ////- If >0 : preserve cells with all points on the surface if the
        ////  resulting volume after snapping (by approximation) is larger than
        ////  minVolCollapseRatio times old volume (i.e. not collapsed to flat cell).
        ////  If <0 : delete always.
        ////minVolCollapseRatio 0.1;

        //// ************************************************************************* //
        //";
        //        }

        //        public static string FvSchemesAccurate()
        //        {// An accurate and stable numerical scheme
        //            return
        //        @"/*--------------------------------*- C++ -*----------------------------------*\
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
        //    object      fvSchemes;
        //}

        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //ddtSchemes
        //{
        //    default         steadyState;
        //}

        //gradSchemes
        //{
        //    default cellMDLimited Gauss linear 0.5;
        //}

        //divSchemes
        //{
        //    default         bounded Gauss upwind grad(U);
        //    div(phi,U)      bounded Gauss linearUpwindV grad(U);
        //    div(phi,k)      bounded Gauss upwind grad(U);

        //    //div(phi,epsilon)  bounded Gauss upwind grad(U);
        //    div(phi,omega)  bounded Gauss upwind grad(U);
        //    div((nuEff*dev2(T(grad(U))))) Gauss linear;
        //    div(phi,time)   bounded Gauss upwind grad(U);
        //    div(U) Gauss linear;
        //}

        //laplacianSchemes
        //{
        //    default         Gauss linear corrected;
        //    laplacian(nuEff,time) Gauss linear corrected;
        //}

        //interpolationSchemes
        //{
        //    default         linear;
        //}

        //snGradSchemes
        //{
        //    default         corrected;
        //}

        //fluxRequired
        //{
        //    default         no;
        //    p;
        //}
        //wallDist
        //{
        //	method meshWave;
        //}

        //// ************************************************************************* //
        //";
        //        }

        public static string FvSchemes(OFRunSettings RunSettings)
        {
            if (RunSettings.schemes == fvSchemes.Default)
            {
                return FvSchemesDefault();
            }
            else
            {
                return FvSchemesOptimized();
            }
        }

        public static string FvSchemesOptimized()
        {
            return
        @"/*--------------------------------*- C++ -*----------------------------------*\
  =========                 |
  \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox
   \\    /   O peration     | Website:  https://openfoam.org
    \\  /    A nd           | Version:  8
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
    default                         steadyState;
}

gradSchemes
{
    default                         Gauss linear;
    grad(U)                         cellLimited Gauss linear 0.333;
    grad(k)                         cellLimited Gauss linear 0.333;
    grad(epsilon)                   cellLimited Gauss linear 0.333;
}

divSchemes
{
    default                         none;
    turbulenceScheme                bounded Gauss limitedLinear 0.333;
    div(phi,U)                      bounded Gauss linearUpwind grad(U);
    div(phi,k)                      $turbulenceScheme;
    div(phi,epsilon)                $turbulenceScheme;
    div(phi,omega)                  $turbulenceScheme;
    div(U)                          Gauss limitedLinear 0.333;
    div((nuEff*dev2(T(grad(U)))))   Gauss linear;
    div(phi,aoa)                    Gauss limitedLinear 0.333;
}

laplacianSchemes
{
    default                         Gauss linear limited corrected 0.333;
}

interpolationSchemes
{
    default                         linear;
}

snGradSchemes
{
    default                         limited corrected 0.333;
}

wallDist
{
    method                          meshWave;
}

";
        }

        public static string FvSchemesDefault()

        //BIMHVAC
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
}

divSchemes
{
    default         none;
    div(phi,U)      bounded Gauss upwind;
    div(phi,h)      bounded Gauss upwind;
    div(phi,K)      bounded Gauss upwind;
	div((nuEff*dev2(T(grad(U)))))  Gauss linear;
    div(phi,k)      bounded Gauss upwind;
    div(phi,omega)  bounded Gauss upwind;
	div(phi,epsilon) bounded Gauss upwind;
    div(phi,aoa)    bounded Gauss upwind;
    div(U)          bounded Gauss upwind;
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

fluxRequired
{
    default         no;
}

// ************************************************************************* //

";
        }

        //        //        public static string FvSchemesSimscale()
        //        //        {
        //        //            return
        //        //        @"/*--------------------------------*- C++ -*----------------------------------*\
        //        //| =========                 |                                                 |
        //        //| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
        //        //|  \\    /   O peration     | Version:  2.2.2                                 |
        //        //|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
        //        //|    \\/     M anipulation  |                                                 |
        //        //\*---------------------------------------------------------------------------*/
        //        //FoamFile
        //        //{
        //        //    version     2.0;
        //        //    format      ascii;
        //        //    class       dictionary;
        //        //    object      fvSchemes;
        //        //}

        //        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //        //ddtSchemes
        //        //{
        //        //    default         steadyState;
        //        //}

        //        //gradSchemes
        //        //{
        //        //    default cellMDLimited Gauss linear 1.0;
        //        //}

        //        //divSchemes
        //        //{
        //        //    default          Gauss upwind;
        //        //    div(phi,U)       Gauss upwind;

        //        //    //div(phi,k)       Gauss upwind;
        //        //    //div(phi,epsilon) Gauss upwind;
        //        //    div(phi,k)       Gauss linear;
        //        //    div(phi,epsilon) Gauss linear;
        //        //    div(phi,omega)   Gauss upwind;
        //        //    div((nuEff*dev2(T(grad(U))))) Gauss linear;
        //        //    div(phi,time)   Gauss upwind;
        //        //    div(U) Gauss linear;
        //        //}

        //        //laplacianSchemes
        //        //{
        //        //    default         Gauss linear corrected;
        //        //    laplacian(nuEff,time) Gauss linear corrected;
        //        //}

        //        //interpolationSchemes
        //        //{
        //        //    default         linear;
        //        //}

        //        //snGradSchemes
        //        //{
        //        //    default         corrected;
        //        //}

        //        //fluxRequired
        //        //{
        //        //    default         no;
        //        //    p;
        //        //}
        //        //wallDist
        //        //{
        //        //	method meshWave;
        //        //}

        //        //// ************************************************************************* //
        //        //";
        //        //        }

        //        public static string FvSchemesRobust1()
        //        {//A robust numerical scheme but diffusive
        //            return
        //        @"/*--------------------------------*- C++ -*----------------------------------*\
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
        //    object      fvSchemes;
        //}

        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //ddtSchemes
        //{
        //    default         steadyState;
        //}

        //gradSchemes
        //{
        //    default cellMDLimited Gauss linear 1.0;
        //}

        //divSchemes
        //{
        //    default          Gauss upwind;
        //    div(phi,U)       Gauss upwind;

        //    //div(phi,k)       Gauss upwind;
        //    //div(phi,epsilon) Gauss upwind;
        //    div(phi,k)       Gauss linear;
        //    div(phi,epsilon) Gauss linear;
        //    div(phi,omega)   Gauss upwind;
        //    div((nuEff*dev2(T(grad(U))))) Gauss linear;
        //    div(phi,time)   Gauss upwind;
        //    div(U) Gauss linear;
        //}

        //laplacianSchemes
        //{
        //    default         Gauss linear corrected;
        //    laplacian(nuEff,time) Gauss linear corrected;
        //}

        //interpolationSchemes
        //{
        //    default         linear;
        //}

        //snGradSchemes
        //{
        //    default         corrected;
        //}

        //fluxRequired
        //{
        //    default         no;
        //    p;
        //}
        //wallDist
        //{
        //	method meshWave;
        //}

        //// ************************************************************************* //
        //";
        //        }

        //        public static string FvSchemesAccurateOscillatory()
        //        {// An even more accurate but oscillatory scheme
        //            return
        //        @"/*--------------------------------*- C++ -*----------------------------------*\
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
        //    object      fvSchemes;
        //}

        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //ddtSchemes
        //{
        //    default         steadyState;
        //}

        //gradSchemes
        //{
        //    default Gauss linear;
        //}

        //divSchemes
        //{
        //    default         Gauss linearUpwind grad(U);
        //    div(phi,U)       Gauss linear;

        //    //div(phi,k)       Gauss linearUpwind grad(U);
        //    //div(phi,epsilon) Gauss linearUpwind grad(U);
        //    div(phi,k)       Gauss linear;
        //    div(phi,epsilon) Gauss linear;
        //    div(phi,omega)   Gauss linearUpwind grad(U);
        //    div((nuEff*dev2(T(grad(U))))) Gauss linear;
        //    div(phi,time)   Gauss linearUpwind grad(U);
        //}

        //laplacianSchemes
        //{
        //    default         Gauss linear corrected;
        //    laplacian(nuEff,time) Gauss linear corrected;
        //}

        //interpolationSchemes
        //{
        //    default         linear;
        //}

        //snGradSchemes
        //{
        //    default         corrected;
        //}

        //fluxRequired
        //{
        //    default         no;
        //    p;
        //}
        //wallDist
        //{
        //	method meshWave;
        //}

        //// ************************************************************************* //
        //";
        //        }

        //        public static string FvSchemesOrtho70_80()
        //        {
        //            // An accurate numerical scheme on orthogonal (70-80) meshes
        //            return
        //        @"/*--------------------------------*- C++ -*----------------------------------*\
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
        //    object      fvSchemes;
        //}

        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //ddtSchemes
        //{
        //    default         steadyState;
        //}

        //gradSchemes
        //{
        //    default cellMDLimited leastSquares 1.0;
        //}

        //divSchemes
        //{
        //    default          Gauss linearUpwind;
        //    div(phi,U)       Gauss linearUpwind grad(U);

        //    //div(phi,k)       Gauss linearUpwind;
        //    //div(phi,epsilon) Gauss linearUpwind;
        //    div(phi,k)       Gauss linear;
        //    div(phi,epsilon) Gauss linear;
        //    div(phi,omega)   Gauss linearUpwind;
        //    div((nuEff*dev2(T(grad(U))))) Gauss linear;
        //    div(phi,time)   Gauss linearUpwind grad(U);
        //}

        //laplacianSchemes
        //{
        //    default         Gauss linear limited 0.5;
        //    laplacian(nuEff,time) Gauss linear limited 0.5;
        //}

        //interpolationSchemes
        //{
        //    default         linear;
        //}

        //snGradSchemes
        //{
        //    default         limited 0.5;
        //}

        //fluxRequired
        //{
        //    default         no;
        //    p;
        //}
        //wallDist
        //{
        //	method meshWave;
        //}

        //// ************************************************************************* //
        //";
        //        }

        //        public static string FvSchemesOrtho60_70()
        //        {
        //            // An accurate numerical scheme on orthogonal (60-70) meshes
        //            return
        //        @"/*--------------------------------*- C++ -*----------------------------------*\
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
        //    object      fvSchemes;
        //}

        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //ddtSchemes
        //{
        //    default         steadyState;
        //}

        //gradSchemes
        //{
        //    default cellMDLimited Gauss linear 0.5;
        //}

        //divSchemes
        //{
        //    div(phi,U)       Gauss linearUpwind grad(U);

        //    //div(phi,k)       Gauss linearUpwind;
        //    //div(phi,epsilon) Gauss linearUpwind;
        //    div(phi,k)       Gauss linear;
        //    div(phi,epsilon) Gauss linear;
        //    div(phi,omega)   Gauss linearUpwind;
        //    div((nuEff*dev2(T(grad(U))))) Gauss linear;
        //    div(phi,time)   Gauss linearUpwind grad(U);
        //}

        //laplacianSchemes
        //{
        //    default         Gauss linear limited 0.77;
        //    laplacian(nuEff,time) Gauss linear limited 0.77;
        //}

        //interpolationSchemes
        //{
        //    default         linear;
        //}

        //snGradSchemes
        //{
        //    default         limited 0.77;
        //}

        //fluxRequired
        //{
        //    default         no;
        //    p;
        //}
        //wallDist
        //{
        //	method meshWave;
        //}

        //// ************************************************************************* //
        //";
        //        }

        //        public static string FvSchemesOrtho40_60()
        //        {
        //            // An accurate numerical scheme on orthogonal (40-60) meshes
        //            return
        //        @"/*--------------------------------*- C++ -*----------------------------------*\
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
        //    object      fvSchemes;
        //}

        //// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        //ddtSchemes
        //{
        //    default         steadyState;
        //}

        //gradSchemes
        //{
        //    default cellMDLimited Gauss linear 0.5;
        //}

        //divSchemes
        //{
        //    default          Gauss linearUpwind;
        //    div(phi,U)       Gauss linearUpwind grad(U);

        //    //div(phi,k)       Gauss linearUpwind;
        //    //div(phi,epsilon) Gauss linearUpwind;
        //    div(phi,k)       Gauss linear;
        //    div(phi,epsilon) Gauss linear;
        //    div(phi,omega)   Gauss linearUpwind;
        //    div((nuEff*dev2(T(grad(U))))) Gauss linear;
        //    div(phi,time)   Gauss linearUpwind grad(U);
        //}

        //laplacianSchemes
        //{
        //    default         Gauss linear limited 1.0;
        //    laplacian(nuEff,time) Gauss linear limited 1.0;
        //}

        //interpolationSchemes
        //{
        //    default         linear;
        //}

        //snGradSchemes
        //{
        //    default         limited 1.0;
        //}

        //fluxRequired
        //{
        //    default         no;
        //    p;
        //}
        //wallDist
        //{
        //	method meshWave;
        //}

        //// ************************************************************************* //
        //";
        //        }

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

        public static string GetRelaxationFactors(OFRunSettings RunSettings)
        {
            StringBuilder sb = new StringBuilder();

            if (RunSettings.relaxationFactors == RelaxationFactors.Fast) { sb.Append(@"relaxationFactors
{
    fields
    {
        p               0.4;
        aoa             0.5;
    }
    equations
    {
        U               0.6;
        k               0.6;
        epsilon         0.6;
	    omega			0.6;
    }
}"); }
            else if (RunSettings.relaxationFactors == RelaxationFactors.Fluent)
            {
                sb.Append(@"relaxationFactors
{
    fields
    {
        p               0.7;
        aoa             0.5;
    }
    equations
    {
        U               0.3;
        k               0.3;
        epsilon         0.3;
	    omega			0.3;
    }
}"
        );
            }
            else if (RunSettings.relaxationFactors == RelaxationFactors.Robust) { sb.Append(@"relaxationFactors
{
    fields
    {
       p               0.3;
       aoa             0.5;
    }
    equations
    {
       U               0.1;
       k               0.1;
       epsilon         0.1;
	   omega		   0.1;
    }
}"); }
            else if (RunSettings.relaxationFactors == RelaxationFactors.Optimized) { sb.Append(@"relaxationFactors
{
    fields
    {
		p               0.3;
        aoa             0.5;
    }

    equations
    {
        U               0.3;
        k               0.1;
        omega           0.1;
		epsilon         0.1;
    }
}
"); }

            return sb.ToString();
        }

        //        relaxationFactors
        //{
        //    fields
        //    {
        //        p_rgh           0.3;
        //		p               0.3;
        //        AoA             0.5;
        //    }

        //    equations
        //    {
        //        U               0.3;
        //        T               0.5;
        //        h               0.3;
        //        rho             0.3;
        //        k               0.1;
        //        omega           0.1;
        //		epsilon         0.1;
        //    }

        //}

        //    AoA
        //{
        //    solver GAMG;
        //    tolerance       1e-7;
        //    relTol          1e-8;
        //    nPreSweeps      0;
        //    nPostSweeps     2;
        //    cacheAgglomeration true;
        //    smoother GaussSeidel;
        //    agglomerator faceAreaPair;
        //    nCellsInCoarsestLevel 10;
        //    mergeLevels     1;
        //    maxIter         100;
        //}

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
        tolerance       1e-8;
        relTol          0.01;
    }

	Phi
    {
        solver          GAMG;
        smoother        GaussSeidel;
        tolerance       1e-8;
        relTol          0.01;
    }
    aoa
    {
    solver          PBiCG;
    preconditioner  DILU;
    tolerance       1e-05;
    relTol          0.1;
    minIter 1;
    maxIter 10;
    }

    ""(U|k|omega|epsilon)""
    {
                solver smoothSolver;
                smoother symGaussSeidel;
                tolerance       1e-5;
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

    nCorrectors     2;
    nNonOrthogonalCorrectors 2;
    pRefCell        0;
    pRefValue       0;
}

potentialFlow
{
    nNonOrthogonalCorrectors 5;
}

cache
{
    grad(U);
}

");

            return sb.ToString();
        }

        public static string FvSolutionOptimized(OFRunSettings RunSettings)
        {
            StringBuilder sb = new StringBuilder(); sb.Append(@"

/*--------------------------------*- C++ -*----------------------------------*\
  =========                 |
  \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox
   \\    /   O peration     | Website:  https://openfoam.org
    \\  /    A nd           | Version:  8
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
        solver          PCG;
        preconditioner  DIC;
        tolerance       1e-12;
        relTol          0.01;
        minIter         1;
    }

    U
    {
        solver          PBiCGStab;
        preconditioner  DILU;
        tolerance       1e-12;
        relTol          0.1;
        minIter         1;
    }
    Phi
    {
        solver GAMG;
        smoother DIC;
        cacheAgglomeration on;
        agglomerator faceAreaPair;
        nCellsInCoarsestLevel 10;
        mergeLevels 1;
        tolerance 1e-12;
        relTol 0.01;
    }
    k
    {
        solver          PBiCGStab;
        preconditioner  DILU;
        tolerance       1e-12;
        relTol          0.1;
        minIter         1;
    }

    omega
    {
        solver          PBiCGStab;
        preconditioner  DILU;
        tolerance       1e-12;
        relTol          0.1;
        minIter         1;
    }

	epsilon
    {
        solver          PBiCGStab;
        preconditioner  DILU;
        tolerance       1e-12;
        relTol          0.1;
        minIter         1;
    }
    aoa
    {
      solver              PBiCGStab;
      preconditioner      DILU;
      tolerance           1e-12;
      relTol              0.1;
      minIter             1;
      maxIter             10;
    }
}

SIMPLE
{
    residualControl
    {
	    p               1e-5;
        U               1e-4;
    }

    nCorrectors     2;
    nNonOrthogonalCorrectors 2;
    pRefCell            0;
    pRefValue           0;
}
cache
{
    grad(U);
}

potentialFlow
{
    nNonOrthogonalCorrectors 5;
}

");

            return sb.ToString();
        }

        public static string FvSolution(OFRunSettings RunSettings)
        {
            StringBuilder sb = new StringBuilder();

            if (RunSettings.schemes == fvSchemes.Default)
            {
                sb.AppendLine(FvSolutionDefault(RunSettings));
            }
            else
            {
                sb.AppendLine(FvSolutionOptimized(RunSettings));
            }

            sb.AppendLine(GetRelaxationFactors(RunSettings));

            return sb.ToString();
        }

        public static string surfaceFeaturesDict()
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
  =========                 |
  \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox
   \\    /   O peration     | Website:  https://openfoam.org
    \\  /    A nd           | Version:  8
     \\/     M anipulation  |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      surfaceFeaturesDict;
}

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

surfaces
(
    ""building.stl""
    ""ground.stl""
);

includedAngle    150;

subsetFeatures
{
    nonManifoldEdges yes;
    openEdges        yes;
}

trimFeatures
{
    minElem          0;
    minLen           0;
}

writeObj             yes;

// ************************************************************************* //
";
        }

        public static string TransportProperties()

        // sets the kinematic viscosity in L^2/T
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
            sb.Append(RunSettings.turbModel.ToString());
            sb.AppendLine(@";
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
        object banana;
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
