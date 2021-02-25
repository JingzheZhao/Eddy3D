using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy
{
    public class ProgressDialog : Dialog
    {
        public Label Status;
        public bool Canceled = false;
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
            BackgroundColor = Colors.Gray;
            //Icon = Icon.FromResource("Properties.Resources.urbano_icon.png");
            ClientSize = new Size(400, 200);
            ShowInTaskbar = true;

            // controls
            Status = new Label();
            pbar = new ProgressBar();
            var cancel = new Button { Text = "Cancel" };
            var cts = new CancellationTokenSource();
            var uiThread = SynchronizationContext.Current;

            // events
            cancel.Click += (s, e) =>
            {
                Canceled = true;
                Close();
            };
            Closing += (s, e) =>
            {
                cts.Cancel();
            };

            // layout
            var layout = new DynamicLayout { Padding = 10, Spacing = new Size(5, 5) };
            layout.BeginVertical();
            layout.Add(pbar, true, false);
            layout.EndVertical();
            layout.BeginVertical();
            layout.BeginHorizontal();
            layout.Add(new Spinner { Height = 20, Enabled = true }, false, false);
            layout.Add(new Drawable { Width = 5 }, false, false);
            layout.Add(Status, true, false);
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
                if (uiThread != null) uiThread.Send((object state) => { Close(); }, null);
            });
        }
    }

    public class ProgressBar : Drawable
    {
        public float Progress; // 0-1
        private Color backColor = Colors.Gray;
        private Color fillColor = Colors.Blue;

        public ProgressBar()
        {
            Height = 20;
            Width = 100;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var rect = new RectangleF(Size);
            e.Graphics.FillRectangle(new SolidBrush(backColor), rect);
            float w = Progress > 1 ? 1 : Progress;
            if (w > 0)
            {
                rect.Width *= w;
                e.Graphics.FillRectangle(new SolidBrush(fillColor), rect);
            }
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
                else dialog.Status.Text = value;
            }, null);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }
    }
}