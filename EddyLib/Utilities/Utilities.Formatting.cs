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

        public static void AppendPV(System.Text.StringBuilder sb, Point3d p)
        {
            sb.Append(p.X.ToString("0.###", eddy3dculture))
              .Append(' ')
              .Append(p.Y.ToString("0.###", eddy3dculture))
              .Append(' ')
              .Append(p.Z.ToString("0.###", eddy3dculture));
        }

        public static void AppendPV(System.Text.StringBuilder sb, Vector3d v)
        {
            sb.Append(v.X.ToString("0.###", eddy3dculture))
              .Append(' ')
              .Append(v.Y.ToString("0.###", eddy3dculture))
              .Append(' ')
              .Append(v.Z.ToString("0.###", eddy3dculture));
        }

        public static void WritePV(System.IO.TextWriter tw, Point3d p)
        {
            tw.Write(p.X.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(p.Y.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(p.Z.ToString("0.###", eddy3dculture));
        }

        public static void WritePV(System.IO.TextWriter tw, Vector3d v)
        {
            tw.Write(v.X.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(v.Y.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(v.Z.ToString("0.###", eddy3dculture));
        }

        public static void WritePV(System.IO.TextWriter tw, Point3d p, Vector3d n)
        {
            tw.Write(p.X.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(p.Y.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(p.Z.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(n.X.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(n.Y.ToString("0.###", eddy3dculture));
            tw.Write(' ');
            tw.Write(n.Z.ToString("0.###", eddy3dculture));
        }

        public static string FormatDouble(double d) =>
        // Preserve small non-zero physical values (e.g. roughness length z0 = 0.00015).
        String.Format(eddy3dculture, "{0:0.########}", d);
    }
}
