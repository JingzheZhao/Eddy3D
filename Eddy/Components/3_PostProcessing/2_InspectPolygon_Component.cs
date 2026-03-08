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
            //dd.Text = Type;
            return base.Read(reader);
        }

        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        public class CustomAttributes : GH_ComponentAttributes
        {
            public CustomAttributes(InspectPolygon_Component owner) : base(owner)
            {
            }

            #region Custom layout logic

            private RectangleF isSensor { get; set; }
            private RectangleF isHour { get; set; }

            protected override void Layout()
            {
                base.Layout();

                //We'll extend the basic layout by adding three regions to the bottom of this component,
                isSensor = new RectangleF(Bounds.X, Bounds.Bottom, Bounds.Width, 20);
                isHour = new RectangleF(Bounds.X, Bounds.Bottom + 20, Bounds.Width, 20);

                Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 40);
            }

            #endregion Custom layout logic

            #region Custom Mouse handling

            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    InspectPolygon_Component comp = Owner as InspectPolygon_Component;

                    if (isSensor.Contains(e.CanvasLocation))
                    {
                        if (comp.ProbePolyMode == "Polygon") return GH_ObjectResponse.Handled;
                        comp.RecordUndoEvent("Polygon");
                        comp.ProbePolyMode = "Polygon";
                        comp.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }

                    if (isHour.Contains(e.CanvasLocation))
                    {
                        if (comp.ProbePolyMode == "Hour") return GH_ObjectResponse.Handled;
                        comp.RecordUndoEvent("Hour");
                        comp.ProbePolyMode = "Hour";
                        comp.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
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

                        GH_Capsule buttonSensor = GH_Capsule.CreateTextCapsule(isSensor, isSensor, comp.ProbePolyMode == "Polygon" ? GH_Palette.Grey : GH_Palette.White, "Polygon", 2, 0);
                        buttonSensor.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                        buttonSensor.Dispose();

                        GH_Capsule buttonHour = GH_Capsule.CreateTextCapsule(isHour, isHour, comp.ProbePolyMode == "Hour" ? GH_Palette.Grey : GH_Palette.White, "Hour", 2, 0);
                        buttonHour.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
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

            List<Point3d> points = new List<Point3d>();
            List<Mesh> meshes = new List<Mesh>();
            List<float> data = new List<float>();

            if (ProbePolyMode == "Hour")
            {
                foreach (var p in rpolyList)
                {
                    if (p == null) continue;
                    if (metric == RPolyMetric.SurfaceTemperature)
                    {
                        if (p.SurfaceTemperature != null && p.SimulationType == SimulationType.Simulated)
                        {
                            if (p.Centroid != null) points.Add(p.Centroid.Value);
                            if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                            else meshes.Add(null);
                            data.Add(p.SurfaceTemperature[h]);
                        }
                        else if (p.TemperatureOverride != null && p.SimulationType == SimulationType.TemperatureInput)
                        {
                            if (p.Centroid != null) points.Add(p.Centroid.Value);
                            if (p.Mesh != null) meshes.Add(p.Mesh.Value);
                            else meshes.Add(null);
                            data.Add(p.TemperatureOverride[h]);
                        }
                        else if (p.SimulationType == SimulationType.Ambient)
                        {
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
                    else if (p.SimulationType == SimulationType.Ambient)
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