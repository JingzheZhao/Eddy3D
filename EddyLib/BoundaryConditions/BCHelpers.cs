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
                uref != null ? FormatSummaryDouble(uref[i]) : "",
                zref != null ? FormatSummaryDouble(zref[i]) : null,
                z0  != null ? FormatSummaryDouble(z0[i])  : null,
                zGround != null ? FormatSummaryDouble(zGround[i]) : null
            }.Where(s => s != null).ToArray());
            }
            return rows;
        }

        private static string FormatSummaryDouble(double value)
        {
            var abs = Math.Abs(value);
            if (abs == 0)
            {
                return "0";
            }

            // Keep common values compact but do not collapse small non-zero values to 0.
            return abs >= 0.01
                ? value.ToString("0.##", CultureInfo.InvariantCulture)
                : value.ToString("0.######", CultureInfo.InvariantCulture);
        }

        // Core: measure strings with GH's font and add spaces until columns line up in pixels.
        private static string BuildPixelAligned(string[] headers, List<string[]> rows, string epwPath)
        {
            var colCount = headers.Length;
            var outSb = new StringBuilder();

            try
            {
                // Attempt to pixel-align using Grasshopper's font (fails on macOS headless)
                var font = GH_FontServer.Small;
                var gapPx = 12f; // space between columns
                var fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
                fmt.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                float Measure(Graphics g, string s) => g.MeasureString(s, font, int.MaxValue, fmt).Width;

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

                    var starts = new float[colCount];
                    float x = 0;
                    for (int c = 0; c < colCount; c++)
                    {
                        starts[c] = x;
                        x += colWidths[c] + gapPx;
                    }

                    string PadTo(string baseText, float targetX)
                    {
                        var sb = new StringBuilder(baseText);
                        while (Measure(g, sb.ToString()) < targetX)
                            sb.Append(' ');
                        return sb.ToString();
                    }

                    outSb.AppendLine("Boundary Conditions Summary:");

                    var line = headers[0];
                    for (int c = 1; c < colCount; c++)
                        line = PadTo(line, starts[c]) + headers[c];
                    outSb.AppendLine(line);

                    var headerWidth = Measure(g, line);
                    outSb.AppendLine(new string('-', (int)Math.Max(8, headerWidth / 3)));

                    for (int r = 0; r < rows.Count; r++)
                    {
                        var row = rows[r];
                        var rowLine = row[0];
                        for (int c = 1; c < colCount; c++)
                            rowLine = PadTo(rowLine, starts[c]) + row[c];
                        outSb.AppendLine(rowLine);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback: character-based padding for macOS / headless tests
                outSb.Clear();
                var colWidths = new int[colCount];
                for (int c = 0; c < colCount; c++)
                {
                    int w = headers[c].Length;
                    for (int r = 0; r < rows.Count; r++)
                        w = Math.Max(w, rows[r][c].Length);
                    colWidths[c] = w;
                }

                int gap = 3; // spaces

                outSb.AppendLine("Boundary Conditions Summary:");

                var lineSb = new StringBuilder();
                for (int c = 0; c < colCount; c++)
                {
                    lineSb.Append(headers[c].PadRight(colWidths[c] + (c < colCount - 1 ? gap : 0)));
                }
                var line = lineSb.ToString();
                outSb.AppendLine(line);
                outSb.AppendLine(new string('-', line.Length));

                for (int r = 0; r < rows.Count; r++)
                {
                    var rowLineSb = new StringBuilder();
                    for (int c = 0; c < colCount; c++)
                    {
                        rowLineSb.Append(rows[r][c].PadRight(colWidths[c] + (c < colCount - 1 ? gap : 0)));
                    }
                    outSb.AppendLine(rowLineSb.ToString());
                }
            }

            if (!string.IsNullOrEmpty(epwPath))
                outSb.AppendLine(epwPath);

            return outSb.ToString();
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
