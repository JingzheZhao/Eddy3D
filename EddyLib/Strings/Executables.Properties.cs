using System.Text;

namespace EddyLib.Strings
{
    /// <summary>
    /// OpenFOAM transport and turbulence property templates.
    /// </summary>
    public partial class OFExecDicts
    {
        /// <summary>
        /// Generates surfaceFeaturesDict.
        /// </summary>
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

        /// <summary>
        /// Generates transportProperties (kinematic viscosity).
        /// </summary>
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

        /// <summary>
        /// Generates turbulenceProperties based on turbulence model.
        /// </summary>
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

        /// <summary>
        /// Generates residuals function object dictionary.
        /// </summary>
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

        /// <summary>
        /// Generates decomposeParDict for parallel runs.
        /// </summary>
        public static string DecomposeParDict(OFRunSettings RunSettings)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"// * * * * * * * * * * //
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
