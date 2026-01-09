using EddyLib.BCs;
using System;
using System.Text;

namespace EddyLib.Strings

{
    public partial class BCDicts
    {
        #region Helpers

        /// <summary>
        /// Generates standard OpenFOAM file header.
        /// </summary>
        private static string GetOpenFOAMHeader(string fieldClass, string objectName, string dimensions, string location = "0")
        {
            return $@"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.2.2                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{{
    version     2.0;
    format      ascii;
    class       {fieldClass};
    location    ""{location}"";
    object      {objectName};
}}

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

dimensions      {dimensions};

";
        }

        /// <summary>
        /// Calculates dot product of flow direction with face normal.
        /// </summary>
        private static double CalcFlowDot(OFCylDomain dom, int directionIndex, int faceIndex)
        {
            var flowDir = dom.BCond.BCs[directionIndex].flowDir;
            var normal = dom.sides.FaceNormals[faceIndex];
            return flowDir.X * normal.X + flowDir.Y * normal.Y;
        }

        /// <summary>
        /// Determines if a face is an inlet (facing the flow).
        /// </summary>
        private static bool IsInlet(OFCylDomain dom, int directionIndex, int faceIndex)
        {
            return CalcFlowDot(dom, directionIndex, faceIndex) < dotCutoff;
        }

        #endregion Helpers

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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y; //check
                if (dot < dotCutoff && !DOM.HasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type atmBoundaryLayerInletEpsilon;
        #include	""ABLConditions"";
}");
                }
                else if (dot >= dotCutoff && !DOM.HasTerrain)
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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y; //check
                if (dot < dotCutoff && DOM.HasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type fixedValue;
        value		$internalField;
}");
                }
                else if (dot >= dotCutoff && DOM.HasTerrain)
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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y; //check
                if (dot < dotCutoff && !DOM.HasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type atmBoundaryLayerInletK;
        #include	""ABLConditions"";
}");
                }
                else if (dot >= dotCutoff && !DOM.HasTerrain)
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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y; //check
                if (dot < dotCutoff && DOM.HasTerrain)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{	type fixedValue;
        value		$internalField;
}");
                }
                else if (dot >= dotCutoff && DOM.HasTerrain)
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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y;
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

        public static string AOA_Cyl(OFCylDomain DOM, int d)
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
    ");

            for (int i = 0; i < DOM.sides.Faces.Count; i++)
            {
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y;
                if (dot < dotCutoff)
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"{  	type fixedValue;
value	uniform 0;
}");
                }
                else
                {
                    sb.AppendLine("patch" + i);
                    sb.Append(@"
    {
          type zeroGradient;
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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y; //check
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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y; //check
                if (dot < dotCutoff)
                {
                    sb.AppendLine("patch" + i);
                    sb.AppendLine(@"
{   type            fixedValue;
    value           uniform (" + Utilities.FormatDouble(DOM.BCond.BCs[d].flowDir.X * DOM.BCond.BCs[d].URef) + " " + Utilities.FormatDouble(DOM.BCond.BCs[d].flowDir.Y * DOM.BCond.BCs[d].URef) + " " + Utilities.FormatDouble(DOM.BCond.BCs[d].flowDir.Z * DOM.BCond.BCs[d].URef) + @" );
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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y; //check
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
                double dot = DOM.BCond.BCs[d].flowDir.X * DOM.sides.FaceNormals[i].X + DOM.BCond.BCs[d].flowDir.Y * DOM.sides.FaceNormals[i].Y; //check
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
    }
}
