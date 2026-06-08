using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace EddyLib.UI
{
    public class Eddy_ComponentButtonAttributes : Eddy_ComponentAttributes
    {
        public string ButtonText { get; set; } = "Select Template";
        public GH_Palette ButtonPalette { get; set; } = GH_Palette.Black;

        public Eddy_ComponentButtonAttributes(GH_Component component) : base(component)
        {
        }

        private Rectangle ButtonBounds { get; set; }
        private bool _isHovered = false;

        protected override void Layout()
        {
            base.Layout();
            Rectangle rec0 = GH_Convert.ToRectangle(Bounds);
            rec0.Height += 22;

            Rectangle rec1 = rec0;
            rec1.Y = rec1.Bottom - 22;
            rec1.Height = 22;
            rec1.Inflate(-2, -2);

            Bounds = rec0;
            this.ButtonBounds = rec1;
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);

            if (channel == GH_CanvasChannel.Objects)
            {
                GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, ButtonPalette, ButtonText, 2, 0);
                button.Render(graphics, Selected || (!Owner.Locked && _isHovered), Owner.Locked, Owner.Hidden);
                button.Dispose();
            }
        }

        private static MethodInfo _attachCursorMethod;
        private static bool _attachCursorMethodSearched = false;

        private static void AttachHandCursor(GH_Canvas canvas)
        {
            if (!_attachCursorMethodSearched)
            {
                var cursorServerType = Grasshopper.Instances.CursorServer.GetType();
                _attachCursorMethod = cursorServerType.GetMethod("AttachCursor", new[] { typeof(object), typeof(string) });
                _attachCursorMethodSearched = true;
            }

            if (_attachCursorMethod != null)
            {
                _attachCursorMethod.Invoke(Grasshopper.Instances.CursorServer, new object[] { canvas, "GH_Hand" });
            }
            else
            {
                // Fallback using dynamic to bypass compilation dependency on System.Windows.Forms.Control
                try
                {
                    ((dynamic)Grasshopper.Instances.CursorServer).AttachCursor(canvas, "GH_Hand");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to attach cursor via dynamic fallback: {ex.Message}");
                }
            }
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            bool newHover = false;
            if (!Owner.Locked)
            {
                var hoverBounds = ButtonBounds;
                hoverBounds.Inflate(2, 2);

                if (hoverBounds.Contains(Point.Round(e.CanvasLocation)))
                {
                    newHover = true;
                    AttachHandCursor(sender);
                }
            }

            if (newHover != _isHovered)
            {
                _isHovered = newHover;
                // GH_Canvas inherits from System.Windows.Forms.Control, but EddyLib
                // does not reference WinForms — invoke Invalidate dynamically.
                try { ((dynamic)sender).Invalidate(); } catch { }
            }

            return _isHovered ? GH_ObjectResponse.Handled : base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            var clickBounds = ButtonBounds;
            clickBounds.Inflate(2, 2);
            if (clickBounds.Contains(System.Drawing.Point.Round(e.CanvasLocation)))
            {
                if (IsLeftClick(e))
                {
                    if (Owner is GH_Component comp && TryShowComponentMenu(comp, sender, e.ControlLocation))
                    {
                        return GH_ObjectResponse.Handled;
                    }

                    return GH_ObjectResponse.Handled;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }

        private static bool IsLeftClick(GH_CanvasMouseEvent mouseEvent)
        {
            var button = mouseEvent.GetType().GetProperty("Button", BindingFlags.Instance | BindingFlags.Public)?.GetValue(mouseEvent);
            return string.Equals(button?.ToString(), "Left", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryShowComponentMenu(GH_Component component, GH_Canvas canvas, Point location)
        {
            var contextMenuType = Type.GetType("System.Windows.Forms.ContextMenuStrip, System.Windows.Forms");
            if (contextMenuType == null) return false;

            var menu = Activator.CreateInstance(contextMenuType);
            if (menu == null) return false;

            var appendMethod = component.GetType().GetMethod(
                "AppendMenuItems",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { contextMenuType },
                null);

            if (appendMethod == null) return false;
            appendMethod.Invoke(component, new[] { menu });

            var showControlPointMethod = contextMenuType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "Show") return false;
                    var p = m.GetParameters();
                    return p.Length == 2 && p[1].ParameterType == typeof(Point);
                });

            if (showControlPointMethod != null)
            {
                showControlPointMethod.Invoke(menu, new object[] { canvas, location });
                return true;
            }

            var showPointMethod = contextMenuType
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "Show") return false;
                    var p = m.GetParameters();
                    return p.Length == 1 && p[0].ParameterType == typeof(Point);
                });

            if (showPointMethod != null)
            {
                showPointMethod.Invoke(menu, new object[] { location });
                return true;
            }

            return false;
        }

        public override bool IsTooltipRegion(PointF canvasPoint)
        {
            var hoverBounds = ButtonBounds;
            hoverBounds.Inflate(2, 2);
            if (hoverBounds.Contains(Point.Round(canvasPoint)))
            {
                return true;
            }
            return base.IsTooltipRegion(canvasPoint);
        }

        public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
        {
            var hoverBounds = ButtonBounds;
            hoverBounds.Inflate(2, 2);
            if (hoverBounds.Contains(Point.Round(canvasPoint)))
            {
                e.Title = ButtonText;
                e.Text = "Click to execute or show menu.";
                return;
            }
            base.SetupTooltip(canvasPoint, e);
        }

    }
}
