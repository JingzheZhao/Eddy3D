using Eddy.Properties;
using EddyLib;
using EddyLib.Radiation;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Eddy.Components.Radiation
{
    public class InspectPolygon_Component : GH_Component
    {
        public string ProbePolyMode = "Polygon";

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetString("type", ProbePolyMode);
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            ProbePolyMode = reader.GetString("type");
            UpdateLabels();
            return base.Read(reader);
        }

        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        private void UpdateLabels()
        {
            if (Params.Input.Count < 3 || Params.Output.Count < 3) return;

            var input = Params.Input[2];
            var output = Params.Output[2];

            if (ProbePolyMode == "Hour")
            {
                input.Name = "Hour";
                input.NickName = "h";
                input.Description = "The hour index (0-8759) to visualize across all polygons.";

                output.Name = "Polygon Data";
                output.Description = "Data values for all polygons at the selected hour.";
            }
            else
            {
                input.Name = "Polygon Index";
                input.NickName = "i";
                input.Description = "The polygon index to visualize across all hours.";

                output.Name = "Annual Data";
                output.Description = "Data values for all hours at the selected polygon.";
            }
        }

        public class CustomAttributes : GH_ComponentAttributes
        {
            private static System.Reflection.MethodInfo _attachCursorMethod;
            private object[] _cursorArgs = new object[2];
            private int _hoverIndex = -1;

            public CustomAttributes(InspectPolygon_Component owner) : base(owner)
            {
                if (_attachCursorMethod == null)
                {
                    var cursorServer = Grasshopper.Instances.CursorServer;
                    if (cursorServer != null)
                    {
                        _attachCursorMethod = cursorServer.GetType().GetMethod("AttachCursor");
                    }
                }
            }

            #region Custom layout logic

            private RectangleF isPolygon { get; set; }
            private RectangleF isHour { get; set; }

            protected override void Layout()
            {
                base.Layout();
                _hoverIndex = -1;

                float btnHeight = 20;
                float margin = 2;

                //We'll extend the basic layout by adding two regions to the bottom of this component,
                isPolygon = new RectangleF(Bounds.X + margin, Bounds.Bottom + margin, Bounds.Width - 2 * margin, btnHeight - margin);
                isHour = new RectangleF(Bounds.X + margin, Bounds.Bottom + btnHeight, Bounds.Width - 2 * margin, btnHeight - margin);

                Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 2 * btnHeight + margin);
            }

            #endregion Custom layout logic

            #region Custom Mouse handling

            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    InspectPolygon_Component comp = Owner as InspectPolygon_Component;

                    var clickPolygon = isPolygon;
                    clickPolygon.Inflate(2f, 2f);
                    if (clickPolygon.Contains(e.CanvasLocation))
                    {
                        if (comp.ProbePolyMode == "Polygon") return GH_ObjectResponse.Handled;
                        comp.RecordUndoEvent("Polygon");
                        comp.ProbePolyMode = "Polygon";
                        comp.UpdateLabels();
                        comp.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }

                    var clickHour = isHour;
                    clickHour.Inflate(2f, 2f);
                    if (clickHour.Contains(e.CanvasLocation))
                    {
                        if (comp.ProbePolyMode == "Hour") return GH_ObjectResponse.Handled;
                        comp.RecordUndoEvent("Hour");
                        comp.ProbePolyMode = "Hour";
                        comp.UpdateLabels();
                        comp.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }

            public override GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                int newHover = -1;
                if (!Owner.Locked)
                {
                    var hoverPolygon = isPolygon;
                    hoverPolygon.Inflate(2f, 2f);
                    var hoverHour = isHour;
                    hoverHour.Inflate(2f, 2f);

                    if (hoverPolygon.Contains(e.CanvasLocation))
                    {
                        newHover = 0;
                    }
                    else if (hoverHour.Contains(e.CanvasLocation))
                    {
                        newHover = 1;
                    }

                    if (newHover != -1)
                    {
                        if (_attachCursorMethod != null)
                        {
                            var cursorServer = Grasshopper.Instances.CursorServer;
                            if (cursorServer != null)
                            {
                                _cursorArgs[0] = sender;
                                _cursorArgs[1] = "GH_Hand";
                                _attachCursorMethod.Invoke(cursorServer, _cursorArgs);
                            }
                        }
                        else
                        {
                            try { ((dynamic)Grasshopper.Instances.CursorServer).AttachCursor(sender, "GH_Hand"); } catch { }
                        }
                    }
                }

                if (newHover != _hoverIndex)
                {
                    _hoverIndex = newHover;
                    sender.Invalidate();
                }

                return _hoverIndex != -1 ? GH_ObjectResponse.Handled : base.RespondToMouseMove(sender, e);
            }

            public override bool IsTooltipRegion(PointF canvasPoint)
            {
                var hoverPolygon = isPolygon;
                hoverPolygon.Inflate(2f, 2f);
                var hoverHour = isHour;
                hoverHour.Inflate(2f, 2f);

                return hoverPolygon.Contains(canvasPoint) || hoverHour.Contains(canvasPoint);
            }

            public override void SetupTooltip(PointF canvasPoint, GH_TooltipDisplayEventArgs e)
            {
                var hoverPolygon = isPolygon;
                hoverPolygon.Inflate(2f, 2f);
                var hoverHour = isHour;
                hoverHour.Inflate(2f, 2f);

                if (hoverPolygon.Contains(canvasPoint))
                {
                    e.Title = "Polygon Mode";
                    e.Text = "Switch to visualize annual data for a specific polygon index.";
                }
                else if (hoverHour.Contains(canvasPoint))
                {
                    e.Title = "Hour Mode";
                    e.Text = "Switch to visualize spatial data for a specific hour (0-8759).";
                }
            }

            #endregion Custom Mouse handling

            #region Custom Render logic

            protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
            {
                switch (channel)
                {
                    case GH_CanvasChannel.Objects:
                        //We need to draw everything outselves.
                        // base.RenderComponentCapsule(canvas, graphics, true, false, false, true, true, true);
                        base.RenderComponentCapsule(canvas, graphics, true, true, false, true, true, true);

                        InspectPolygon_Component comp = Owner as InspectPolygon_Component;

                        GH_Capsule buttonPolygon = GH_Capsule.CreateTextCapsule(isPolygon, isPolygon, comp.ProbePolyMode == "Polygon" ? GH_Palette.Grey : GH_Palette.White, "Polygon", 2, 0);
                        buttonPolygon.Render(graphics, Selected || _hoverIndex == 0, Owner.Locked, Owner.Hidden);
                        buttonPolygon.Dispose();

                        GH_Capsule buttonHour = GH_Capsule.CreateTextCapsule(isHour, isHour, comp.ProbePolyMode == "Hour" ? GH_Palette.Grey : GH_Palette.White, "Hour", 2, 0);
                        buttonHour.Render(graphics, Selected || _hoverIndex == 1, Owner.Locked, Owner.Hidden);
                        buttonHour.Dispose();

                        break;

                    default:
                        base.Render(canvas, graphics, channel);
                        break;
                }
            }

            #endregion Custom Render logic
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Initializes a new instance of the ThermalSystem_Component class.
        /// </summary>
        public InspectPolygon_Component()
          : base("Surface Result Inspector", "InPoly",
@"Surface Result Inspector

Visualizes simulation results on surface polygons (e.g., building facades, ground). Displays metrics like Surface Temperature or Radiation exposure.

" + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")
        {
            UpdateLabels();
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Polygon", "Polygon", "Simulation Polygon", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Metric", "Met", "Metric", GH_ParamAccess.item, 0);
            var types = Enum.GetNames(typeof(RPolyMetric));
            Param_Integer param = pManager[1] as Param_Integer;
            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }
            pManager.AddIntegerParameter("Index", "i", "Hour or sensor index", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "Pt", "Point", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.list);
            pManager.AddNumberParameter("Data", "Data", "Data", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int _metricI = 0;
            if (!DA.GetData(1, ref _metricI)) return;
            RPolyMetric metric = (RPolyMetric)_metricI;

            int h = 0;
            if (!DA.GetData(2, ref h)) return;

            List<IGH_Goo> gooPolyList = new List<IGH_Goo>();
            if (!DA.GetDataList(0, gooPolyList)) { }
            List<RPolygon> rpolyList = new List<RPolygon>();

            foreach (var gooProbe in gooPolyList)
            {
                if (gooProbe != null)
                {
                    RPolygon poly = null;
                    if (gooProbe.CastTo<RPolygon>(out poly))
                    {
                        if (poly.Type != RadiationSurfaceType.Sky)
                        {
                            rpolyList.Add(poly);
                        }
                    }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Polygon provided cannot be cast into the correct format. Are you sure you are passing the correct input?");
                        return;
                    }
                }
            }

            if (rpolyList.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No valid non-sky polygons were provided.");
                return;
            }

            List<Point3d> points = new List<Point3d>();
            List<Mesh> meshes = new List<Mesh>();
            List<float> data = new List<float>();

            if (ProbePolyMode == "Hour")
            {
                if (rpolyList.Count > 0)
                {
                    var firstPoly = rpolyList[0];
                    int maxHour = -1;
                    if (firstPoly.SurfaceTemperature != null) maxHour = firstPoly.SurfaceTemperature.Length;
                    else if (firstPoly.TemperatureOverride != null) maxHour = firstPoly.TemperatureOverride.Length;

                    if (maxHour != -1 && (h < 0 || h >= maxHour))
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Hour index {h} is outside the valid range 0-{maxHour - 1}.");
                        return;
                    }
                }

                foreach (var p in rpolyList)
                {
                    if (p == null) continue;
                    if (metric == RPolyMetric.SurfaceTemperature)
                    {
                        if (p.SurfaceTemperature != null && p.SimulationType == SimulationType.Simulated)
                        {
                            if (h < 0 || h >= p.SurfaceTemperature.Length) continue;
                            if (p.Centroid != null) points.Add(p.Centroid.Value);
                            if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                            else meshes.Add(null);
                            data.Add(p.SurfaceTemperature[h]);
                        }
                        else if (p.TemperatureOverride != null && p.SimulationType == SimulationType.TemperatureInput)
                        {
                            if (h < 0 || h >= p.TemperatureOverride.Length) continue;
                            if (p.Centroid != null) points.Add(p.Centroid.Value);
                            if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                            else meshes.Add(null);
                            data.Add(p.TemperatureOverride[h]);
                        }
                        else if (p.TemperatureOverride != null && p.SimulationType == SimulationType.Ambient)
                        {
                            if (h < 0 || h >= p.TemperatureOverride.Length) continue;
                            if (p.Centroid != null) points.Add(p.Centroid.Value);
                            if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                            else meshes.Add(null);
                            data.Add(p.TemperatureOverride[h]);
                        }
                    }
                    else if (metric == RPolyMetric.SeenByProbes)
                    {
                        if (p.Centroid != null) points.Add(p.Centroid.Value);
                        if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                        else meshes.Add(null);
                        data.Add((float)p.SeenByProbes);
                    }
                }

                DA.SetDataList(0, points);
                DA.SetDataList(1, meshes);
                DA.SetDataList(2, data);
            }
            else
            {
                if (h < 0 || h >= rpolyList.Count)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Polygon index {h} is outside the valid range 0-{rpolyList.Count - 1}.");
                    return;
                }

                var p = rpolyList[h];

                if (metric == RPolyMetric.SurfaceTemperature)
                {
                    if (p.SurfaceTemperature != null && p.SimulationType == SimulationType.Simulated)
                    {
                        if (p.Centroid != null) points.Add(p.Centroid.Value);
                        if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                        else meshes.Add(null);
                        data.AddRange(p.SurfaceTemperature);
                    }
                    else if (p.TemperatureOverride != null && p.SimulationType == SimulationType.TemperatureInput)
                    {
                        if (p.Centroid != null) points.Add(p.Centroid.Value);
                        if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                        else meshes.Add(null);
                        data.AddRange(p.TemperatureOverride);
                    }
                    else if (p.TemperatureOverride != null && p.SimulationType == SimulationType.Ambient)
                    {
                        if (p.Centroid != null) points.Add(p.Centroid.Value);
                        if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                        else meshes.Add(null);
                        data.AddRange(p.TemperatureOverride);
                    }
                }
                else if (metric == RPolyMetric.SeenByProbes)
                {
                    if (p.Centroid != null) points.Add(p.Centroid.Value);
                    if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                    else meshes.Add(null);
                    data.Add((float)p.SeenByProbes);
                }

                DA.SetDataList(0, points);
                DA.SetDataList(1, meshes);
                DA.SetDataList(2, data);
            }

            if (metric == RPolyMetric.SurfaceTemperature && data.Count == 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "No surface temperature values were found. Run MRT with EnergyPlus surface temperatures enabled and check that the VFC setting is not filtering out all polygons.");
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.Eddy_MRT_InspectSurface;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{3FA34D17-4514-4FDB-849A-4BFD83038ACC}"); }
        }
    }
}
