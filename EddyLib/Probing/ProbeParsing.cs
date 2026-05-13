using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib
{
    internal static class ProbeParsing
    {
        internal static GH_Number[] ParseScalars(string fullPath, int pointCount)
        {
            var lastLine = ReadLastDataLine(fullPath);
            if (string.IsNullOrEmpty(lastLine))
                return new GH_Number[pointCount];

            var span = lastLine.AsSpan();
            var results = new GH_Number[pointCount];
            for (int i = 0; i < pointCount; i++) results[i] = new GH_Number(0);
            int valIdx = 0;

            // Skip initial spaces if any
            while (span.Length > 0 && span[0] == ' ')
            {
                span = span.Slice(1);
            }

            // Find the end of the time token (first space)
            int idx = span.IndexOf(' ');
            if (idx != -1)
            {
                span = span.Slice(idx + 1);
            }
            else
            {
                return results; // No data after time
            }

            while ((idx = span.IndexOf(' ')) != -1 && valIdx < pointCount)
            {
                if (idx > 0)
                {
                    var temp = double.Parse(span.Slice(0, idx), System.Globalization.CultureInfo.InvariantCulture);
                    var target = new GH_Number(0);
                    GH_Convert.ToGHNumber(temp, GH_Conversion.Both, ref target);
                    results[valIdx++] = target;
                }
                span = span.Slice(idx + 1);
            }

            if (span.Length > 0 && valIdx < pointCount)
            {
                var temp = double.Parse(span, System.Globalization.CultureInfo.InvariantCulture);
                var target = new GH_Number(0);
                GH_Convert.ToGHNumber(temp, GH_Conversion.Both, ref target);
                results[valIdx] = target;
            }

            return results;
        }

        internal static GH_Vector[] ParseVectors(string fullPath, int pointCount)
        {
            var lastLine = ReadLastDataLine(fullPath);
            if (string.IsNullOrEmpty(lastLine))
                return new GH_Vector[pointCount];

            var span = lastLine.AsSpan();
            var results = new GH_Vector[pointCount];
            for (int i = 0; i < pointCount; i++) results[i] = new GH_Vector(Vector3d.Zero);
            int maxVals = pointCount * 3;
            var values = new double[maxVals];
            int valIdx = 0;

            // Skip initial spaces if any
            while (span.Length > 0 && span[0] == ' ')
            {
                span = span.Slice(1);
            }

            // Find the end of the time token (first space)
            int idx = span.IndexOf(' ');
            if (idx != -1)
            {
                span = span.Slice(idx + 1);
            }
            else
            {
                return results; // No data after time
            }

            while ((idx = span.IndexOfAny(' ', '(', ')')) != -1 && valIdx < maxVals)
            {
                if (idx > 0)
                {
                    values[valIdx++] = double.Parse(span.Slice(0, idx), System.Globalization.CultureInfo.InvariantCulture);
                }
                span = span.Slice(idx + 1);
            }

            if (span.Length > 0 && valIdx < maxVals)
            {
                values[valIdx++] = double.Parse(span, System.Globalization.CultureInfo.InvariantCulture);
            }

            for (int i = 0; i < pointCount; i++)
            {
                if (i * 3 + 2 < valIdx)
                {
                    var temp = new Vector3d(values[i * 3], values[i * 3 + 1], values[i * 3 + 2]);
                    var target = new GH_Vector();
                    GH_Convert.ToGHVector(temp, GH_Conversion.Both, ref target);
                    results[i] = target;
                }
            }

            return results;
        }

        /// <summary>
        /// Reads the file backwards to find the last valid data line.
        /// This avoids reading the entire file into memory (O(N) -> O(1) for typical cases).
        /// </summary>
        private static string ReadLastDataLine(string fullPath)
        {
            try
            {
                using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length == 0) return string.Empty;

                    long pos = fs.Length;
                    const int bufferSize = 4096;
                    byte[] buffer = new byte[bufferSize];
                    List<byte> currentLineBytes = new List<byte>();

                    while (pos > 0)
                    {
                        int bytesToRead = (int)Math.Min(pos, bufferSize);
                        pos -= bytesToRead;
                        fs.Seek(pos, SeekOrigin.Begin);
                        int read = fs.Read(buffer, 0, bytesToRead);

                        // Iterate backwards through the buffer
                        for (int i = read - 1; i >= 0; i--)
                        {
                            byte b = buffer[i];
                            if (b == '\n')
                            {
                                if (currentLineBytes.Count > 0)
                                {
                                    // We found a line (accumulated backwards)
                                    currentLineBytes.Reverse();
                                    string line = Encoding.UTF8.GetString(currentLineBytes.ToArray()).Trim();
                                    currentLineBytes.Clear();

                                    if (IsValidLine(line))
                                    {
                                        return line;
                                    }
                                }
                            }
                            else if (b != '\r') // Ignore CR
                            {
                                currentLineBytes.Add(b);
                            }
                        }
                    }

                    // Process the first line of the file if we reached the beginning
                    if (currentLineBytes.Count > 0)
                    {
                        currentLineBytes.Reverse();
                        string line = Encoding.UTF8.GetString(currentLineBytes.ToArray()).Trim();
                        if (IsValidLine(line))
                        {
                            return line;
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Fallback or handle appropriately
                return string.Empty;
            }

            return string.Empty;
        }

        private static bool IsValidLine(string line)
        {
            return !string.IsNullOrWhiteSpace(line) &&
                   !line.StartsWith("#", StringComparison.Ordinal) &&
                   !line.StartsWith("Time", StringComparison.OrdinalIgnoreCase);
        }
    }
}
