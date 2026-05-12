using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace EddyLib.Strings
{
    /// <summary>
    /// OpenFOAM control dictionary templates.
    /// </summary>
    public partial class OFExecDicts
    {
        /// <summary>
        /// Generates the controlDict for OpenFOAM simulations.
        /// </summary>
        public static string ControlDict(OFRunSettings RunSettings, OFBaseDomain DOM, List<Mesh> topologies, int numberOfTopologies)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  12                                    |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
        version     2.0;
        format ascii;
        class dictionary;
        object controlDict;
}

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
libs
(
        ""libOpenFOAM.so""
        ""libutilityFunctionObjects.so""
        ""libsolverFunctionObjects.so""
        ""libatmosphericModels.so""");
            sb.Append(@"
);
            application foamRun;
            solver      incompressibleFluid;
            startFrom startTime;
            startTime       0;
            stopAt endTime;
            endTime         " + RunSettings.endTime + @";
            deltaT          1;
            writeControl timeStep;
            writeInterval   " + RunSettings.writeInterval + @";
            purgeWrite      " + RunSettings.purgeWrite + @";
            writeFormat binary;
            writePrecision  8;
            writeCompression uncompressed;
            timeFormat general;
            timePrecision   6;
            runTimeModifiable true;
");

            return sb.ToString();
        }

        /// <summary>
        /// Generates OpenFOAM 12 system/functions content.
        /// </summary>
        public static string FunctionsDict(OFRunSettings RunSettings, OFBaseDomain DOM, List<Mesh> topologies, int numberOfTopologies)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  12                                    |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
        version     2.0;
        format ascii;
        class dictionary;
        object functions;
}

#includeFunc residuals
");

            sb.AppendLine(FunctionObjCP(DOM, RunSettings, topologies, numberOfTopologies));
            if (RunSettings.debugMode)
            {
                sb.AppendLine(FunctionObjFieldMinMax());
                sb.AppendLine(FunctionObjFieldAverage());
            }
            sb.AppendLine(FunctionObjStabilityLimiters(RunSettings));
            if (RunSettings.aoa_domain == true)
            {
                sb.AppendLine(EddyLib.Strings.OFExecDicts.FunctionObjAOA());
            }

            return sb.ToString();
        }
    }
}
