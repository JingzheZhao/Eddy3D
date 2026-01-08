namespace EddyLib.Strings
{
    /// <summary>
    /// OpenFOAM fvSchemes templates.
    /// </summary>
    public partial class OFExecDicts
    {
        /// <summary>
        /// Selects fvSchemes based on run settings.
        /// </summary>
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

        /// <summary>
        /// Optimized fvSchemes for urban microclimate simulations.
        /// </summary>
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

        /// <summary>
        /// Default fvSchemes.
        /// </summary>
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
    }
}
