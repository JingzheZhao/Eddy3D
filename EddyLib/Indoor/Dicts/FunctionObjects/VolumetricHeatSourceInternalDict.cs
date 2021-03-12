using EddyLib.Indoor.FunctionObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.Dicts
{
    public class VolumetricHeatSourceInternalDict : FunctionObjectDictInternal
    {
        public VolumetricHeatSourceInternalDict(VolumetricHeatSource VH, Point3d PointInsideDomain)
        {
            this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)VH, PointInsideDomain));
            this.FvOptionsDictString = CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(VH));
        }

        private static Dictionary<string, dynamic> GetInternalFvOptionsDict(VolumetricHeatSource input)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> scalarSemiImplicitSourceCoeffsDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> injectionRateSuSpDict = new Dictionary<string, dynamic>();

            Dict.Add(input.Name + "_" + input.Name, InternalDict);

            InternalDict.Add("type", "scalarSemiImplicitSource");
            InternalDict.Add("active", "on");
            InternalDict.Add("selectionMode", "cellZone");
            InternalDict.Add("cellZone", input.cellZone + "_" + input.Name);

            InternalDict.Add("scalarSemiImplicitSourceCoeffs", scalarSemiImplicitSourceCoeffsDict);

            scalarSemiImplicitSourceCoeffsDict.Add("selectionMode", "cellZone");
            scalarSemiImplicitSourceCoeffsDict.Add("cellZone", input.cellZone + "_" + input.Name);
            scalarSemiImplicitSourceCoeffsDict.Add("volumeMode", input.volumeType);
            scalarSemiImplicitSourceCoeffsDict.Add("injectionRateSuSp", injectionRateSuSpDict);

            injectionRateSuSpDict.Add("h", @"(" + input.Power.ToString() + " 0 )");

            return Dict;
        }
    }
}