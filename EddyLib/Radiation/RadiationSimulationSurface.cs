using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    public class RadiationSimulationSurface
    {

        public GeometryBase AttachMetaData(GeometryBase geo) {

            //geo.UserDictionary.Set("EddyConstruction", val);
            //geo.UserDictionary.Set("EddyPoint", pt);
            //geo.UserDictionary.Set("EddyNormal", n);
            return geo;
        }


        public enum RadiationSurfaceType
        {
            Building,
            Ground,
            Vegetation,
            Tree,
            Sky
        }
    }
}
