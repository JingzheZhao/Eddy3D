using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;
using Rhino.Geometry;

namespace EddyLib.Indoor.Dicts
{
    public class MomentumSinkInternalDict : GenericDict
    {

        public List<String> InternalDict = new List<string>();

        public MomentumSinkInternalDict(MomentumSink momSink, Point3d PointInsideDomain)
        {
            this.DictionaryName = "momentumSink";
            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(momSink)));

            string[] parts = {
               String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");


            //this.TopoSetDictString = CppMapSerializerDyn.Serialize(GetInternalTopoSetDict((FunctionObject)MomSink, PointInsideDomain));
            // this.FunctionObjectSubDictString = CppMapSerializerDyn.Serialize(GetInternalFvOptionsDict(MomSink));


        }

        public static Dictionary<string, dynamic> GetInternalFvOptionsDict(MomentumSink input)

        {
            //var PorosityCoeffs_D = d;
            //var PorosityCoeffs_F = f;

            //var PorosityCoeffs_D = new double[] { 00.0, 00.0, 00.0 };
            //var PorosityCoeffs_F = new double[] { 0.0, 0.0, 0.0 };

            //if (treeType == TreeType.coarse)
            //{
            //    PorosityCoeffs_D = new double[] { 20.0, 20.0, 20.0 };
            //    PorosityCoeffs_F = new double[] { 20.0, 20.0, 20.0 };
            //}
            //else if (treeType == TreeType.medium)
            //{
            //    PorosityCoeffs_D = new double[] { 40.0, 40.0, 40.0 };
            //    PorosityCoeffs_F = new double[] { 40.0, 40.0, 40.0 };
            //}
            //else
            //{
            //    PorosityCoeffs_D = new double[] { 60.0, 60.0, 60.0 };
            //    PorosityCoeffs_F = new double[] { 60.0, 60.0, 60.0 };
            //}

            // https://www.cfd-online.com/Forums/openfoam-solving/78705-darcy-forchheimer-law-specifying-porous-zones.html
            // e1 and e2 are the vectors that are used to specify the porosity.In the porousZones file, you have to specify three components of f and d.The first component is in the direction of e1, the second in the direction of e2 and the third in the direction perpendicular to e1 and e2.An example can be found in tutorials / incompressible / porousSimpleFoam / angledDuctImplicit.

            //Furthermore,
            //d = beta / viscocity[1 / m ^ 2]
            //f = 2 * alpha / density[1 / m]

            Dictionary<string, dynamic> Dict0 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict4 = new Dictionary<string, dynamic>();

            Dict0.Add(input.ID, Dict1);

            Dict1.Add("type", "explicitPorositySource");
            Dict1.Add("explicitPorositySourceCoeffs", Dict2);

            Dict2.Add("selectionMode", "cellZone");
            Dict2.Add("cellZone", "ms.ID");
            Dict2.Add("type", "DarcyForchheimer");
            Dict2.Add("f", input.F.Select(p => p.ToString()));
            Dict2.Add("d", input.D.Select(p => p.ToString()));
            Dict2.Add("coordinateSystem", Dict3);

            Dict3.Add("type", "cartesian");

            Dict3.Add("origin", "(0 0 0)");
            Dict3.Add("coordinateRotation", Dict4);

            Dict4.Add("e1", "(1 0 0)");
            Dict4.Add("e2", "(0 1 0)");

            return Dict0;
        }
    }
}