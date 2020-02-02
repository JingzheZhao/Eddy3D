using System;
using System.Text;

namespace EddyLib.StrTemp
{
    public class BCDicts
    {
        #region Generic

        private static double dotCutoff = -0.1;

        public static string ABL(OFBaseDomain DOM, int d)
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  v3.0+                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       IOobject;
    location    ""0"";
    object ABLConditions;
        }
        // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
        Uref		" + DOM.BCond.URef + @";
        Zref		" + DOM.BCond.zref + @";
        z0 uniform " + DOM.BCond.z0 + @";
        flowDir (" + DOM.BCond.flowDir[d].X + " " + DOM.BCond.flowDir[d].Y + " " + DOM.BCond.flowDir[d].Z + @");
        zDir (0 0 1);
        zGround uniform " + DOM.BCond.zGround + @";
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";
        }

        public static string InitialConditions(OFBaseDomain DOM, int d)
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  v3.0+                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       IOobject;
    location    ""0"";
    object initialConditions;
        }
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
flowVelocity (" + DOM.BCond.flowDir[d].X * DOM.BCond.URef + " " + DOM.BCond.flowDir[d].Y * DOM.BCond.URef + " " + DOM.BCond.flowDir[d].Z * DOM.BCond.URef + @");
pressure    0;
turbulentKE " + Math.Round(DOM.BCond.k, 4) + @";
turbulentEpsilon " + Math.Round(DOM.BCond.epsilon, 4) + @";
turbulentOmega	" + Math.Round(DOM.BCond.omega, 4) + @";
#inputMode		merge;
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";
        }

        #endregion Generic

        #region Cyl

        public static string Epsilon_Cyl(OFCylDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"/*--------------------------------*- C++ -*----------------------------------*\
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
    class       volScalarField;
    location    ""0"";
    object epsilon;
        }
        // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        dimensions [0 2 -3 0 0 0 0];

#include		""initialConditions"";

internalField uniform $turbulentEpsilon;

boundaryField
{
    frontAndBack
    {
        type slip;
    }

    ground
    {
        type epsilonWallFunction;
        Cmu             0.09;
        kappa           0.4;
        E               9.8;
        value           $internalField;
    }
ground_perim
    {
        type epsilonWallFunction;
        Cmu             0.09;
        kappa           0.4;
        E               9.8;
        value           $internalField;
    }

building
    {
         type epsilonWallFunction;
value		$internalField;
    }
   ");

            // Without terrain

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i]; //check
                if (dot < dotCutoff && !DOM.hasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type atmBoundaryLayerInletEpsilon;
        #include	""ABLConditions"";
}");
                }
                else if (dot >= dotCutoff && !DOM.hasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
          type inletOutlet;
inletValue $internalField;
        value		$internalField;
    }");
                }
            }

            // With terrain

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i]; //check
                if (dot < dotCutoff && DOM.hasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type fixedValue;
        value		$internalField;
}");
                }
                else if (dot >= dotCutoff && DOM.hasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
          type inletOutlet;
         inletValue $internalField;
         value		$internalField;
    }");
                }
            }

            sb.AppendLine(@"
}

// ************************************************************************* //
");
            return sb.ToString();
        }

        public static string K_Cyl(OFCylDomain DOM, int d)
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
    format      ascii;
    class       volScalarField;
    object      k;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

dimensions      [0 2 -2 0 0 0 0];

#include		""initialConditions"";

