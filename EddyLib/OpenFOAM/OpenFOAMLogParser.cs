using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Parses OpenFOAM log files to extract runtime status.
    /// </summary>
    public static class OpenFOAMLogParser
    {
        public static OpenFOAMLogStatus ParseSimulationLog(string logPath, OpenFOAMLogParseOptions options = null)
        {
            var status = new OpenFOAMLogStatus { LogPath = logPath };

            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
                return status;

            status.HasLog = true;
            options = options ?? new OpenFOAMLogParseOptions();

            try
            {
                using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    ParseSimulationStream(reader, status, options);
                }
            }
            catch (Exception ex)
            {
                status.HasError = true;
                status.ErrorMessage = ex.Message;
            }

            return status;
        }

        private static void ParseSimulationStream(StreamReader reader, OpenFOAMLogStatus status, OpenFOAMLogParseOptions options)
        {
            bool hasError = false;
            bool finished = false;
            double? currentTime = null;
            double? executionTime = null;
            string lastNonEmpty = null;

            var records = new List<(double Time, double Exec)>();

            string rawLine;
            while ((rawLine = reader.ReadLine()) != null)
            {
                var lineSpan = rawLine.AsSpan().Trim();
                if (lineSpan.IsEmpty)
                    continue;

                lastNonEmpty = rawLine.Trim();

                // Reset state on new run
                if (lineSpan.IndexOf("Create time".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hasError = false;
                    finished = false;
                    currentTime = null;
                    executionTime = null;
                    status.ErrorMessage = null;
                    records.Clear();
                }

                if (IsErrorLine(lineSpan))
                {
                    hasError = true;
                    if (string.IsNullOrWhiteSpace(status.ErrorMessage))
                        status.ErrorMessage = lastNonEmpty;
                }

                if (IsEndLine(lineSpan))
                    finished = true;

                if (TryParseTimeLine(lineSpan, out var timeVal))
                    currentTime = timeVal;

                if (TryParseExecutionTime(lineSpan, out var execVal))
                {
                    executionTime = execVal;
                    if (currentTime.HasValue)
                        records.Add((currentTime.Value, execVal));
                }
            }

            if (!hasError && currentTime.HasValue && options?.TotalIterations.HasValue == true
                && currentTime.Value >= options.TotalIterations.Value)
            {
                finished = true;
            }

            status.LastLogLine = lastNonEmpty;
            status.HasError = hasError;
            status.IsFinished = finished && !hasError;
            status.CurrentIteration = currentTime;
            status.TotalIterations = options?.TotalIterations;
            status.ExecutionTimeSeconds = executionTime;

            if (!status.IsFinished && currentTime.HasValue && options?.TotalIterations.HasValue == true)
            {
                var avg = RollingAverageDuration(records, options.RollingWindow);
                if (avg.HasValue)
                {
                    var remaining = Math.Max(0, options.TotalIterations.Value - currentTime.Value);
                    status.EstimatedRemaining = TimeSpan.FromSeconds(remaining * avg.Value);
                }
            }
        }

        public static OpenFOAMLogStatus ParseMeshingLog(string logPath, OpenFOAMLogParseOptions options = null)
        {
            var status = new OpenFOAMLogStatus { LogPath = logPath };

            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
                return status;

            status.HasLog = true;
            options = options ?? new OpenFOAMLogParseOptions();

            try
            {
                using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    ParseMeshingStream(reader, status, options);
                }
            }
            catch (Exception ex)
            {
                status.HasError = true;
                status.ErrorMessage = ex.Message;
            }

            return status;
        }

        private static void ParseMeshingStream(StreamReader reader, OpenFOAMLogStatus status, OpenFOAMLogParseOptions options)
        {
            bool hasError = false;
            bool finished = false;
            string lastNonEmpty = null;

            string phase = null;
            int? currentMorphIteration = null;
            int? totalMorphIterations = null;
            double currentMorphDuration = 0;
            var morphDurations = new List<double>();

            string rawLine;
            while ((rawLine = reader.ReadLine()) != null)
            {
                var lineSpan = rawLine.AsSpan().Trim();
                if (lineSpan.IsEmpty)
                    continue;

                lastNonEmpty = rawLine.Trim();

                // Reset state on new run
                if (lineSpan.IndexOf("Create time".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hasError = false;
                    finished = false;
                    status.ErrorMessage = null;
                    phase = null;
                    currentMorphIteration = null;
                    totalMorphIterations = null;
                    currentMorphDuration = 0;
                    morphDurations.Clear();
                }

                if (IsErrorLine(lineSpan))
                {
                    hasError = true;
                    if (string.IsNullOrWhiteSpace(status.ErrorMessage))
                        status.ErrorMessage = lastNonEmpty;
                }

                if (lineSpan.StartsWith("Finished meshing".AsSpan(), StringComparison.OrdinalIgnoreCase))
                    finished = true;

                if (IsEndLine(lineSpan))
                    finished = true;

                if (lineSpan.IndexOf("Morphing phase".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    phase = "Morphing";
                else if (lineSpan.IndexOf("Refinement phase".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    phase = "Refinement";

                if (TryParseMorphIterationsTotal(lineSpan, out var totalMorph))
                    totalMorphIterations = totalMorph;

                if (TryParseMorphIteration(lineSpan, out var morphIter))
                {
                    if (currentMorphIteration.HasValue && currentMorphDuration > 0)
                        morphDurations.Add(currentMorphDuration);

                    currentMorphIteration = morphIter;
                    currentMorphDuration = 0;
                    phase = "Morphing";
                }

                if (currentMorphIteration.HasValue)
                {
                    if (TryParseDurationLine(lineSpan, "Calculated surface displacement in", out var seconds) ||
                        TryParseDurationLine(lineSpan, "Displacement smoothed in", out seconds) ||
                        TryParseDurationLine(lineSpan, "Moved mesh in", out seconds))
                    {
                        currentMorphDuration += seconds;
                    }
                }
            }

            if (currentMorphIteration.HasValue && currentMorphDuration > 0)
                morphDurations.Add(currentMorphDuration);

            status.LastLogLine = lastNonEmpty;
            status.HasError = hasError;
            status.IsFinished = finished && !hasError;
            status.Phase = phase;
            status.MorphIteration = currentMorphIteration;
            status.MorphIterationsTotal = totalMorphIterations;

            if (!status.IsFinished
                && string.Equals(phase, "Morphing", StringComparison.OrdinalIgnoreCase)
                && currentMorphIteration.HasValue
                && totalMorphIterations.HasValue)
            {
                var avg = RollingAverage(morphDurations, options.RollingWindow);
                if (avg.HasValue)
                {
                    var remaining = Math.Max(0, totalMorphIterations.Value - 1 - currentMorphIteration.Value);
                    status.EstimatedRemaining = TimeSpan.FromSeconds(remaining * avg.Value);
                }
            }
        }

        private static bool IsEndLine(ReadOnlySpan<char> line) =>
            line.Equals("End", StringComparison.OrdinalIgnoreCase);

        private static bool IsErrorLine(ReadOnlySpan<char> line)
        {
            if (line.IndexOf("FOAM FATAL".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (line.StartsWith("Aborting", StringComparison.OrdinalIgnoreCase))
                return true;

            if (line.IndexOf("Segmentation fault".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private static bool TryParseTimeLine(ReadOnlySpan<char> line, out double time)
        {
            time = 0;
            if (!line.StartsWith("Time", StringComparison.OrdinalIgnoreCase))
                return false;

            return TryParseAfterEquals(line, out time);
        }

        private static bool TryParseExecutionTime(ReadOnlySpan<char> line, out double execTime)
        {
            execTime = 0;
            if (line.IndexOf("ExecutionTime".AsSpan(), StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return TryParseAfterEquals(line, out execTime);
        }

        private static bool TryParseMorphIterationsTotal(ReadOnlySpan<char> line, out int total)
        {
            total = 0;
            const string marker = "Snapping to features in";
            int idx = line.IndexOf(marker.AsSpan(), StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            var tail = line.Slice(idx + marker.Length).Trim();
            return TryParseFirstIntegerToken(tail, out total);
        }

        private static bool TryParseMorphIteration(ReadOnlySpan<char> line, out int iteration)
        {
            iteration = 0;
            const string marker = "Morph iteration";
            int idx = line.IndexOf(marker.AsSpan(), StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            var tail = line.Slice(idx + marker.Length).Trim();
            return TryParseFirstIntegerToken(tail, out iteration);
        }

        private static bool TryParseDurationLine(ReadOnlySpan<char> line, string marker, out double seconds)
        {
            seconds = 0;
            if (line.IndexOf(marker.AsSpan(), StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return TryParseAfterEquals(line, out seconds);
        }

        private static bool TryParseAfterEquals(ReadOnlySpan<char> line, out double value)
        {
            value = 0;
            var idx = line.IndexOf('=');
            if (idx < 0 || idx + 1 >= line.Length)
                return false;

            var tail = line.Slice(idx + 1).Trim();
            return TryParseFirstDoubleToken(tail, out value);
        }

        private static bool TryParseFirstDoubleToken(ReadOnlySpan<char> span, out double value)
        {
            value = 0;
            var token = GetFirstToken(span);
            if (token.IsEmpty) return false;
            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseFirstIntegerToken(ReadOnlySpan<char> span, out int value)
        {
            value = 0;
            var token = GetFirstToken(span);
            if (token.IsEmpty) return false;
            return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static ReadOnlySpan<char> GetFirstToken(ReadOnlySpan<char> span)
        {
            int end = 0;
            while (end < span.Length && !char.IsWhiteSpace(span[end]))
            {
                end++;
            }
            return span.Slice(0, end);
        }

        private static double? RollingAverageDuration(List<(double Time, double Exec)> records, int window)
        {
            if (records == null || records.Count < 2)
                return null;

            var durations = new List<double>();
            for (int i = 1; i < records.Count; i++)
            {
                var delta = records[i].Exec - records[i - 1].Exec;
                if (delta > 0)
                    durations.Add(delta);
            }

            return RollingAverage(durations, window);
        }

        private static double? RollingAverage(List<double> values, int window)
        {
            if (values == null || values.Count == 0)
                return null;

            int take = Math.Max(1, window);
            var slice = values.Skip(Math.Max(0, values.Count - take));
            return slice.Average();
        }
    }
}
