using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace EddyLib.Strings
{
    /// <summary>
    /// OpenFOAM function object templates.
    /// </summary>
    public partial class OFExecDicts
    {
        /// <summary>
        /// Generates Age of Air function object.
        /// </summary>
        public static string FunctionObjAOA()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"aoa
    {
        type            scalarTransport;
        libs (""libfieldFunctionObjects.so"");

        writeControl    outputTime;
            D               1.0;
            field aoa;
            resetOnStartUp  false;
            schemesField aoa;
            bounded01       true;
            write           true;

            fvOptions
        {
                aoa_00
            {
                    type semiImplicitSource;
                    active          true;
					selectionMode all;
					volumeMode specific;
                    sources
                {
                        aoa
                    {
                            explicit 1;
							implicit 0;
                        }
                    }
                }
            }
        }");

            return sb.ToString();
        }

        private static string FunctionObjFieldMinMax()
        {
            return @"fieldMinMax
{
    type fieldMinMax;
    libs (""libfieldFunctionObjects.so"");
    writeToFile true;
    log true;
    mode magnitude;
    fields (U p k epsilon omega nut aoa);
}";
        }

        private static string FunctionObjFieldAverage()
        {
            return @"average
{
    type            volFieldValue;
    libs            (""libfieldFunctionObjects.so"");
    fields (U p);
    operation       weightedVolAverage;
    regionType      all;
    writeFields     false;
    log true;
}";
        }

        private static string FunctionObjStabilityLimiters(OFRunSettings runSettings)
        {
            // Reserved for optional OpenFOAM 12 field limiter function objects.
            return string.Empty;
        }

        /// <summary>
        /// Generates pressure coefficient function object.
        /// </summary>
        public static string FunctionObjCP(OFBaseDomain DOM, OFRunSettings RunSettings, List<Mesh> evaluationTopology, int d)
        {
            PressureCoeff BCondCP = new PressureCoeff(DOM.MaxHeightBuilding, DOM.BCond);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"pressureCoefficients
{
                    type pressure;
                    libs (""libfieldFunctionObjects.so"");
                    enabled true;
                    writeControl timeStep;
                    writeInterval " + RunSettings.writeInterval + @";
                    UInf (" + Utilities.FormatPV(BCondCP.Uinf[d]) + @");     // the undistrubed velocity at building height
                    pInf " + Utilities.FormatDouble(Math.Round(BCondCP.pinf[d], 1)) + @";        // the dynamic undisturbed pressure at building height
                    pRef " + Utilities.FormatDouble(Math.Round(BCondCP.pref[d], 1)) + @";        // the dynamic pressure at reference height (usually 10 m)
                    rhoInf              1.2;
                    calcTotal true;
                    calcCoeff true;
                }");

            if (evaluationTopology != null)
            {
                for (int i = 0; i < evaluationTopology.Count; i++)
                {
                    sb.Append(@"
patch" + i + @"
{
    type                    swakExpression;
    valueType               faceSet;
    outputControlMode       timeStep;
    outputInterval          1;
    setName                 patch" + i + @";
    aliases
    {
        c_p     total(p)_coeff;
    }
    expression              ""c_p"";
    accumulations (weightedAverage)
    ;
            verbose                 true;
            autoInterpolate         true;
            warnAutoInterpolate     false;
}
");
                }
            }

            return sb.ToString();
        }
    }
}
