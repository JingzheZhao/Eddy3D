namespace EddyLib.Indoor.Dicts
{
    public class ControlDict : GenericDict
    {
        public ControlDict()
        {
            this.Name = "controlDict";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);
            this.FullDictString = @"
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