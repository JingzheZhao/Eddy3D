using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib.Radiation
{
    public partial class SkyViewFactor
    {
        #region 4. RaysFile

        public static string SensorPoints(List<Point3d> pts, List<Vector3d> pts_norm)
        {
            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < pts.Count; k++)
            {
                sb.AppendLine(FormatPointAndNormal(pts[k], pts_norm[k]));
            }
            return sb.ToString();
        }

        public static string Rays(Point3d pt, List<Vector3d> pts_norm)
        {
            StringBuilder sb = new StringBuilder();

            for (int k = 0; k < pts_norm.Count; k++)
            {
                sb.AppendLine(FormatPointAndNormal(pt, pts_norm[k]));
            }
            return sb.ToString();
        }

        private static string FormatPointAndNormal(Point3d p, Vector3d n)
        {
            return String.Format(radianceCulture, "{0:0.000} {1:0.000} {2:0.000} {3:0.000} {4:0.000} {5:0.000}", p.X, p.Y, p.Z, n.X, n.Y, n.Z);
        }

        #endregion 4. RaysFile
    }
}
