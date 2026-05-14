using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Eddy
{
    /// <summary>
    /// Custom component attributes that render small dropdown arrow buttons (▼)
    /// directly on the component body next to specified integer input parameters.
    /// Clicking an arrow shows a popup menu to select a value.
    /// When the parameter is wired (has sources), the arrow is hidden.
    /// </summary>
    internal sealed class DropdownComponentAttributes : GH_ComponentAttributes
    {
        /// <summary>
        /// Configuration for a single dropdown: input param index, option names, default index.
        /// </summary>
        public readonly struct DropdownDef
        {
            public readonly int ParamIndex;
            public readonly string[] Names;
            public readonly int DefaultIndex;

            public DropdownDef(int paramIndex, string[] names, int defaultIndex = 0)
            {
                ParamIndex = paramIndex;
                Names = names;
                DefaultIndex = defaultIndex;
            }
        }

        private readonly DropdownDef[] _defs;
        private readonly System.Collections.Generic.Dictionary<int, RectangleF> _btnBounds = new();
        private int _hoveredParamIndex = -1;

        private static System.Reflection.MethodInfo _attachCursorMethod;
        private static bool _attachCursorMethodSearched = false;

        public DropdownComponentAttributes(GH_Component owner, DropdownDef[] dropdowns) : base(owner)
        {
            _defs = dropdowns;
        }

        protected override void Layout()
        {
            base.Layout();
            _btnBounds.Clear();

            foreach (var def in _defs)
            {
                if (def.ParamIndex >= Owner.Params.Input.Count) continue;
                var p = Owner.Params.Input[def.ParamIndex];
                var b = p.Attributes.Bounds;
                // Clickable area: safely to the right of the input grip (which is at b.Left)
                // This ensures perfectly vertical alignment for all dropdowns.
                _btnBounds[def.ParamIndex] = new RectangleF(b.Left + 12f, b.Top + 2f, 16f, b.Height - 4f);
            }
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects) return;

            var prevSmoothing = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int alpha = GH_Canvas.ZoomFadeLow;
            if (alpha < 5) return;

            foreach (var def in _defs)
            {
                if (!_btnBounds.TryGetValue(def.ParamIndex, out var r)) continue;
                var param = Owner.Params.Input[def.ParamIndex];
                if (param.SourceCount > 0) continue;

                // Draw a subtle button background so it's clear it's a clickable target
                float bgAlpha = (def.ParamIndex == _hoveredParamIndex) ? 0.3f : 0.15f;
                using (var bgBrush = new SolidBrush(Color.FromArgb((int)(alpha * bgAlpha), SystemColors.ControlText)))
                {
                    var rect = new RectangleF(r.X, r.Y, r.Width, r.Height);
                    graphics.FillRectangle(bgBrush, rect);
                }

                // Draw ▼ inside the button
                float cx = r.X + r.Width * 0.5f;
                float cy = r.Y + r.Height * 0.5f;
                float s = 3.5f; 
                
                var pts = new[]
                {
                    new PointF(cx - s, cy - s * 0.3f),
                    new PointF(cx + s, cy - s * 0.3f),
                    new PointF(cx, cy + s * 0.7f)
                };
                
                using var brush = new SolidBrush(Color.FromArgb((int)(alpha * 0.8f), SystemColors.ControlText));
                graphics.FillPolygon(brush, pts);
            }

            graphics.SmoothingMode = prevSmoothing;
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            int newHover = -1;
            foreach (var def in _defs)
            {
                if (!_btnBounds.TryGetValue(def.ParamIndex, out var r)) continue;
                var param = Owner.Params.Input[def.ParamIndex];
                if (param.SourceCount > 0) continue;

                // We expand the click target slightly for ease of use
                var clickRect = new RectangleF(r.X - 2f, r.Y - 2f, r.Width + 4f, r.Height + 4f);
                if (clickRect.Contains(e.CanvasLocation))
                {
                    newHover = def.ParamIndex;

                    if (!_attachCursorMethodSearched)
                    {
                        var cursorServerType = Grasshopper.Instances.CursorServer.GetType();
                        _attachCursorMethod = cursorServerType.GetMethod("AttachCursor", new[] { typeof(object), typeof(string) });
                        _attachCursorMethodSearched = true;
                    }

                    if (_attachCursorMethod != null)
                    {
                        _attachCursorMethod.Invoke(Grasshopper.Instances.CursorServer, new object[] { sender, "GH_Hand" });
                    }
                    else
                    {
                        // Fallback using dynamic to bypass compilation dependency on System.Windows.Forms.Control
                        try
                        {
                            ((dynamic)Grasshopper.Instances.CursorServer).AttachCursor(sender, "GH_Hand");
                        }
                        catch
                        {
                            // Ignore failure
                        }
                    }
                    break;
                }
            }

            if (newHover != _hoveredParamIndex)
            {
                _hoveredParamIndex = newHover;
                sender.Invalidate();
            }

            return newHover != -1 ? GH_ObjectResponse.Handled : base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left)
            {
                foreach (var def in _defs)
                {
                    if (!_btnBounds.TryGetValue(def.ParamIndex, out var r)) continue;

                    // We expand the click target slightly for ease of use
                    var clickRect = r;
                    clickRect.Inflate(2f, 2f);
                    if (!clickRect.Contains(e.CanvasLocation)) continue;

                    var param = Owner.Params.Input[def.ParamIndex];
                    if (param.SourceCount > 0) continue;

                    ShowMenu(sender, def, r);
                    return GH_ObjectResponse.Handled;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        private void ShowMenu(GH_Canvas canvas, DropdownDef def, RectangleF bounds)
        {
            _hoveredParamIndex = -1;
            canvas.Invalidate();
            var intParam = Owner.Params.Input[def.ParamIndex] as Param_Integer;
            if (intParam == null) return;

            // Determine current value
            int current = def.DefaultIndex;
            if (intParam.PersistentDataCount > 0)
            {
                var first = intParam.PersistentData.AllData(true).FirstOrDefault();
                if (first is GH_Integer gi) current = gi.Value;
            }

            var menu = new ToolStripDropDown();
            for (int i = 0; i < def.Names.Length; i++)
            {
                int val = i;
                var item = new ToolStripMenuItem(def.Names[i]) { Checked = (val == current) };
                item.Click += (s, ev) =>
                {
                    Owner.RecordUndoEvent($"Set {intParam.NickName}");
                    intParam.PersistentData.Clear();
                    intParam.PersistentData.Append(new GH_Integer(val));
                    Owner.ExpireSolution(true);
                };
                menu.Items.Add(item);
            }

            canvas.ActiveInteraction = null;
            var screenPt = canvas.PointToScreen(Point.Round(new PointF(bounds.Left, bounds.Bottom)));
            menu.Show(screenPt);
        }
    }
}
