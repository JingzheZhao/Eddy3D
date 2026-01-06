using Rhino.Geometry;
using System;
using System.Globalization;

namespace EddyLib
{
    public static partial class Utilities
    {
        // Radiance isn't exactly culture-aware, so we have to make everything here en-US
        private static readonly CultureInfo eddy3dculture = new CultureInfo("en-US");

        //public static string FormatPointAndNormal(Point3d p, Vector3d n) =>
        //String.Format(eddy3dculture, "{0:0.###} {1:0.###} {2:0.###} {3:0.###} {4:0.###} {5:0.###}", p.X, p.Y, p.Z, n.X, n.Y, n.Z);

        public static string FormatPV(Point3d p) =>
        String.Format(eddy3dculture, "{0:0.###} {1:0.###} {2:0.###}", p.X, p.Y, p.Z);

        public static string FormatPV(Vector3d v) =>
        String.Format(eddy3dculture, "{0:0.###} {1:0.###} {2:0.###}", v.X, v.Y, v.Z);

        public static string FormatDouble(double d) =>
        String.Format(eddy3dculture, "{0:0.###}", d);
    }
}
