using EddyLib.Indoor.FunctionObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

#if DEBUG
[assembly: InternalsVisibleTo("UnitTest")]
#endif

namespace EddyLib.Indoor.Dicts
{
    public class ControlDict : GenericDict

    {
        public List<String> InternalDict = new List<string>();

        public ControlDict(IndoorDomain IndoorDom)
        {
            this.DictionaryName = "controlDict";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);

            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetDict(IndoorDom)));

            string[] parts = {
               this.Header, "\n", Libraries(),"\n",
         String.Join("\n",this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        //ADD LIBRARIES

        //        private string Libraries = @"
        //libs
        //(
        //        ""libOpenFOAM.so\""
        //        ""libutilityFunctionObjects.so""
        //        ""libsolverFunctionObjects.so""
        //);
        //            ";

        private static String Libraries()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"libs");

            sb.AppendLine(@"(");

            sb.AppendLine("\"libOpenFOAM.so\"");
            sb.AppendLine("\"libutilityFunctionObjects.so\"");
            sb.AppendLine("\"libsolverFunctionObjects.so\"");

            sb.AppendLine(@")");

            return sb.ToString();
        }

        private static Dictionary<string, dynamic> GetDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            //OLD IMPLEMENTAION
            //Dictionary<string, dynamic> FunctionObjectlDict = new Dictionary<string, dynamic>();

            InternalDict.Add("application", "buoyantSimpleFoam");
            //InternalDict.Add("application", "extractFromSurface");
            InternalDict.Add("startFrom", "startTime");
            InternalDict.Add("startTime", "0");
            InternalDict.Add("stopAt", "endTime");
            InternalDict.Add("endTime", IndoorDom.endTime);

            InternalDict.Add("deltaT", 1);
            InternalDict.Add("writeControl", "timeStep");
            InternalDict.Add("writeInterval", 10.ToString());
            InternalDict.Add("purgeWrite", 10);
            InternalDict.Add("writeFormat", "binary");
            InternalDict.Add("writePrecision", 9);
            InternalDict.Add("writeCompression", "off");
            InternalDict.Add("timeFormat", "general");
            InternalDict.Add("timePrecision", 6);
            InternalDict.Add("runTimeModifiable", "true");

            InternalDict.Add("functions", FunctionObjectInclude(IndoorDom));

            return InternalDict;
        }

        private static String FunctionObjectInclude(IndoorDomain IndDom)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"{");

            sb.AppendLine("#includeFunc  residuals");

            sb.AppendLine(@"	fieldMinMax
{
                type fieldMinMax;
                libs (""libfieldFunctionObjects.so"");
                writeFields false;
                log true;
                mode magnitude;
                fields (U  T);
            }

            average
{
                type volFieldValue;
                libs (""libfieldFunctionObjects.so"");
                fields (U T);
                operation weightedVolAverage;
                regionType all;
                writeFields false;
                log true;
            }");

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
                    type scalarSemiImplicitSource;
                    active          true;
                    cellZone all;
                    scalarSemiImplicitSourceCoeffs
                {
                        volumeMode specific;
                        selectionMode all;
                        injectionRateSuSp
                    {
                            aoa (1 0);
                        }
                    }
                }
            }
                

        }");
            sb.AppendLine(
                @"covid19
    {
                type scalarTransport;
                libs (""libfieldFunctionObjects.so"");

                writeControl outputTime;
                D               16e-5;
                field covid19;
                resetOnStartUp  false;
                schemesField covid19;
                bounded01       true;
                write           true;

                fvOptions
        {
                    covid19_00
            {
                        type scalarSemiImplicitSource;
                        active          true;

                        scalarSemiImplicitSourceCoeffs
                {
                            volumeMode absolute;
                            selectionMode cellZone;
                            cellZone ViralEmitter_0; //Todo: Add Emitter Name
                            injectionRateSuSp
                    {
                                covid19 (1.076e-4 0); 
                            }
                        }
                    }
                }
            }");

            //if (IndDom.FOs.OfType<VolumetricHeatSource>().Any())
            //{ sb.AppendLine("#includeFunc volumetricHeatSources");}

            //if (IndDom.FOs.OfType<MomentumSinkIndoor>().Any())
            //{ sb.AppendLine("#includeFunc momentumSinks"); }

            //if (IndDom.FOs.OfType<MomentumSource>().Any())
            //{ sb.AppendLine("#includeFunc momentumSources"); }

            //if (IndDom.FOs.OfType<CO2Emitter>().Any())
            //{ sb.AppendLine("#includeFunc co2Emitters"); }

            //if (IndDom.FOs.OfType<ViralEmitter>().Any())
            //{ sb.AppendLine("#includeFunc viralEmitters"); }

            sb.Append(@"}");

            return sb.ToString();
        }

        //List<String> fos = new List<String>();

        //if (IndoorDom.FOs.OfType<VolumetricHeatSource>().Any())

        //{ fos.Add("volumetricHeatSources"); }

        //if (IndoorDom.FOs.OfType<MomentumSink>().Any())
        //{ fos.Add("momentumSink"); }

        //FunctionObjectlDict.Add("#includeFunc", fos);

        //SNAPPYHEX

        //OLD IMPLEMENTAION

        //Add include statements to the dictionary if the respective FuntionObjects are added to IndoorDomai

        //if (IndoorDom.FOs.OfType<VolumetricHeatSource>().Any())

        //{
        //    FunctionObjectlDict.Add("#includeFunc1", "volumetricHeatSources");
        //}

        //if (IndoorDom.FOs.OfType<MomentumSink>().Any())

        //{
        //    FunctionObjectlDict.Add("#includeFunc2", "momentumSink");
        //}

        //if (IndoorDom.FOs.OfType<MomentumSource>().Any())

        //{
        //    FunctionObjectlDict.Add("#includeFunc3", "momentumSource");
        //}

        //if (IndoorDom.FOs.OfType<CO2Emitter>().Any())

        //{
        //    FunctionObjectlDict.Add("#includeFunc4", "co2Emitter");
        //}

        //if (IndoorDom.FOs.OfType<ViralEmitter>().Any())

        //{
        //    FunctionObjectlDict.Add("#includeFunc5", "viralEmitter");
        //}

        //class IncludeStatements
        //{
        //    public IncludeStatements(IndoorDomain IndoorDom)

        //    { }

        //    public override string ToString(object IndoorDom)
        //    {
        //        StringBuilder sb = new StringBuilder();
        //        sb.Append(@"{");

        //        if (IndoorDom.FOs.OfType<VolumetricHeatSource>().Any()) { sb.AppendLine(""; }

        //        return GenericDict.InParenthesis(sb.ToString());

        //    }

        //}
    }
}