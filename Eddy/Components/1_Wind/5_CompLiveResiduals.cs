using Eddy.Properties;
using EddyLib;
using EddyLib.OpenFOAM;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Eddy
{
    /// <summary>
    /// Displays a lightweight live residual chart directly on the Grasshopper canvas.
    /// </summary>
    public class LiveResiduals_Component : GH_Component
    {
        private const double RefreshSeconds = 1.0;
        private const int MaxStoredPoints = 2000;

        private static readonly Color[] SeriesPalette =
        {
            Color.FromArgb(0x38, 0x58, 0xF9),
            Color.FromArgb(0xF5, 0x6C, 0x42),
            Color.FromArgb(0x2E, 0xB8, 0x7D),
            Color.FromArgb(0xA0, 0x58, 0xFF),
            Color.FromArgb(0xD1, 0xA3, 0x00),
            Color.FromArgb(0xF0, 0x4E, 0x98),
            Color.FromArgb(0x00, 0xA5, 0xCF)
        };

        private static readonly string[] PreferredResidualFieldOrder =
        {
            "Ux", "Uy", "Uz", "p", "k", "epsilon"
        };

        private readonly ResidualStreamState _state = new ResidualStreamState();
        private ResidualPlotSnapshot _snapshot = ResidualPlotSnapshot.Empty;

        private bool _liveRequested;
        private bool _updateScheduled;
        private string _status = "idle";
        private string _activeFile = string.Empty;
        private int? _activeDirection;

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        public LiveResiduals_Component()
          : base(
              "Live Residuals", "ResLive",
              @"Live Residual Plot

Draws residuals.dat directly on the Grasshopper canvas with lightweight timed updates.
Use this for quick convergence monitoring without external plotting windows.

" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        public override void CreateAttributes()
        {
            m_attributes = new LiveResidualsAttributes(this);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Result", "Res",
                "Simulation result from Wind Simulation component.",
                GH_ParamAccess.item);

            pManager.AddBooleanParameter(
                "Live", "Live",
                "Set to true to enable timed live updates.",
                GH_ParamAccess.item, true);

            pManager.AddIntegerParameter(
                "Wind Direction", "Dir",
                "Wind direction folder to monitor. Use -1 for the first available direction.",
                GH_ParamAccess.item, -1);
            pManager[2].Optional = true;

        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("File", "F", "Residuals file being monitored.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            OFResult result = null;
            if (!DA.GetData(0, ref result) || result == null)
            {
                _status = "missing result";
                _activeFile = string.Empty;
                _activeDirection = null;
                _snapshot = ResidualPlotSnapshot.Empty;
                _liveRequested = false;
                Message = "No data";
                DA.SetData(0, _activeFile);
                return;
            }

            bool live = false;
            int requestedDir = -1;

            DA.GetData(1, ref live);
            DA.GetData(2, ref requestedDir);

            _liveRequested = live;

            string residualPath = ResolveResidualFile(result, requestedDir, out int? activeDirection);
            _activeFile = residualPath ?? string.Empty;
            _activeDirection = activeDirection;

            if (string.IsNullOrWhiteSpace(residualPath))
            {
                ResetState();
                _status = "residuals.dat not found";
            }
            else
            {
                UpdateStateFromFile(residualPath, MaxStoredPoints);
                _snapshot = BuildSnapshot(logScale: true, xMaxTarget: result.RunSettings?.endTime);
            }

            Message = live ? "Live" : "Idle";

            if (live)
            {
                ScheduleNextUpdate(RefreshSeconds);
            }

            DA.SetData(0, _activeFile);
        }

        private void ScheduleNextUpdate(double refreshSeconds)
        {
            if (_updateScheduled)
            {
                return;
            }

            var doc = OnPingDocument();
            if (doc == null)
            {
                return;
            }

            _updateScheduled = true;
            int delay = (int)Math.Round(refreshSeconds * 1000.0, MidpointRounding.AwayFromZero);
            doc.ScheduleSolution(delay, _ =>
            {
                _updateScheduled = false;
                if (_liveRequested && !Locked && !Hidden)
                {
                    ExpireSolution(false);
                }
            });
        }

        private void ResetState()
        {
            _state.Reset();
            _snapshot = ResidualPlotSnapshot.Empty;
            _activeFile = string.Empty;
            _activeDirection = null;
        }

        private void UpdateStateFromFile(string residualPath, int maxPoints)
        {
            try
            {
                _state.EnsurePath(residualPath);

                if (!File.Exists(residualPath))
                {
                    _status = "residuals.dat not found";
                    _snapshot = ResidualPlotSnapshot.Empty;
                    return;
                }

                using (var fs = new FileStream(residualPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (fs.Length < _state.LastOffset)
                    {
                        _state.ResetForCurrentPath();
                    }

                    fs.Seek(_state.LastOffset, SeekOrigin.Begin);
                    using (var reader = new StreamReader(fs))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            ParseResidualLine(line);
                        }

                        _state.LastOffset = fs.Position;
                    }
                }

                _state.Trim(maxPoints);

                if (_state.Time.Count == 0)
                {
                    _status = "waiting for residuals";
                }
                else
                {
                    _status = $"samples: {_state.Time.Count}";
                }
            }
            catch (Exception ex)
            {
                _status = "parse error";
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Live residual parser: " + ex.Message);
            }
        }

        private void ParseResidualLine(string rawLine)
        {
            if (string.IsNullOrWhiteSpace(rawLine))
            {
                return;
            }

            string line = rawLine.Trim();
            if (line.StartsWith("#", StringComparison.Ordinal))
            {
                ParseHeaderLine(line);
                return;
            }

            var tokens = SplitTokens(line);
            if (tokens.Length < 2)
            {
                return;
            }

            if (!TryParseDouble(tokens[0], out double time))
            {
                return;
            }

            if (_state.Time.Count > 0 && time <= _state.Time[_state.Time.Count - 1])
            {
                // OpenFOAM can append a new run to the same residuals.dat.
                // When time restarts (for example 1000 -> 0), clear prior samples
                // and continue plotting the latest run.
                if (time < _state.Time[_state.Time.Count - 1])
                {
                    _state.ClearSamples();
                }
                else
                {
                    return;
                }
            }

            if (_state.Time.Count > 0 && time <= _state.Time[_state.Time.Count - 1])
            {
                return;
            }

            if (_state.Fields.Count == 0)
            {
                int fieldCount = tokens.Length - 1;
                for (int i = 0; i < fieldCount; i++)
                {
                    string generated = i < PreferredResidualFieldOrder.Length
                        ? PreferredResidualFieldOrder[i]
                        : "f" + (i + 1).ToString(CultureInfo.InvariantCulture);
                    _state.AddField(generated, i);
                }
            }

            _state.Time.Add(time);

            for (int i = 0; i < _state.Fields.Count; i++)
            {
                string field = _state.Fields[i];
                double value = double.NaN;
                int tokenIndex = _state.GetTokenIndex(field, i) + 1;
                if (tokenIndex < tokens.Length && TryParseDouble(tokens[tokenIndex], out double parsed))
                {
                    value = parsed;
                }
                _state.Values[field].Add(value);
            }
        }

        private void ParseHeaderLine(string line)
        {
            string trimmed = line.TrimStart('#').Trim();
            if (!trimmed.StartsWith("Time", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (_state.Time.Count > 0)
            {
                // A repeated "Time ..." header usually indicates that a fresh run was
                // appended to the file. Reset stream state so we track that newest run.
                _state.ResetForCurrentPath();
            }

            var tokens = SplitTokens(trimmed);
            if (tokens.Length <= 1)
            {
                return;
            }

            _state.ClearFields();
            var rawFields = new List<string>();
            var rawIndexByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 1; i < tokens.Length; i++)
            {
                string rawField = tokens[i]?.Trim();
                if (string.IsNullOrWhiteSpace(rawField))
                {
                    continue;
                }

                rawFields.Add(rawField);
                if (!rawIndexByName.ContainsKey(rawField))
                {
                    rawIndexByName[rawField] = rawFields.Count - 1;
                }
            }

            foreach (string field in ReorderFields(rawFields))
            {
                int tokenIndex = rawIndexByName.TryGetValue(field, out int idx) ? idx : _state.Fields.Count;
                _state.AddField(field, tokenIndex);
            }
        }

        private static IEnumerable<string> ReorderFields(IEnumerable<string> rawFields)
        {
            if (rawFields == null)
            {
                return Enumerable.Empty<string>();
            }

            var unique = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string field in rawFields)
            {
                if (string.IsNullOrWhiteSpace(field))
                {
                    continue;
                }

                if (seen.Add(field))
                {
                    unique.Add(field);
                }
            }

            var ordered = new List<string>(unique.Count);
            foreach (string preferred in PreferredResidualFieldOrder)
            {
                string match = unique.FirstOrDefault(f => string.Equals(f, preferred, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                {
                    ordered.Add(match);
                }
            }

            foreach (string field in unique)
            {
                if (!ordered.Any(x => string.Equals(x, field, StringComparison.OrdinalIgnoreCase)))
                {
                    ordered.Add(field);
                }
            }

            return ordered;
        }

        private static string[] SplitTokens(string line)
        {
            return line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryParseDouble(string token, out double value)
        {
            value = double.NaN;
            if (string.IsNullOrWhiteSpace(token) ||
                token.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private ResidualPlotSnapshot BuildSnapshot(bool logScale, double? xMaxTarget)
        {
            if (_state.Time.Count == 0 || _state.Fields.Count == 0)
            {
                return ResidualPlotSnapshot.Empty;
            }

            var times = _state.Time.ToArray();
            var series = new List<ResidualSeriesSnapshot>(_state.Fields.Count);

            for (int i = 0; i < _state.Fields.Count; i++)
            {
                string field = _state.Fields[i];
                var values = _state.Values[field].ToArray();
                double latest = double.NaN;
                for (int j = values.Length - 1; j >= 0; j--)
                {
                    if (!double.IsNaN(values[j]))
                    {
                        latest = values[j];
                        break;
                    }
                }

                series.Add(new ResidualSeriesSnapshot(
                    field,
                    values,
                    latest,
                    SeriesPalette[i % SeriesPalette.Length]));
            }

            return new ResidualPlotSnapshot(times, series, logScale, xMaxTarget);
        }

        private static string ResolveResidualFile(OFResult result, int requestedDir, out int? activeDirection)
        {
            activeDirection = null;

            if (result == null || string.IsNullOrWhiteSpace(result.WorkingDirectory))
            {
                return null;
            }

            // Explicit direction input always has priority.
            if (requestedDir >= 0)
            {
                string explicitPath = TryGetResidualFileForDirection(result.WorkingDirectory, requestedDir);
                if (!string.IsNullOrWhiteSpace(explicitPath))
                {
                    activeDirection = requestedDir;
                    return explicitPath;
                }
            }

            var candidates = new List<string>();
            var dirs = result.Domain?.BCond?.WindDirections ?? new List<int>();

            // Auto mode: pick the direction with the most recently written simulation log.
            int? activeDir = GetMostRecentlyUpdatedDirection(result.WorkingDirectory, dirs);
            if (activeDir.HasValue)
            {
                string activePath = TryGetResidualFileForDirection(result.WorkingDirectory, activeDir.Value);
                if (!string.IsNullOrWhiteSpace(activePath))
                {
                    activeDirection = activeDir.Value;
                    return activePath;
                }
            }

            foreach (int dir in dirs.Distinct())
            {
                string path = TryGetResidualFileForDirection(result.WorkingDirectory, dir);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    activeDirection = dir;
                    return path;
                }
            }

            candidates.Add(Path.Combine(result.WorkingDirectory, "postProcessing", "residuals", "0", "residuals.dat"));
            candidates.Add(Path.Combine(result.WorkingDirectory, "postProcessing", "residuals", "residuals.dat"));

            foreach (string candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
                {
                    activeDirection = TryInferDirectionFromResidualPath(result.WorkingDirectory, candidate);
                    return candidate;
                }
            }

            return null;
        }

        private static int? TryInferDirectionFromResidualPath(string workingDir, string residualPath)
        {
            if (string.IsNullOrWhiteSpace(workingDir) || string.IsNullOrWhiteSpace(residualPath))
            {
                return null;
            }

            try
            {
                string baseDir = Path.GetFullPath(workingDir)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string fullResidualPath = Path.GetFullPath(residualPath);

                if (!fullResidualPath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                string relative = fullResidualPath.Substring(baseDir.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                string[] segments = relative.Split(
                    new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                    StringSplitOptions.RemoveEmptyEntries);

                if (segments.Length == 0)
                {
                    return null;
                }

                if (int.TryParse(segments[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int direction))
                {
                    return direction;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        private static string TryGetResidualFileForDirection(string workingDir, int direction)
        {
            if (string.IsNullOrWhiteSpace(workingDir) || direction < 0)
            {
                return null;
            }

            string caseDir = Path.Combine(workingDir, direction.ToString(CultureInfo.InvariantCulture));
            string nested = Path.Combine(caseDir, "postProcessing", "residuals", "0", "residuals.dat");
            if (File.Exists(nested))
            {
                return nested;
            }

            string direct = Path.Combine(caseDir, "postProcessing", "residuals", "residuals.dat");
            if (File.Exists(direct))
            {
                return direct;
            }

            return null;
        }

        private static int? GetMostRecentlyUpdatedDirection(string workingDir, IEnumerable<int> directions)
        {
            if (string.IsNullOrWhiteSpace(workingDir) || directions == null)
            {
                return null;
            }

            int? bestDirection = null;
            DateTime bestWriteTimeUtc = DateTime.MinValue;

            foreach (int direction in directions.Distinct())
            {
                if (direction < 0)
                {
                    continue;
                }

                string caseDir = Path.Combine(workingDir, direction.ToString(CultureInfo.InvariantCulture));
                string simLogPath = OpenFOAMLogLocator.FindLatestSimulationLog(caseDir);
                if (string.IsNullOrWhiteSpace(simLogPath) || !File.Exists(simLogPath))
                {
                    continue;
                }

                DateTime writeTimeUtc = File.GetLastWriteTimeUtc(simLogPath);
                if (writeTimeUtc >= bestWriteTimeUtc)
                {
                    bestWriteTimeUtc = writeTimeUtc;
                    bestDirection = direction;
                }
            }

            return bestDirection;
        }

        internal ResidualPlotSnapshot Snapshot => _snapshot;
        internal string PlotStatus => _status;
        internal string PlotDirectionLabel => _activeDirection.HasValue
            ? "Dir: " + _activeDirection.Value.ToString(CultureInfo.InvariantCulture) + "\u00B0"
            : "Dir: N/A";

        protected override Bitmap Icon => Resources.Eddy_live_residuals;

        public override Guid ComponentGuid => new Guid("0CE4FBF8-8E59-4F01-9C00-D160C3B1459C");

        private sealed class ResidualStreamState
        {
            public string FilePath { get; private set; } = string.Empty;
            public long LastOffset { get; set; }
            public List<double> Time { get; } = new List<double>();
            public List<string> Fields { get; } = new List<string>();
            public Dictionary<string, List<double>> Values { get; } =
                new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<string, int> FieldTokenIndexes { get; } =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public void EnsurePath(string path)
            {
                if (!string.Equals(FilePath, path, StringComparison.OrdinalIgnoreCase))
                {
                    FilePath = path;
                    ResetForCurrentPath();
                }
            }

            public void ResetForCurrentPath()
            {
                LastOffset = 0;
                Time.Clear();
                Fields.Clear();
                Values.Clear();
                FieldTokenIndexes.Clear();
            }

            public void Reset()
            {
                FilePath = string.Empty;
                ResetForCurrentPath();
            }

            public void ClearSamples()
            {
                Time.Clear();
                foreach (var values in Values.Values)
                {
                    values.Clear();
                }
            }

            public void AddField(string name, int tokenIndex = -1)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return;
                }

                bool exists = Values.ContainsKey(name);
                if (!exists)
                {
                    Fields.Add(name);
                    Values[name] = new List<double>();
                }

                if (tokenIndex >= 0)
                {
                    FieldTokenIndexes[name] = tokenIndex;
                }
                else if (!FieldTokenIndexes.ContainsKey(name))
                {
                    FieldTokenIndexes[name] = Fields.Count - 1;
                }
            }

            public void ClearFields()
            {
                Fields.Clear();
                Values.Clear();
                FieldTokenIndexes.Clear();
            }

            public int GetTokenIndex(string field, int fallbackIndex)
            {
                return FieldTokenIndexes.TryGetValue(field, out int index) ? index : fallbackIndex;
            }

            public void Trim(int maxPoints)
            {
                if (maxPoints <= 0 || Time.Count <= maxPoints)
                {
                    return;
                }

                int removeCount = Time.Count - maxPoints;
                Time.RemoveRange(0, removeCount);
                foreach (var kv in Values)
                {
                    if (kv.Value.Count >= removeCount)
                    {
                        kv.Value.RemoveRange(0, removeCount);
                    }
                    else
                    {
                        kv.Value.Clear();
                    }
                }
            }
        }

        internal sealed class ResidualPlotSnapshot
        {
            public static readonly ResidualPlotSnapshot Empty =
                new ResidualPlotSnapshot(Array.Empty<double>(), Array.Empty<ResidualSeriesSnapshot>(), true, null);

            public ResidualPlotSnapshot(
                IReadOnlyList<double> time,
                IReadOnlyList<ResidualSeriesSnapshot> series,
                bool logScale,
                double? xMaxTarget)
            {
                Time = time ?? Array.Empty<double>();
                Series = series ?? Array.Empty<ResidualSeriesSnapshot>();
                LogScale = logScale;
                XMaxTarget = xMaxTarget;
            }

            public IReadOnlyList<double> Time { get; }
            public IReadOnlyList<ResidualSeriesSnapshot> Series { get; }
            public bool LogScale { get; }
            public double? XMaxTarget { get; }
            public bool HasData => Time.Count > 1 && Series.Count > 0;
        }

        internal sealed class ResidualSeriesSnapshot
        {
            public ResidualSeriesSnapshot(string name, IReadOnlyList<double> values, double latestValue, Color color)
            {
                Name = name;
                Values = values ?? Array.Empty<double>();
                LatestValue = latestValue;
                Color = color;
            }

            public string Name { get; }
            public IReadOnlyList<double> Values { get; }
            public double LatestValue { get; }
            public Color Color { get; }
        }

        private sealed class LiveResidualsAttributes : GH_ComponentAttributes
        {
            private const float ExpandedWidth = 562.5f;
            private const float PlotHeight = 110.0f;
            private RectangleF _plotBounds;

            public LiveResidualsAttributes(LiveResiduals_Component owner)
                : base(owner)
            {
            }

            protected override void Layout()
            {
                base.Layout();
                Bounds = new RectangleF(Bounds.X, Bounds.Y, ExpandedWidth, Bounds.Height);
                _plotBounds = new RectangleF(Bounds.X + 4, Bounds.Bottom + 4, Bounds.Width - 8, PlotHeight);
                Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + PlotHeight + 8);
            }

            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);

                if (channel != GH_CanvasChannel.Objects)
                {
                    return;
                }

                var owner = Owner as LiveResiduals_Component;
                if (owner == null)
                {
                    return;
                }

                DrawChartFrame(graphics);
                DrawPlot(graphics, owner.Snapshot, owner.PlotStatus, owner.PlotDirectionLabel);
            }

            private void DrawChartFrame(Graphics g)
            {
                using (var fill = new SolidBrush(Color.FromArgb(248, 248, 248)))
                using (var border = new Pen(Color.FromArgb(180, 180, 180), 1.0f))
                {
                    g.FillRectangle(fill, _plotBounds);
                    g.DrawRectangle(border, _plotBounds.X, _plotBounds.Y, _plotBounds.Width, _plotBounds.Height);
                }
            }

            private void DrawPlot(Graphics g, ResidualPlotSnapshot snapshot, string status, string directionLabel)
            {
                DrawDirectionLabel(g, directionLabel);

                if (!snapshot.HasData)
                {
                    var textRect = new RectangleF(_plotBounds.X + 6, _plotBounds.Y + 6, _plotBounds.Width - 12, _plotBounds.Height - 12);
                    using (var brush = new SolidBrush(Color.FromArgb(90, 90, 90)))
                    {
                        g.DrawString(status ?? "no data", GH_FontServer.Small, brush, textRect);
                    }
                    return;
                }

                var plotRect = new RectangleF(_plotBounds.X + 52, _plotBounds.Y + 18, _plotBounds.Width - 60, _plotBounds.Height - 36);
                if (plotRect.Width < 20 || plotRect.Height < 20)
                {
                    return;
                }

                double xMin = 0.0;
                double xMax = snapshot.XMaxTarget.HasValue && snapshot.XMaxTarget.Value > 0
                    ? snapshot.XMaxTarget.Value
                    : snapshot.Time.Last();
                if (Math.Abs(xMax - xMin) < 1e-12)
                {
                    xMax = xMin + 1.0;
                }

                if (!TryGetYRange(snapshot, xMin, xMax, out double yMin, out double yMax))
                {
                    return;
                }

                g.SmoothingMode = SmoothingMode.AntiAlias;
                DrawGrid(g, plotRect);
                DrawAxes(g, plotRect);
                DrawYAxisLabels(g, snapshot, plotRect, yMin, yMax);
                DrawXAxisLabels(g, plotRect, xMin, xMax);

                var clippedState = g.Save();
                g.SetClip(plotRect);
                foreach (var series in snapshot.Series)
                {
                    DrawSeries(g, plotRect, snapshot, series, xMin, xMax, yMin, yMax);
                }
                g.Restore(clippedState);

                DrawLegend(g, snapshot, plotRect);
            }

            private void DrawDirectionLabel(Graphics g, string directionLabel)
            {
                string text = string.IsNullOrWhiteSpace(directionLabel) ? "Dir: N/A" : directionLabel;
                var labelRect = new RectangleF(_plotBounds.Right - 118, _plotBounds.Y + 4, 112, 14);
                using (var brush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                {
                    g.DrawString(
                        text,
                        GH_FontServer.Small,
                        brush,
                        labelRect,
                        new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Near });
                }
            }

            private static void DrawYAxisLabels(Graphics g, ResidualPlotSnapshot snapshot, RectangleF rect, double yMin, double yMax)
            {
                if (snapshot.LogScale)
                {
                    DrawLogYAxisLabels(g, rect, yMin, yMax);
                    return;
                }

                double yMid = yMin + ((yMax - yMin) * 0.5);

                DrawYAxisLabel(g, rect, rect.Top, FormatAxisValue(yMax));
                DrawYAxisLabel(g, rect, rect.Top + (rect.Height * 0.5f), FormatAxisValue(yMid));
                DrawYAxisLabel(g, rect, rect.Bottom, FormatAxisValue(yMin));
            }

            private static void DrawLogYAxisLabels(Graphics g, RectangleF rect, double yMin, double yMax)
            {
                int expMin = (int)Math.Ceiling(yMin);
                int expMax = (int)Math.Floor(yMax);
                if (expMax < expMin)
                {
                    DrawYAxisLabel(g, rect, rect.Top, FormatAxisValue(Math.Pow(10.0, yMax)));
                    DrawYAxisLabel(g, rect, rect.Bottom, FormatAxisValue(Math.Pow(10.0, yMin)));
                    return;
                }

                int count = expMax - expMin + 1;
                int step = Math.Max(1, (int)Math.Ceiling(count / 7.0));

                for (int exp = expMin; exp <= expMax; exp += step)
                {
                    float y = (float)(rect.Bottom - ((exp - yMin) / (yMax - yMin)) * rect.Height);
                    DrawYAxisLabel(g, rect, y, "1e" + exp.ToString(CultureInfo.InvariantCulture));
                }
            }

            private static void DrawYAxisLabel(Graphics g, RectangleF rect, float y, string text)
            {
                using (var pen = new Pen(Color.FromArgb(185, 185, 185), 1.0f))
                using (var brush = new SolidBrush(Color.FromArgb(95, 95, 95)))
                {
                    g.DrawLine(pen, rect.Left - 4, y, rect.Left, y);
                    g.DrawString(
                        text,
                        GH_FontServer.Small,
                        brush,
                        new RectangleF(rect.Left - 48, y - 7, 44, 14),
                        new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
                }
            }

            private static void DrawXAxisLabels(Graphics g, RectangleF rect, double xMin, double xMax)
            {
                const int tickCount = 5;
                for (int i = 0; i < tickCount; i++)
                {
                    double t = i / (double)(tickCount - 1);
                    float x = (float)(rect.Left + (t * rect.Width));
                    double xVal = xMin + ((xMax - xMin) * t);
                    string text = FormatXAxisValue(xVal);

                    using (var pen = new Pen(Color.FromArgb(185, 185, 185), 1.0f))
                    using (var brush = new SolidBrush(Color.FromArgb(95, 95, 95)))
                    {
                        g.DrawLine(pen, x, rect.Bottom, x, rect.Bottom + 4);
                        g.DrawString(
                            text,
                            GH_FontServer.Small,
                            brush,
                            new RectangleF(x - 24, rect.Bottom + 5, 48, 12),
                            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near });
                    }
                }
            }

            private static string FormatAxisValue(double value)
            {
                if (Math.Abs(value) < 1e-300)
                {
                    return "0";
                }

                double abs = Math.Abs(value);
                if (abs >= 1e-3 && abs < 1e3)
                {
                    return value.ToString("0.###", CultureInfo.InvariantCulture);
                }

                return value.ToString("0.##E+0", CultureInfo.InvariantCulture);
            }

            private static string FormatXAxisValue(double value)
            {
                if (Math.Abs(value) >= 1000.0)
                {
                    return value.ToString("0", CultureInfo.InvariantCulture);
                }

                if (Math.Abs(value) >= 10.0)
                {
                    return value.ToString("0.#", CultureInfo.InvariantCulture);
                }

                return value.ToString("0.##", CultureInfo.InvariantCulture);
            }

            private static void DrawAxes(Graphics g, RectangleF rect)
            {
                using (var axisPen = new Pen(Color.FromArgb(165, 165, 165), 1.0f))
                {
                    g.DrawLine(axisPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
                    g.DrawLine(axisPen, rect.Left, rect.Bottom, rect.Right, rect.Bottom);
                }
            }

            private static void DrawGrid(Graphics g, RectangleF rect)
            {
                using (var pen = new Pen(Color.FromArgb(224, 224, 224), 1.0f))
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        float y = rect.Top + (rect.Height * i / 4.0f);
                        g.DrawLine(pen, rect.Left, y, rect.Right, y);
                    }

                    for (int i = 1; i <= 3; i++)
                    {
                        float x = rect.Left + (rect.Width * i / 4.0f);
                        g.DrawLine(pen, x, rect.Top, x, rect.Bottom);
                    }
                }
            }

            private static bool TryGetYRange(
                ResidualPlotSnapshot snapshot,
                double xMin,
                double xMax,
                out double yMin,
                out double yMax)
            {
                yMin = double.PositiveInfinity;
                yMax = double.NegativeInfinity;

                foreach (var series in snapshot.Series)
                {
                    int count = Math.Min(snapshot.Time.Count, series.Values.Count);
                    for (int i = 0; i < count; i++)
                    {
                        double time = snapshot.Time[i];
                        if (time < xMin || time > xMax)
                        {
                            continue;
                        }

                        double v = series.Values[i];
                        if (double.IsNaN(v) || double.IsInfinity(v))
                        {
                            continue;
                        }

                        double y = snapshot.LogScale ? Math.Log10(Math.Max(v, 1e-12)) : v;
                        if (double.IsNaN(y) || double.IsInfinity(y))
                        {
                            continue;
                        }

                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                    }
                }

                if (double.IsInfinity(yMin) || double.IsInfinity(yMax))
                {
                    return false;
                }

                if (Math.Abs(yMax - yMin) < 1e-12)
                {
                    yMax = yMin + 1.0;
                }

                return true;
            }

            private static void DrawSeries(
                Graphics g,
                RectangleF rect,
                ResidualPlotSnapshot snapshot,
                ResidualSeriesSnapshot series,
                double xMin,
                double xMax,
                double yMin,
                double yMax)
            {
                var points = new List<PointF>(series.Values.Count);
                int count = Math.Min(snapshot.Time.Count, series.Values.Count);
                for (int i = 0; i < count; i++)
                {
                    double time = snapshot.Time[i];
                    if (time < xMin || time > xMax)
                    {
                        if (points.Count > 1)
                        {
                            float widthOutOfRange = IsKSeries(series) ? 2.2f : 1.6f;
                            using (var penOutOfRange = new Pen(series.Color, widthOutOfRange))
                            {
                                g.DrawLines(penOutOfRange, points.ToArray());
                            }
                        }
                        points.Clear();
                        continue;
                    }

                    double value = series.Values[i];
                    if (double.IsNaN(value) || double.IsInfinity(value))
                    {
                        if (points.Count > 1)
                        {
                            using (var penBreak = new Pen(series.Color, 1.6f))
                            {
                                g.DrawLines(penBreak, points.ToArray());
                            }
                        }
                        points.Clear();
                        continue;
                    }

                    double yValue = snapshot.LogScale ? Math.Log10(Math.Max(value, 1e-12)) : value;
                    float x = (float)(rect.Left + ((time - xMin) / (xMax - xMin)) * rect.Width);
                    float y = (float)(rect.Bottom - ((yValue - yMin) / (yMax - yMin)) * rect.Height);
                    points.Add(new PointF(x, y));
                }

                if (points.Count > 1)
                {
                    float width = IsKSeries(series) ? 2.2f : 1.6f;
                    using (var pen = new Pen(series.Color, width))
                    {
                        g.DrawLines(pen, points.ToArray());
                    }
                }
            }

            private static void DrawLegend(Graphics g, ResidualPlotSnapshot snapshot, RectangleF rect)
            {
                float x = rect.Left;
                float y = rect.Top - 14;
                const int maxLegend = 8;
                var displaySeries = snapshot.Series
                    .OrderBy(s => GetLegendOrder(s?.Name))
                    .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                    .Take(maxLegend)
                    .ToList();

                for (int i = 0; i < displaySeries.Count; i++)
                {
                    var series = displaySeries[i];
                    using (var pen = new Pen(series.Color, 2f))
                    using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
                    {
                        g.DrawLine(pen, x, y + 7, x + 10, y + 7);
                        g.DrawString(series.Name, GH_FontServer.Small, brush, x + 12, y);
                    }
                    x += 58;
                }
            }

            private static bool IsKSeries(ResidualSeriesSnapshot series)
            {
                return series != null &&
                       !string.IsNullOrWhiteSpace(series.Name) &&
                       string.Equals(series.Name.Trim(), "k", StringComparison.OrdinalIgnoreCase);
            }

            private static int GetLegendOrder(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return PreferredResidualFieldOrder.Length + 1000;
                }

                for (int i = 0; i < PreferredResidualFieldOrder.Length; i++)
                {
                    if (string.Equals(name.Trim(), PreferredResidualFieldOrder[i], StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }

                return PreferredResidualFieldOrder.Length + 1000;
            }
        }
    }
}
