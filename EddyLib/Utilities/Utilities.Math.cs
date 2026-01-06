using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EddyLib
{
    public static partial class Utilities
    {
        private static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue / 1024) >= 1)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n1} {1}", dValue, SizeSuffixes[i]);
        }

        public static Vector3d AverageVectors(List<Vector3d> list)
        {
            Vector3d val3 = Vector3d.Zero;
            int num9 = 0;
            int num10 = list.Count - 1;
            for (int m = 0; m <= num10; m++)
            {
                if (list[m] != null)
                {
                    val3 += list[m];
                    num9++;
                }
            }

            // if (num9 != 0)
            // {
            return (val3 / (double)num9);

            //}
        }

        public static Point3d AveragePoints(List<Point3d> list)
        {
            Point3d val2 = Point3d.Origin;
            int num7 = 0;
            int num8 = list.Count - 1;
            for (int l = 0; l <= num8; l++)
            {
                if (list[l] != null)
                {
                    val2 += list[l];
                    num7++;
                }
            }

            //if (num7 != 0)
            // {
            return (val2 / (double)num7);

            //  }
        }

        public static double Deg2Rad(double angleDeg)
        {
            return Math.PI * angleDeg / 180.0;
        }

        public static double Rad2Deg(Vector3d windVec)
        {
            Vector3d vec1 = new Vector3d(0, 1, 0);
            Vector3d vec2 = windVec;

            double rad = Math.Acos(vec1 * vec2 / vec1.Length * vec2.Length);
            double ang = rad * 180 / Math.PI;
            return ang;
        }

        public static double Vec2Dir(Vector3d vec)
        {
            var res = Math.Atan2(vec.Y, vec.X) * 180 / Math.PI;
            return res;
        }

        public static int Vec2DirOFCoord(Vector3d vec)
        {
            // Standard 0 deg is plus X

            var transform = (Math.Atan2(vec.Y, vec.X) * 180 / Math.PI) + 90;

            var deg = 0.0;

            if (transform < 0)

            {
                deg = -1 * transform;
            }
            else if (transform <= 270 && transform > 0)
            {
                deg = 360 - transform;
            }
            else
            { deg = transform; }

            return (int)Math.Round(deg);
        }

        public static List<int> NormalizeWindDirs(List<int> windDir)
        {
            if (windDir.Count == 0)
            {
                windDir.Add(0);
            }

            // Translate dirs > 359 into correct format

            for (int i = 0; i < windDir.Count; i++)
            {
                if (windDir[i] > 359)
                {
                    int j = windDir[i] / 360;
                    windDir[i] = windDir[i] - (360 * j);
                }
                else { windDir[i] = windDir[i]; }
            }

            return windDir;
        }

        public static Vector3d Dir2Vec(double d)
        {
            return new Vector3d(-1 * Math.Sin(d * Math.PI / 180), -1 * Math.Cos(d * Math.PI / 180), 0);
        }

        public static double AngleBetweenVectors(Vector3d vector1, Vector3d vector2)
        {
            double sin = vector1.X * vector2.Y - vector2.X * vector1.Y;
            double cos = vector1.X * vector2.X + vector1.Y * vector2.Y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);
        }
    }
}
