using Eddy.Properties;
using EddyLib;
using EddyLib.OpenFOAM;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;

namespace Eddy
{
    /// <summary>
    /// Displays live meshing progress from blockMesh, surfaceFeatures, and snappyHexMesh logs.
    /// </summary>
    public class MeshingProgress_Component : GH_Component
    {
        private const double RefreshSeconds = 1.0;

        private bool _liveRequested;
        private bool _updateScheduled;
        private OpenFOAMLogStatus _status = new OpenFOAMLogStatus();
        private string _statusText = "idle";
        private string _meshDirectory = string.Empty;

        public override GH_Exposure Exposure => GH_Exposure.quinary;

        public MeshingProgress_Component()
          : base(
              "Meshing Progress", "MeshProgress",
              @"Meshing Progress

Monitors blockMesh, surfaceFeatures, and snappyHexMesh logs directly on the Grasshopper canvas.

" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        public override void CreateAttributes()
        {
            m_attributes = new MeshingProgressAttributes(this);
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Result", "Res",
                "Simulation result from Wind Simulation component.",
                GH_ParamAccess.item);

            pManager.AddBooleanParameter(
                "Live", "Live",
                "Set to true to enable timed live updates.",
                GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("File", "F", "Active meshing log file being monitored.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Progress", "P", "Estimated meshing progress from 0 to 1.", GH_ParamAccess.item);
            pManager.AddTextParameter("Phase", "Ph", "Current meshing phase.", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Meshing status summary.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Done", "D", "True when snappyHexMesh has finished without fatal errors.", GH_ParamAccess.item);
            pManager.AddTextParameter("ETA", "ETA", "Estimated remaining time when available.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Warnings", "W", "Number of non-fatal warnings seen in the current logs.", GH_ParamAccess.item);
            pManager.AddTextParameter("Last Line", "L", "Last non-empty line in the active meshing log.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            OFResult result = null;
            if (!DA.GetData(0, ref result) || result == null)
            {
                _status = new OpenFOAMLogStatus();
                _statusText = "missing result";
                _meshDirectory = string.Empty;
                _liveRequested = false;
                Message = "Connect a result";
                SetOutputs(DA);
                return;
            }

            bool live = true;
            DA.GetData(1, ref live);
            _liveRequested = live;

            _meshDirectory = ResolveMeshDirectory(result);
            _status = OpenFOAMLogParser.ParseMeshingWorkflow(
                _meshDirectory,
                new OpenFOAMLogParseOptions { RollingWindow = 5 });
            _statusText = FormatStatus(_status);

            if (_status.HasError)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, _status.ErrorMessage ?? "Meshing log contains a fatal OpenFOAM error.");
            }

            Message = live ? "Live" : "Toggle 'Live' to monitor";

            if (live && !_status.IsFinished)
            {
                ScheduleNextUpdate(RefreshSeconds);
            }

            SetOutputs(DA);
        }

        private void SetOutputs(IGH_DataAccess DA)
        {
            DA.SetData(0, _status?.LogPath ?? string.Empty);
            DA.SetData(1, _status?.Progress ?? 0.0);
            DA.SetData(2, FormatPhase(_status));
            DA.SetData(3, _statusText ?? string.Empty);
            DA.SetData(4, _status?.IsFinished ?? false);
            DA.SetData(5, OpenFOAMStatusFormatter.FormatRemainingTime(_status));
            DA.SetData(6, _status?.WarningCount ?? 0);
            DA.SetData(7, _status?.LastLogLine ?? string.Empty);
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

        private static string ResolveMeshDirectory(OFResult result)
        {
            if (!string.IsNullOrWhiteSpace(result?.MeshSettings?.meshWorkingDir))
                return result.MeshSettings.meshWorkingDir;

            if (!string.IsNullOrWhiteSpace(result?.WorkingDirectory))
                return Path.Combine(result.WorkingDirectory, "mesh");

            return string.Empty;
        }

        private static string FormatStatus(OpenFOAMLogStatus status)
        {
            if (status == null)
                return "unknown";

            if (!status.HasLog)
                return "log not found";

            if (status.HasError)
                return "fatal error";

            if (status.IsFinished)
                return "done";

            return string.IsNullOrWhiteSpace(status.StepName)
                ? "running"
                : status.StepName + " running";
        }

        private static string FormatPhase(OpenFOAMLogStatus status)
        {
            if (status == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(status.Phase))
                return status.Phase;

            return status.StepName ?? string.Empty;
        }

        internal OpenFOAMLogStatus Status => _status;
        internal string StatusText => _statusText;
        internal string MeshDirectory => _meshDirectory;

        protected override Bitmap Icon => Resources.Eddy_mesh;

        public override Guid ComponentGuid => new Guid("{8D6B31F6-5875-4D98-A11F-4E20D4728D78}");

        private sealed class MeshingProgressAttributes : GH_ComponentAttributes
        {
            private const float ExpandedWidth = 420.0f;
            private const float PanelHeight = 96.0f;
            private RectangleF _panelBounds;

            public MeshingProgressAttributes(MeshingProgress_Component owner)
                : base(owner)
            {
            }

            protected override void Layout()
            {
                base.Layout();
                Bounds = new RectangleF(Bounds.X, Bounds.Y, ExpandedWidth, Bounds.Height);
                _panelBounds = new RectangleF(Bounds.X + 4, Bounds.Bottom + 4, Bounds.Width - 8, PanelHeight);
                Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + PanelHeight + 8);
            }

            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);

                if (channel != GH_CanvasChannel.Objects)
                {
                    return;
                }

                var owner = Owner as MeshingProgress_Component;
                if (owner == null)
                {
                    return;
                }

                DrawPanel(graphics, owner.Status, owner.StatusText);
            }

