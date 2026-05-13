using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace EddyLib
{
    public sealed class ResidualsData
    {
        public ResidualsData(List<double> iterations, List<string> fieldNames, List<List<double>> values)
        {
            Iterations = iterations ?? throw new ArgumentNullException(nameof(iterations));
            FieldNames = fieldNames ?? throw new ArgumentNullException(nameof(fieldNames));
            Values = values ?? throw new ArgumentNullException(nameof(values));
        }

        public IReadOnlyList<double> Iterations { get; }

        public IReadOnlyList<string> FieldNames { get; }

        public IReadOnlyList<IReadOnlyList<double>> Values { get; }
    }

    public static class Residuals
    {
        public static ResidualsData Parse(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Residuals file path is required.", nameof(filePath));
            }

            var iterations = new List<double>();
            var fieldNames = new List<string>();
            var values = new List<List<double>>();
            var rowValues = new List<double>(); // reuse buffer

            foreach (var rawLine in Utilities.ReadLinesSafe(filePath))
            {
                ReadOnlySpan<char> span = rawLine.AsSpan().Trim();
                if (span.Length == 0)
                {
                    continue;
                }

                if (span[0] == '#')
                {
                    if (fieldNames.Count == 0)
                    {
                        var header = span.Slice(1).Trim();
                        var headerFields = SplitFieldsSpan(header);
                        if (headerFields.Count > 1)
                        {
                            for (int i = 1; i < headerFields.Count; i++)
                            {
                                fieldNames.Add(headerFields[i]);
                            }
                        }
                    }
                    continue;
                }

                // Optimization: avoid string.Split to reduce GC allocations on O(N) lines parsing.
                // Collect parsed values temporarily to ensure the row is complete (>= 2 columns)
                // before appending to the main lists, preserving original parallel array sync and exceptions.
                int start = 0;
                rowValues.Clear(); // reuse buffer
                for (int i = 0; i <= span.Length; i++)
                {
                    if (i == span.Length || span[i] == ' ' || span[i] == '\t')
                    {
                        if (i > start)
                        {
                            var token = span.Slice(start, i - start);
                            rowValues.Add(double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture));
                        }
                        start = i + 1;
                    }
                }

                if (rowValues.Count < 2)
                {
                    continue;
                }

                EnsureValueLists(values, rowValues.Count - 1);
                iterations.Add(rowValues[0]);

                for (int i = 1; i < rowValues.Count; i++)
                {
                    values[i - 1].Add(rowValues[i]);
                }
            }

            if (fieldNames.Count == 0)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    fieldNames.Add($"Field{i + 1}");
                }
            }

            return new ResidualsData(iterations, fieldNames, values);
        }

        private static List<string> SplitFieldsSpan(ReadOnlySpan<char> span)
        {
            var results = new List<string>();
            int start = 0;
            for (int i = 0; i <= span.Length; i++)
            {
                if (i == span.Length || span[i] == ' ' || span[i] == '\t')
                {
                    if (i > start)
                    {
                        results.Add(span.Slice(start, i - start).ToString());
                    }
                    start = i + 1;
                }
            }
            return results;
        }

        private static void EnsureValueLists(List<List<double>> values, int count)
        {
            while (values.Count < count)
            {
                values.Add(new List<double>());
            }
        }
    }
}
