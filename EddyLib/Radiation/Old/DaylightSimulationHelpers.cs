using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace EddyLib.Radiation
{
    public class DaylightSimulationHelpers
    {
        //============================================EXT SENSOR POINT GEN=========================================

        public static Vector3d[] genFaceSensorsNormals(Mesh m)
        {
            Vector3d[] FaceSensorNormals = new Vector3d[m.FaceNormals.Count];
            for (int f = 0; f < m.FaceNormals.Count; f++)
            {
                FaceSensorNormals[f] = m.FaceNormals[f];
                FaceSensorNormals[f].Unitize();
            }
            return FaceSensorNormals;
        }

        public static Point3d[] genFaceSensorPoints(Mesh m, Vector3d[] v, double offset)
        {
            Point3d[] FaceSensors = new Point3d[m.Faces.Count];

            for (int f = 0; f < m.Faces.Count; f++)
            {
                if (m.Faces[f].IsTriangle)
                {
                    Point3d p1 = m.Vertices[m.Faces[f].A];
                    Point3d p2 = m.Vertices[m.Faces[f].B];
                    Point3d p3 = m.Vertices[m.Faces[f].C];

                    Point3d sen = ((p1 + p2 + p3) / 3.0) + v[f] * offset;
                    FaceSensors[f] = sen;
                }
                else
                {
                    Point3d p1 = m.Vertices[m.Faces[f].A];
                    Point3d p2 = m.Vertices[m.Faces[f].B];
                    Point3d p3 = m.Vertices[m.Faces[f].C];
                    Point3d p4 = m.Vertices[m.Faces[f].D];

                    Point3d sen = ((p1 + p2 + p3 + p4) / 4.0) + v[f] * offset;
                    FaceSensors[f] = sen;
                }
            }
            return FaceSensors;
        }

        public static Point3d[] genFaceSensorPoints(Mesh m)
        {
            Point3d[] FaceSensors = new Point3d[m.Faces.Count];

            for (int f = 0; f < m.Faces.Count; f++)
            {
                if (m.Faces[f].IsTriangle)
                {
                    Point3d p1 = m.Vertices[m.Faces[f].A];
                    Point3d p2 = m.Vertices[m.Faces[f].B];
                    Point3d p3 = m.Vertices[m.Faces[f].C];

                    Point3d sen = ((p1 + p2 + p3) / 3.0);
                    FaceSensors[f] = sen;
                }
                else
                {
                    Point3d p1 = m.Vertices[m.Faces[f].A];
                    Point3d p2 = m.Vertices[m.Faces[f].B];
                    Point3d p3 = m.Vertices[m.Faces[f].C];
                    Point3d p4 = m.Vertices[m.Faces[f].D];

                    Point3d sen = ((p1 + p2 + p3 + p4) / 4.0);
                    FaceSensors[f] = sen;
                }
            }
            return FaceSensors;
        }

        //===================================Mesh Face Value Lookup
        public static int FaceValueLookup(Point3d pt, Mesh EnvMesh, int[][] VertexRadValues, int h)  // used to/in sensor class
        {
            MeshPoint mp = EnvMesh.ClosestMeshPoint(pt, 0.5);
            int roundVal = VertexRadValues[mp.FaceIndex][h];
            return roundVal;
        }

        //===================================BARYCENTRIC FROM ENV MESH TO SENSOR VALUES=====================================
        public static int BarycentricInterpolation(Point3d pt, Mesh EnvMesh, int[][] VertexRadValues, int h)  // used to/in sensor class
        {
            double ipolval = -99;

            MeshPoint mp = EnvMesh.ClosestMeshPoint(pt, 0.5);

            if (mp != null)
            {
                double valA = VertexRadValues[EnvMesh.Faces[mp.FaceIndex].A][h] * mp.T[0];
                double valB = VertexRadValues[EnvMesh.Faces[mp.FaceIndex].B][h] * mp.T[1];
                double valC = VertexRadValues[EnvMesh.Faces[mp.FaceIndex].C][h] * mp.T[2];
                double valD = VertexRadValues[EnvMesh.Faces[mp.FaceIndex].D][h] * mp.T[3];
                if (valA < 0) { valA = 0.0; }
                if (valB < 0) { valB = 0.0; }
                if (valC < 0) { valC = 0.0; }
                if (valD < 0) { valD = 0.0; }
                ipolval = valA + valB + valC + valD;
            }
            else // ZUR SICHERHEIT
            {
                double mindist = double.MaxValue;
                int vertexindex = 0;
                for (int i = 0; i < EnvMesh.Vertices.Count; i++) { if (mindist > pt.DistanceTo(EnvMesh.Vertices[i])) { mindist = pt.DistanceTo(EnvMesh.Vertices[i]); vertexindex = i; } }
                ipolval = VertexRadValues[vertexindex][h];
            }

            int roundVal = (int)Math.Round(ipolval);
            return roundVal;
        }

        public static Mesh ColorMeshFaces2(Mesh m, double[] results, double maxval, Color low, Color hi)
        {
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m.TextureCoordinates.SetTextureCoordinate(i, 0.0f, 0.0f);
            }

            for (int f = 0; f < m.Faces.Count; f++)
            {
                //Calc area of the triangle
                MeshFace fc = m.Faces.GetFace(f);
                Vector3d v1 = new Point3d(m.Vertices[m.Faces[f].B]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                Vector3d v2 = new Point3d(m.Vertices[m.Faces[f].C]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                double area = 0.5 * (Vector3d.CrossProduct(v1, v2).Length);
                //Calc area of a Quad (2 triangles)
                if (m.Faces[f].IsQuad)
                {
                    Vector3d v3 = new Point3d(m.Vertices[m.Faces[f].D]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                    area += 0.5 * (Vector3d.CrossProduct(v1, v3).Length);
                }

                m.TextureCoordinates[m.Faces[f].A] = new Point2f(m.TextureCoordinates[m.Faces[f].A].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].A].Y + (float)area);
                m.TextureCoordinates[m.Faces[f].B] = new Point2f(m.TextureCoordinates[m.Faces[f].B].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].B].Y + (float)area);
                m.TextureCoordinates[m.Faces[f].C] = new Point2f(m.TextureCoordinates[m.Faces[f].C].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].C].Y + (float)area);

                if (m.Faces[f].IsQuad)
                {
                    m.TextureCoordinates[m.Faces[f].D] = new Point2f(m.TextureCoordinates[m.Faces[f].D].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].D].Y + (float)area);
                }
            }

            float maxtex = 0.0f;
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                if (m.TextureCoordinates[i].Y != 0.0f)
                    m.TextureCoordinates[i] = new Point2f(m.TextureCoordinates[i].X / m.TextureCoordinates[i].Y, 0.5f);

                if (maxtex < m.TextureCoordinates[i].X) maxtex = m.TextureCoordinates[i].X;
            }

            if (maxtex != 0.0f) maxtex = 1.0f / (float)maxval;
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m.TextureCoordinates[i] = new Point2f(maxtex * m.TextureCoordinates[i].X, 0.5f);

                double pc = m.TextureCoordinates[i].X;
                if (pc < 0.0) pc = 0.0;
                else if (pc > 1.0) pc = 1.0;
                Color cc = ColorGenerator.GetColourFromLinearGradient(pc, low, hi);
                m.VertexColors.SetColor(i, cc);
            }
            return m;
        }

        public static Mesh ColorMeshFaces2(Mesh m, double[] results, double maxval, Color low, Color mid, Color hi)
        {
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m.TextureCoordinates.SetTextureCoordinate(i, 0.0f, 0.0f);
            }

            for (int f = 0; f < m.Faces.Count; f++)
            {
                //Calc area of the triangle
                MeshFace fc = m.Faces.GetFace(f);
                Vector3d v1 = new Point3d(m.Vertices[m.Faces[f].B]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                Vector3d v2 = new Point3d(m.Vertices[m.Faces[f].C]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                double area = 0.5 * (Vector3d.CrossProduct(v1, v2).Length);
                //Calc area of a Quad (2 triangles)
                if (m.Faces[f].IsQuad)
                {
                    Vector3d v3 = new Point3d(m.Vertices[m.Faces[f].D]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                    area += 0.5 * (Vector3d.CrossProduct(v1, v3).Length);
                }

                m.TextureCoordinates[m.Faces[f].A] = new Point2f(m.TextureCoordinates[m.Faces[f].A].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].A].Y + (float)area);
                m.TextureCoordinates[m.Faces[f].B] = new Point2f(m.TextureCoordinates[m.Faces[f].B].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].B].Y + (float)area);
                m.TextureCoordinates[m.Faces[f].C] = new Point2f(m.TextureCoordinates[m.Faces[f].C].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].C].Y + (float)area);

                if (m.Faces[f].IsQuad)
                {
                    m.TextureCoordinates[m.Faces[f].D] = new Point2f(m.TextureCoordinates[m.Faces[f].D].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].D].Y + (float)area);
                }
            }

            float maxtex = 0.0f;
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                if (m.TextureCoordinates[i].Y != 0.0f)
                    m.TextureCoordinates[i] = new Point2f(m.TextureCoordinates[i].X / m.TextureCoordinates[i].Y, 0.5f);

                if (maxtex < m.TextureCoordinates[i].X) maxtex = m.TextureCoordinates[i].X;
            }

            if (maxtex != 0.0f) maxtex = 1.0f / (float)maxval;
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m.TextureCoordinates[i] = new Point2f(maxtex * m.TextureCoordinates[i].X, 0.5f);

                double pc = m.TextureCoordinates[i].X;
                if (pc < 0.0) pc = 0.0;
                else if (pc > 1.0) pc = 1.0;
                Color cc = ColorGenerator.GetTriColour(pc, low, mid, hi);
                m.VertexColors.SetColor(i, cc);
            }
            return m;
        }

        public static Mesh ColorMeshFaces2(Mesh m, double[] results, double maxval)
        {
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m.TextureCoordinates.SetTextureCoordinate(i, 0.0f, 0.0f);
            }

            for (int f = 0; f < m.Faces.Count; f++)
            {
                //Calc area of the triangle
                MeshFace fc = m.Faces.GetFace(f);
                Vector3d v1 = new Point3d(m.Vertices[m.Faces[f].B]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                Vector3d v2 = new Point3d(m.Vertices[m.Faces[f].C]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                double area = 0.5 * (Vector3d.CrossProduct(v1, v2).Length);
                //Calc area of a Quad (2 triangles)
                if (m.Faces[f].IsQuad)
                {
                    Vector3d v3 = new Point3d(m.Vertices[m.Faces[f].D]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                    area += 0.5 * (Vector3d.CrossProduct(v1, v3).Length);
                }

                m.TextureCoordinates[m.Faces[f].A] = new Point2f(m.TextureCoordinates[m.Faces[f].A].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].A].Y + (float)area);
                m.TextureCoordinates[m.Faces[f].B] = new Point2f(m.TextureCoordinates[m.Faces[f].B].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].B].Y + (float)area);
                m.TextureCoordinates[m.Faces[f].C] = new Point2f(m.TextureCoordinates[m.Faces[f].C].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].C].Y + (float)area);

                if (m.Faces[f].IsQuad)
                {
                    m.TextureCoordinates[m.Faces[f].D] = new Point2f(m.TextureCoordinates[m.Faces[f].D].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].D].Y + (float)area);
                }
            }

            float maxtex = 0.0f;
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                if (m.TextureCoordinates[i].Y != 0.0f)
                    m.TextureCoordinates[i] = new Point2f(m.TextureCoordinates[i].X / m.TextureCoordinates[i].Y, 0.5f);

                if (maxtex < m.TextureCoordinates[i].X) maxtex = m.TextureCoordinates[i].X;
            }

            if (maxtex != 0.0f) maxtex = 1.0f / (float)maxval;
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m.TextureCoordinates[i] = new Point2f(maxtex * m.TextureCoordinates[i].X, 0.5f);

                double pc = m.TextureCoordinates[i].X;
                if (pc < 0.0) pc = 0.0;
                else if (pc > 1.0) pc = 1.0;
                Color cc = ColorGenerator.GetTriColour(pc, ColorGenerator.UDBLUE, ColorGenerator.UDYELLOW, ColorGenerator.UDRED);
                m.VertexColors.SetColor(i, cc);
            }
            return m;
        }

        public static Mesh ColorMeshFaces2(Mesh m, double[] results)
        {
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m.TextureCoordinates.SetTextureCoordinate(i, 0.0f, 0.0f);
            }

            for (int f = 0; f < m.Faces.Count; f++)
            {
                //Calc area of the triangle
                MeshFace fc = m.Faces.GetFace(f);
                Vector3d v1 = new Point3d(m.Vertices[m.Faces[f].B]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                Vector3d v2 = new Point3d(m.Vertices[m.Faces[f].C]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                double area = 0.5 * (Vector3d.CrossProduct(v1, v2).Length);
                //Calc area of a Quad (2 triangles)
                if (m.Faces[f].IsQuad)
                {
                    Vector3d v3 = new Point3d(m.Vertices[m.Faces[f].D]) - (new Point3d(m.Vertices[m.Faces[f].A]));
                    area += 0.5 * (Vector3d.CrossProduct(v1, v3).Length);
                }

                m.TextureCoordinates[m.Faces[f].A] = new Point2f(m.TextureCoordinates[m.Faces[f].A].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].A].Y + (float)area);
                m.TextureCoordinates[m.Faces[f].B] = new Point2f(m.TextureCoordinates[m.Faces[f].B].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].B].Y + (float)area);
                m.TextureCoordinates[m.Faces[f].C] = new Point2f(m.TextureCoordinates[m.Faces[f].C].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].C].Y + (float)area);

                if (m.Faces[f].IsQuad)
                {
                    m.TextureCoordinates[m.Faces[f].D] = new Point2f(m.TextureCoordinates[m.Faces[f].D].X + (float)(area * results[f]), m.TextureCoordinates[m.Faces[f].D].Y + (float)area);
                }
            }

            float maxtex = 0.0f;
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                if (m.TextureCoordinates[i].Y != 0.0f)
                    m.TextureCoordinates[i] = new Point2f(m.TextureCoordinates[i].X / m.TextureCoordinates[i].Y, 0.5f);

                if (maxtex < m.TextureCoordinates[i].X) maxtex = m.TextureCoordinates[i].X;
            }

            if (maxtex != 0.0f) maxtex = 1.0f / maxtex;
            for (int i = 0; i < m.Vertices.Count; ++i)
            {
                m.TextureCoordinates[i] = new Point2f(maxtex * m.TextureCoordinates[i].X, 0.5f);

                double pc = m.TextureCoordinates[i].X;
                if (pc < 0.0) pc = 0.0;
                else if (pc > 1.0) pc = 1.0;
                Color cc = ColorGenerator.GetTriColour(pc, ColorGenerator.UDBLUE, ColorGenerator.UDYELLOW, ColorGenerator.UDRED);
                m.VertexColors.SetColor(i, cc);
            }
            return m;
        }

        //==========================================Min Max Remap=============================================

        public static double GetMax(List<double> dl)
        {
            double max = double.MinValue;
            for (int i = 0; i < dl.Count; ++i)
            {
                if (max < dl[i]) max = dl[i];
            }
            return max;
        }

        public static double GetAbsMax(List<double> dl)
        {
            double max = double.MinValue;
            for (int i = 0; i < dl.Count; ++i)
            {
                if (max < Math.Abs(dl[i])) max = Math.Abs(dl[i]);
            }
            return max;
        }

        public static double GetAbsMax(double[] dl)
        {
            double max = double.MinValue;
            for (int i = 0; i < dl.Length; ++i)
            {
                if (max < Math.Abs(dl[i])) max = Math.Abs(dl[i]);
            }
            return max;
        }

        public static int GetAbsMax(int[] dl)
        {
            int max = int.MinValue;
            for (int i = 0; i < dl.Length; ++i)
            {
                if (max < Math.Abs(dl[i])) max = Math.Abs(dl[i]);
            }
            return max;
        }

        public static int GetAbsMax(List<int> dl)
        {
            int max = int.MinValue;
            for (int i = 0; i < dl.Count; ++i)
            {
                if (max < Math.Abs(dl[i])) max = Math.Abs(dl[i]);
            }
            return max;
        }

        public static double Remap(double OldValue, double OldMax, double OldMin, double NewMax, double NewMin)
        {
            double OldRange = (OldMax - OldMin);
            double NewRange = (NewMax - NewMin);
            return (((OldValue - OldMin) * NewRange) / OldRange) + NewMin;
        }

        public class ColorGenerator
        {
            public static double Remap(double value, double from1, double to1, double from2, double to2)
            {
                return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
            }

            public static Color UDRED = Color.FromArgb(255, 255, 0, 55);
            public static Color UDBLUE = Color.FromArgb(255, 0, 109, 255);
            public static Color UDYELLOW = Color.FromArgb(255, 255, 255, 0);

            public static void WriteGradiantImg(string path, Color left, Color centre, Color right)
            {
                Bitmap bmp = new Bitmap(100, 20, PixelFormat.Format24bppRgb);
                double w = (double)bmp.Width;
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color c = GetTriColour(x / w, left, centre, right);
                    for (int y = 0; y < bmp.Height; y++)
                        bmp.SetPixel(x, y, c);
                }
                bmp.Save(path, ImageFormat.Png);
            }

            public static Color GetTriColour(double percent, Color left, Color centre, Color right)
            {
                if (percent < 0 || percent > 1)
                    throw new Exception("Percent must be between 0 and 1");

                //double weight = Math.Sin(percent * Math.PI);
                double weight = (Math.Cos((percent * 2 - 1) * Math.PI) + 1) / 2;

                return GetColourFromLinearGradient(weight,
                   percent < 0.5 ? left : right, centre);
            }

            public static Eto.Drawing.Color GetTriColour(double percent, Eto.Drawing.Color left, Eto.Drawing.Color centre, Eto.Drawing.Color right)
            {
                if (percent < 0 || percent > 1)
                    throw new Exception("Percent must be between 0 and 1");

                //double weight = Math.Sin(percent * Math.PI);
                double weight = (Math.Cos((percent * 2 - 1) * Math.PI) + 1) / 2;

                return GetColourFromLinearGradient(weight,
                   percent < 0.5 ? left : right, centre);
            }

            public static Color GetColourFromLinearGradient(double percent, Color start, Color end)
            {
                double a, r, g, b;

                if (percent < 0 || percent > 1)
                    throw new Exception("Percent must be between 0 and 1");

                double npercent = 1.0 - percent;

                a = Math.Min(start.A, end.A) + Math.Abs(start.A - end.A) * (start.A > end.A ? npercent : percent);
                r = Math.Min(start.R, end.R) + Math.Abs(start.R - end.R) * (start.R > end.R ? npercent : percent);
                g = Math.Min(start.G, end.G) + Math.Abs(start.G - end.G) * (start.G > end.G ? npercent : percent);
                b = Math.Min(start.B, end.B) + Math.Abs(start.B - end.B) * (start.B > end.B ? npercent : percent);

                return Color.FromArgb((int)a, (int)r, (int)g, (int)b);
            }

            public static Eto.Drawing.Color GetColourFromLinearGradient(double percent, Eto.Drawing.Color start, Eto.Drawing.Color end)
            {
                double a, r, g, b;

                if (percent < 0 || percent > 1)
                    throw new Exception("Percent must be between 0 and 1");

                double npercent = 1.0 - percent;

                a = Math.Min(start.A, end.A) + Math.Abs(start.A - end.A) * (start.A > end.A ? npercent : percent);
                r = Math.Min(start.R, end.R) + Math.Abs(start.R - end.R) * (start.R > end.R ? npercent : percent);
                g = Math.Min(start.G, end.G) + Math.Abs(start.G - end.G) * (start.G > end.G ? npercent : percent);
                b = Math.Min(start.B, end.B) + Math.Abs(start.B - end.B) * (start.B > end.B ? npercent : percent);

                return Eto.Drawing.Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255), (int)(a * 255));
            }
        }
    }
}