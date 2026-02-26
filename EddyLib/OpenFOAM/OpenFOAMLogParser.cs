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

            try
            {
                var lines = ReadLastRunLines(logPath);
                AnalyzeSimulation(lines, status, options ?? new OpenFOAMLogParseOptions());
            }
            catch (Exception ex)
            {
                status.HasError = true;
                status.ErrorMessage = ex.Message;
            }

            return status;
        }

        public static OpenFOAMLogStatus ParseMeshingLog(string logPath, OpenFOAMLogParseOptions options = null)
        {
            var status = new OpenFOAMLogStatus { LogPath = logPath };

            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
                return status;

            status.HasLog = true;

            try
            {
                var lines = ReadLastRunLines(logPath);
                AnalyzeMeshing(lines, status, options ?? new OpenFOAMLogParseOptions());
            }
            catch (Exception ex)
            {
                status.HasError = true;
                status.ErrorMessage = ex.Message;
            }

            return status;
        }

        private static void AnalyzeSimulation(IReadOnlyList<string> lines, OpenFOAMLogStatus status, OpenFOAMLogParseOptions options)
        {
            bool hasError = false;
            bool finished = false;
            double? currentTime = null;
            double? executionTime = null;
            string lastNonEmpty = null;

            var records = new List<(double Time, double Exec)>();

            foreach (var raw in lines)
            {
                var line = raw?.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                lastNonEmpty = line;

                if (IsErrorLine(line))
                {
                    hasError = true;
                    if (string.IsNullOrWhiteSpace(status.ErrorMessage))
                        status.ErrorMessage = line;
                }

                if (IsEndLine(line))
                    finished = true;

                if (TryParseTimeLine(line, out var timeVal))
                    currentTime = timeVal;

                if (TryParseExecutionTime(line, out var execVal))
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

        private static void AnalyzeMeshing(IReadOnlyList<string> lines, OpenFOAMLogStatus status, OpenFOAMLogParseOptions options)
        {
            bool hasError = false;
            bool finished = false;
            string lastNonEmpty = null;

            string phase = null;
            int? currentMorphIteration = null;
            int? totalMorphIterations = null;
            double currentMorphDuration = 0;
            var morphDurations = new List<double>();

            foreach (var raw in lines)
            {
                var line = raw?.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                lastNonEmpty = line;

                if (IsErrorLine(line))
                {
                    hasError = true;
                    if (string.IsNullOrWhiteSpace(status.ErrorMessage))
                        status.ErrorMessage = line;
                }

                if (line.StartsWith("Finished meshing", StringComparison.OrdinalIgnoreCase))
                    finished = true;

                if (IsEndLine(line))
                    finished = true;

                if (line.IndexOf("Morphing phase", StringComparison.OrdinalIgnoreCase) >= 0)
                    phase = "Morphing";
                else if (line.IndexOf("Refinement phase", StringComparison.OrdinalIgnoreCase) >= 0)
                    phase = "Refinement";

                if (TryParseMorphIterationsTotal(line, out var totalMorph))
                    totalMorphIterations = totalMorph;

                if (TryParseMorphIteration(line, out var morphIter))
                {
                    if (currentMorphIteration.HasValue && currentMorphDuration > 0)
                        morphDurations.Add(currentMorphDuration);

                    currentMorphIteration = morphIter;
                    currentMorphDuration = 0;
                    phase = "Morphing";
                }

                if (currentMorphIteration.HasValue)
                {
                    if (TryParseDurationLine(line, "Calculated surface displacement in", out var seconds) ||
                        TryParseDurationLine(line, "Displacement smoothed in", out seconds) ||
                        TryParseDurationLine(line, "Moved mesh in", out seconds))
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

        /// <summary>
        /// Reads the log file and extracts lines for the most recent run only.
        /// This is memory-optimized to avoid loading the entire file content at once.
        /// </summary>
        private static List<string> ReadLastRunLines(string path)
        {
            var lines = new List<string>();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // "Create time" marks the start of a new run (or restart)
                    if (line.IndexOf("Create time", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        lines.Clear();
                    }
                    lines.Add(line);
                }
            }
            return lines;
        }

        private static bool IsEndLine(string line) =>
            line.Equals("End", StringComparison.OrdinalIgnoreCase);

        private static bool IsErrorLine(string line)
        {
            if (line.IndexOf("FOAM FATAL", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (line.StartsWith("Aborting", StringComparison.OrdinalIgnoreCase))
                return true;

            if (line.IndexOf("Segmentation fault", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private static bool TryParseTimeLine(string line, out double time)
        {
            time = 0;
            if (!line.StartsWith("Time", StringComparison.OrdinalIgnoreCase))
                return false;

            return TryParseAfterEquals(line, out time);
        }

        private static bool TryParseExecutionTime(string line, out double execTime)
        {
            execTime = 0;
            if (line.IndexOf("ExecutionTime", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return TryParseAfterEquals(line, out execTime);
        }

        private static bool TryParseMorphIterationsTotal(string line, out int total)
        {
            total = 0;
            const string marker = "Snapping to features in";
            int idx = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            var tail = line.Substring(idx + marker.Length).Trim();
            var token = tail.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out total);
        }

        private static bool TryParseMorphIteration(string line, out int iteration)
        {
            iteration = 0;
            const string marker = "Morph iteration";
            if (line.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            var tail = line.Substring(line.IndexOf(marker, StringComparison.OrdinalIgnoreCase) + marker.Length).Trim();
            var token = tail.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out iteration);
        }

        private static bool TryParseDurationLine(string line, string marker, out double seconds)
        {
            seconds = 0;
            if (line.IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            return TryParseAfterEquals(line, out seconds);
        }

        private static bool TryParseAfterEquals(string line, out double value)
        {
            value = 0;
            var idx = line.IndexOf('=');
            if (idx < 0 || idx + 1 >= line.Length)
                return false;

            var tail = line.Substring(idx + 1).Trim();
            var token = tail.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(token))
                return false;

            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
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
