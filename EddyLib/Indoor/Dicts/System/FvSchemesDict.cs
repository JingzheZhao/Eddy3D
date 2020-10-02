using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    public class FvSchemesDict : GenericDict
    {
        public string fullDict;

        public FvSchemesDict()
        {
            this.Name = "fvSchemes";

            this.Header = GetHeader(this);
            this.Location = DictLocation.system;

            this.FullDictString = @"FoamFile
{
    version         1912;
    format          ascii;
    class           dictionary;
    location        ""system"";
    object          fvSchemes;
}

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
    div(((rho*nuEff)*dev2(T(grad(U))))) Gauss linear;
    div(phi,k)      bounded Gauss upwind;
    div(phi,omega)  bounded Gauss upwind;
    div(phi,AoA)    bounded Gauss upwind;
}

laplacianSchemes
{
    default         Gauss linear corrected;
    laplacian(DkEff,k) Gauss linear uncorrected;
    laplacian(DomegaEff,omega) Gauss linear uncorrected;
    laplacian(DAoA,AoA) Gauss linear limited 0.333;
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
}

wallDist
{
    method          meshWave;
}

";
        }
    }
}