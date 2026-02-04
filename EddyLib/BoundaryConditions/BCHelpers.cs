using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.BCs
{
    public class BCHelpers
    {
        // Full version: Wind Dir + Uref + zref + z0 + zGround
        public static string BuildTable(
         IList<int> windDirs, IList<double> uref, IList<double> zref, IList<double> z0, IList<double> zGround, string epwPath)
        {
            var headers = new[] { "Wind Dir", "Uref", "zref", "z0", "zGround" };
            var rows = ToRows(windDirs, uref, zref, z0, zGround);
            return BuildPixelAligned(headers, rows, epwPath);
        }

        public static string BuildTable(
            IList<int> windDirs, IList<double> uref, IList<double> z0, string epwPath)
        {
            var headers = new[] { "Wind Dir", "Uref", "z0" };
            var rows = ToRows(windDirs, uref, null, z0, null);
            return BuildPixelAligned(headers, rows, epwPath);
        }

        private static List<string[]> ToRows(
            IList<int> windDirs, IList<double> uref, IList<double> zref, IList<double> z0, IList<double> zGround)
        {
            int n = windDirs?.Count ?? 0;
            var rows = new List<string[]>(n);
            for (int i = 0; i < n; i++)
            {
                rows.Add(new[]
                {
                windDirs[i].ToString(CultureInfo.InvariantCulture),
                uref?[i].ToString("0.##", CultureInfo.InvariantCulture) ?? "",
                zref != null ? zref[i].ToString("0.##", CultureInfo.InvariantCulture) : null,
                z0  != null ? z0[i].ToString("0.##", CultureInfo.InvariantCulture)  : null,
                zGround != null ? zGround[i].ToString("0.##", CultureInfo.InvariantCulture) : null
            }.Where(s => s != null).ToArray());
            }
            return rows;
        }

        // Core: measure strings with GH's font and add spaces until columns line up in pixels.
        private static string BuildPixelAligned(string[] headers, List<string[]> rows, string epwPath)
        {
            // Use GH's standard small UI font (same as tooltips)
            var font = GH_FontServer.Small;
            var gapPx = 12f; // space between columns
            var fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
            fmt.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

            // Measure function
            float Measure(Graphics g, string s) => g.MeasureString(s, font, int.MaxValue, fmt).Width;

            // Compute max pixel width per column (header + all rows)
            var colCount = headers.Length;
            var colWidths = new float[colCount];

            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                for (int c = 0; c < colCount; c++)
                {
                    float w = Measure(g, headers[c]);
                    for (int r = 0; r < rows.Count; r++)
                        w = Math.Max(w, Measure(g, rows[r][c]));
                    colWidths[c] = (float)Math.Ceiling(w);
                }

                // Column start x-positions
                var starts = new float[colCount];
                float x = 0;
                for (int c = 0; c < colCount; c++)
                {
                    starts[c] = x;
                    x += colWidths[c] + gapPx;
                }

                // Build lines, padding with spaces until next column start is reached in pixels
                string PadTo(string baseText, float targetX)
                {
                    var sb = new StringBuilder(baseText);
                    while (Measure(g, sb.ToString()) < targetX)
                        sb.Append(' ');
                    return sb.ToString();
                }

                var outSb = new StringBuilder();
                outSb.AppendLine("Boundary Conditions Summary:");

                // Header
                var line = headers[0];
                for (int c = 1; c < colCount; c++)
                    line = PadTo(line, starts[c]) + headers[c];
                outSb.AppendLine(line);

                // Separator (approximate using dashes to header width)
                var headerWidth = Measure(g, line);
                outSb.AppendLine(new string('-', (int)Math.Max(8, headerWidth / 3))); // 6px-ish per char

                // Rows
                for (int r = 0; r < rows.Count; r++)
                {
                    var row = rows[r];
                    var rowLine = row[0];
                    for (int c = 1; c < colCount; c++)
                        rowLine = PadTo(rowLine, starts[c]) + row[c];
                    outSb.AppendLine(rowLine);
                }

                if (!string.IsNullOrEmpty(epwPath))
                    outSb.AppendLine(epwPath);

                return outSb.ToString();
            }
        }

        public static void AdjustInputList<T>(
            IGH_DataAccess DA,
            string name,
            List<T> list,
            int targetCount,
            T defaultValue,
            ref bool flag)
        {
            var tempList = new List<T>();
            bool hasData = DA.GetDataList(name, tempList) && tempList.Count > 0;

            // Always mutate 'list' in-place so the caller sees the changes.
            list.Clear();

            if (hasData)
            {
                if (tempList.Count == 1)
                {
                    // Repeat single item across all wind directions
                    for (int i = 0; i < targetCount; i++)
                        list.Add(tempList[0]);
                    // This is intended behavior (not "missing"), so no need to set 'flag' here.
                }
                else
                {
                    // Copy provided values; pad to targetCount with defaults if needed
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (i < tempList.Count) list.Add(tempList[i]);
                        else { list.Add(defaultValue); flag = true; } // we had to assume/pad
                    }
                }
            }
            else
            {
                // No values provided: use defaults
                for (int i = 0; i < targetCount; i++)
                    list.Add(defaultValue);
                flag = true; // truly missing input
            }
        }

        public static void AdjustInputList<T>(
            IGH_DataAccess DA,
            int index,
            List<T> list,
            int targetCount,
            T defaultValue,
            ref bool flag)
        {
            var tempList = new List<T>();
            bool hasData = DA.GetDataList(index, tempList) && tempList.Count > 0;

            // Always mutate 'list' in-place so the caller sees the changes.
            list.Clear();

            if (hasData)
            {
                if (tempList.Count == 1)
                {
                    // Repeat single item across all wind directions
                    for (int i = 0; i < targetCount; i++)
                        list.Add(tempList[0]);
                    // This is intended behavior (not "missing"), so no need to set 'flag' here.
                }
                else
                {
                    // Copy provided values; pad to targetCount with defaults if needed
                    for (int i = 0; i < targetCount; i++)
                    {
                        if (i < tempList.Count) list.Add(tempList[i]);
                        else { list.Add(defaultValue); flag = true; } // we had to assume/pad
                    }
                }
            }
            else
            {
                // No values provided: use defaults
                for (int i = 0; i < targetCount; i++)
                    list.Add(defaultValue);
                flag = true; // truly missing input
            }
        }
    }
}