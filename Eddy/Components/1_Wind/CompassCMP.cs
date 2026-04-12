using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;
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

        private Color _currentPreviewColor = Color.DimGray;
        private Polyline _arrow;
        private Curve _circleCurve;
        private double _currentRadius = 10.0;
        private Point3d _currentCenter = Point3d.Origin;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Wind Direction", "Dir", 
                "Wind direction in degrees (0=N, 90=E, 180=S, 270=W)", 
                GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Radius", "R", "Radius of the compass circle", GH_ParamAccess.item, 10.0);
            pManager.AddPointParameter("Base Point", "P", "Center of the compass", GH_ParamAccess.item, Point3d.Origin);
            pManager.AddColourParameter("Color", "C", "Color of the compass display", GH_ParamAccess.item, Color.DimGray);
            pManager.AddNumberParameter("Arrow Scale", "S", "Scale of the directional arrow", GH_ParamAccess.item, 1.0);
            
            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddVectorParameter("Vector", "Vec", "Wind direction vector", GH_ParamAccess.item);
            pManager.AddTextParameter("Direction Name", "Name", "Name of the wind direction (e.g., NNE)", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double angleDeg = 0.0;
            double radius = 10.0;
            Point3d center = Point3d.Origin;
            Color col = Color.DimGray;
            double scale = 1.0;

            DA.GetData(0, ref angleDeg);
            DA.GetData(1, ref radius);
            DA.GetData(2, ref center);
            DA.GetData(3, ref col);
            DA.GetData(4, ref scale);

            _currentPreviewColor = col;
            _currentRadius = radius;
            _currentCenter = center;

            // Normalize angle to [0, 360)
            double normAngle = ((angleDeg % 360.0) + 360.0) % 360.0;
            
            // Map to the nearest 16th for the name
            int nameIndex = (int)Math.Round(normAngle / 22.5) % 16;
            string[] names = { 
                "North", "North-northeast", "Northeast", "East-northeast", 
                "East", "East-southeast", "Southeast", "South-southeast", 
                "South", "South-southwest", "Southwest", "West-southwest", 
                "West", "West-northwest", "Northwest", "North-northwest" 
            };
            string name = names[nameIndex];

            double rad = normAngle * Math.PI / 180.0;

            // Mathematical mapping for Clockwise from North:
            // 0 deg -> (0, -1)
            // 90 deg -> (-1, 0)
            double vx = -Math.Sin(rad);
            double vy = -Math.Cos(rad);
            Vector3d direction = new Vector3d(vx, vy, 0);
            direction.Unitize();

            // ── Geometry ──
            _circleCurve = new Circle(Plane.WorldXY, center, radius).ToNurbsCurve();
            
            // ── Simple Arrow (Outside pointing in) ──
            Vector3d fromDir = -direction; 
            Point3d tip = center + fromDir * (radius * 1.02);
            double shaftLen = radius * 0.3 * scale;
            Point3d start = tip + fromDir * shaftLen;
            
            double headSize = (radius * 0.1) * scale;
            Vector3d side = new Vector3d(-fromDir.Y, fromDir.X, 0) * headSize;
            Point3d p1 = tip + fromDir * headSize + side;
            Point3d p2 = tip + fromDir * headSize - side;

            _arrow = new Polyline();
            _arrow.Add(start);
            _arrow.Add(tip);
            _arrow.Add(p1);
            _arrow.Add(tip);
            _arrow.Add(p2);

            DA.SetData(0, direction);
            DA.SetData(1, name);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (_currentPreviewColor.A == 0) return;

            // Only one circle drawn here, no outputs to duplicate it
            if (_circleCurve != null)
                args.Display.DrawCurve(_circleCurve, _currentPreviewColor, 2);

            if (_arrow != null)
                args.Display.DrawPolyline(_arrow, _currentPreviewColor, 2);

            base.DrawViewportWires(args);
        }
    }
}
