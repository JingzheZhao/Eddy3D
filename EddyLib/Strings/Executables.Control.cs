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
|  \\    /   O peration     | Version:  2.2.2                                 |
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
            application simpleFoam;
            startFrom latestTime;
            startTime       1;
            stopAt endTime;
            endTime         " + RunSettings.iter + @";
            deltaT          1;
            writeControl timeStep;
            writeInterval   " + RunSettings.writeInterval + @";
            purgeWrite      " + RunSettings.keepTimeSteps + @";
            writeFormat binary;
            writePrecision  8;
            writeCompression uncompressed;
            timeFormat general;
            timePrecision   6;
            runTimeModifiable true;
            functions
{
#includeFunc residuals
");

            sb.AppendLine(FunctionObjCP(DOM, RunSettings, topologies, numberOfTopologies));
            sb.AppendLine(FunctionObjFieldMinMax());
            sb.AppendLine(FunctionObjFieldAverage());
            if (RunSettings.aoa_domain == true)
            {
                sb.AppendLine(EddyLib.Strings.OFExecDicts.FunctionObjAOA());
            }

            sb.AppendLine(@"};");

            return sb.ToString();
        }
    }
}
