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
        #region 6. LoadResultFile

        public static int[] LoadResultFile(int HCnt, string Path, bool Run)
        {
            // Optimization: Use streaming to avoid loading all lines into memory.
            // Use Span to avoid string allocations for trimming.

            var result = new List<int>();
            int currentHitCount = 0;
            int rayIndex = 0;

            // File.ReadLines is lazy and efficient
            foreach (string line in File.ReadLines(Path))
            {
                ReadOnlySpan<char> span = line.AsSpan().TrimStart();

                // Check if line starts with '*'
                if (span.Length > 0 && span[0] == '*')
                {
                    currentHitCount++;
                }

                rayIndex++;

                if (rayIndex == HCnt)
                {
                    result.Add(currentHitCount);
                    currentHitCount = 0;
                    rayIndex = 0;
                }
            }

            return result.ToArray();
        }

        #endregion 6. LoadResultFile
    }
}
