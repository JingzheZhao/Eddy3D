using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;

namespace EddyLib.Indoor.Dicts
{
    public class TopoSetSubDict

    {

        //SOULD BE REMOVED

        public string TopoSetDictString;
        public string FunctionObjectSubDictString;

        public static Dictionary<string, dynamic> GetInternalTopoSetDict(FunctionObject input, Point3d PointInsideDomain)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> sourceInfo = new Dictionary<string, dynamic>();

            Dict.Add(input.ID, InternalDict);

            InternalDict.Add("name", input.ID);
            InternalDict.Add("type", "cellZoneSet");

            InternalDict.Add("action", "new");
            InternalDict.Add("source", "surfaceToCell");

            InternalDict.Add("sourceInfo", sourceInfo);

            sourceInfo.Add("surface", "triSurfaceMesh");
            sourceInfo.Add("file", "./constant/triSurface/" + input.ID + ".stl");
            sourceInfo.Add("outsidePoints", Utilities.FormatPV(PointInsideDomain));
            sourceInfo.Add("includeCut", "yes");
            sourceInfo.Add("includeInside", "yes");
            sourceInfo.Add("includeOutside", "no");
            sourceInfo.Add("nearDistance", "0.08");
            sourceInfo.Add("curvature", "-100");

            return Dict;
        }
    }
}