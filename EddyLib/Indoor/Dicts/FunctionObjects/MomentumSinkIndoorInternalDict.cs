using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.Indoor.Dicts
{
    public class MomentumSinkIndoorInternalDict : GenericDict
    {
        public List<String> InternalDict = new List<string>();

        //public MomentumSinkIndoorInternalDict(MomentumSinkIndoor momSink, Point3d PointInsideDomain)
        public MomentumSinkIndoorInternalDict(List<MomentumSinkIndoor> momentumSinks, Point3d PointInsideDomain)
        {
            this.DictionaryName = "momentumSinks";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            foreach (MomentumSinkIndoor i in momentumSinks) { this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(i))); }
            //this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(momSink)));

            string[] parts = {
               String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");

            //this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)MomSink, PointInsideDomain));
            // this.FunctionObjectSubDictString = CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(MomSink));
        }

        public static Dictionary<string, dynamic> GetInternalFvOptionsDict(MomentumSinkIndoor input)

        {
            Dictionary<string, dynamic> Dict0 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict4 = new Dictionary<string, dynamic>();

            Dict0.Add(input.ID, Dict1);

            Dict1.Add("type", "explicitPorositySource");
            Dict1.Add("explicitPorositySourceCoeffs", Dict2);

            Dict2.Add("selectionMode", "cellZone");
            Dict2.Add("cellZone", input.ID);
            Dict2.Add("type", "DarcyForchheimer");
            Dict2.Add("f", "(9e9  9e9 9e9)");
            Dict2.Add("d", "(9e9  9e9 9e9)");
            Dict2.Add("coordinateSystem", Dict3);

            Dict3.Add("type", "cartesian");

            Dict3.Add("origin", "(0 0 0)");
            Dict3.Add("coordinateRotation", Dict4);

            Dict4.Add("type", "axesRotation");
            Dict4.Add("e1", "(1 0 0)");
            Dict4.Add("e2", "(0 1 0)");

            return Dict0;
        }
    }
}