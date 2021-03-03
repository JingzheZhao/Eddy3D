using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    public class RPolygon
    {
        public double rin = 0.0;
        public double rout = 0.0;
        
        public double refl = 0.0;

        public Vector3d n;
        public Point3d cen;
        public double area = 0.0;
        public Mesh m;

        public string matName;

        public RSurface parent;
     }

    
}
