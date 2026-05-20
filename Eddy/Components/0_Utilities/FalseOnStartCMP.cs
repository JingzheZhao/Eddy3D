using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Eddy
{
    public class FalseOnStartCMP : GH_Component
    {
        private bool _run = false;

        public FalseOnStartCMP()
          : base("Safety Toggle", "SafetyToggle",
              "A boolean toggle that is always FALSE when a file is opened. Useful for preventing automatic execution of heavy ML models.",
              "Eddy3D", "0 | Utilities")
        {
        }

        public override Guid ComponentGuid => new Guid("{C8E1D2B3-A4B5-4C6D-7E8F-9A0B1C2D3E4F}");

        protected override System.Drawing.Bitmap Icon => Properties.Resources.Eddy_safe_toggle;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            // No inputs needed, primarily a manual toggle
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "Boolean value (Always false on start).", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Message = _run ? "TRUE (Double-click)" : "FALSE (Double-click)";
            DA.SetData(0, _run);
        }

        public override void CreateAttributes()
        {
            Attributes = new SafetyToggleAttributes(this);
        }

        public override void AppendAdditionalMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "TRUE", (s, e) => { RecordUndoEvent("Toggle Safety"); _run = true; ExpireSolution(true); }, true, _run);
            Menu_AppendItem(menu, "FALSE", (s, e) => { RecordUndoEvent("Toggle Safety"); _run = false; ExpireSolution(true); }, true, !_run);
        }

        public void Toggle()
        {
            RecordUndoEvent("Toggle Safety");
            _run = !_run;
            ExpireSolution(true);
        }

        // ── Serialization ──────────────────────────────────────────────────────────
        // By NOT overriding Read/Write or ensuring we always read 'false', 
        // we satisfy the "False on Start" requirement.
        
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // We consciously do NOT write the _run state so it remains false on next load
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            _run = false; // Force false on every load
            return base.Read(reader);
        }
    }

    internal class SafetyToggleAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
    {
        private static System.Reflection.MethodInfo _attachCursorMethod;
        private static bool _attachCursorMethodSearched = false;

        public SafetyToggleAttributes(FalseOnStartCMP owner) : base(owner)
        {
            if (!_attachCursorMethodSearched)
            {
                var cursorServerType = Grasshopper.Instances.CursorServer.GetType();
                _attachCursorMethod = cursorServerType.GetMethod("AttachCursor", new[] { typeof(object), typeof(string) });
                _attachCursorMethodSearched = true;
            }
        }

        public override Grasshopper.GUI.Canvas.GH_ObjectResponse RespondToMouseDoubleClick(Grasshopper.GUI.Canvas.GH_Canvas sender, Grasshopper.GUI.GH_CanvasMouseEvent e)
        {
            ((FalseOnStartCMP)Owner).Toggle();
            return Grasshopper.GUI.Canvas.GH_ObjectResponse.Handled;
        }

        public override bool IsTooltipRegion(PointF canvasPoint)
        {
            return Bounds.Contains(canvasPoint);
        }

        public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
        {
            e.Title = "Safety Toggle";
            e.Text = "Double-click to toggle the 'Run' state. This component always resets to FALSE when the file is opened.";
        }

        public override Grasshopper.GUI.Canvas.GH_ObjectResponse RespondToMouseMove(Grasshopper.GUI.Canvas.GH_Canvas sender, Grasshopper.GUI.GH_CanvasMouseEvent e)
        {
            if (!Owner.Locked && Bounds.Contains(e.CanvasLocation))
            {
                if (_attachCursorMethod != null)
                {
                    _attachCursorMethod.Invoke(Grasshopper.Instances.CursorServer, new object[] { sender, "GH_Hand" });
                }
                else
                {
                    // Fallback using dynamic to bypass compilation dependency on System.Windows.Forms.Control
                    try { ((dynamic)Grasshopper.Instances.CursorServer).AttachCursor(sender, "GH_Hand"); } catch { }
                }
                return Grasshopper.GUI.Canvas.GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseMove(sender, e);
        }
    }
}
