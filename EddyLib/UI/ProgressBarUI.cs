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
        private Label TimeRemaining;
        private Label ProgressPercent;
        private Stopwatch stopwatch;
        private UITimer timer;

        public bool Canceled = false;
        private bool isFinished = false;
        private Eto.Forms.ProgressBar pbar;
        private string _baseTitle;
        private float _progress = 0f;

        public float Progress
        {
            get { return _progress; }
            set
            {
                _progress = Math.Clamp(value, 0f, 1f);
                pbar.Value = (int)(_progress * 100);
                string percent = FormatPercent(_progress);
                ProgressPercent.Text = percent;
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            var elapsed = stopwatch.Elapsed;
            TimeElapsed.Text = FormatTime(elapsed);

            string etaStr = "--:--:--";
            if (_progress > 0.001f && _progress < 1f)
            {
                double totalMs = elapsed.TotalMilliseconds / _progress;
                double remainingMs = totalMs - elapsed.TotalMilliseconds;
                var remaining = TimeSpan.FromMilliseconds(remainingMs);
                etaStr = FormatTime(remaining);
            }
            else if (_progress >= 1f)
            {
                etaStr = "00:00:00";
            }
            TimeRemaining.Text = etaStr;

            string percent = FormatPercent(_progress).Trim();
            Title = $"{_baseTitle} - {percent} (ETA: {etaStr})";
        }

        private static string FormatTime(TimeSpan t)
        {
            return $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
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

            TimeRemaining = new Label
            {
                Text = "--:--:--",
                VerticalAlignment = VerticalAlignment.Center,
                Font = fontMono,
                TextColor = SystemColors.ControlText,
                ToolTip = "Estimated time remaining"
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
            timer = new UITimer { Interval = refreshRate / 1000.0 };
            timer.Elapsed += (s, e) => { UpdateStatus(); };
            timer.Start();

            pbar = new Eto.Forms.ProgressBar { MaxValue = 100, Value = 0, Height = 14, ToolTip = "Simulation Progress" };

            var secondaryColor = new Color(SystemColors.ControlText, 0.5f);

            var progressLabel = new Label
            {
                Text = "PROGRESS",
                Font = fontCaption,
                TextColor = secondaryColor,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var logHeaderLabel = new Label
            {
                Text = "SIMULATION LOG",
                Font = fontCaption,
                TextColor = secondaryColor,
            };

            StatusLog = new TextArea
            {
                ReadOnly = true,
                Font = fontMono,
                SpellCheck = false,
                Wrap = true,
                BackgroundColor = SystemColors.ControlBackground,
                TextColor = SystemColors.ControlText,
                ToolTip = "Simulation Log Output"
            };

            var copyLog = new Button { Text = "Copy Log", ToolTip = "Copy simulation log to clipboard (Enter)" };
            var cancel = new Button { Text = "Cancel", ToolTip = "Abort the current simulation (Esc)" };
            
            DefaultButton = copyLog;
            AbortButton = cancel;

            var cts = new CancellationTokenSource();
            var uiContext = SynchronizationContext.Current;

            cancel.Click += (s, e) =>
            {
                if (isFinished)
                {
                    Close();
                    return;
                }

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
                        uiContext?.Post(_ => { copyLog.Text = "Copy Log"; }, null));
                }
                catch
                {
                    copyLog.Text = "Failed";
                    Task.Delay(2000).ContinueWith(_ =>
                        uiContext?.Post(_ => { copyLog.Text = "Copy Log"; }, null));
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
            var statsRow = new DynamicLayout { Spacing = new Size(8, 0) };
            statsRow.BeginHorizontal();
            statsRow.Add(null, true, false);
            statsRow.Add(new Label { Text = "Elapsed:", Font = SystemFonts.Label(11), TextColor = secondaryColor }, false, false);
            statsRow.Add(TimeElapsed, false, false);
            statsRow.Add(new Drawable { Width = 12 }, false, false); // Spacer
            statsRow.Add(new Label { Text = "Remaining:", Font = SystemFonts.Label(11), TextColor = secondaryColor }, false, false);
            statsRow.Add(TimeRemaining, false, false);
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

            // Redirect Console.Out and Console.Error through ProgressWriter, keeping previous writers in chain
            var previousOut = Console.Out;
            var previousError = Console.Error;
            Console.SetOut(new ProgressWriter(this, uiContext, previousOut));
            Console.SetError(new ProgressWriter(this, uiContext, previousError));

            var run = task(cts);

            run.ContinueWith((r) =>
            {
                isFinished = true;
                timer.Stop();

                if (r.IsFaulted && r.Exception != null)
                {
                    var inner = r.Exception.Flatten().InnerException ?? r.Exception;
                    Console.Error.WriteLine($"Simulation failed: {inner.GetType().Name}: {inner.Message}");
                    if (inner.StackTrace != null) Console.Error.WriteLine(inner.StackTrace);

                    if (uiContext != null) uiContext.Post(_ =>
                    {
                        Status.Text = "Simulation failed. See log for details.";
                        Status.TextColor = Colors.Red;
                        cancel.Text = "Close";
                        cancel.ToolTip = "Close this dialog (Esc, Enter)";
                        DefaultButton = cancel;
                    }, null);
                }
                else
                {
                    if (uiContext != null) uiContext.Post((object state) => { Close(); }, null);
                }

                Console.SetOut(previousOut);
                Console.SetError(previousError);
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
        private SynchronizationContext uiContext;
        private TextWriter chained;
        public const string ProgressKey = "{%} ";

        public ProgressWriter(ProgressDialog prog_dialog, SynchronizationContext ui_context, TextWriter chained = null)
        {
            dialog = prog_dialog;
            uiContext = ui_context;
            this.chained = chained;
        }

        public override void Write(string value)
        {
            chained?.Write(value);
            if (uiContext != null) uiContext.Post((object state) =>
            {
                dialog.Status.Text = value;
                dialog.StatusLog.Append(value, true);
            }, null);
        }

        public override void WriteLine(string value)
        {
            chained?.WriteLine(value);
            if (uiContext != null) uiContext.Post((object state) =>
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
