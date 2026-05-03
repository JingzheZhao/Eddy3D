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
        private string _baseTitle;

        public float Progress
        {
            get { return pbar.Value / 100f; }
            set
            {
                float clamped = Math.Clamp(value, 0f, 1f);
                pbar.Value = (int)(clamped * 100);
                string percent = FormatPercent(clamped);
                ProgressPercent.Text = percent;
                Title = $"{_baseTitle} - {percent.Trim()}";
            }
        }

        public ProgressDialog(Func<CancellationTokenSource, Task> task, string title = "Simulation", double refreshRate = 1000)
        {
            _baseTitle = title;
            Title = $"{_baseTitle} - 0%";
            ClientSize = new Size(640, 520);
            MinimumSize = new Size(580, 440);
            Resizable = true;
            ShowInTaskbar = true;

            // --- Typography ---
            var fontTitle = SystemFonts.Bold(18);
            var fontBody = SystemFonts.Label(12);
            var fontCaption = SystemFonts.Bold(10);
            var fontMono = Fonts.Monospace(11);

            var titleLabel = new Label
            {
                Text = title,
                Font = fontTitle,
                TextColor = SystemColors.ControlText
            };

            Status = new Label
            {
                Text = "Initializing...",
                Wrap = WrapMode.Word,
                Font = fontBody,
                Height = 44, // Room for 2 lines
                TextColor = SystemColors.ControlText,
                ToolTip = "Current simulation status"
            };

            TimeElapsed = new Label
            {
                Text = "00:00:00",
                VerticalAlignment = VerticalAlignment.Center,
                Font = fontMono,
                TextColor = SystemColors.ControlText,
                ToolTip = "Time elapsed since simulation started"
            };

            ProgressPercent = new Label
            {
                Text = "  0%",
                VerticalAlignment = VerticalAlignment.Center,
                Font = fontTitle, // Use same size as title for emphasis
                TextColor = SystemColors.Highlight,
                ToolTip = "Simulation Progress Percentage"
            };

            stopwatch = Stopwatch.StartNew();
            timer = new UITimer { Interval = 1.0 };
            timer.Elapsed += (s, e) => { TimeElapsed.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss"); };
            timer.Start();

            pbar = new Eto.Forms.ProgressBar { MaxValue = 100, Value = 0, Height = 14, ToolTip = "Simulation Progress" };

            var progressLabel = new Label
            {
                Text = "PROGRESS",
                Font = fontCaption,
                TextColor = Color.FromArgb(128, 128, 128),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var logHeaderLabel = new Label
            {
                Text = "SIMULATION LOG",
                Font = fontCaption,
                TextColor = Color.FromArgb(128, 128, 128),
            };

            StatusLog = new TextArea
            {
                ReadOnly = true,
                Font = fontMono,
                SpellCheck = false,
                Wrap = false,
                BackgroundColor = SystemColors.ControlBackground,
                TextColor = SystemColors.ControlText,
                ToolTip = "Simulation Log Output"
            };

            var copyLog = new Button { Text = "Copy Log", ToolTip = "Copy simulation log to clipboard (Enter)" };
            var cancel = new Button { Text = "Cancel", ToolTip = "Abort the current simulation (Esc)" };
            
            DefaultButton = copyLog;
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

            // --- Layout ---
            var layout = new DynamicLayout { Padding = new Padding(24), Spacing = new Size(0, 10) };

            // Header Section
            layout.Add(titleLabel, true, false);
            layout.Add(Status, true, false);
            
            // Progress Section
            layout.BeginVertical(spacing: new Size(0, 4));
            var progressHeader = new DynamicLayout { Spacing = new Size(8, 0) };
            progressHeader.BeginHorizontal();
            progressHeader.Add(progressLabel, false, false);
            progressHeader.Add(null, true, false);
            progressHeader.Add(ProgressPercent, false, false);
            progressHeader.EndHorizontal();
            layout.Add(progressHeader, true, false);
            layout.Add(pbar, true, false);
            layout.EndVertical();

            // Stats Row
            var statsRow = new DynamicLayout { Spacing = new Size(4, 0) };
            statsRow.BeginHorizontal();
            statsRow.Add(null, true, false);
            statsRow.Add(new Label { Text = "Elapsed:", Font = SystemFonts.Label(11), TextColor = Color.FromArgb(128, 128, 128) }, false, false);
            statsRow.Add(TimeElapsed, false, false);
            statsRow.EndHorizontal();
            layout.Add(statsRow, true, false);

            // Log Section (Expands)
            layout.Add(logHeaderLabel, true, false);
            layout.Add(StatusLog, true, true);

            // Action Row
            var buttonRow = new DynamicLayout { Spacing = new Size(12, 0) };
            buttonRow.BeginHorizontal();
            buttonRow.Add(null, true, false);
            buttonRow.Add(copyLog, false, false);
            buttonRow.Add(cancel, false, false);
            buttonRow.EndHorizontal();
            layout.Add(buttonRow, true, false);

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