            private void DrawPanel(Graphics g, OpenFOAMLogStatus status, string statusText)
            {
                using (var fill = new SolidBrush(Color.FromArgb(248, 248, 248)))
                using (var border = new Pen(Color.FromArgb(180, 180, 180), 1.0f))
                {
                    g.FillRectangle(fill, _panelBounds);
                    g.DrawRectangle(border, _panelBounds.X, _panelBounds.Y, _panelBounds.Width, _panelBounds.Height);
                }

                if (status == null || !status.HasLog)
                {
                    DrawCenteredMessage(g, statusText);
                    return;
                }

                g.SmoothingMode = SmoothingMode.AntiAlias;

                string phase = FormatPhase(status);
                string title = string.IsNullOrWhiteSpace(phase) ? "Meshing" : phase;
                string eta = OpenFOAMStatusFormatter.FormatRemainingTime(status);
                string warningText = "Warnings: " + status.WarningCount.ToString(CultureInfo.InvariantCulture);

                using (var titleBrush = new SolidBrush(Color.FromArgb(50, 50, 50)))
                using (var metaBrush = new SolidBrush(Color.FromArgb(90, 90, 90)))
                {
                    g.DrawString(title, GH_FontServer.StandardBold, titleBrush,
                        new RectangleF(_panelBounds.X + 10, _panelBounds.Y + 8, _panelBounds.Width - 180, 18),
                        new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });

                    g.DrawString("ETA: " + eta, GH_FontServer.Small, metaBrush,
                        new RectangleF(_panelBounds.Right - 150, _panelBounds.Y + 9, 140, 14),
                        new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });

                    g.DrawString(warningText, GH_FontServer.Small, metaBrush,
                        new RectangleF(_panelBounds.Right - 150, _panelBounds.Y + 25, 140, 14),
                        new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
                }

                DrawProgressBar(g, status);
                DrawStepLabels(g, status);
                DrawLastLine(g, status);
            }

