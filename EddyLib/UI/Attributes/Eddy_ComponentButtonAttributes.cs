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
        public Eddy_ComponentButtonAttributes(GH_Component component) : base(component)
        {
        }

        private Rectangle ButtonBounds { get; set; }

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
                GH_Capsule button = GH_Capsule.CreateTextCapsule(ButtonBounds, ButtonBounds, GH_Palette.Black, "Select Template", 2, 0);
                button.Render(graphics, Selected, false, false);
                button.Dispose();
            }
        }

        public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (ButtonBounds.Contains(System.Drawing.Point.Round(e.CanvasLocation)))
            {
                var cursorServerType = Grasshopper.Instances.CursorServer.GetType();
                var attachMethod = cursorServerType.GetMethod("AttachCursor", new[] { typeof(object), typeof(string) });
                if (attachMethod != null)
                {
                    attachMethod.Invoke(Grasshopper.Instances.CursorServer, new object[] { sender, "GH_Hand" });
                }
                else
                {
                    // Fallback using dynamic to bypass compilation dependency on System.Windows.Forms.Control
                    try { ((dynamic)Grasshopper.Instances.CursorServer).AttachCursor(sender, "GH_Hand"); } catch { }
                }
                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseMove(sender, e);
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (ButtonBounds.Contains(System.Drawing.Point.Round(e.CanvasLocation)))
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
    }
}
