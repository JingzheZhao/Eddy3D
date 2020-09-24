using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Indoor
{
    public enum OFIndoorGeometryType
    {
        Inlet = 0,
        Outlet = 1,
        Geometry = 2
        }

    class OFIndoorGeometry
    {
        string Name { get; set; } // must be unique
        Vector3d Normal { get; set; }
        Mesh Geometry { get; set; }
        OFIndoorGeometryType Type { get;  set;}
        double Area { get; set; } 
        double FlowRate { get; set; } = 0;




        Dictionary<string, string> U { get; set; }
        Dictionary<string, string> T { get; set; }
        Dictionary<string, string> alpha { get; set; } 
        Dictionary<string, string> AoA { get; set; } 
        Dictionary<string, string> k { get; set; } 
        Dictionary<string, string> nut { get; set; }
        Dictionary<string, string> p_rgh { get; set; } 
        Dictionary<string, string> omega { get; set; } 
        Dictionary<string, string> p { get; set; }






        public OFIndoorGeometry(string name, Mesh geo , OFIndoorGeometryType type, OFField field) {

            Name = name;


         

            if (type == OFIndoorGeometryType.Inlet) {


                DefineInlet(field);
            }


            else if (type == OFIndoorGeometryType.Outlet)
            {



            }


            else 
            {

                // normal geometry

            }

        }

        private void DefineInlet(OFField input)
        {

            U = GetFixedValue(input);
            
        }


        private static Dictionary<string, string>  GetFixedValue(OFField input) {
            //TODO Patrick: Set up Dict
            Dictionary<string, string> Dict = new Dictionary<string, string>
        {
            { "type",  "fixedValue" },
            { "value",  "uniform (0 0 -4)" },
        };
            return Dict;
        }
        private static Dictionary<string, string> GetZeroGradient()
        {

            Dictionary<string, string> Dict = new Dictionary<string, string>
        {
            { "type",  "zeroGradient" },
         };
            return Dict;
        }

    }
}
