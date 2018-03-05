using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;


namespace Eddy
{
    public class BoundaryConditionTemplates
    {
        public static string ABLConditions(BoundaryConditions BCInflow )
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


        Uref		" + BCInflow.U + @";

        Zref		" + BCInflow.zref + @";

        z0 uniform " + BCInflow.z0 + @";

        flowDir (" + BCInflow.flowDir.X + BCInflow.flowDir.Y + BCInflow.flowDir.Z+ @";);

        zDir (0 0 1);

        zGround uniform " + BCInflow.zGround + @";

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";
        }
        public static string InitialConditions()
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


flowVelocity (0 12 0);

pressure		0;

turbulentKE		0.03456;

turbulentEpsilon	0.0835;

turbulentOmega		20;

#inputMode		merge;

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";
        }
        public static string Epsilon()
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
    location    ""0"";
    object epsilon;
        }
        // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        dimensions [0 2 -3 0 0 0 0];

#include		""initialConditions"";

internalField uniform $turbulentEpsilon;

boundaryField
{
    symmetry
    {
        type symmetry;
    }

    ground
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
    inlet
    {
	type atmBoundaryLayerInletEpsilon;
        #include	""ABLConditions"";

    }
    outlet
    {
        type inletOutlet;
inletValue uniform $turbulentEpsilon;
        value		$internalField;

    }

}


// ************************************************************************* //
";
        }
        public static string K()
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
    object      k;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //



dimensions      [0 2 -2 0 0 0 0];

#include		""initialConditions"";

internalField uniform $turbulentKE;

            boundaryField
{
                symmetry
    {
                    type symmetry;
                }

                outlet
    {
                    type inletOutlet;
                    inletValue uniform $turbulentKE;
                    value       $internalField;
                }
                inlet
    {
                    type atmBoundaryLayerInletK;
# include	""ABLConditions"";
                }
                ground
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
            ";
        }
        public static string Omega()
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
    symmetry
    {
        type symmetry;
    }

    ground
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
        public static string P()
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

            symmetry
    {
                type symmetry;
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

        }

        // ************************************************************************* //
        ";

        }
        public static string U(BoundaryConditions BCInflow)
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

symmetry
    {
        type symmetry;
}


inlet
    {");
            if (BCInflow.btype == BoundaryType.abl) {
                sb.Append(@"type atmBoundaryLayerInletVelocity;
        #include ""ABLConditions"";");
            }
            else {
                sb.Append(@"type fixedValue;
        value uniform ("+ BCInflow.flowDir.X + BCInflow.flowDir.Y + BCInflow.flowDir.Z+ @");");
}
       

    
        
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
        public static string Nut()
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
    symmetry
    {
        type symmetry;
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
# include	""ABLConditions"";
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
}
