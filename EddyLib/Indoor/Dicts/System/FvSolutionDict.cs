using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    public class FvSolutionDict : GenericDict
    {
        public FvSolutionDict()
        {
            this.Name = "fvSolution";

            this.Header = GetHeader(this);
            this.Location = DictLocation.system;

            this.FullDictString = @"FoamFile
{
    version         1912;
    format          ascii;
    class           dictionary;
    location        ""system"";
    object          fvSolution;
}

solvers
{
    p_rgh
    {
        solver          PCG;
        preconditioner  DIC;
        tolerance       1e-8;
        relTol          0.01;
    }

    U
    {
        solver          PBiCGStab;
        preconditioner  DILU;
        tolerance       1e-5;
        relTol          0.1;
    }

    h
    {
        solver          PBiCGStab;
        preconditioner  DILU;
        tolerance       1e-5;
        relTol          0.1;
    }

    k
    {
        solver          PBiCGStab;
        preconditioner  DILU;
        tolerance       1e-5;
        relTol          0.1;
    }

    omega
    {
        solver          PBiCGStab;
        preconditioner  DILU;
        tolerance       1e-5;
        relTol          0.1;
    }

    AoA
    {
        solver          GAMG;
        tolerance       1e-7;
        relTol          1e-8;
        nPreSweeps      0;
        nPostSweeps     2;
        cacheAgglomeration true;
        smoother        GaussSeidel;
        agglomerator    faceAreaPair;
        nCellsInCoarsestLevel 10;
        mergeLevels     1;
        maxIter         100;
    }
}

SIMPLE
{
    residualControl
    {
        p_rgh           1e-4;
        U               1e-3;
        h               1e-3;
    }

    nNonOrthogonalCorrectors 0;
    pRefCell        0;
    pRefValue       0;
}

relaxationFactors
{
    fields
    {
        p_rgh           0.3;
        AoA             0.5;
    }

    equations
    {
        U               0.3;
        T               0.5;
        h               0.3;
        rho             0.3;
        k               0.1;
        omega           0.1;
    }
}

";
        }
    }
}