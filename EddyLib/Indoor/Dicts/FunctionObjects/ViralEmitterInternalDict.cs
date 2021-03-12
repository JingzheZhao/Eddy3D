using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor.Dicts
{
    class ViralEmitterInternalDict : FunctionObjectDictInternal
    {
        public ViralEmitterInternalDict(ViralEmitter VEm, Point3d PointInsideDomain)
        {
            this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)VEm, PointInsideDomain));
            this.FvOptionsDictString = CppMapSerializerDyn.Serialize(GetInternalC02Dict(VEm));
        }

        private static Dictionary<string, dynamic> GetInternalC02Dict(ViralEmitter input)
        {
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict4 = new Dictionary<string, dynamic>();

            Dict1.Add(input.ID, Dict2);

            Dict2.Add("active", "true");
            Dict2.Add("type", "semiImplicitSource");
            Dict2.Add("scalarSemiImplicitSourceCoeffs", Dict3);

            Dict3.Add("selectionMode", "cellZone");
            Dict3.Add("cellZone", input.Name + "_" + input.ID);
            Dict3.Add("volumeMode", "specific");
            Dict3.Add("injectionRateSuSp", Dict4);

            Dict4.Add("Covid19", "(1.076e-4 0)");

            return Dict1;
        }
    }
}
