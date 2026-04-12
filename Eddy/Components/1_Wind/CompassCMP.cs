using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Eddy.Properties;

namespace Eddy
{
    public class CompassCMP : GH_Component
    {
        public CompassCMP()
            : base("Wind Compass", "Compass",
                "Visualize wind direction on a compass circle. Supports 16 directions (0=N, 1=NNE, 2=NE, etc.).",
                "Eddy3D", "1 | Wind")
        {
        }

        protected override Bitmap Icon => Resources.Eddy_compass;

        public override Guid ComponentGuid => new Guid("{C9D0E1F2-3A4B-5C6D-7E8F-9A0B1C2D3E4F}");

        private Color _previewColor = Color.Black;
        private Polyline _arrow;
        private Curve _circleCurve;
        private List<Line> _ticks = new List<Line>();

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Direction Index", "Idx", 
                "Wind direction index (0=N, 1=NNE, 2=NE, 3=ENE, 4=E, 5=ESE, 6=SE, 7=SSE, 8=S, 9=SSW, 10=SW, 11=WSW, 12=W, 13=WNW, 14=NW, 15=NNW)", 
                GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Radius", "R", "Radius of the compass circle", GH_ParamAccess.item, 10.0);
            pManager.AddPointParameter("Base Point", "P", "Center of the compass", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddColourParameter("Color", "C", "Color of the compass display", GH_ParamAccess.item, Color.DimGray);
            
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Circle", "Cir", "Compass circle curve", GH_ParamAccess.item);
            pManager.AddCurveParameter("Arrow", "Arr", "Wind direction arrow (points in the direction of flow)", GH_ParamAccess.item);
            pManager.AddVectorParameter("Vector", "Vec", "Wind direction vector", GH_ParamAccess.item);
            pManager.AddTextParameter("Direction Name", "Name", "Name of the wind direction (e.g., NNE)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int index = 0;
            double radius = 10.0;
            Point3d center = Point3d.Origin;
            Color col = Color.DimGray;

            DA.GetData(0, ref index);
            DA.GetData(1, ref radius);
            DA.GetData(2, ref center);
            DA.GetData(3, ref col);

            _previewColor = col;

            // Wrap index to [0, 15]
            index = ((index % 16) + 16) % 16;

            string[] names = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            string name = names[index];

            double angleDeg = index * 22.5;
            double rad = angleDeg * Math.PI / 180.0;

            // Following Eddy3D convention: North is (0, -1)
            double vx = -Math.Sin(rad);
            double vy = -Math.Cos(rad);
            Vector3d direction = new Vector3d(vx, vy, 0);
            direction.Unitize();

            // ── Geometry ──
            Circle circle = new Circle(Plane.WorldXY, center, radius);
            _circleCurve = circle.ToNurbsCurve();
            
            // ── Ticks ──
            _ticks.Clear();
            for (int i = 0; i < 16; i++)
            {
                double a = i * 22.5 * Math.PI / 180.0;
                // Note: using North convention for ticks too so they align with the indexing
                Vector3d tickDir = new Vector3d(-Math.Sin(a), -Math.Cos(a), 0);
                
                double len = (i % 2 == 0) ? radius * 0.15 : radius * 0.07;
                Point3d pOut = center + tickDir * radius;
                Point3d pIn = center + tickDir * (radius - len);
                _ticks.Add(new Line(pOut, pIn));
            }

            // ── Flow Arrow ──
            Point3d start = center;
            Point3d end = center + direction * radius;
            
            // Create a small arrow head explicitly in XY plane
            double headLen = radius * 0.2;
            Vector3d rev = -direction;
            
            // Perpendicular vector in XY plane
            Vector3d side = new Vector3d(-direction.Y, direction.X, 0);
            
            Point3d p1 = end + rev * headLen + side * (headLen * 0.5);
            Point3d p2 = end + rev * headLen - side * (headLen * 0.5);
            
            _arrow = new Polyline();
            _arrow.Add(start);
            _arrow.Add(end);
            _arrow.Add(p1);
            _arrow.Add(end);
            _arrow.Add(p2);

            DA.SetData(0, _circleCurve);
            DA.SetData(1, _arrow.ToPolylineCurve());
            DA.SetData(2, direction);
            DA.SetData(3, name);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (_circleCurve != null)
                args.Display.DrawCurve(_circleCurve, _previewColor, 2);
            
            if (_arrow != null)
                args.Display.DrawPolyline(_arrow, _previewColor, 3);

            if (_ticks != null)
            {
                foreach (Line tick in _ticks)
                    args.Display.DrawLine(tick, _previewColor, 1);
            }

            base.DrawViewportWires(args);
        }
    }
}
