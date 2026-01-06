using EddyLib.BCs;
using System;
using System.Text;

namespace EddyLib.Strings

{
    public partial class BCDicts
    {
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
            if (!DOM.HasTerrain) { sb.Append(@"inlet
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
            if (!DOM.HasTerrain) { sb.Append(@" inlet
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

        public static string UBoxConstU(OFBaseDomain DOM, int d)
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
        value uniform (" + DOM.BCond.BCs[d].flowDir.X * DOM.BCond.BCs[d].URef + " " + DOM.BCond.BCs[d].flowDir.Y * DOM.BCond.BCs[d].URef + " " + DOM.BCond.BCs[d].flowDir.Z * DOM.BCond.BCs[d].URef + @");
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

        public static string AOA()
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
    object aoa;
}

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
dimensions      [0 0 0 1 0 0 0];

internalField   uniform 0;

boundaryField
{
   frontAndBack
    {
        type zeroGradient;
    }
    ground
    {
         type zeroGradient;
    }
ground_perim
    {
          type zeroGradient;
    }
building
    {
           type zeroGradient;
    }
	inlet
    {
          type            fixedValue;
        value           uniform 0;
    }
    outlet
    {
     type zeroGradient;
    }
}

";
        }

        #endregion Box
    }
}
