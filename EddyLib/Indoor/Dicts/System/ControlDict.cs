using EddyLib.Indoor.FunctionObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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

            InternalDict.Add("application", "foamRun");
            InternalDict.Add("solver", "fluid");
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

            sb.Append(GenerateScalarTransportConfig(userChosenName: "aoa", diffusivity: 2e-5, resetOnStart: false, explicitSource: 1.0, implicitSource: 0.0));

            if (IndDom.FOs.OfType<VolumetricHeatSource>().Any())
            { sb.AppendLine("#includeFunc volumetricHeatSources"); }

            if (IndDom.FOs.OfType<MomentumSinkIndoor>().Any())
            { sb.AppendLine("#includeFunc momentumSinks"); }

            if (IndDom.FOs.OfType<MomentumSource>().Any())
            { sb.AppendLine("#includeFunc momentumSources"); }

            if (IndDom.FOs.OfType<CO2Emitter>().Any())
            { sb.AppendLine("#includeFunc co2Emitters"); }

            if (IndDom.FOs.OfType<ViralEmitter>().Any())
            { sb.AppendLine("#includeFunc viralEmitters"); }

            sb.Append(@"}");

            return sb.ToString();
        }

        private static string GenerateScalarTransportConfig(
            string userChosenName,
            double diffusivity,
            bool resetOnStart,
            double explicitSource,
            double implicitSource
        )
        {
            var sb = new StringBuilder();

            //#########################################################################
            //### Scalar Transport Function Object Settings                         ###
            //#########################################################################

            // The function-object block name:
            sb.AppendLine(userChosenName);
            sb.AppendLine("{");

            // Type of function object
            sb.AppendLine("    type            scalarTransport;");

            // Library (Windows uses .dll, Linux uses .so)
            var lib = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "libsolverFunctionObjects.dll"
                : "libsolverFunctionObjects.so";
            sb.AppendLine($"    libs            (\"{lib}\");");
            sb.AppendLine();

            // --- Field and Physical Properties ---
            // Name of the scalar field to be transported
            sb.AppendLine($"    field           {userChosenName};");

            // Diffusivity 'D' (use general format so 1e-12 stays 1e-12, not 0.0)
            // D may be a constant or built from alphaD/alphaDt; D=0 is allowed but can yield a weak diagonal.
            sb.AppendLine($"    D               {diffusivity.ToString("G9", CultureInfo.InvariantCulture)};");
            sb.AppendLine();

            // --- Controls ---
            // Note: 'resetOnStartUp' is recognized in OpenCFD releases; OpenFOAM-8 (Foundation) ignores it.
            sb.AppendLine($"    resetOnStartUp  {(resetOnStart ? "true" : "false")};");

            // Use discretization schemes named for this field (div(phi,{name}), laplacian(D{name},{name}))
            sb.AppendLine($"    schemesField    {userChosenName};");
            sb.AppendLine();

            // Write the transported field at output times.
            sb.AppendLine("    writeControl    outputTime;");
            sb.AppendLine();

            //#########################################################################
            //### Finite Volume Options (fvOptions) for Source Terms                ###
            //#########################################################################
            sb.AppendLine("    fvOptions");
            sb.AppendLine("    {");
            sb.AppendLine($"        {userChosenName}_source");
            sb.AppendLine("        {");
            sb.AppendLine("            type            semiImplicitSource;");
            sb.AppendLine("            active          true;");
            sb.AppendLine("            selectionMode   all;");
            sb.AppendLine("            volumeMode      specific;");
            sb.AppendLine("            sources");
            sb.AppendLine("            {");
            sb.AppendLine($"                {userChosenName}");
            sb.AppendLine("                {");
            // Su (explicit) and Sp (implicit) as separate entries (v8 supports this form)
            sb.AppendLine($"                    explicit    {explicitSource.ToString("G9", CultureInfo.InvariantCulture)};");
            sb.AppendLine($"                    implicit    {implicitSource.ToString("G9", CultureInfo.InvariantCulture)};");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
