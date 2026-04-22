using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib
{
    /// <summary>
    /// Mathematical utility methods.
    /// </summary>
    public static partial class Utilities
    {
        private static readonly string[] SizeSuffixes =
            { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        /// <summary>
        /// Formats a byte count with appropriate size suffix (KB, MB, GB, etc.)
        /// </summary>
        public static string SizeSuffix(long value)
        {
            if (value < 0) return "-" + SizeSuffix(-value);
            if (value == 0) return "0 bytes";

            int i = 0;
            decimal dValue = value;
            while (Math.Round(dValue / 1024) >= 1 && i < SizeSuffixes.Length - 1)
            {
                dValue /= 1024;
                i++;
            }

            return $"{dValue:n1} {SizeSuffixes[i]}";
        }

        /// <summary>
        /// Computes the average of a list of vectors.
        /// </summary>
        public static Vector3d AverageVectors(List<Vector3d> vectors)
        {
            if (vectors == null || vectors.Count == 0)
                return Vector3d.Zero;

            Vector3d sum = Vector3d.Zero;
            int count = 0;

            foreach (var vec in vectors)
            {
                // Vector3d is a struct, can't be null, but check for zero-length validity
                sum += vec;
                count++;
            }

            return count > 0 ? sum / count : Vector3d.Zero;
        }

        /// <summary>
        /// Computes the average (centroid) of a list of points.
        /// </summary>
        public static Point3d AveragePoints(List<Point3d> points)
        {
            if (points == null || points.Count == 0)
                return Point3d.Origin;

            double x = 0, y = 0, z = 0;
            foreach (var pt in points)
            {
                x += pt.X;
                y += pt.Y;
                z += pt.Z;
            }

            int count = points.Count;
            return new Point3d(x / count, y / count, z / count);
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        public static double Deg2Rad(double angleDeg)
        {
            return angleDeg * Math.PI / 180.0;
        }

        /// <summary>
        /// Calculates angle in degrees between a vector and North (Y-axis).
        /// </summary>
        public static double Rad2Deg(Vector3d windVec)
        {
            Vector3d north = Vector3d.YAxis;
            windVec.Unitize();

            double dotProduct = north * windVec;
            // Clamp to handle floating point errors
            dotProduct = Math.Max(-1.0, Math.Min(1.0, dotProduct));

            return Math.Acos(dotProduct) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Converts a vector to compass direction in degrees (0-360).
        /// </summary>
        public static double Vec2Dir(Vector3d vec)
        {
            return Math.Atan2(vec.Y, vec.X) * 180.0 / Math.PI;
        }

        /// <summary>
        /// Converts a vector to OpenFOAM wind direction (meteorological convention).
        /// 0° = North, 90° = East, 180° = South, 270° = West.
        /// </summary>
        public static int Vec2DirOFCoord(Vector3d vec)
        {
            // Convert from math convention (0° = East, CCW) to meteorological (0° = North, CW)
            double mathDeg = Math.Atan2(vec.Y, vec.X) * 180.0 / Math.PI;
            double meteoDeg = 90.0 - mathDeg;

            // Normalize to 0-360 range
            meteoDeg = ((meteoDeg % 360.0) + 360.0) % 360.0;

            return (int)Math.Round(meteoDeg);
        }

        /// <summary>
        /// Normalizes wind directions to 0-359 range.
        /// </summary>
        public static List<int> NormalizeWindDirs(List<int> windDirs)
        {
            if (windDirs == null || windDirs.Count == 0)
            {
                return new List<int> { 0 };
            }

            return windDirs.Select(dir => ((dir % 360) + 360) % 360).ToList();
        }

        /// <summary>
        /// Converts a wind direction (degrees) to a unit vector.
        /// Uses meteorological convention: 0° = North wind (blowing from North to South).
        /// </summary>
        public static Vector3d Dir2Vec(double degrees)
        {
            double radians = degrees * Math.PI / 180.0;
            // Wind FROM direction, so negative
            return new Vector3d(-Math.Sin(radians), -Math.Cos(radians), 0);
        }

        /// <summary>
        /// Calculates the signed angle between two 2D vectors in degrees.
        /// Positive = CCW rotation from vector1 to vector2.
        /// </summary>
        public static double AngleBetweenVectors(Vector3d vector1, Vector3d vector2)
        {
            double cross = vector1.X * vector2.Y - vector2.X * vector1.Y;
            double dot = vector1.X * vector2.X + vector1.Y * vector2.Y;
            return Math.Atan2(cross, dot) * 180.0 / Math.PI;
        }
    }
}
