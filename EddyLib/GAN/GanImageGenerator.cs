using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace EddyLib.GAN
{
    /// <summary>
    /// Data produced by the GAN input generation pipeline.
    /// Contains the normalised float array ready for the API, plus spatial
    /// metadata needed to reconstruct the result mesh.
    /// </summary>
    public class GanInputData
    {
        /// <summary>
        /// Flat float array of length 3*512*512 in channel-first order (R, G, B),
        /// normalised to [-1, 1]. Ready to send to the /predict_array endpoint.
        /// </summary>
        public float[] InputArray { get; set; }

        /// <summary>Centre of the analysis rectangle (rotation pivot).</summary>
        public Point3d Center { get; set; }

        /// <summary>Top-left corner used for pixel → world coordinate mapping.</summary>
        public Point3d SCorner { get; set; }

        /// <summary>World-space size of one pixel.</summary>
        public double PixelSize { get; set; }

        /// <summary>Wind direction in degrees (used for inverse rotation).</summary>
        public int WindDirection { get; set; }
    }

    /// <summary>
    /// Generates the normalised float array that the GAN model expects,
    /// directly from building geometry — no intermediate Bitmap.
    ///
    /// Ported from the decompiled CFDComponent.
    /// </summary>
    public static class GanImageGenerator
    {
        public const int ImageSize = 512;
        private const int PixelCount = ImageSize * ImageSize;

        /// <summary>
        /// Full pipeline: geometry + analysis plane → normalised float array.
        /// </summary>
        public static GanInputData GenerateInput(
            Mesh building,
            Rectangle3d analysisPlane,
            int windDirection,
            double vSize = 3.0,
            double colorSize = 100.0)
        {
            // Clone so we don't mutate the caller's mesh
            var bldg = building.DuplicateMesh();

            // --- Step 1: Rotate geometry by wind direction ---
            var center = analysisPlane.Center;
            var rotation = Transform.Rotation(
                windDirection / 180.0 * Math.PI, Vector3d.ZAxis, center);
            if (!bldg.Transform(rotation))
                throw new InvalidOperationException("Failed to rotate building mesh.");

            // --- Step 2: Analysis plane bounds ---
            var p0 = analysisPlane.PointAt(0.0);
            var p1 = analysisPlane.PointAt(1.0);
            var p2 = analysisPlane.PointAt(2.0);
            var p3 = analysisPlane.PointAt(3.0);

            var flatBrep = Brep.CreateFromCornerPoints(
                new Point3d(p0.X, p0.Y, 0),
                new Point3d(p1.X, p1.Y, 0),
                new Point3d(p2.X, p2.Y, 0),
                new Point3d(p3.X, p3.Y, 0),
                0.1);

            double squareLength = p0.DistanceTo(p1);

            // Extrude for bounding box
            var extrusionCurve = analysisPlane.ToNurbsCurve();
            var extrusionSurface = Surface.CreateExtrusion(extrusionCurve, new Vector3d(0, 0, 100.0));
            var extrusionBrep = Brep.CreateFromSurface(extrusionSurface);
            var boundingBox = extrusionBrep.GetBoundingBox(true);

            var bbMin = boundingBox.Min;
            var bbMax = boundingBox.Max;
            var sCorner = new Point3d(bbMin.X, bbMax.Y, 0);
            var box = new Box(boundingBox);

            // --- Step 3: Create point grid and ray-cast ---
            double pixelSize = squareLength / (ImageSize - 1.0);
            var flatMeshes = Mesh.CreateFromBrep(flatBrep);
            var flatMesh = flatMeshes[0];

            var remeshParams = new QuadRemeshParameters
            {
                TargetEdgeLength = squareLength / 511.0
            };
            var baseMesh = flatMesh.QuadRemesh(remeshParams);

            var insideOutside = new Dictionary<int, bool>();
            var insidePoints = new List<Point3d>();

            for (int i = 0; i < baseMesh.Vertices.Count; i++)
            {
                Point3d pt = (Point3d)baseMesh.Vertices[i];
                var ray = new Ray3d(
                    new Point3d(pt.X, pt.Y, 999.0),
                    new Vector3d(0, 0, -1));

                if (Intersection.MeshRay(bldg, ray) < 0.0)
                {
                    insidePoints.Add(pt);
                    insideOutside[i] = true;
                }
                else
                {
                    insideOutside[i] = false;
                }
            }

            // --- Step 4: Construct ground mesh for coloring ---
            var groundMesh = ConstructMesh(insidePoints, pixelSize);

            // --- Step 5: Compute coloring data (distance + height) ---
            ComputeColoringData(baseMesh, groundMesh, bldg,
                out var colorDist, out var heightValues);

            // --- Step 6: Generate normalised float array directly ---
            var allPoints = baseMesh.Vertices.ToPoint3dArray();
            float[] inputArray = GenerateArray(
                allPoints, pixelSize, colorDist, heightValues,
                sCorner, insideOutside, colorSize, box, vSize);

            return new GanInputData
            {
                InputArray = inputArray,
                Center = center,
                SCorner = sCorner,
                PixelSize = pixelSize,
                WindDirection = windDirection,
            };
        }

        /// <summary>
        /// Generate the normalised float array (channel-first, [-1, 1]).
        /// This replaces the bitmap generation + BitmapToFloat conversion.
        /// </summary>
        private static float[] GenerateArray(
            Point3d[] allPoints,
            double pointGap,
            List<double> colorDist,
            List<double> heightValues,
            Point3d start,
            Dictionary<int, bool> insideOutside,
            double upper,
            Box box,
            double vSize)
        {
            // First pass: generate RGB pixel colours (same as the old coloring() method)
            var pixelColors = new Color[ImageSize * ImageSize];
            // Default to black
            for (int i = 0; i < pixelColors.Length; i++)
                pixelColors[i] = Color.Black;

            double x0 = start.X;
            double y0 = start.Y;

            for (int i = 0; i < allPoints.Length; i++)
            {
                int col = Convert.ToInt32((allPoints[i].X - x0) / pointGap);
                int row = Convert.ToInt32((y0 - allPoints[i].Y) / pointGap);

                if (col < 0 || col >= ImageSize || row < 0 || row >= ImageSize)
                    continue;

                Color c;
                if (insideOutside[i])
                {
                    // Outside buildings: Inferno colormap based on distance
                    double pct = ColorUtils.Clamp01(colorDist[i] / upper);
                    c = InfernoColormap.GetColor(pct);
                }
                else
                {
                    // Inside buildings: grayscale based on height
                    c = ColorUtils.GreyScale(heightValues[i], box, vSize);
                }

                pixelColors[row * ImageSize + col] = c;
            }

            // Second pass: convert to normalised float array in channel-first format
            // Layout: [R_0..R_N, G_0..G_N, B_0..B_N] where N = 512*512
            float[] result = new float[3 * PixelCount];
            for (int i = 0; i < PixelCount; i++)
            {
                Color c = pixelColors[i];
                result[i] = (c.R - 127.5f) / 127.5f;                    // R channel
                result[i + PixelCount] = (c.G - 127.5f) / 127.5f;      // G channel
                result[i + 2 * PixelCount] = (c.B - 127.5f) / 127.5f;  // B channel
            }

            return result;
        }

        private static Mesh ConstructMesh(List<Point3d> points, double gap, double probingHeight = 2.0)
        {
            var mesh = new Mesh();
            double halfGap = gap / 2.0 + 0.05;
            int vertIdx = 0;

            foreach (var pt in points)
            {
                mesh.Vertices.Add(new Point3d(pt.X + halfGap, pt.Y + halfGap, probingHeight));
                mesh.Vertices.Add(new Point3d(pt.X - halfGap, pt.Y + halfGap, probingHeight));
                mesh.Vertices.Add(new Point3d(pt.X - halfGap, pt.Y - halfGap, probingHeight));
                mesh.Vertices.Add(new Point3d(pt.X + halfGap, pt.Y - halfGap, probingHeight));
                mesh.Faces.AddFace(vertIdx, vertIdx + 1, vertIdx + 2, vertIdx + 3);
                vertIdx += 4;
            }

            return mesh;
        }

        private static void ComputeColoringData(
            Mesh intersectMesh,
            Mesh groundMesh,
            Mesh building,
            out List<double> colorDist,
            out List<double> heightValues)
        {
            // Combined mesh for height ray-casting
            var combined = new Mesh();
            combined.Append(new[] { building, groundMesh });
            var mf = combined.Faces;
            var mv = combined.Vertices;

            int count = intersectMesh.Vertices.Count;
            var tempColor = new List<double>(new double[count]);
            var tempBw = new List<double>(new double[count]);

            Parallel.For(0, count, i =>
            {
                Point3d pt = (Point3d)intersectMesh.Vertices[i];

                // Distance to nearest building surface
                var closestPt = building.ClosestMeshPoint(pt, 9999.0);
                tempColor[i] = closestPt.Point.DistanceTo(pt);

                // Height under point via ray-cast
                var ray = new Ray3d(
                    new Point3d(pt.X, pt.Y, 9999.0),
                    new Vector3d(0, 0, -1));

                int[] faceIndices = null;
                double t = Intersection.MeshRay(combined, ray, out faceIndices);

                if (!double.IsNaN(t) && faceIndices != null && faceIndices.Length > 0)
                {
                    var face = mf[faceIndices[0]];
                    float z = mv[face.A].Z + mv[face.B].Z + mv[face.C].Z + mv[face.D].Z;
                    tempBw[i] = z / 4.0;
                }
            });

            colorDist = tempColor;
            heightValues = tempBw;
        }
    }
}
