using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace EddyLib
{
    internal static class ProbeParsing
    {
        private static readonly Regex VectorCleanupRegex = new Regex("[()]", RegexOptions.Compiled);

        internal static GH_Number[] ParseScalars(string fullPath, int pointCount)
        {
            var lastLine = ReadLastDataLine(fullPath);
            var parts = SplitLine(lastLine);

            var results = new GH_Number[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                var temp = double.Parse(parts[i + 1]);
                var target = new GH_Number(0);
                GH_Convert.ToGHNumber(temp, GH_Conversion.Both, ref target);
                results[i] = target;
            }

            return results;
        }

        internal static GH_Vector[] ParseVectors(string fullPath, int pointCount)
        {
            var lastLine = ReadLastDataLine(fullPath);
            var cleaned = VectorCleanupRegex.Replace(lastLine, string.Empty);
            var parts = SplitLine(cleaned);

            var results = new GH_Vector[pointCount];
            int counter = 1;
            for (int i = 0; i < pointCount; i++)
            {
                var temp = new Vector3d(double.Parse(parts[counter]), double.Parse(parts[counter + 1]), double.Parse(parts[counter + 2]));
                var target = new GH_Vector();
                GH_Convert.ToGHVector(temp, GH_Conversion.Both, ref target);
                results[i] = target;
                counter += 3;
            }

            return results;
        }

        private static string ReadLastDataLine(string fullPath)
        {
            return File.ReadLines(fullPath)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Where(line => !line.StartsWith("#", StringComparison.Ordinal))
                .Where(line => !line.StartsWith("Time", StringComparison.OrdinalIgnoreCase))
                .Last();
        }

        private static string[] SplitLine(string line)
        {
            return line.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
