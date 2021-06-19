using EddyLib.Indoor.FunctionObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.Indoor.Dicts
{
    public class VolumetricHeatSourceInternalDict : GenericDict
    {
        public List<String> InternalDict = new List<string>();

        //public VolumetricHeatSourceInternalDict(VolumetricHeatSource VH, Point3d PointInsideDomain)
        public VolumetricHeatSourceInternalDict(List<VolumetricHeatSource> volumetricHeatSources, Point3d PointInsideDomain)
        {
            this.DictionaryName = "volumetricHeatSources";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            //this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalVHSDict(VH)));
            foreach (VolumetricHeatSource i in volumetricHeatSources) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalVHSDict(i))); }

            string[] parts = {
               String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");

            //this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)VH, PointInsideDomain));
            //this.FunctionObjectSubDictString = CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(VH));
        }

        private static Dictionary<string, dynamic> GetInternalVHSDict(VolumetricHeatSource input)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> scalarSemiImplicitSourceCoeffsDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> injectionRateSuSpDict = new Dictionary<string, dynamic>();

            Dict.Add(input.ID, InternalDict);

            //Dict.Add(input.Name + "_" + input.Name, InternalDict);

            InternalDict.Add("type", "scalarSemiImplicitSource");
            InternalDict.Add("active", "on");
            InternalDict.Add("selectionMode", "cellZone");
            InternalDict.Add("cellZone", input.ID);

            //InternalDict.Add("cellZone", input.cellZone + "_" + input.Name);

            InternalDict.Add("scalarSemiImplicitSourceCoeffs", scalarSemiImplicitSourceCoeffsDict);

            scalarSemiImplicitSourceCoeffsDict.Add("volumeMode", "absolute");
            scalarSemiImplicitSourceCoeffsDict.Add("selectionMode", "cellZone");
            scalarSemiImplicitSourceCoeffsDict.Add("cellZone", input.ID);
            //scalarSemiImplicitSourceCoeffsDict.Add("cellZone", input.cellZone + "_" + input.Name);

            //scalarSemiImplicitSourceCoeffsDict.Add("volumeMode", input.volumeType);
            scalarSemiImplicitSourceCoeffsDict.Add("injectionRateSuSp", injectionRateSuSpDict);

            injectionRateSuSpDict.Add("h", @"(" + input.Power.ToString() + " 0 )");

            return Dict;
        }
    }
}