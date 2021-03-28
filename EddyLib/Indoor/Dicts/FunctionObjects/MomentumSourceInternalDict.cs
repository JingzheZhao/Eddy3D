using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;
using Rhino.Geometry;

namespace EddyLib.Indoor.Dicts
{
    public class MomentumSourceInternalDict : GenericDict
    {

        public List<String> InternalDict = new List<string>();

        //public MomentumSourceInternalDict(MomentumSource momSource, Point3d PointInsideDomain)
        public MomentumSourceInternalDict(List<MomentumSource> momentumSources, Point3d PointInsideDomain)
        {
            this.DictionaryName = "momentumSources";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            // this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(momSource)));
            foreach (MomentumSource i in momentumSources) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(i))); }

            string[] parts = {
               String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");

            //this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)MomSource, PointInsideDomain));
            //this.FunctionObjectSubDictString = CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(MomSource));
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
            meanVelocityForceCoeffs.Add("cellZone", input.ID);
            meanVelocityForceCoeffs.Add("fields", "(U)");
            meanVelocityForceCoeffs.Add("Ubar", Utilities.FormatPV(input.Ubar));
            meanVelocityForceCoeffs.Add("relaxation", "1.0");

            return Dict;
        }
    }
}