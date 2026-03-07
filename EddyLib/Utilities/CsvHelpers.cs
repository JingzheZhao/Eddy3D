using System;
using System.IO;
using System.Linq;

namespace EddyLib
{
    /// <summary>
    /// Helper methods for CSV file I/O operations.
    /// </summary>
    public static class CsvHelpers
    {
        /// <summary>
        /// Writes a jagged array of doubles to a CSV file.
        /// </summary>
        public static void JaggedArray2CSV(double[][] data, string filePath)
        {
            // Bolt: Stream characters directly to output to bypass string concatenation
            // inside the nested loop, avoiding O(N^2) memory allocations and GC overhead.
            using (StreamWriter outfile = new StreamWriter(filePath))
            {
                for (int x = 0; x < data.Length; x++)
                {
                    for (int y = 0; y < data[x].Length; y++)
                    {
                        outfile.Write(data[x][y].ToString());
                        outfile.Write(',');
                    }
                    outfile.WriteLine();
                }
            }
        }

        /// <summary>
        /// Reads a CSV file into a jagged array of objects.
        /// </summary>
        public static object[][] CSV2JaggedArray(string filePath)
        {
            object[][] data = File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();
            return data;
        }

        /// <summary>
        /// Writes a 2D array of doubles to a CSV file.
        /// </summary>
        public static void Array2DToCSV(double[,] data, string filePath, bool truncateDoubles, int truncateBy = 1)
        {
            // Bolt: Stream characters directly to output to bypass string concatenation
            // inside the nested loop, avoiding O(N^2) memory allocations and GC overhead.
            using (StreamWriter outfile = new StreamWriter(filePath))
            {
                for (int x = 0; x <= data.GetUpperBound(0); x++)
                {
                    for (int y = 0; y <= data.GetUpperBound(1); y++)
                    {
                        double value = truncateDoubles ? Math.Round(data[x, y], truncateBy) : data[x, y];
                        outfile.Write(value.ToString());
                        outfile.Write(',');
                    }
                    outfile.WriteLine();
                }
            }
        }

        /// <summary>
        /// Writes a 1D array of doubles to a CSV file.
        /// </summary>
        public static void Array1DToCSV(double[] data, string filePath, bool truncateDoubles = true, int truncateBy = 1)
        {
            // Bolt: Stream characters directly to output to bypass string concatenation
            // avoiding unnecessary memory allocations and GC overhead.
            using (StreamWriter outfile = new StreamWriter(filePath))
            {
                for (int x = 0; x <= data.GetUpperBound(0); x++)
                {
                    double value = truncateDoubles ? Math.Round(data[x], truncateBy) : data[x];
                    outfile.Write(value.ToString());
                    outfile.WriteLine(',');
                }
            }
        }
    }
}
