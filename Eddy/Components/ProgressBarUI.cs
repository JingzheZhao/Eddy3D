using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Urbano.Simulation
{
    public class ProgressDialog : Dialog
    {
        public Label Status;
        private Label TimeElapsed;
        private Label TimeRemaining;
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
                Title = $"Simulation Progress - {(int)(value * 100)}%";
                UpdateTimeRemaining();
            }
        }

        private void UpdateTimeRemaining()
        {
            if (Progress <= 0 || Progress >= 1)
            {
                TimeRemaining.Text = "Remaining: --:--:--";
                return;
            }

            var elapsed = stopwatch.Elapsed;
            var totalEstimated = TimeSpan.FromTicks((long)(elapsed.Ticks / Progress));
            var remaining = totalEstimated - elapsed;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;

            TimeRemaining.Text = $"Remaining: {(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        }

        public ProgressDialog(Func<CancellationTokenSource, Task> task, double refreshRate = 1000)
        {
            Title = "Simulation Progress - 0%";
            //BackgroundColor = Colors.Gray; // Removed to respect system theme
            //Icon = Icon.FromResource("Properties.Resources.urbano_icon.png");
            ClientSize = new Size(400, 200);
            MinimumSize = new Size(400, 200);
            Resizable = true;
            ShowInTaskbar = true;

            // controls
            Status = new Label() { Text = "Starting simulation...", Wrap = WrapMode.Word, ToolTip = "Current simulation status" };

            TimeElapsed = new Label { Text = "Elapsed: 00:00:00", VerticalAlignment = VerticalAlignment.Center };
            TimeRemaining = new Label { Text = "Remaining: --:--:--", VerticalAlignment = VerticalAlignment.Center };
            stopwatch = Stopwatch.StartNew();
            timer = new UITimer { Interval = 1.0 };
            timer.Elapsed += (s, e) =>
            {
                TimeElapsed.Text = $"Elapsed: {(int)stopwatch.Elapsed.TotalHours:D2}:{stopwatch.Elapsed.Minutes:D2}:{stopwatch.Elapsed.Seconds:D2}";
                UpdateTimeRemaining();
            };
            timer.Start();

            pbar = new ProgressBar { ToolTip = "Simulation Progress" };
            var cancel = new Button { Text = "Cancel", ToolTip = "Abort the current simulation (Esc, Enter)" };
            AbortButton = cancel; // Add Escape key support
            DefaultButton = cancel; // Map the Enter key to the Cancel action when focused
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
            layout.Add(new Drawable { Width = 5 }, false, false);
            layout.Add(TimeRemaining, false, false);
            layout.EndHorizontal();
            layout.EndVertical();
            layout.BeginVertical();
            layout.Add(null, true, true);
            layout.Add(cancel, true, false);
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
                if (r.IsFaulted && r.Exception != null)
                {
                    var inner = r.Exception.Flatten().InnerException ?? r.Exception;
                    if (uiThread != null) uiThread.Post(_ =>
                    {
                        Status.Text = $"Simulation failed: {inner.GetType().Name}: {inner.Message}";
                        Status.TextColor = Colors.Red;
                        cancel.Text = "Close";
                        cancel.ToolTip = "Close this dialog (Esc, Enter)";
                        DefaultButton = cancel;
                    }, null);
                }
                else
                {
                    if (uiThread != null) uiThread.Post((object state) => { Close(); }, null);
                }
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
            if (context != null) context.Post((object state) =>
            {
                dialog.Status.Text = value;
            }, null);
        }

        public override void WriteLine(string value)
        {
            if (context != null) context.Post((object state) =>
            {
                if (value.StartsWith(ProgressKey))
                {
                    if (float.TryParse(value.Remove(0, ProgressKey.Length), out float val))
                    {
                        dialog.Progress = val / 100f;
                    }
                }
                else dialog.Status.Text = value;
            }, null);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }
    }
}
