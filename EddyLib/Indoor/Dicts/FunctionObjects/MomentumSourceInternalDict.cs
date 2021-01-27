using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;
using Rhino.Geometry;

namespace EddyLib.Indoor.Dicts
{
    public class MomentumSourceInternalDict : FunctionObjectDictInternal
    {
        public MomentumSourceInternalDict(MomentumSource MomSource, Point3d PointInsideDomain)
        {
            this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)MomSource, PointInsideDomain));
            this.FvOptionsDictString = CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(MomSource));
        }

        private static Dictionary<string, dynamic> GetInternalFvOptionsDict(MomentumSource input)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> meanVelocityForceCoeffs = new Dictionary<string, dynamic>();

            Dict.Add(input.ID, InternalDict);

            InternalDict.Add("type", "meanVelocityForce");
            InternalDict.Add("active", "yes");

            InternalDict.Add("meanVelocityForceCoeffs", meanVelocityForceCoeffs);

            meanVelocityForceCoeffs.Add("selectionMode", "cellZone");
            meanVelocityForceCoeffs.Add("cellZone", input.ID + "_" + input.Name);
            meanVelocityForceCoeffs.Add("fields", "(U)");
            meanVelocityForceCoeffs.Add("Ubar", Utilities.FormatPV(input.Ubar));
            meanVelocityForceCoeffs.Add("relaxation", "1.0");

            return Dict;
        }
    }
}