            private void DrawCenteredMessage(Graphics g, string statusText)
            {
                string message = string.IsNullOrWhiteSpace(statusText) ? "No meshing logs found" : statusText;
                if (string.Equals(message, "missing result", StringComparison.OrdinalIgnoreCase))
                    message = "Connect a valid simulation result.";
                else if (string.Equals(message, "log not found", StringComparison.OrdinalIgnoreCase))
                    message = "Run meshing to generate logs.";

                using (var brush = new SolidBrush(Color.FromArgb(140, 140, 140)))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    g.DrawString(message, GH_FontServer.StandardItalic, brush, _panelBounds, format);
                }
            }

            private void DrawProgressBar(Graphics g, OpenFOAMLogStatus status)
            {
                var barRect = new RectangleF(_panelBounds.X + 10, _panelBounds.Y + 42, _panelBounds.Width - 20, 16);
                double progress = Math.Max(0.0, Math.Min(1.0, status.Progress ?? 0.0));
                var fillRect = new RectangleF(barRect.X, barRect.Y, (float)(barRect.Width * progress), barRect.Height);

                Color fillColor = status.HasError
                    ? Color.FromArgb(196, 67, 67)
                    : status.IsFinished
                        ? Color.FromArgb(54, 145, 88)
                        : Color.FromArgb(50, 111, 200);

                using (var bg = new SolidBrush(Color.FromArgb(226, 226, 226)))
                using (var fill = new SolidBrush(fillColor))
                using (var border = new Pen(Color.FromArgb(170, 170, 170), 1.0f))
                using (var textBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
                {
                    g.FillRectangle(bg, barRect);
                    if (fillRect.Width > 0.5f)
                        g.FillRectangle(fill, fillRect);
                    g.DrawRectangle(border, barRect.X, barRect.Y, barRect.Width, barRect.Height);

                    string label = (progress * 100.0).ToString("0", CultureInfo.InvariantCulture) + "%";
                    g.DrawString(label, GH_FontServer.Small, textBrush, barRect,
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            }

            private void DrawStepLabels(Graphics g, OpenFOAMLogStatus status)
            {
                var labelRect = new RectangleF(_panelBounds.X + 10, _panelBounds.Y + 62, _panelBounds.Width - 20, 14);
                string step = status.StepName ?? string.Empty;
                string text = "blockMesh   surfaceFeatures   snappyHexMesh";

                using (var brush = new SolidBrush(Color.FromArgb(92, 92, 92)))
                {
                    g.DrawString(text, GH_FontServer.Small, brush, labelRect,
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }

                if (string.IsNullOrWhiteSpace(step))
                    return;

                float third = labelRect.Width / 3.0f;
                float x = labelRect.X;
                if (step.Equals("surfaceFeatures", StringComparison.OrdinalIgnoreCase))
                    x += third;
                else if (step.Equals("snappyHexMesh", StringComparison.OrdinalIgnoreCase))
                    x += third * 2.0f;

                using (var pen = new Pen(Color.FromArgb(55, 110, 200), 1.3f))
                {
                    g.DrawLine(pen, x + 8, labelRect.Bottom - 1, x + third - 8, labelRect.Bottom - 1);
                }
            }

            private void DrawLastLine(Graphics g, OpenFOAMLogStatus status)
            {
                string line = string.IsNullOrWhiteSpace(status.LastLogLine)
                    ? "Waiting for log output..."
                    : status.LastLogLine;

                line = Truncate(line, 92);

                using (var brush = new SolidBrush(Color.FromArgb(80, 80, 80)))
                {
                    g.DrawString(line, GH_FontServer.Small, brush,
                        new RectangleF(_panelBounds.X + 10, _panelBounds.Y + 78, _panelBounds.Width - 20, 13),
                        new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
                }
            }

            private static string Truncate(string text, int maxLength)
            {
                if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                    return text ?? string.Empty;

                return text.Substring(0, Math.Max(0, maxLength - 3)) + "...";
            }
        }
    }
}
