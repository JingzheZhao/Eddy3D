using System;
using System.Drawing;
using System.IO;
using Rhino.Geometry;

namespace EddyLib.GAN
{
    /// <summary>
    /// Processes GAN API output into Rhino geometry.
    /// Ported from Prediction.SaveAsBitmap in the decompiled CFDComponent.
    /// </summary>
    public static class GanOutputProcessor
    {
        public const double PreviewHeightMeters = 2.0;

        /// <summary>
        /// Creates a coloured result mesh from the GAN output image.
        /// Each pixel becomes a quad face with vertex colours.
        /// The mesh is previewed on a horizontal plane at pedestrian height and
        /// rotated back to the original (pre-wind-direction) orientation.
        /// </summary>
        public static Mesh CreateResultMesh(
            byte[] imageBytes,
            int width,
            int height,
            Point3d sCorner,
            double pixelSize,
            int windDirection,
            Point3d rotationCenter)
        {
            using (var ms = new MemoryStream(imageBytes))
            using (var image = Image.FromStream(ms))
            using (var outputImage = new Bitmap(image))
            {
                return CreateResultMeshFromPixels(
                    width,
                    height,
                    sCorner,
                    pixelSize,
                    windDirection,
                    rotationCenter,
                    (col, row) => outputImage.GetPixel(col, row));
            }
        }

        internal static Mesh CreateResultMeshFromPixels(
            int width,
            int height,
            Point3d sCorner,
            double pixelSize,
            int windDirection,
            Point3d rotationCenter,
            Func<int, int, Color> getPixel)
        {
            var mesh = new Mesh();
            int vertIndex = 0;
            double x0 = sCorner.X;
            double y0 = sCorner.Y;
            double z = PreviewHeightMeters;

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    Color c = getPixel(col, row);
                    Point3d[] quad = CreatePixelQuad(x0, y0, z, pixelSize, col, row);
                    Point3d p0 = quad[0];
                    Point3d p1 = quad[1];
                    Point3d p2 = quad[2];
                    Point3d p3 = quad[3];

                    mesh.Vertices.Add(p0);
                    mesh.Vertices.Add(p1);
                    mesh.Vertices.Add(p2);
                    mesh.Vertices.Add(p3);

                    mesh.VertexColors.Add(c);
                    mesh.VertexColors.Add(c);
                    mesh.VertexColors.Add(c);
                    mesh.VertexColors.Add(c);

                    mesh.Faces.AddFace(vertIndex, vertIndex + 1, vertIndex + 2, vertIndex + 3);
                    vertIndex += 4;
                }
            }

            // Rotate back to original orientation
            var inverseRotation = Transform.Rotation(
                -windDirection / 180.0 * Math.PI, Vector3d.ZAxis, rotationCenter);
            mesh.Transform(inverseRotation);

            return mesh;
        }

        internal static Point3d[] CreatePixelQuad(
            double x0,
            double y0,
            double z,
            double pixelSize,
            int col,
            int row)
        {
            var p0 = new Point3d(x0 + pixelSize * col, y0 - pixelSize * row, z);
            var p1 = new Point3d(p0.X + pixelSize, p0.Y, z);
            var p2 = new Point3d(p0.X + pixelSize, p0.Y - pixelSize, z);
            var p3 = new Point3d(p0.X, p0.Y - pixelSize, z);

            return new[] { p0, p1, p2, p3 };
        }
    }
}
