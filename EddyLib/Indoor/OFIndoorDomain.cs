using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor
{
    class OFIndoorDomain
    {
        BoundingBox BoudingBox;

        List<OFIndoorGeometry> Geometry; //Surfaces or Volumes. IE Walls, table, whatever
        List<OFIndoorGeometry> Inlets; //Surfaces
        List<OFIndoorGeometry> Outlets;//Surfaces

        List<OFIndoorEmitters> Emitters; //Volumes

        List<Point3d> Edges;

        double CellSize;

    }
}
