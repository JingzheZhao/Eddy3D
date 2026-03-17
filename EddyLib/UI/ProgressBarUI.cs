using Eto.Drawing;
using Eto.Forms;
using System.Diagnostics;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.UI
{
    public class ProgressDialog : Dialog
    {
        public Label Status;
        public TextArea StatusLog;
        private Label TimeElapsed;
        private Stopwatch stopwatch;
        private UITimer timer;

        public bool Canceled = false;
        private bool isFinished = false;
        private ProgressBar pbar;

        public float Progress
        {
            get { return pbar.Progress; }
            set
            {
                pbar.Progress = value;
                pbar.Invalidate();
            }
        }

        public ProgressDialog(Func<CancellationTokenSource, Task> task, double refreshRate = 1000)
        {
            Title = "Simulation Progress";
            //Icon = Icon.FromResource("Properties.Resources.eddy_icon.png");
            MinimumSize = new Size(450, 250);
            Resizable = true;
            ShowInTaskbar = true;

            // controls
            Status = new Label() { ToolTip = "Current simulation status" };
            StatusLog = new TextArea() { Height = 150, ReadOnly = true, Font = Fonts.Monospace(10), ToolTip = "Detailed simulation log output" };

            TimeElapsed = new Label { Text = "00:00:00", VerticalAlignment = VerticalAlignment.Center, ToolTip = "Time elapsed since simulation started" };
            stopwatch = Stopwatch.StartNew();
            timer = new UITimer { Interval = 1.0 };
            timer.Elapsed += (s, e) => { TimeElapsed.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss"); };
            timer.Start();

            pbar = new ProgressBar { ToolTip = "Simulation Progress" };
            var cancel = new Button { Text = "Cancel", ToolTip = "Abort the current simulation" };
            AbortButton = cancel;
            var copyLog = new Button { Text = "Copy Log", ToolTip = "Copy the simulation log to clipboard" };
            DefaultButton = copyLog; // Map the Enter key to the Copy Log action when focused
            var cts = new CancellationTokenSource();
            var uiThread = SynchronizationContext.Current;

            // events
            cancel.Click += (s, e) =>
            {
                if (MessageBox.Show(this, "Are you sure you want to abort the simulation?", "Abort Simulation", MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes)
                {
                    Canceled = true;
                    Close();
                }
            };
            copyLog.Click += (s, e) =>
            {
                try
                {
                    Clipboard.Instance.Text = StatusLog.Text;
                    copyLog.Text = "Copied!";
                    Task.Delay(2000).ContinueWith((t) =>
                    {
                        if (uiThread != null) uiThread.Post((object state) => { copyLog.Text = "Copy Log"; }, null);
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to copy log: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxType.Error);
                }
            };
            Closing += (s, e) =>
            {
                if (!isFinished && !Canceled)
                {
                    if (MessageBox.Show(this, "Are you sure you want to abort the simulation?", "Abort Simulation", MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                    Canceled = true;
                }

                if (!e.Cancel) cts.Cancel();
                if (!e.Cancel) timer.Stop();
            };

            // layout
            var layout = new DynamicLayout { Padding = 10, Spacing = new Size(5, 5) };
            layout.BeginVertical();
            layout.Add(pbar, true, false);
            layout.EndVertical();
            layout.BeginVertical();
            layout.BeginHorizontal();
            layout.Add(new Spinner { Height = 20, Enabled = true, ToolTip = "Simulation is running" }, false, false);
            layout.Add(new Drawable { Width = 5 }, false, false);
            layout.Add(Status, true, false);
            layout.Add(TimeElapsed, false, false);
            layout.EndHorizontal();
            layout.EndVertical();
            layout.BeginVertical();
            layout.Add(StatusLog, true, true);
            layout.EndVertical();

            layout.BeginVertical();
            layout.Add(null, true, true);
            layout.BeginHorizontal();
            layout.Add(null, true, false);
            layout.Add(copyLog, false, false);
            layout.Add(cancel, false, false);
            layout.EndHorizontal();
            layout.EndVertical();
            Content = layout;

            // set output to write to status label
            Console.SetOut(new ProgressWriter(this, SynchronizationContext.Current));

            // start task
            var run = task(cts);

            // when finished, close dialog
            run.ContinueWith((r) =>
            {
                isFinished = true;
                if (uiThread != null) uiThread.Send((object state) => { Close(); }, null);
            });
        }
    }

    public class ProgressBar : Drawable
    {
        public float Progress; // 0-1
        private Color backColor = SystemColors.Control;
        private Color fillColor = SystemColors.Highlight;

        public ProgressBar()
        {
            Height = 20;
            Width = 100;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var rect = new RectangleF(Size);
            var fullRect = rect;
            e.Graphics.FillRectangle(new SolidBrush(backColor), rect);
            float w = Progress > 1 ? 1 : Progress;
            if (w < 0) w = 0;

            if (w > 0)
            {
                rect.Width *= w;
                e.Graphics.FillRectangle(new SolidBrush(fillColor), rect);
            }

            // Draw percentage text
            string text = $"{(int)(w * 100)}%";
            var font = SystemFonts.Label();
            var textSize = e.Graphics.MeasureString(font, text);
            var textLocation = new PointF(
                fullRect.X + (fullRect.Width - textSize.Width) / 2,
                fullRect.Y + (fullRect.Height - textSize.Height) / 2
            );
            e.Graphics.DrawText(font, Colors.Black, textLocation + new SizeF(1, 1), text);
            e.Graphics.DrawText(font, Colors.White, textLocation, text);
        }
    }

    public class ProgressWriter : TextWriter
    {
        private ProgressDialog dialog;
        private SynchronizationContext context;
        public const string ProgressKey = "{%} ";

        public ProgressWriter(ProgressDialog prog_dialog, SynchronizationContext ui_context)
        {
            dialog = prog_dialog;
            context = ui_context;
        }

        public override void Write(string value)
        {
            if (context != null) context.Send((object state) =>
            {
                dialog.Status.Text = value;
                dialog.StatusLog.Append(value + Environment.NewLine, true);
            }, null);
        }

        public override void WriteLine(string value)
        {
            if (context != null) context.Send((object state) =>
            {
                if (value.StartsWith(ProgressKey))
                {
                    if (float.TryParse(value.Remove(0, ProgressKey.Length), out float val))
                    {
                        dialog.Progress = val / 100f;
                    }
                }
                else
                {
                    dialog.Status.Text = value;
                    dialog.StatusLog.Append(value + Environment.NewLine, true);
                }
            }, null);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }
    }
}