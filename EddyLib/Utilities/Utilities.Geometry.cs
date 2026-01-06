using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EddyLib
{
    public static partial class Utilities
    {
        public static int[] GetOutsidePoints(Mesh FineGround, Mesh BuildingMesh, int TargetCount)
        {
            // Using PLINQ for parallel processing
            var outsidePointIdx = FineGround.Vertices.AsParallel().AsOrdered().Select((p, i) =>
            {
                // Create a new Ray3d object each time with the desired origin and direction
                var ray = new Ray3d(new Point3d(p.X, p.Y, 9999), new Vector3d(0, 0, -1));

                return Intersection.MeshRay(BuildingMesh, ray) >= 0 ? i : -1;
            })
            .Where(index => index != -1)
            .ToArray();

            return outsidePointIdx;
        }

        public static List<int> GetOutsidePointsWindingNumber(Mesh Ground, Mesh BuildingMesh, double tol)
        {
            //double[] wns = new double[Ground.Vertices.Count];

            //Point3d[] outsidePts = new Point3d[Ground.Vertices.Count];

            List<int> l = new List<int>();

            //BuildingMesh.Faces.ConvertQuadsToTriangles();

            Parallel.For(0, Ground.Vertices.Count, v =>
            {
                double wn = WindingNumber(BuildingMesh, Ground.Vertices[v]);

                //wns[v] = wn;

                if (Math.Abs(wn) >= tol)
                {
                    l.Add(v);
                }
            }

            );

            // Sort list, otherwise it fails

            List<int> ll = l;

            return ll;
        }

        //Lifted from geometry3sharp library

        /// <summary>
        /// Compute mesh winding number, from Jacobson et al, Robust Inside-Outside Segmentation using Generalized Winding Numbers
        /// http://igl.ethz.ch/projects/winding-number/
        /// returns ~0 for points outside a closed, consistently oriented mesh, and a positive or negative integer
        /// for points inside, with value > 1 depending on how many "times" the point inside the mesh (like in 2D polygon winding)
        /// </summary>
        public static double WindingNumber(Mesh mesh, Point3d v)
        {
            double sum = 0;
            foreach (MeshFace face in mesh.Faces)
                sum += GetTriSolidAngle(mesh, face, v);
            return sum / (4.0 * Math.PI);
        }

        public static double GetTriSolidAngle(Mesh mesh, MeshFace face, Point3d p)
        {
            int ta = face.A;
            int tb = face.B;
            int tc = face.C;

            Vector3d a = new Vector3d(mesh.Vertices[ta].X - p.X, mesh.Vertices[ta].Y - p.Y, mesh.Vertices[ta].Z - p.Z);
            Vector3d b = new Vector3d(mesh.Vertices[tb].X - p.X, mesh.Vertices[tb].Y - p.Y, mesh.Vertices[tb].Z - p.Z);
            Vector3d c = new Vector3d(mesh.Vertices[tc].X - p.X, mesh.Vertices[tc].Y - p.Y, mesh.Vertices[tc].Z - p.Z);

            // note: top and bottom are reversed here from formula in the paper? but it doesn't work otherwise...
            double la = a.Length, lb = b.Length, lc = c.Length;
            double bottom = (la * lb * lc) + a * b * lc + b * c * la + c * a * lb;
            double top = a.X * (b.Y * c.Z - c.Y * b.Z) - a.Y * (b.X * c.Z - c.X * b.Z) + a.Z * (b.X * c.Y - c.X * b.Y);
            return 2.0 * Math.Atan2(top, bottom);
        }

        public static bool CheckDomainDimensionsOK(Mesh BuildingGeometry, out double distance)
        {
            bool domainOK = true;
            var BBoxCrude = BuildingGeometry.GetBoundingBox(true);
            distance = 0.0;

            foreach (Point3d pt in BBoxCrude.GetCorners())
            {
                int threshold = 50000;
                if (pt.X > threshold || pt.Y > threshold || pt.Z > threshold)
                {
                    domainOK = false;

                    distance = pt.MaximumCoordinate - threshold;
                }
            }

            return domainOK;
        }

        public static Point3d[] Probes2Point3D(double[][] input)
        {
            int numberOfProbes = input.Count();

            var outputList = new Point3d[numberOfProbes];

            for (int i = 0; i < numberOfProbes; i++)
            {
                outputList[i] = new Point3d(input[i][0], input[i][1], input[i][2]);
            }

            return outputList;
        }

        public static Vector3d[] CSVVectorComponents2Vector3D(double[,] input)
        {
            //double[hours, windDirs]

            int numberOfDirs = input.GetUpperBound(1);
            int numberOfSensors = input.GetUpperBound(0);

            var outputList = new Vector3d[numberOfDirs];

            for (int p = 0; p < numberOfSensors; p++)
            {
                for (int dir = 0; dir < numberOfDirs; dir++)
                {
                    outputList[p] = new Vector3d(input[p, dir + 0], input[p, dir + 1], input[p, dir + 2]);
                }
            }

            return outputList;
        }

        public static Mesh ConvertToQuads(Mesh m)
        {
            if (m.Faces.Count > 0)
            {
                m.Faces.ConvertTrianglesToQuads(Math.PI / 90, .875);
            }

            return m;
        }

        public static double MeshFaceArea(int meshfaceindex, Mesh m)
        {
            //get points into a nice, concise format
            Point3d[] pts = new Point3d[4];
            pts[0] = m.Vertices[m.Faces[meshfaceindex].A];
            pts[1] = m.Vertices[m.Faces[meshfaceindex].B];
            pts[2] = m.Vertices[m.Faces[meshfaceindex].C];
            if (m.Faces[meshfaceindex].IsQuad)
            {
                pts[3] = m.Vertices[m.Faces[meshfaceindex].D];
            }

            //calculate areas of triangles
            double a = pts[0].DistanceTo(pts[1]);
            double b = pts[1].DistanceTo(pts[2]);
            double c = pts[2].DistanceTo(pts[0]);
            double p = 0.5 * (a + b + c);
            double area1 = Math.Sqrt(p * (p - a) * (p - b) * (p - c));

            //if quad, calc area of second triangle
            double area2 = 0;
            if (m.Faces[meshfaceindex].IsQuad)
            {
                a = pts[0].DistanceTo(pts[2]);
                b = pts[2].DistanceTo(pts[3]);
                c = pts[3].DistanceTo(pts[0]);
                p = 0.5 * (a + b + c);
                area2 = Math.Sqrt(p * (p - a) * (p - b) * (p - c));
            }

            return area1 + area2;
        }

        public static Point3d CenterBottomBoundingBox(Mesh geometry)
        {
            BoundingBox empty = BoundingBox.Empty;
            var box = geometry.GetBoundingBox(true);
            empty.Union(box);
            Point3d CenterGround = empty.Center + 0.5 * -Vector3d.ZAxis * (empty.Max.Z - empty.Min.Z);

            return CenterGround;
        }

        public static bool CheckForDuplicates(List<GeometryBase> geo)
        {
            bool equal = false;

            for (int i = 0; i < geo.Count - 1; i++)
            {
                for (int j = 0; j < geo.Count; j++)
                {
                    if (i != j)
                    {
                        equal = GeometryBase.GeometryEquals(geo[i], geo[j]);
                        if (equal == true)
                        {
                            break;
                        }
                    }
                }
            }

            return equal;
        }
    }
}
