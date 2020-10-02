using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    public class ControlDict : GenericDict
    {
        public string fullDict;

        public ControlDict()
        {
            this.fullDict = @"
/*---------------------------------------------------------------------------*\
|=========                 |                                                  |
|\\      /   F ield        | OpenFOAM: The Open Source CFD Toolbox            |
| \\    /    O peration    | Version:  1912                                   |
|  \\  /     A nd          | Web:      www.OpenFOAM.org                       |
|   \\/      M anipulation |                                                  |
\*---------------------------------------------------------------------------*/

FoamFile
{
    version         1912;
    format          ascii;
    class           dictionary;
    location        ""system"";
    object          controlDict;
}

application     buoyantSimpleFoam;

startFrom       startTime;

startTime       0;

stopAt          endTime;

endTime         3000;

deltaT          1;

writeControl    timeStep;

writeInterval   1;

purgeWrite      3;

writeFormat     binary;

writePrecision  9;

writeCompression off;

timeFormat      general;

timePrecision   6;

runTimeModifiable true;

functions
{
#includeFunc residuals
    AoA
    {
        type            scalarTransport;
        libs
        (
            ""libsolverFunctionObjects.dll""
        );

        writeControl    outputTime;
        D               1.0;
        field           AoA;
        resetOnStartUp  false;
        schemesField    AoA;
        bounded01       true;
        write           true;
        fvOptions
        {
            IncrementTime
            {
                type            scalarSemiImplicitSource;
                cellZone        all;
                scalarSemiImplicitSourceCoeffs
                {
                    volumeMode      specific;
                    selectionMode   all;
                    injectionRateSuSp
                    {
                        AoA             (1 0);
                    }
                }
            }
        }
    }
}

";
        }
    }
}