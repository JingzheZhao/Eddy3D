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

            var parts = lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var results = new GH_Number[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                // parts[0] is time, so data starts at index 1
                if (i + 1 < parts.Length)
                {
                    var temp = double.Parse(parts[i + 1]);
                    var target = new GH_Number(0);
                    GH_Convert.ToGHNumber(temp, GH_Conversion.Both, ref target);
                    results[i] = target;
                }
            }

            return results;
        }

        internal static GH_Vector[] ParseVectors(string fullPath, int pointCount)
        {
            var lastLine = ReadLastDataLine(fullPath);
            if (string.IsNullOrEmpty(lastLine))
                return new GH_Vector[pointCount];

            // Optimized parsing: split by space, '(', and ')' directly.
            // This avoids regex allocation and intermediate string creation.
            var parts = lastLine.Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            var results = new GH_Vector[pointCount];
            int counter = 1; // Start at 1 to skip Time
            for (int i = 0; i < pointCount; i++)
            {
                if (counter + 2 < parts.Length)
                {
                    var temp = new Vector3d(double.Parse(parts[counter]), double.Parse(parts[counter + 1]), double.Parse(parts[counter + 2]));
                    var target = new GH_Vector();
                    GH_Convert.ToGHVector(temp, GH_Conversion.Both, ref target);
                    results[i] = target;
                    counter += 3;
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
