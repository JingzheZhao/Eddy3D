using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.Domain
{
    /// <summary>
    /// Builds cylindrical domain meshes with core/perimeter structure.
    /// Extracted from OFCylDomain for modularity.
    /// </summary>
    public class CylindricalMeshBuilder
    {
        private readonly Point3d _center;
        private readonly double _innerRectSize;
        private readonly double _circleRadius;
        private readonly double _height;
        private readonly int _divsRadial;
        private readonly int _divisionsX;
        private readonly int _divisionsZ;
        private int _divPerim;

        /// <summary>
        /// Core bottom mesh.
        /// </summary>
        public Mesh CoreBottom { get; private set; }

        /// <summary>
        /// Core top mesh.
        /// </summary>
        public Mesh CoreTop { get; private set; }

        /// <summary>
        /// Perimeter bottom mesh.
        /// </summary>
        public Mesh PerimBottom { get; private set; }

        /// <summary>
        /// Perimeter top mesh.
        /// </summary>
        public Mesh PerimTop { get; private set; }

        /// <summary>
        /// Side walls mesh.
        /// </summary>
        public Mesh Sides { get; private set; }

        /// <summary>
        /// Combined domain mesh.
        /// </summary>
        public Mesh DomainMesh { get; private set; }

        /// <summary>
        /// Ground mesh (core).
        /// </summary>
        public Mesh GroundMesh { get; private set; }

        /// <summary>
        /// Ground perimeter mesh.
        /// </summary>
        public Mesh GroundPerimMesh { get; private set; }

        /// <summary>
        /// Location inside the mesh for snappyHexMesh.
        /// </summary>
        public Point3d LocationInMesh { get; private set; }

        /// <summary>
        /// Points on the inner rectangle boundary.
        /// </summary>
        public Point3d[] PointsOnRect { get; private set; }

        /// <summary>
        /// Points on the outer circle boundary.
        /// </summary>
        public Point3d[] PointsOnCircle { get; private set; }

        /// <summary>
        /// Concentric division polylines for visualization.
        /// </summary>
        public List<Polyline> ConcentricDivisions { get; private set; }

        /// <summary>
        /// Outer circles at each Z level.
        /// </summary>
        public List<Circle> OuterCircles { get; private set; }

        /// <summary>
        /// Number of perimeter divisions.
        /// </summary>
        public int DivisionsPerim => _divPerim;

        /// <summary>
        /// Number of Z divisions.
        /// </summary>
        public int DivisionsZ => _divisionsZ;

        /// <summary>
        /// Perimeter grading factor.
        /// </summary>
        public double GradingPerim { get; set; } = 1.0;

        /// <summary>
        /// Creates a cylindrical mesh builder.
        /// </summary>
        public CylindricalMeshBuilder(
            Point3d center,
            double innerRectSize,
            double circleRadius,
            double height,
            int divsRadial,
            int divisionsX,
            double coreBlockSize)
        {
            _center = center;
            _innerRectSize = innerRectSize;
            _circleRadius = circleRadius;
            _height = height;
            _divsRadial = Math.Max(1, divsRadial);
            _divisionsX = divisionsX;

            // Calculate cell size and divisions
            double cellSizeCore = 2 * (innerRectSize / _divsRadial);
            _divisionsZ = Math.Max(1, (int)(height / cellSizeCore));

            CoreBottom = new Mesh();
            CoreTop = new Mesh();
            PerimBottom = new Mesh();
            PerimTop = new Mesh();
            Sides = new Mesh();
            DomainMesh = new Mesh();
            GroundMesh = new Mesh();
            GroundPerimMesh = new Mesh();
            ConcentricDivisions = new List<Polyline>();
            OuterCircles = new List<Circle>();

            Build(coreBlockSize);
        }

        private void Build(double coreBlockSize)
        {
            // Location in mesh for snappyHexMesh
            LocationInMesh = _center + (Vector3d.ZAxis * (_height - 0.1));
            LocationInMesh += _circleRadius * 0.6 * Vector3d.XAxis;

            Plane pl = new Plane(_center, Vector3d.ZAxis);
            Interval xinter = new Interval(-_innerRectSize, _innerRectSize);

            // Create core bottom mesh
            CoreBottom = Mesh.CreateFromPlane(pl, xinter, xinter, _divsRadial, _divsRadial);

            // Ensure minimum radius
            double minRad = Math.Sqrt(2 * (_innerRectSize * _innerRectSize)) * DomainConstants.MinRadiusMultiplier;
            double circRad = Math.Max(_circleRadius, minRad);

            // Get naked edges and adjust
            Polyline nakedEdges = CoreBottom.GetNakedEdges()[0];
            Polyline adjustedPolyline = AdjustPolylineSeamAndOrientation(nakedEdges, true);

            SetPointsOnRect(_divsRadial, adjustedPolyline);
            SetPointsOnCircle(_center, circRad, adjustedPolyline);

            double blockDimensionCore = CalculateBlockDimensionCore(PointsOnRect);
            _divPerim = CalculateDivisionsPerim(PointsOnRect, PointsOnCircle, blockDimensionCore);

            // Get visualization divisions
            ConcentricDivisions = GetConcentricPolyDivisions(PointsOnRect, PointsOnCircle, _divPerim, _height);
            OuterCircles = GetOuterCircles(_divisionsZ, circRad, _center, (int)coreBlockSize);

            // Build mesh components
            CoreTop.Append(CoreBottom);
            CoreTop.Translate(Vector3d.ZAxis * _height);

            PerimBottom = BuildPerimeterRing(adjustedPolyline, PointsOnCircle);
            PerimTop.Append(PerimBottom);
            PerimTop.Translate(Vector3d.ZAxis * _height);

            Sides = BuildSideWalls(PointsOnCircle, _height);

            // Flip normals as needed
            CheckAndFlipMeshNormals();
            WeldAllMeshes();

            // Combine meshes
            GroundMesh.Append(CoreBottom);
            GroundPerimMesh.Append(PerimBottom);

            DomainMesh.Append(PerimBottom);
            DomainMesh.Append(CoreBottom);
            DomainMesh.Append(PerimTop);
            DomainMesh.Append(CoreTop);
            DomainMesh.Append(Sides);
            DomainMesh.Normals.ComputeNormals();
            DomainMesh.Weld(DomainConstants.WeldAngle);
            DomainMesh.Vertices.CombineIdentical(true, true);
        }

        private Polyline AdjustPolylineSeamAndOrientation(Polyline nakedEdge, bool reverseOrientation)
        {
            NurbsCurve curve = nakedEdge.ToNurbsCurve();
            double tStart = curve.Domain.Min;
            double tEnd = curve.Domain.Max;

            curve.GetNextDiscontinuity(Continuity.G1_continuous, tStart, tEnd, out double discontinuityParam);
            curve.ChangeClosedCurveSeam(discontinuityParam);

            if (reverseOrientation)
            {
                curve.Reverse();
            }

            if (!curve.TryGetPolyline(out Polyline newPolyline))
            {
                throw new InvalidOperationException("Failed to convert curve back to polyline.");
            }

            return newPolyline;
        }

        private void SetPointsOnRect(int divisions, Polyline nakedEdges)
        {
            nakedEdges.ToNurbsCurve().DivideByCount(divisions * 4, true, out Point3d[] points);
            PointsOnRect = points;
        }

        private void SetPointsOnCircle(Point3d center, double circleRadius, Polyline nakedEdges)
        {
            var points = new List<Point3d>();
            Point3d newCenter = new Point3d(center.X, center.Y, 0);
            Circle c = new Circle(newCenter, circleRadius);

            for (int i = 0; i < nakedEdges.Count; i++)
            {
                Vector3d vec = newCenter - nakedEdges[i];
                vec.Unitize();
                vec *= (circleRadius + 1);

                Rhino.Geometry.Intersect.Intersection.LineCircle(
                    new Line(newCenter, vec), c,
                    out _, out Point3d p1, out _, out _);

                points.Add(new Point3d(p1.X, p1.Y, center.Z));
            }

            PointsOnCircle = points.ToArray();
        }

        private static double CalculateBlockDimensionCore(Point3d[] core)
        {
            Vector3d vec = new Vector3d(core[1].X, core[1].Y, core[1].Z) -
                           new Vector3d(core[0].X, core[0].Y, core[0].Z);
            return vec.Length;
        }

        private static int CalculateDivisionsPerim(Point3d[] core, Point3d[] perim, double blockDim)
        {
            Vector3d vec = new Vector3d(perim[0].X, perim[0].Y, perim[0].Z) -
                           new Vector3d(core[0].X, core[0].Y, core[0].Z);
            return Math.Max(1, (int)(vec.Length / blockDim));
        }

        private Mesh BuildPerimeterRing(Polyline poly, Point3d[] pointsOnCircle)
        {
            Mesh mesh = new Mesh();
            int vcount = 0;

            for (int i = 0; i < poly.Count - 1; i++)
            {
                mesh.Vertices.Add(poly[i]);
                mesh.Vertices.Add(pointsOnCircle[i]);
                mesh.Vertices.Add(pointsOnCircle[i + 1]);
                mesh.Vertices.Add(poly[i + 1]);

                mesh.Faces.AddFace(new MeshFace(vcount, vcount + 1, vcount + 2, vcount + 3));
                vcount += 4;
            }

            mesh.Normals.ComputeNormals();
            return mesh;
        }

        private Mesh BuildSideWalls(Point3d[] points, double height)
        {
            Mesh mesh = new Mesh();
            int vcount = 0;

            for (int i = 0; i < points.Length - 1; i++)
            {
                mesh.Vertices.Add(points[i]);
                mesh.Vertices.Add(points[i] + Vector3d.ZAxis * height);
                mesh.Vertices.Add(points[i + 1] + Vector3d.ZAxis * height);
                mesh.Vertices.Add(points[i + 1]);

                mesh.Faces.AddFace(new MeshFace(vcount, vcount + 1, vcount + 2, vcount + 3));
                vcount += 4;
            }

            mesh.Normals.ComputeNormals();
            return mesh;
        }

        private void CheckAndFlipMeshNormals()
        {
            FlipIfNeeded(PerimBottom, Vector3d.ZAxis, shouldBePositive: false);
            FlipIfNeeded(PerimTop, Vector3d.ZAxis, shouldBePositive: true);
            FlipIfNeeded(CoreTop, Vector3d.ZAxis, shouldBePositive: true);
            FlipIfNeeded(CoreBottom, Vector3d.ZAxis, shouldBePositive: false);

            // Sides should point outward
            if (Sides.Faces.Count > 0)
            {
                var firstIndex = Sides.Faces[0].A;
                var firstPoint = Sides.Vertices[firstIndex];
                var firstNormal = Sides.FaceNormals[0];
                var vec = firstPoint - new Point3d(_center.X, _center.Y, firstPoint.Z);

                if (vec * firstNormal < 0)
                {
                    Sides.Flip(true, true, true);
                }
            }
        }

        private static void FlipIfNeeded(Mesh mesh, Vector3d axis, bool shouldBePositive)
        {
            if (mesh.Normals.Count == 0) return;

            var normal = mesh.Normals[0];
            normal.Unitize();
            double dot = normal * axis;

            if ((shouldBePositive && dot < 0) || (!shouldBePositive && dot > 0))
            {
                mesh.Flip(true, true, true);
            }
        }

        private void WeldAllMeshes()
        {
            CoreBottom.Weld(DomainConstants.WeldAngle);
            CoreTop.Weld(DomainConstants.WeldAngle);
            PerimBottom.Weld(DomainConstants.WeldAngle);
            PerimTop.Weld(DomainConstants.WeldAngle);
            Sides.Weld(DomainConstants.WeldAngle);
        }

        private List<Circle> GetOuterCircles(int divsZ, double outerRad, Point3d centerBottom, int blockSize)
        {
            var list = new List<Circle>();
            for (int i = 0; i < divsZ; i++)
            {
                Point3d center = centerBottom + (Vector3d.ZAxis * i * blockSize);
                list.Add(new Circle(center, outerRad));
            }
            return list;
        }

        private List<Polyline> GetConcentricPolyDivisions(
            Point3d[] pointsOnRect,
            Point3d[] pointsOnCircle,
            int divPerim,
            double topOfDomain)
        {
            // Create radial divisions
            var radialDivisions = new List<Polyline>();
            for (int i = 0; i < pointsOnRect.Length; i++)
            {
                radialDivisions.Add(new Polyline(new[] { pointsOnRect[i], pointsOnCircle[i] }));
            }

            // Create intersection points
            var divPointsCut = new List<Point3d[]>();
            for (int i = 0; i < pointsOnRect.Length; i++)
            {
                new PolylineCurve(radialDivisions[i]).DivideByCount(divPerim, true, out Point3d[] ar);
                divPointsCut.Add(ar);
            }

            // Flatten to sequential list
            var fullList = new List<Point3d>();
            foreach (var ar in divPointsCut)
            {
                foreach (var pt in ar)
                {
                    fullList.Add(pt);
                }
            }

            // Build concentric rings
            var innerRadialList = new List<Point3d>();
            for (int j = 0; j < divPerim; j++)
            {
                for (int i = 0; i < fullList.Count; i++)
                {
                    innerRadialList.Add(fullList[i + j]);
                    i += divPerim;
                }
                innerRadialList.Add(fullList[j]);
            }

            // Remove duplicates and split into rings
            var noDupes = innerRadialList.Distinct().ToList();
            var lists = SplitPointList(noDupes, pointsOnRect.Length);

            // Close each ring
            foreach (var l in lists)
            {
                l.Add(l[0]);
            }

            // Create bottom and top versions
            var vec = Vector3d.ZAxis * topOfDomain;
            var xf = Transform.Translation(vec);

            var result = new List<Polyline>();
            foreach (var l in lists)
            {
                var pl = new Polyline(l);
                result.Add(pl);

                var plTop = new Polyline(l);
                plTop.Transform(xf);
                result.Add(plTop);
            }

            return result;
        }

        private static List<List<Point3d>> SplitPointList(List<Point3d> source, int chunkSize)
        {
            var result = new List<List<Point3d>>();
            for (int i = 0; i < source.Count; i += chunkSize)
            {
                result.Add(source.GetRange(i, Math.Min(chunkSize, source.Count - i)));
            }
            return result;
        }
    }
}
