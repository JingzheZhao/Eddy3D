using EddyLib.Geometry;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
   
    public class EddyProbe
    {

        public EddyProbe() { }
        public EddyProbe(Point3d pt , Vector3d vec) {
            Point =  pt;
            Normal =  vec;
        }

        public EddyProbe(Point3d pt, Vector3d vec, Mesh geo)
        {
            Point = pt;
            Normal = vec;
            PreviewGeo = geo;
        }
        public Point3d Point { get; set; }
      
        public Vector3d Normal { get; set; }

         
        public Mesh PreviewGeo { get; set; }

    }
}