internalField uniform $turbulentKE;

            boundaryField
{
                frontAndBack
    {
                    type slip;
                }

             ");

            // Without terrain

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i]; //check
                if (dot < dotCutoff && !DOM.hasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type atmBoundaryLayerInletK;
        #include	""ABLConditions"";
}");
                }
                else if (dot >= dotCutoff && !DOM.hasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
          type inletOutlet;
inletValue $internalField;
        value		$internalField;
    }");
                }
            }

            // With terrain

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i]; //check
                if (dot < dotCutoff && DOM.hasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type fixedValue;
        value		$internalField;
}");
                }
                else if (dot >= dotCutoff && DOM.hasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
          type inletOutlet;
         inletValue $internalField;
         value		$internalField;
    }");
                }
            }

            sb.AppendLine(@"
                ground
    {
                    type kqRWallFunction;
                    value       $internalField;
                }
ground_perim
    {
                    type kqRWallFunction;
                    value       $internalField;
                }

                building
    {
                    type kqRWallFunction;
                    value       $internalField;
                }
            }

            // ************************************************************************* //
            ");
            return sb.ToString();
        }

        public static string Omega_Cyl(OFCylDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"
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
    class volScalarField;
    location    ""0"";
    object omega;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

dimensions [0 0 -1 0 0 0 0];

#include		""initialConditions"";

internalField uniform $turbulentOmega;

boundaryField
{
    frontAndBack
    {
        type slip;
    }

    ground
    {
        type omegaWallFunction;
        Cmu             0.09;
        kappa           0.4;
        E               9.8;
        value           $internalField;
    }
 ground_perim
    {
        type omegaWallFunction;
        Cmu             0.09;
        kappa           0.4;
        E               9.8;
        value           $internalField;
    }

    building
    {
         type omegaWallFunction;
value		$internalField;
    }
    ");

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i];
                if (dot < dotCutoff)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{  	type fixedValue;
value	$internalField;
}");
                }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
           type inletOutlet;
inletValue	$internalField;
value		$internalField;
    }");
                }
            }

            sb.AppendLine(@"
}

// ************************************************************************* //
");
            return sb.ToString();
        }

        public static string P_Cyl(OFCylDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"/*--------------------------------*- C++ -*----------------------------------*\
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
    class       volScalarField;
    object      p;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

dimensions      [0 2 -2 0 0 0 0];

#include		""initialConditions"";

internalField uniform $pressure;

        boundaryField
{
            frontAndBack
    {
                type slip;
            }

            building
    {
                type zeroGradient;
            }

 ");

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i]; //check
                if (dot < dotCutoff)
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"{ type zeroGradient;}");
                }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
    {
                type fixedValue;
                value uniform $pressure;
    }");
                }
            }

            sb.AppendLine(@"

            ground
    {
                type zeroGradient;
            }
 ground_perim
    {
                type zeroGradient;
            }
        }

        // ************************************************************************* //
        ");
            return sb.ToString();
        }

        public static string UCylConstU(OFCylDomain DOM, int d)
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
    format      ascii;
    class       volVectorField;
    location    ""0"";
    object      U;
    }
    // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

dimensions [0 1 -1 0 0 0 0];

#include ""initialConditions"";

internalField uniform $flowVelocity;

boundaryField
{
frontAndBack
{
        type slip;
}

");

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i]; //check
                if (dot < dotCutoff)
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
{   type            fixedValue;
    value           uniform (" + DOM.BCond.flowDir[d].X * DOM.BCond.URef + " " + DOM.BCond.flowDir[d].Y * DOM.BCond.URef + " " + DOM.BCond.flowDir[d].Z * DOM.BCond.URef + @" );
}");
                }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
{
    type inletOutlet;
    value $internalField;
    inletValue uniform (0 0 0);
}");
                }
            }

            sb.AppendLine(@"
ground
    {
        type fixedValue;
        value uniform (0 0 0);
    }

ground_perim
    {
        type fixedValue;
        value uniform (0 0 0);
    }

building
    {
        type fixedValue;
        value uniform (0 0 0);
    }
}

// ************************************************************************* //
");
            return sb.ToString();
        }

        public static string U_CylABL(OFCylDomain DOM, int d)
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
    format      ascii;
    class       volVectorField;
    location    ""0"";
    object      U;
    }
    // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

dimensions [0 1 -1 0 0 0 0];

#include ""initialConditions"";

internalField uniform $flowVelocity;

boundaryField
{
frontAndBack
    {
        type slip;
}

");

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i]; //check
                if (dot < dotCutoff)
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"{ type atmBoundaryLayerInletVelocity;
        #include ""ABLConditions"";
}");
                }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
    {
        type inletOutlet;
        value $internalField;
        inletValue uniform (0 0 0);
    }");
                }
            }

            //            if (BCInflow.btype == BoundaryType.abl) {
            //            }
            //            else {
            //                sb.Append(@"type fixedValue;
            //        value uniform ("+ BCInflow.flowDir.X +" "+ BCInflow.flowDir.Y +" "+ BCInflow.flowDir.Z+ @");");
            //}

            sb.AppendLine(@"
ground
    {
        type fixedValue;
        value uniform (0 0 0);
    }

ground_perim
    {
        type fixedValue;
        value uniform (0 0 0);
    }

building
    {
        type fixedValue;
        value uniform (0 0 0);
    }
}

