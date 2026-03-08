using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EddyLib
{
    public static partial class Utilities
    {
        /// <summary>
        /// Gets indices of ground vertices that are outside (above) the building mesh using ray casting.
        /// </summary>
        public static int[] GetOutsidePoints(Mesh FineGround, Mesh BuildingMesh, int TargetCount)
        {
            return FineGround.Vertices
                .AsParallel()
                .AsOrdered()
                .Select((p, i) =>
                {
                    var ray = new Ray3d(new Point3d(p.X, p.Y, 9999), -Vector3d.ZAxis);
                    return Intersection.MeshRay(BuildingMesh, ray) >= 0 ? i : -1;
                })
                .Where(index => index != -1)
                .ToArray();
        }

        /// <summary>
        /// Gets indices of ground vertices inside the building mesh using winding number algorithm.
        /// Thread-safe implementation using ConcurrentBag.
        /// </summary>
        public static List<int> GetOutsidePointsWindingNumber(Mesh Ground, Mesh BuildingMesh, double tol)
        {
            var insideIndices = new ConcurrentBag<int>();

            Parallel.For(0, Ground.Vertices.Count, v =>
            {
                double wn = WindingNumber(BuildingMesh, Ground.Vertices[v]);
                if (Math.Abs(wn) >= tol)
                {
                    insideIndices.Add(v);
                }
            });

            // Sort for consistent ordering
            return insideIndices.OrderBy(x => x).ToList();
        }

        /// <summary>
        /// Computes mesh winding number for a point.
        /// From Jacobson et al, "Robust Inside-Outside Segmentation using Generalized Winding Numbers"
        /// Returns ~0 for points outside, positive/negative integer for points inside.
        /// </summary>
        public static double WindingNumber(Mesh mesh, Point3d point)
        {
            double sum = 0;
            foreach (MeshFace face in mesh.Faces)
            {
                sum += GetTriSolidAngle(mesh, face, point);
            }
            return sum / (4.0 * Math.PI);
        }

        /// <summary>
        /// Computes the solid angle subtended by a triangle as seen from a point.
        /// </summary>
        public static double GetTriSolidAngle(Mesh mesh, MeshFace face, Point3d p)
        {
            // Vectors from point to triangle vertices
            Point3f va = mesh.Vertices[face.A];
            Point3f vb = mesh.Vertices[face.B];
            Point3f vc = mesh.Vertices[face.C];

            Vector3d a = new Vector3d(va.X - p.X, va.Y - p.Y, va.Z - p.Z);
            Vector3d b = new Vector3d(vb.X - p.X, vb.Y - p.Y, vb.Z - p.Z);
            Vector3d c = new Vector3d(vc.X - p.X, vc.Y - p.Y, vc.Z - p.Z);

            double la = a.Length, lb = b.Length, lc = c.Length;

            // Denominator: |a||b||c| + (a·b)|c| + (b·c)|a| + (c·a)|b|
            double bottom = (la * lb * lc) + (a * b) * lc + (b * c) * la + (c * a) * lb;

            // Numerator: scalar triple product a·(b×c)
            double top = a.X * (b.Y * c.Z - c.Y * b.Z)
                       - a.Y * (b.X * c.Z - c.X * b.Z)
                       + a.Z * (b.X * c.Y - c.X * b.Y);

            return 2.0 * Math.Atan2(top, bottom);
        }

        /// <summary>
        /// Checks if domain dimensions are within acceptable bounds.
        /// </summary>
        public static bool CheckDomainDimensionsOK(Mesh BuildingGeometry, out double distance)
        {
            const int Threshold = 50000;
            var bbox = BuildingGeometry.GetBoundingBox(true);
            distance = 0.0;

            foreach (Point3d pt in bbox.GetCorners())
            {
                if (pt.X > Threshold || pt.Y > Threshold || pt.Z > Threshold)
                {
                    distance = pt.MaximumCoordinate - Threshold;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Converts 2D array of probe coordinates to Point3d array.
        /// </summary>
        public static Point3d[] Probes2Point3D(double[][] input)
        {
            return input.Select(arr => new Point3d(arr[0], arr[1], arr[2])).ToArray();
        }

        /// <summary>
        /// Converts CSV vector components to Vector3d array.
        /// </summary>
        public static Vector3d[] CSVVectorComponents2Vector3D(double[,] input)
        {
            int numberOfDirs = input.GetUpperBound(1);
            int numberOfSensors = input.GetUpperBound(0);

            var outputList = new Vector3d[numberOfDirs];

            for (int p = 0; p < numberOfSensors; p++)
            {
                for (int dir = 0; dir < numberOfDirs; dir++)
                {
                    outputList[p] = new Vector3d(input[p, dir], input[p, dir + 1], input[p, dir + 2]);
                }
            }

            return outputList;
        }

        /// <summary>
        /// Converts triangular mesh faces to quads where possible.
        /// </summary>
        public static Mesh ConvertToQuads(Mesh m)
        {
            if (m.Faces.Count > 0)
            {
                m.Faces.ConvertTrianglesToQuads(Math.PI / 90, 0.875);
            }
            return m;
        }

        /// <summary>
        /// Calculates the area of a mesh face using Heron's formula.
        /// </summary>
        public static double MeshFaceArea(int faceIndex, Mesh m)
        {
            MeshFace face = m.Faces[faceIndex];
            Point3d p0 = m.Vertices[face.A];
            Point3d p1 = m.Vertices[face.B];
            Point3d p2 = m.Vertices[face.C];

            double area1 = TriangleAreaHeron(p0, p1, p2);

            if (face.IsQuad)
            {
                Point3d p3 = m.Vertices[face.D];
                double area2 = TriangleAreaHeron(p0, p2, p3);
                return area1 + area2;
            }

            return area1;
        }

        /// <summary>
        /// Calculates triangle area using Heron's formula.
        /// </summary>
        private static double TriangleAreaHeron(Point3d p0, Point3d p1, Point3d p2)
        {
            double a = p0.DistanceTo(p1);
            double b = p1.DistanceTo(p2);
            double c = p2.DistanceTo(p0);
            double s = 0.5 * (a + b + c);
            return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
        }

        /// <summary>
        /// Gets the center-bottom point of a mesh's bounding box.
        /// </summary>
        public static Point3d CenterBottomBoundingBox(Mesh geometry)
        {
            var box = geometry.GetBoundingBox(true);
            return new Point3d(box.Center.X, box.Center.Y, box.Min.Z);
        }

        /// <summary>
        /// Checks if the geometry list contains any duplicate objects.
        /// </summary>
        public static bool CheckForDuplicates(List<GeometryBase> geo)
        {
            // Use HashSet approach for O(n) average case
            for (int i = 0; i < geo.Count; i++)
            {
                for (int j = i + 1; j < geo.Count; j++)
                {
                    if (GeometryBase.GeometryEquals(geo[i], geo[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
