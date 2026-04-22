using System;
using System.Drawing;

namespace EddyLib.GAN
{
    /// <summary>
    /// Viridis colormap for GAN result mesh display.
    /// Uses the same anchor colors as the Wind Predictor Viridis palette.
    /// </summary>
    public static class ViridisColormap
    {
        private static readonly Color[] Stops =
        {
            Color.FromArgb(68, 1, 84),
            Color.FromArgb(59, 82, 139),
            Color.FromArgb(33, 145, 140),
            Color.FromArgb(94, 201, 98),
            Color.FromArgb(253, 231, 37)
        };

        /// <summary>Returns an interpolated Viridis color for a value in [0, 1].</summary>
        public static Color GetColor(double value)
        {
            double clamped = Math.Max(0.0, Math.Min(1.0, value));
            double scaled = clamped * (Stops.Length - 1);
            int idx = (int)Math.Floor(scaled);
            int next = Math.Min(Stops.Length - 1, idx + 1);
            double frac = scaled - idx;

            Color a = Stops[idx];
            Color b = Stops[next];

            return Color.FromArgb(
                Interpolate(a.R, b.R, frac),
                Interpolate(a.G, b.G, frac),
                Interpolate(a.B, b.B, frac));
        }

        private static int Interpolate(int start, int end, double fraction)
        {
            return Convert.ToInt32(Math.Round(start + (end - start) * fraction));
        }
    }
}
