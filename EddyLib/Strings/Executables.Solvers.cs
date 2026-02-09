using System.Text;

namespace EddyLib.Strings
{
    /// <summary>
    /// OpenFOAM fvSolution and relaxation templates.
    /// </summary>
    public partial class OFExecDicts
    {
        private static string GetSimpleConsistentLine(OFRunSettings runSettings)
        {
            return runSettings != null && runSettings.simpleConsistent
                ? "    consistent      yes;"
                : string.Empty;
        }

        /// <summary>
        /// Generates relaxation factors based on settings.
        /// </summary>
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

        /// <summary>
        /// Selects fvSolution based on run settings.
        /// </summary>
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

        /// <summary>
        /// Default fvSolution settings.
        /// </summary>
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
");
            sb.AppendLine(GetSimpleConsistentLine(RunSettings));
            sb.Append(@"    
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

        /// <summary>
        /// Optimized fvSolution settings for urban microclimate.
        /// </summary>
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
");
            sb.AppendLine(GetSimpleConsistentLine(RunSettings));
            sb.Append(@"    
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
    }
}
