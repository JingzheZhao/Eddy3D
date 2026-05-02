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
        private Label ProgressPercent;
        private Stopwatch stopwatch;
        private UITimer timer;

        public bool Canceled = false;
        private bool isFinished = false;
        private Eto.Forms.ProgressBar pbar;

        public float Progress
        {
            get { return pbar.Value / 100f; }
            set
            {
                float clamped = Math.Clamp(value, 0f, 1f);
                pbar.Value = (int)(clamped * 100);
                string percent = FormatPercent(clamped);
                ProgressPercent.Text = percent;
                Title = $"MRT Simulation - {percent.Trim()}";
            }
        }

        public ProgressDialog(Func<CancellationTokenSource, Task> task, double refreshRate = 1000)
        {
            Title = "MRT Simulation - 0%";
            ClientSize = new Size(640, 430);
            MinimumSize = new Size(560, 380);
            Resizable = true;
            ShowInTaskbar = true;

            var titleLabel = new Label
            {
                Text = "MRT Simulation",
                Font = SystemFonts.Bold(16),
            };

            Status = new Label
            {
                Text = "Starting simulation...",
                Wrap = WrapMode.Word,
                Font = SystemFonts.Label(12),
                Height = 44,
            };

            TimeElapsed = new Label
            {
                Text = "00:00:00",
                VerticalAlignment = VerticalAlignment.Center,
                Font = SystemFonts.Label(12),
            };

            ProgressPercent = new Label
            {
                Text = "  0%",
                VerticalAlignment = VerticalAlignment.Center,
                Font = SystemFonts.Label(12),
            };

            stopwatch = Stopwatch.StartNew();
            timer = new UITimer { Interval = 1.0 };
            timer.Elapsed += (s, e) => { TimeElapsed.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss"); };
            timer.Start();

            pbar = new Eto.Forms.ProgressBar { MaxValue = 100, Value = 0 };

            var progressCaption = new Label
            {
                Text = "Progress",
                Font = SystemFonts.Bold(12),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var logHeader = new Label
            {
                Text = "Log",
                Font = SystemFonts.Bold(12),
            };

            StatusLog = new TextArea
            {
                ReadOnly = true,
                Font = Fonts.Monospace(12),
                SpellCheck = false,
                Wrap = false,
                Height = 210,
            };

            var copyLog = new Button { Text = "Copy Log", ToolTip = "Copy simulation log to clipboard" };
            DefaultButton = copyLog;
            var cancel = new Button { Text = "Cancel", ToolTip = "Abort the current simulation (Esc, Enter)" };
            AbortButton = cancel;
            var cts = new CancellationTokenSource();
            var uiThread = SynchronizationContext.Current;

            cancel.Click += (s, e) =>
            {
                if (MessageBox.Show(this, "Abort the simulation?", "Confirm", MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.Yes)
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
                    Task.Delay(2000).ContinueWith(_ =>
                        uiThread?.Post(_ => { copyLog.Text = "Copy Log"; }, null));
                }
                catch
                {
                    copyLog.Text = "Failed";
                    Task.Delay(2000).ContinueWith(_ =>
                        uiThread?.Post(_ => { copyLog.Text = "Copy Log"; }, null));
                }
            };
            Closing += (s, e) =>
            {
                if (!isFinished && !Canceled)
                {
                    if (MessageBox.Show(this, "Abort the simulation?", "Confirm", MessageBoxButtons.YesNo, MessageBoxType.Question) == DialogResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                    Canceled = true;
                }
                if (!e.Cancel) { cts.Cancel(); timer.Stop(); }
            };

            var layout = new DynamicLayout { Padding = new Padding(16), Spacing = new Size(0, 8) };

            layout.Add(titleLabel, true, false);
            layout.Add(Status, true, false);
            layout.Add(CreateProgressHeader(progressCaption), true, false);
            layout.Add(pbar, true, false);
            layout.Add(CreateElapsedRow(), true, false);
            layout.Add(logHeader, true, false);
            layout.Add(StatusLog, true, true);
            layout.Add(CreateButtonRow(copyLog, cancel), true, false);

            Content = layout;

            // Redirect Console.Out through ProgressWriter, keeping the previous writer in chain
            var previousOut = Console.Out;
            Console.SetOut(new ProgressWriter(this, SynchronizationContext.Current, previousOut));

            var run = task(cts);

            run.ContinueWith((r) =>
            {
                isFinished = true;
                if (r.IsFaulted && r.Exception != null)
                {
                    var inner = r.Exception.Flatten().InnerException ?? r.Exception;
                    Console.Error.WriteLine($"Simulation failed: {inner.GetType().Name}: {inner.Message}");
                    if (inner.StackTrace != null) Console.Error.WriteLine(inner.StackTrace);
                }
                Console.SetOut(previousOut);
                if (uiThread != null) uiThread.Send((object state) => { Close(); }, null);
            });
        }

        private static string FormatPercent(float value)
        {
            return $"{(int)(value * 100),3}%";
        }

        private DynamicLayout CreateProgressHeader(Label progressCaption)
        {
            var row = new DynamicLayout { Spacing = new Size(8, 0) };
            row.BeginHorizontal();
            row.Add(progressCaption, false, false);
            row.Add(null, true, false);
            row.Add(ProgressPercent, false, false);
            row.EndHorizontal();
            return row;
        }

        private DynamicLayout CreateElapsedRow()
        {
            var label = new Label
            {
                Text = "Elapsed",
                Font = SystemFonts.Label(12),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var row = new DynamicLayout { Spacing = new Size(8, 0) };
            row.BeginHorizontal();
            row.Add(null, true, false);
            row.Add(label, false, false);
            row.Add(TimeElapsed, false, false);
            row.EndHorizontal();
            return row;
        }

        private static DynamicLayout CreateButtonRow(Button copyLog, Button cancel)
        {
            var row = new DynamicLayout { Spacing = new Size(8, 0) };
            row.BeginHorizontal();
            row.Add(null, true, false);
            row.Add(copyLog, false, false);
            row.Add(cancel, false, false);
            row.EndHorizontal();
            return row;
        }
    }

    public class ProgressWriter : TextWriter
    {
        private ProgressDialog dialog;
        private SynchronizationContext context;
        private TextWriter chained;
        public const string ProgressKey = "{%} ";

        public ProgressWriter(ProgressDialog prog_dialog, SynchronizationContext ui_context, TextWriter chained = null)
        {
            dialog = prog_dialog;
            context = ui_context;
            this.chained = chained;
        }

        public override void Write(string value)
        {
            chained?.Write(value);
            if (context != null) context.Send((object state) =>
            {
                dialog.Status.Text = value;
                dialog.StatusLog.Append(value + Environment.NewLine, true);
            }, null);
        }

        public override void WriteLine(string value)
        {
            chained?.WriteLine(value);
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
