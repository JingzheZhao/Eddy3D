using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.Indoor.Dicts
{
    public class ControlDict : GenericDict




    {
        public List<String> InternalDict = new List<string>();


        public ControlDict(int endTime, List<FunctionObject> FOs)
        {
            this.DictionaryName = "controlDict";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);


       


            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetDict(endTime,  FOs)));


            string[] parts = {
               this.Header, "\n",
         String.Join("\n", this.InternalDict.ToArray())


            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static Dictionary<string, dynamic> GetDict(int endTime, List<FunctionObject> FOs)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> FunctionObjectlDict = new Dictionary<string, dynamic>();


            Dict.Add("controlDict", InternalDict);

            InternalDict.Add("application", "extractFromSurface");
            InternalDict.Add("startFrom", "startTime");
            InternalDict.Add("startTime", "0");
            InternalDict.Add("stopAt", "endTime");
            InternalDict.Add("endTime", endTime);

            InternalDict.Add("deltaT", 1);
            InternalDict.Add("writeControl", "timeStep");
            InternalDict.Add("writeInterval", 10);
            InternalDict.Add("purgeWrite", 10);
            InternalDict.Add("writeFormat", "binary");
            InternalDict.Add("writePrecision", 9);
            InternalDict.Add("writeCompression", "off");
            InternalDict.Add("timeFormat", "general");
            InternalDict.Add("timePrecision", 6);
            InternalDict.Add("runTimeModifiable", "true");

            InternalDict.Add("functions", FunctionObjectlDict);


         if ( FOs.OfType<VolumetricHeatSource>().Any())
          
            {
                FunctionObjectlDict.Add("#includeFunc", "volumetricHeatSources");
            }



            return Dict;
        }


    } 

}
