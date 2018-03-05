using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using Rhino.Geometry;

namespace Eddy
{
    public enum BoundaryType
    {
        constant,
        abl
    }

    public class BoundaryConditions
    {
        public double U;
        public double z0;
        public double zref;
        public double zGround;
        public Vector3d flowDir;
        public BoundaryType btype = BoundaryType.abl;

    }
}
