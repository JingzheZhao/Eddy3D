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

            foreach (var rawLine in File.ReadLines(filePath))
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith("#"))
                {
                    if (fieldNames.Count == 0)
                    {
                        var header = line.TrimStart('#').Trim();
                        var headerFields = SplitFields(header);
                        if (headerFields.Length > 1)
                        {
                            for (int i = 1; i < headerFields.Length; i++)
                            {
                                fieldNames.Add(headerFields[i]);
                            }
                        }
                    }
                    continue;
                }

                var parts = SplitFields(line);
                if (parts.Length < 2)
                {
                    continue;
                }

                EnsureValueLists(values, parts.Length - 1);
                iterations.Add(ParseDouble(parts[0]));

                for (int i = 1; i < parts.Length; i++)
                {
                    values[i - 1].Add(ParseDouble(parts[i]));
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

        private static string[] SplitFields(string line)
        {
            return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void EnsureValueLists(List<List<double>> values, int count)
        {
            while (values.Count < count)
            {
                values.Add(new List<double>());
            }
        }

        private static double ParseDouble(string value)
        {
            return double.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}
