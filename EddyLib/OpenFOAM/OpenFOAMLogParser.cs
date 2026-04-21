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

        public static OpenFOAMLogStatus ParseMeshingWorkflow(string meshDir, OpenFOAMLogParseOptions options = null)
        {
            if (string.IsNullOrWhiteSpace(meshDir) || !Directory.Exists(meshDir))
                return new OpenFOAMLogStatus();

            return ParseMeshingWorkflowLogs(
                OpenFOAMLogLocator.FindLatestBlockMeshLog(meshDir),
                OpenFOAMLogLocator.FindLatestSurfaceFeaturesLog(meshDir),
                OpenFOAMLogLocator.FindLatestMeshingLog(meshDir),
                options);
        }

        public static OpenFOAMLogStatus ParseMeshingWorkflowLogs(
            string blockMeshLogPath,
            string surfaceFeaturesLogPath,
            string snappyHexMeshLogPath,
            OpenFOAMLogParseOptions options = null)
        {
            options = options ?? new OpenFOAMLogParseOptions();

            var blockMesh = ParseMeshingUtilityLog(blockMeshLogPath, "blockMesh");
            var surfaceFeatures = ParseMeshingUtilityLog(surfaceFeaturesLogPath, "surfaceFeatures");
            var snappyHexMesh = ParseMeshingLog(snappyHexMeshLogPath, options);

            if (IsLogOlderThan(surfaceFeaturesLogPath, blockMeshLogPath))
                surfaceFeatures = EmptyStep(surfaceFeaturesLogPath, "surfaceFeatures");

            string latestPreSnappyLog = LatestLogPath(blockMeshLogPath, surfaceFeaturesLogPath);
            if (IsLogOlderThan(snappyHexMeshLogPath, latestPreSnappyLog))
                snappyHexMesh = EmptyStep(snappyHexMeshLogPath, "snappyHexMesh");

            bool hasAnyLog = blockMesh.HasLog || surfaceFeatures.HasLog || snappyHexMesh.HasLog;
            var active = snappyHexMesh.HasLog
                ? snappyHexMesh
                : surfaceFeatures.HasLog
                    ? surfaceFeatures
                    : blockMesh;

            var status = new OpenFOAMLogStatus
            {
                LogPath = active?.LogPath,
                HasLog = hasAnyLog,
                HasError = blockMesh.HasError || surfaceFeatures.HasError || snappyHexMesh.HasError,
                ErrorMessage = FirstNonEmpty(blockMesh.ErrorMessage, surfaceFeatures.ErrorMessage, snappyHexMesh.ErrorMessage),
                IsFinished = snappyHexMesh.HasLog && snappyHexMesh.IsFinished
                    && !blockMesh.HasError && !surfaceFeatures.HasError && !snappyHexMesh.HasError,
                Phase = active?.Phase,
                StepName = active?.StepName,
                StepIndex = snappyHexMesh.HasLog ? 3 : surfaceFeatures.HasLog ? 2 : blockMesh.HasLog ? 1 : (int?)null,
                StepCount = 3,
                MorphIteration = snappyHexMesh.MorphIteration,
                MorphIterationsTotal = snappyHexMesh.MorphIterationsTotal,
                EstimatedRemaining = snappyHexMesh.EstimatedRemaining,
                LastLogLine = active?.LastLogLine,
                WarningCount = blockMesh.WarningCount + surfaceFeatures.WarningCount + snappyHexMesh.WarningCount,
                LastWarningLine = LastNonEmpty(blockMesh.LastWarningLine, surfaceFeatures.LastWarningLine, snappyHexMesh.LastWarningLine),
                BlockMeshLogPath = blockMeshLogPath,
                SurfaceFeaturesLogPath = surfaceFeaturesLogPath,
                SnappyHexMeshLogPath = snappyHexMeshLogPath
            };

            double blockProgress = StepProgress(blockMesh, surfaceFeatures.HasLog || snappyHexMesh.HasLog);
            double surfaceProgress = StepProgress(surfaceFeatures, snappyHexMesh.HasLog);
            double snappyProgress = snappyHexMesh.HasLog ? Clamp01(snappyHexMesh.Progress ?? 0.0) : 0.0;

            status.Progress = Clamp01((0.15 * blockProgress) + (0.10 * surfaceProgress) + (0.75 * snappyProgress));

            if (status.IsFinished)
                status.Progress = 1.0;

            return status;
        }

        private static OpenFOAMLogStatus EmptyStep(string logPath, string stepName)
        {
            return new OpenFOAMLogStatus
            {
                LogPath = logPath,
                StepName = stepName,
                Phase = stepName
            };
        }

        private static OpenFOAMLogStatus ParseMeshingUtilityLog(string logPath, string stepName)
        {
            var status = new OpenFOAMLogStatus
            {
                LogPath = logPath,
                StepName = stepName,
                Phase = stepName
            };

            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
                return status;

            status.HasLog = true;
            bool hasError = false;
            bool finished = false;
            int warningCount = 0;
            string lastWarning = null;
            string lastNonEmpty = null;
            double progress = 0.2;

            try
            {
                using (var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    string rawLine;
                    while ((rawLine = reader.ReadLine()) != null)
                    {
                        var lineSpan = rawLine.AsSpan().Trim();
                        if (lineSpan.IsEmpty)
                            continue;

                        lastNonEmpty = rawLine.Trim();

                        if (lineSpan.IndexOf("Create time".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            hasError = false;
                            finished = false;
                            status.ErrorMessage = null;
                            warningCount = 0;
                            lastWarning = null;
                            progress = 0.2;
                        }

                        if (IsWarningLine(lineSpan))
                        {
                            warningCount++;
                            lastWarning = lastNonEmpty;
                        }

                        if (IsErrorLine(lineSpan))
                        {
                            hasError = true;
                            if (string.IsNullOrWhiteSpace(status.ErrorMessage))
                                status.ErrorMessage = lastNonEmpty;
                        }

                        if (lineSpan.IndexOf("Writing".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                            progress = Math.Max(progress, 0.8);

                        if (IsEndLine(lineSpan))
                        {
                            finished = true;
                            progress = 1.0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                hasError = true;
                status.ErrorMessage = ex.Message;
            }

            status.HasError = hasError;
            status.IsFinished = finished && !hasError;
            status.LastLogLine = lastNonEmpty;
            status.WarningCount = warningCount;
            status.LastWarningLine = lastWarning;
            status.Progress = Clamp01(progress);
            return status;
        }

        private static bool IsLogOlderThan(string candidatePath, string newerPath)
        {
            if (!TryGetWriteTimeUtc(candidatePath, out var candidateTime))
                return false;

            if (!TryGetWriteTimeUtc(newerPath, out var newerTime))
                return false;

            return candidateTime < newerTime;
        }

        private static string LatestLogPath(params string[] paths)
        {
            string latestPath = null;
            DateTime latestTime = DateTime.MinValue;

            if (paths == null)
                return null;

            foreach (var path in paths)
            {
                if (!TryGetWriteTimeUtc(path, out var time))
                    continue;

                if (time >= latestTime)
                {
                    latestTime = time;
                    latestPath = path;
                }
            }

            return latestPath;
        }

        private static bool TryGetWriteTimeUtc(string path, out DateTime writeTimeUtc)
        {
            writeTimeUtc = default;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            writeTimeUtc = File.GetLastWriteTimeUtc(path);
            return true;
        }

        private static void ParseMeshingStream(StreamReader reader, OpenFOAMLogStatus status, OpenFOAMLogParseOptions options)
        {
            bool hasError = false;
            bool finished = false;
            string lastNonEmpty = null;
            int warningCount = 0;
            string lastWarning = null;
            double progress = 0.02;

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
                    warningCount = 0;
                    lastWarning = null;
                    progress = 0.02;
                    morphDurations.Clear();
                }

                if (IsWarningLine(lineSpan))
                {
                    warningCount++;
                    lastWarning = lastNonEmpty;
                }

                if (IsErrorLine(lineSpan))
                {
                    hasError = true;
                    if (string.IsNullOrWhiteSpace(status.ErrorMessage))
                        status.ErrorMessage = lastNonEmpty;
                }

                if (lineSpan.StartsWith("Finished meshing".AsSpan(), StringComparison.OrdinalIgnoreCase))
                {
                    finished = true;
                    progress = 1.0;
                }

                if (IsEndLine(lineSpan))
                {
                    finished = true;
                    progress = 1.0;
                }

                if (lineSpan.IndexOf("Read mesh".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    progress = Math.Max(progress, 0.06);

                if (lineSpan.IndexOf("Morphing phase".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    phase = "Morphing";
                    progress = Math.Max(progress, 0.58);
                }
                else if (lineSpan.IndexOf("Refinement phase".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    phase = "Refinement";
                    progress = Math.Max(progress, 0.12);
                }

                if (lineSpan.IndexOf("Feature refinement iteration".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    progress = Math.Max(progress, 0.16);
                else if (lineSpan.IndexOf("Surface refinement iteration".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    progress = Math.Max(progress, 0.27);
                else if (lineSpan.IndexOf("Shell refinement iteration".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    progress = Math.Max(progress, 0.39);
                else if (lineSpan.IndexOf("Splitting mesh at surface intersections".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    progress = Math.Max(progress, 0.50);
                else if (lineSpan.IndexOf("Repatching faces".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    progress = Math.Max(progress, 0.93);
                else if (lineSpan.IndexOf("Checking final mesh".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                    progress = Math.Max(progress, 0.96);

                if (TryParseMorphIterationsTotal(lineSpan, out var totalMorph))
                    totalMorphIterations = totalMorph;

                if (TryParseMorphIteration(lineSpan, out var morphIter))
                {
                    if (currentMorphIteration.HasValue && currentMorphDuration > 0)
                        morphDurations.Add(currentMorphDuration);

                    currentMorphIteration = morphIter;
                    currentMorphDuration = 0;
                    phase = "Morphing";

                    if (totalMorphIterations.HasValue && totalMorphIterations.Value > 0)
                    {
                        double fraction = Math.Min(1.0, Math.Max(0.0, (morphIter + 1.0) / totalMorphIterations.Value));
                        progress = Math.Max(progress, 0.60 + (0.30 * fraction));
                    }
                    else
                    {
                        progress = Math.Max(progress, 0.60);
                    }
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
            status.StepName = "snappyHexMesh";
            status.StepIndex = 3;
            status.StepCount = 3;
            status.MorphIteration = currentMorphIteration;
            status.MorphIterationsTotal = totalMorphIterations;
            status.WarningCount = warningCount;
            status.LastWarningLine = lastWarning;
            status.Progress = Clamp01(progress);

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

        private static bool IsWarningLine(ReadOnlySpan<char> line)
        {
            if (line.IndexOf("FOAM Warning".AsSpan(), StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (line.StartsWith("Warning".AsSpan(), StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

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

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
                return null;

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static string LastNonEmpty(params string[] values)
        {
            if (values == null)
                return null;

            for (int i = values.Length - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                    return values[i];
            }

            return null;
        }

        private static double StepProgress(OpenFOAMLogStatus step, bool inferFinished)
        {
            if (step == null)
                return inferFinished ? 1.0 : 0.0;

            if (!step.HasLog)
                return inferFinished ? 1.0 : 0.0;

            if (step.IsFinished)
                return 1.0;

            return Clamp01(step.Progress ?? 0.0);
        }

        private static double Clamp01(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0.0;

            if (value < 0.0)
                return 0.0;

            if (value > 1.0)
                return 1.0;

            return value;
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
