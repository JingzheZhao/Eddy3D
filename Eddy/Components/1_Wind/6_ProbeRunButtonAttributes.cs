using EddyLib;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Types;
using GH_IO.Serialization;
using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Eddy
{
    public class GH_ToggleParam : GH_PersistentParam<GH_Boolean>
    {
        public Action<bool> HandleToggle;
        public bool Toggle;

        // Parameterless ctor is REQUIRED by Grasshopper's parameter deserializer. Without it,
        // loading any .gh file that contains a Probe component throws "Parameter type is unknown"
        // and "Input parameter chunk is missing", which forced users to rewire from scratch.
        public GH_ToggleParam()
            : this("Run", "Run", "Run the component.", false)
        {
        }

        public GH_ToggleParam(string name, string nickname, string description, bool defaultItem = false)
            : base(name, nickname, description, EddyVersion.Name, "Params")
        {
            Toggle = defaultItem;
            if (PersistentDataCount == 0)
            {
                PersistentData.Append(new GH_Boolean(Toggle));
                HandleToggle?.Invoke(Toggle);
            }
        }

        public override Guid ComponentGuid => new Guid("{5D2D8ED8-57F8-4548-A533-7EA6A50E1AB5}");

        protected override Bitmap Icon => GH_StandardIcons.BlankParameterIcon_24x24;

        public void SetToggle(bool value)
        {
            if (Toggle == value)
            {
                return;
            }

            Toggle = value;
            HandleToggle?.Invoke(value);

            PersistentData.Clear();
            PersistentData.Append(new GH_Boolean(value));
            ExpireSolution(true);
        }

        protected override GH_GetterResult Prompt_Plural(ref List<GH_Boolean> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Singular(ref GH_Boolean value)
        {
            return GH_GetterResult.cancel;
        }

        public override bool Read(GH_IReader reader)
        {
            base.Read(reader);

            bool value = false;
            if (reader.TryGetBoolean("Value", ref value))
            {
                Toggle = value;
            }

            HandleToggle?.Invoke(Toggle);
            return true;
        }

        public override bool Write(GH_IWriter writer)
        {
            base.Write(writer);
            writer.SetBoolean("Value", Toggle);
            return true;
        }
    }

    internal sealed class ProbeRunButtonAttributes : GH_ComponentAttributes
    {
        private const float ToggleSize = 10f;
        private const float TogglePadding = 5f;
        private const int ToggleRadius = 5;

        private static MethodInfo _attachCursorMethod;
        private static bool _attachCursorMethodSearched;

        private readonly Dictionary<GH_ToggleParam, RectangleF> _toggleBounds = new Dictionary<GH_ToggleParam, RectangleF>();
        private GH_ToggleParam _hoveredToggle;

        public ProbeRunButtonAttributes(GH_Component owner)
            : base(owner)
        {
        }

        protected override void Layout()
        {
            base.Layout();

            _toggleBounds.Clear();
            _hoveredToggle = null;
            foreach (GH_ToggleParam toggleParam in FindToggleParams())
            {
                RectangleF toggleBounds = FindToggleBounds(toggleParam);
                if (!toggleBounds.IsEmpty)
                {
                    _toggleBounds[toggleParam] = toggleBounds;
                }
            }

            if (_toggleBounds.Count > 0)
            {
                float right = Math.Max(Bounds.Right, _toggleBounds.Values.Max(bounds => bounds.Right + TogglePadding));
                Bounds = RectangleF.FromLTRB(
                    Bounds.Left,
                    Bounds.Top,
                    right,
                    Bounds.Bottom);
            }
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel != GH_CanvasChannel.Objects || _toggleBounds.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<GH_ToggleParam, RectangleF> entry in _toggleBounds)
            {
                GH_Palette palette = Owner.Locked ? GH_Palette.Locked : GH_Palette.Black;
                GH_Capsule capsule = GH_Capsule.CreateCapsule(entry.Value, palette, ToggleRadius, 0);

                bool isHovered = !Owner.Locked && entry.Key == _hoveredToggle;
                capsule.Render(graphics, Selected || isHovered, Owner.Locked, Owner.Hidden);
                capsule.Dispose();

                if (entry.Key.Toggle)
                {
                    RectangleF inner = entry.Value;
                    inner.Inflate(-2, -2);
                    Brush brush = Owner.Locked ? Brushes.DimGray : Brushes.White;
                    graphics.FillEllipse(brush, inner);
                }
            }
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            GH_ToggleParam newHover = null;
            if (!Owner.Locked)
            {
                foreach (KeyValuePair<GH_ToggleParam, RectangleF> entry in _toggleBounds)
                {
                    // We expand the click target slightly for ease of use
                    var clickRect = entry.Value;
                    clickRect.Inflate(2f, 2f);

                    if (clickRect.Contains(e.CanvasLocation))
                    {
                        newHover = entry.Key;
                        AttachHandCursor(sender);
                        break;
                    }
                }
            }

            if (newHover != _hoveredToggle)
            {
                _hoveredToggle = newHover;
                sender.Invalidate();
            }

            return _hoveredToggle != null ? GH_ObjectResponse.Handled : base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button != MouseButtons.Left || Owner.Locked)
            {
                return base.RespondToMouseDown(sender, e);
            }

            foreach (KeyValuePair<GH_ToggleParam, RectangleF> entry in _toggleBounds)
            {
                // We expand the click target slightly for ease of use
                var clickRect = entry.Value;
                clickRect.Inflate(2f, 2f);

                if (clickRect.Contains(e.CanvasLocation))
                {
                    entry.Key.SetToggle(!entry.Key.Toggle);
                    return GH_ObjectResponse.Handled;
                }
            }

            return base.RespondToMouseDown(sender, e);
        }

        private IEnumerable<GH_ToggleParam> FindToggleParams()
        {
            return Owner is GH_Component component
                ? component.Params.Input.OfType<GH_ToggleParam>()
                : Enumerable.Empty<GH_ToggleParam>();
        }

        private static RectangleF FindToggleBounds(GH_ToggleParam toggleParam)
        {
            RectangleF paramBounds = toggleParam?.Attributes?.Bounds ?? RectangleF.Empty;
            if (paramBounds.IsEmpty)
            {
                return RectangleF.Empty;
            }

            float x = paramBounds.Right + TogglePadding;
            float y = paramBounds.Top + ((paramBounds.Height - ToggleSize) * 0.5f);
            return new RectangleF(x, y, ToggleSize, ToggleSize);
        }

        public override bool IsTooltipRegion(PointF canvasPoint)
        {
            foreach (var entry in _toggleBounds)
            {
                var clickRect = entry.Value;
                clickRect.Inflate(2f, 2f);
                if (clickRect.Contains(canvasPoint)) return true;
            }
            return base.IsTooltipRegion(canvasPoint);
        }

        public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
        {
            foreach (var entry in _toggleBounds)
            {
                var clickRect = entry.Value;
                clickRect.Inflate(2f, 2f);
                if (clickRect.Contains(canvasPoint))
                {
                    e.Title = entry.Key.Name;
                    e.Text = entry.Key.Description;
                    return;
                }
            }
            base.SetupTooltip(canvasPoint, e);
        }

        private static void AttachHandCursor(GH_Canvas canvas)
        {
            if (!_attachCursorMethodSearched)
            {
                _attachCursorMethod = Grasshopper.Instances.CursorServer
                    ?.GetType()
                    .GetMethod("AttachCursor", new[] { typeof(object), typeof(string) });
                _attachCursorMethodSearched = true;
            }

            if (_attachCursorMethod != null)
            {
                _attachCursorMethod.Invoke(Grasshopper.Instances.CursorServer, new object[] { canvas, "GH_Hand" });
                return;
            }

            try
            {
                ((dynamic)Grasshopper.Instances.CursorServer).AttachCursor(canvas, "GH_Hand");
            }
            catch
            {
            }
        }
    }
}
