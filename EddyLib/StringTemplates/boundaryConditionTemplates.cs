using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;


namespace EddyLib
{
    public class BoundaryConditionTemplates
    {
        public static string ABLConditions_Cyl(OFBaseDomain DOM, int d)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
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


        Uref		" + DOM.BCInflow.URef + @";

        Zref		" + DOM.BCInflow.zref + @";

        z0 uniform " + DOM.BCInflow.z0 + @";

        flowDir (" + DOM.BCInflow.flowDir[d].X + " " + DOM.BCInflow.flowDir[d].Y + " " + DOM.BCInflow.flowDir[d].Z + @");

        zDir (0 0 1);

        zGround uniform " + DOM.BCInflow.zGround + @";

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
"
        );

            return sb.ToString();
        }

        
        public static string Epsilon_Cyl(OFBaseDomain DOM, int d)
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

   

            for (int i = 0; i < DOM.side.Faces.Count; i++) {
                double dot = DOM.BCInflow.flowDir[d] * DOM.side.FaceNormals[i]; //check
                if (dot > 0) {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type atmBoundaryLayerInletEpsilon;
        #include	""ABLConditions"";
}"); }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
          type inletOutlet;
inletValue uniform $turbulentEpsilon;
        value		$internalField;
    }"); } }


  
    sb.AppendLine(@"

}


// ************************************************************************* //
");
            return sb.ToString();
        }
        public static string K_Cyl(OFBaseDomain DOM, int d)
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
                symmetry
    {
                    type symmetry;
                }

             ");

   

            for (int i = 0; i < DOM.side.Faces.Count; i++) {
                double dot = DOM.BCInflow.flowDir[d] * DOM.side.FaceNormals[i]; //check
                if (dot > 0) {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{      type atmBoundaryLayerInletK;
#include	""ABLConditions"";
}"); }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
                       type inletOutlet;
                    inletValue uniform $turbulentKE;
                    value       $internalField;
    }"); } }


  
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
        public static string Omega_Cyl(OFBaseDomain DOM, int d)
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

   

            for (int i = 0; i < DOM.side.Faces.Count; i++) {
                double dot = DOM.BCInflow.flowDir[d] * DOM.side.FaceNormals[i]; //check
                if (dot > 0) {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{  	type fixedValue;
value	$internalField;
}"); }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
           type inletOutlet;
inletValue	$internalField;
value		$internalField;

    }"); } }


  
    sb.AppendLine(@"

}


// ************************************************************************* //
");
            return sb.ToString();

        }
        public static string P_Cyl(OFBaseDomain DOM, int d)
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

            symmetry
    {
                type symmetry;
            }



            building
    {
                type zeroGradient;
            }


 ");

   

            for (int i = 0; i < DOM.side.Faces.Count; i++) {
                double dot = DOM.BCInflow.flowDir[d] * DOM.side.FaceNormals[i]; //check
                if (dot > 0) {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"{ type zeroGradient;}"); }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
    {
                type fixedValue;
                value uniform $pressure;
    }"); } }


  
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
        ")       ;
            return sb.ToString();

        }

        public static string UCylConstU(OFBaseDomain DOM, int d)
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


");

            for (int i = 0; i < DOM.side.Faces.Count; i++)
            {
                double dot = DOM.BCInflow.flowDir[d] * DOM.side.FaceNormals[i]; //check
                if (dot > 0)
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"{type            fixedValue;
value           uniform (" + DOM.BCInflow.flowDir[d].X* DOM.BCInflow.URef + " " + DOM.BCInflow.flowDir[d].Y* DOM.BCInflow.URef + " " + DOM.BCInflow.flowDir[d].Z* DOM.BCInflow.URef + @" );
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

   
        public static string U_CylABL(OFBaseDomain DOM, int d)
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


");

            for (int i = 0; i < DOM.side.Faces.Count; i++) {
                double dot = DOM.BCInflow.flowDir[d] * DOM.side.FaceNormals[i]; //check
                if (dot > 0) {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"{ type atmBoundaryLayerInletVelocity;
        #include ""ABLConditions"";
}"); }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
    {
        type inletOutlet;
        value $internalField;
        inletValue uniform (0 0 0);
    }"); } }


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
        public static string Nut_Cyl(OFBaseDomain DOM, int d)
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
    symmetry
    {
        type symmetry;
    }
");

   

            for (int i = 0; i < DOM.side.Faces.Count; i++) {
                double dot = DOM.BCInflow.flowDir[d] * DOM.side.FaceNormals[i]; //check
                if (dot > 0) {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@" 
{      
type calculated; 
value uniform 0;
}"); }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
{
type calculated;
value uniform 0; 
}"); } }


  
    sb.AppendLine(@"ground
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
");
            return sb.ToString();
        }
         public static string ABLConditions(OFBaseDomain DOM, int d)
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
        Uref		" + DOM.BCInflow.URef + @";
        Zref		" + DOM.BCInflow.zref + @";
        z0 uniform " + DOM.BCInflow.z0 + @";
        flowDir (" + DOM.BCInflow.flowDir[d].X +" "+ DOM.BCInflow.flowDir[d].Y +" "+ DOM.BCInflow.flowDir[d].Z+ @");
        zDir (0 0 1);
        zGround uniform " + DOM.BCInflow.zGround + @";
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
flowVelocity (" + DOM.BCInflow.flowDir[d].X * DOM.BCInflow.URef + " "+ DOM.BCInflow.flowDir[d].Y* DOM.BCInflow.URef + " "+ DOM.BCInflow.flowDir[d].Z * DOM.BCInflow.URef + @");
pressure    0;
turbulentKE "      + Math.Round(DOM.BCInflow.k,4)          + @";
turbulentEpsilon "  + Math.Round(DOM.BCInflow.epsilon,4)    + @";
turbulentOmega	"  + Math.Round(DOM.BCInflow.omega,4)      + @";
#inputMode		merge;
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";
        }
        public static string Epsilon(OFBaseDomain DOM)
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
        public static string K(OFBaseDomain DOM)
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
            ";
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
symmetry
    {
        type symmetry;
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
symmetry
    {
        type symmetry;
}
inlet
    {
");
            

                sb.Append(@"type fixedValue;
        value uniform (" + DOM.BCInflow.flowDir[i].X * DOM.BCInflow.URef + " " + DOM.BCInflow.flowDir[i].Y * DOM.BCInflow.URef + " " + DOM.BCInflow.flowDir[i].Z * DOM.BCInflow.URef + @");
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
ground_perim
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
