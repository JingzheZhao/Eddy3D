using System;
using System.Drawing;
using Rhino.Geometry;

namespace EddyLib.GAN
{
    /// <summary>
    /// Color utility functions for GAN input image generation.
    /// Ported from the decompiled CFDComponent Colors class.
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>Clamp a value to [0, 1].</summary>
        public static double Clamp01(double value)
        {
            if (value < 0.0) return 0.0;
            if (value > 1.0) return 1.0;
            return value;
        }

        /// <summary>
        /// Grayscale color based on building height relative to bounding box.
        /// Uses a tri-colour gradient from white through gray to black.
        /// </summary>
        public static Color GreyScale(double heightValue, Box box, double vSize)
        {
            double z = GetZ(box, vSize);
            return GetTriColour(Clamp01(heightValue / z), Color.White, Color.Gray, Color.Black);
        }

        /// <summary>
        /// Compute the effective Z-range in vSize increments.
        /// </summary>
        public static double GetZ(Box box, double vSize)
        {
            BoundingBox bb = box.BoundingBox;
            Point3d min = bb.Min;
            Point3d max = bb.Max;

            var snappedMin = new Point3d(
                Math.Floor(min.X / 1.0) * 1.0,
                Math.Floor(min.Y / 1.0) * 1.0,
                Math.Floor(min.Z / vSize) * vSize);

            var snappedMax = new Point3d(
                Math.Ceiling(max.X / 1.0) * 1.0,
                Math.Ceiling(max.Y / 1.0) * 1.0,
                Math.Ceiling(max.Z / vSize) * vSize);

            var snappedBb = new BoundingBox(snappedMin, snappedMax);
            Vector3d diagonal = snappedBb.Diagonal;
            return Convert.ToInt32(Math.Abs(diagonal.Z / vSize));
        }

        /// <summary>
        /// Three-colour gradient: left-centre-right, selected based on whether
        /// the percentage is below or above 0.5.
        /// </summary>
        public static Color GetTriColour(double percent, Color left, Color centre, Color right)
        {
            double t = (Math.Cos((percent * 2.0 - 1.0) * Math.PI) + 1.0) / 2.0;
            Color startColor = (percent < 0.5) ? left : right;
            return GetColourFromLinearGradient(t, startColor, centre);
        }

        /// <summary>
        /// Linear gradient interpolation between two colors.
        /// </summary>
        public static Color GetColourFromLinearGradient(double percent, Color start, Color end)
        {
            double inv = 1.0 - percent;
            double a = Math.Min(start.A, end.A) + (double)Math.Abs(start.A - end.A) * ((start.A > end.A) ? inv : percent);
            double r = Math.Min(start.R, end.R) + (double)Math.Abs(start.R - end.R) * ((start.R > end.R) ? inv : percent);
            double g = Math.Min(start.G, end.G) + (double)Math.Abs(start.G - end.G) * ((start.G > end.G) ? inv : percent);
            double b = Math.Min(start.B, end.B) + (double)Math.Abs(start.B - end.B) * ((start.B > end.B) ? inv : percent);
            return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
        }
    }
}
