using EddyLib.Indoor.FunctionObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib.Indoor.Dicts
{
    public class TopoSetDict : GenericDict

    {
        //public new string DictionaryName = "topoSetDict";
        //public new DictLocation Location = 
        //public new readonly FieldClass FC = FieldClass.dictionary;


        public List<String> InternalDict = new List<string>();

        public TopoSetDict(List<ViralEmitter> viralEmitters, List<CO2Emitter> cO2Emitters, List<VolumetricHeatSource> volumetricHeatSources, List<MomentumSinkIndoor> momentumSinks, List<MomentumSource> momentumSources, Point3d pointInsideDomain)
        {
            this.DictionaryName = "topoSetDict";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            foreach (ViralEmitter i in viralEmitters) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetTopoSetDict(i,pointInsideDomain)));}
            foreach (CO2Emitter i in cO2Emitters) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetTopoSetDict(i, pointInsideDomain))); }
            foreach (VolumetricHeatSource i in volumetricHeatSources) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetTopoSetDict(i, pointInsideDomain))); }
            foreach (MomentumSinkIndoor i in momentumSinks) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetTopoSetDict(i, pointInsideDomain))); }
            foreach (MomentumSource i in momentumSources) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetTopoSetDict(i, pointInsideDomain))); }

            string[] parts = {
               this.Header, "\n", "actions", "\n", "(",
               String.Join("\n", this.InternalDict.ToArray()),"\n",")"
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        //iterate through a list of function objects with updated IDs and create an entry for each item in the list
        //public TopoSetDict(fixthis, Point3d PointInsideDomain)
        //{
        //    ///FIX FOR TREE DICT

        //    //foreach (FunctionObject i in fo)








        //    //   string[] parts = {
        //    //      this.Header, "\n", //(This is a subdict and doesn't need a header
        //    //String.Join("\n", InternalDicts.Select(x => x.TopoSetDictString.ToString()).ToArray())
        //    //   };

        //    //this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        //}

        public static Dictionary<string, dynamic> GetTopoSetDict(FunctionObject input, Point3d PointInsideDomain)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> sourceInfo = new Dictionary<string, dynamic>();

            Dict.Add("", InternalDict);

            InternalDict.Add("name", input.ID);
            InternalDict.Add("type", "cellZoneSet");

            InternalDict.Add("action", "new");
            InternalDict.Add("source", "surfaceToCell");

            InternalDict.Add("sourceInfo", sourceInfo);

            sourceInfo.Add("surface", "triSurfaceMesh");
            sourceInfo.Add("file",  "\"" +  "./constant/triSurface/" + input.ID + ".stl" + "\"");
            sourceInfo.Add("outsidePoints", "(("+ Utilities.FormatPV(PointInsideDomain) +"))" );
            sourceInfo.Add("includeCut", "yes");
            sourceInfo.Add("includeInside", "yes");
            sourceInfo.Add("includeOutside", "no");
            sourceInfo.Add("nearDistance", "0.08");
            sourceInfo.Add("curvature", "-100");

            return Dict;
        }
    }
}