// ************************************************************************* //
");
            return sb.ToString();
        }

        public static string Nut_Cyl(OFCylDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(@"/*--------------------------------*- C++ -*----------------------------------*\
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
    class volScalarField;
    location    ""0"";
    object nut;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

dimensions [0 2 -1 0 0 0 0];

#include		""initialConditions"";

internalField uniform $turbulentKE;

boundaryField
{
    frontAndBack
    {
        type calculated;
        value uniform 0;
    }
");

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.flowDir[d] * DOM.sides.FaceNormals[i]; //check
                if (dot < dotCutoff)
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
{
type calculated;
value uniform 0;
}");
                }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
{
type calculated;
value uniform 0;
}");
                }
            }

            sb.AppendLine(@"ground
    {
        type nutkAtmRoughWallFunction;
#include	""ABLConditions"";
     value		uniform 0;
    }
ground_perim
    {
        type nutkAtmRoughWallFunction;
#include	""ABLConditions"";
     value		uniform 0;
    }
    building
    {
        type nutUSpaldingWallFunction;
value uniform 0;
    }
}

// ************************************************************************* //
");
            return sb.ToString();
        }

        #endregion Cyl

        #region Box

        public static string Epsilon(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(
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
    class       volScalarField;
    location    ""0"";
    object epsilon;
        }
        // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
        dimensions [0 2 -3 0 0 0 0];
#include		""initialConditions"";
internalField uniform $turbulentEpsilon;
boundaryField
{
    frontAndBack
    {
        type slip;
    }
    ground
    {
        type epsilonWallFunction;
        Cmu             0.09;
        kappa           0.4;
        E               9.8;
        value           $internalField;
    }
ground_perim
    {
        type epsilonWallFunction;
        Cmu             0.09;
        kappa           0.4;
        E               9.8;
        value           $internalField;
    }
building
    {
         type epsilonWallFunction;
         value		$internalField;
    }");
            if (!DOM.hasTerrain) { sb.Append(@"inlet
    {
                    type atmBoundaryLayerInletEpsilon;
# include	""ABLConditions"";
                }
                "); }
            else { sb.Append(@"inlet
    {
                    type    fixedValue;
                    value   $internalField;
                }
                "); }

            sb.Append(@"outlet
    {
        type    inletOutlet;
        inletValue  $internalField;
        value   $internalField;
    }
}
// ************************************************************************* //
");
            return sb.ToString();
        }

        public static string K(OFBaseDomain DOM)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(
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
    class       volScalarField;
    object      k;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
dimensions      [0 2 -2 0 0 0 0];
#include		""initialConditions"";
internalField uniform $turbulentKE;
            boundaryField
{
                frontAndBack
    {
                    type slip;
                }
                outlet
    {
                    type inletOutlet;
                    inletValue uniform $turbulentKE;
                    value       $internalField;
    }
               ");
            if (!DOM.hasTerrain) { sb.Append(@" inlet
                {
                    type atmBoundaryLayerInletK;
                    #include	""ABLConditions"";
                }"); }
            else { sb.Append(@"inlet
    {
                    type    fixedValue;
                    value   $internalField;
                }
                "); }
            sb.Append(@"
                ground
    {
                    type kqRWallFunction;
                    value       $internalField;
                }
 ground_perim
    {
                    type kqRWallFunction;
                    value       $internalField;
                }
                building
    {
                    type kqRWallFunction;
                    value       $internalField;
                }
            }
            // ************************************************************************* //
            ");
            return sb.ToString();
        }

        public static string Omega(OFBaseDomain DOM)
        {
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
    class volScalarField;
    location    ""0"";
    object omega;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
dimensions [0 0 -1 0 0 0 0];
#include		""initialConditions"";
internalField uniform $turbulentOmega;
boundaryField
{
    frontAndBack
    {
        type slip;
    }
    ground
    {
        type omegaWallFunction;
        Cmu             0.09;
        kappa           0.4;
        E               9.8;
        value           $internalField;
    }
ground_perim
    {
        type omegaWallFunction;
        Cmu             0.09;
        kappa           0.4;
        E               9.8;
        value           $internalField;
    }
    building
    {
         type omegaWallFunction;
value		$internalField;
    }
    inlet
    {
	type fixedValue;
value	$internalField;
    }
    outlet
    {
        type inletOutlet;
inletValue	$internalField;
        value		$internalField;
    }
}
// ************************************************************************* //
";
        }

        public static string P(OFBaseDomain DOM)
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
    class       volScalarField;
    object      p;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
dimensions      [0 2 -2 0 0 0 0];
#include		""initialConditions"";
internalField uniform $pressure;
        boundaryField
{
            frontAndBack
    {
                type slip;
            }
            building
    {
                type zeroGradient;
            }
            inlet
    {
                type zeroGradient;
            }
            outlet
    {
                type fixedValue;
                value uniform $pressure;
            }
            ground
    {
                type zeroGradient;
            }
ground_perim
    {
                type zeroGradient;
            }
        }
        // ************************************************************************* //
        ";
        }

        public static string UBoxABL(OFBaseDomain DOM, int i)
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
    format      ascii;
    class       volVectorField;
    location    ""0"";
    object      U;
    }
    // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
dimensions [0 1 -1 0 0 0 0];
#include ""initialConditions"";
internalField uniform $flowVelocity;
boundaryField
{
frontAndBack
    {
        type slip;
}
inlet
    {");
            sb.AppendLine(@"type    atmBoundaryLayerInletVelocity;
                #include ""ABLConditions"";
}");

            sb.Append(@"
outlet
    {
        type inletOutlet;
        value $internalField;
        inletValue uniform (0 0 0);
    }
ground
    {
        type fixedValue;
        value uniform (0 0 0);
    }
ground_perim
    {
        type fixedValue;
        value uniform (0 0 0);
    }
building
    {
        type fixedValue;
        value uniform (0 0 0);
    }
}
// ************************************************************************* //
");
            return sb.ToString();
        }

        public static string UBoxConstU(OFBaseDomain DOM, int i)
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
    format      ascii;
    class       volVectorField;
    location    ""0"";
    object      U;
    }
    // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
dimensions [0 1 -1 0 0 0 0];
#include ""initialConditions"";
internalField uniform $flowVelocity;
boundaryField
{
frontAndBack
    {
        type slip;
}
inlet
    {
");

            sb.Append(@"type fixedValue;
        value uniform (" + DOM.BCond.flowDir[i].X * DOM.BCond.URef + " " + DOM.BCond.flowDir[i].Y * DOM.BCond.URef + " " + DOM.BCond.flowDir[i].Z * DOM.BCond.URef + @");
}");

            sb.Append(@"
outlet
    {
        type inletOutlet;
        value $internalField;
        inletValue uniform (0 0 0);
    }
ground
    {
        type fixedValue;
        value uniform (0 0 0);
    }
ground_perim
    {
        type fixedValue;
        value uniform (0 0 0);
    }
building
    {
        type fixedValue;
        value uniform (0 0 0);
    }
}
// ************************************************************************* //
");
            return sb.ToString();
        }

        public static string Nut(OFBaseDomain DOM)
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
    format ascii;
    class volScalarField;
    location    ""0"";
    object nut;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
dimensions [0 2 -1 0 0 0 0];
#include		""initialConditions"";
internalField uniform $turbulentKE;
boundaryField
{
    frontAndBack
    {
      	type calculated;
        value uniform 0;
    }
    outlet
    {
	type calculated;
value uniform 0;
    }
    inlet
    {
        type calculated;
value uniform 0;
    }
    ground
    {
        type nutkAtmRoughWallFunction;
#include	""ABLConditions"";
value uniform 0;
    }
ground_perim
    {
        type nutkAtmRoughWallFunction;
#include	""ABLConditions"";
value uniform 0;
    }
    building
    {
        type nutUSpaldingWallFunction;
value uniform 0;
    }
}
// ************************************************************************* //
";
        }
    }

    #endregion Box
}