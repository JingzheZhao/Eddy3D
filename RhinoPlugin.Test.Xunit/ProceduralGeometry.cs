using Rhino.Geometry;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Helper methods for creating procedural building geometry in tests.
    /// </summary>
    public static class ProceduralGeometry
    {
        /// <summary>
        /// Creates a multi-building mesh with L-shaped, box, and U-shaped buildings.
        /// </summary>
        public static Mesh CreateProceduralBuilding(double width, double depth, double height)
        {
            var mesh = new Mesh();

            // Building 1: L-shaped building (main building at center)
            var lShaped = CreateLShapedBuilding(0, 0, 0, width * 3, depth * 3, height * 1);
            mesh.Append(lShaped);

            // Building 2: Simple box building - COMPACT RIGHT-FRONT
            var boxBuilding = CreateBox(width * 3 + 5, depth * 0.5, 0, width * 0.7 * 3, depth * 0.7 * 3, height * 0.8 * 1);
            mesh.Append(boxBuilding);

            // Building 3: U-shaped building - COMPACT LEFT-BACK
            var uShaped = CreateUShapedBuilding(-width * 3 - 5, -depth * 0.5, 0, width * 0.8 * 3, depth * 0.8 * 3, height * 0.9 * 1);
            mesh.Append(uShaped);

            mesh.Normals.ComputeNormals();
            mesh.Compact();

            return mesh;
        }

        /// <summary>
        /// Creates a simple rectangular box mesh.
        /// </summary>
        public static Mesh CreateBox(double x, double y, double z, double width, double depth, double height)
        {
            var mesh = new Mesh();

            var corners = new Point3d[]
            {
                new Point3d(x, y, z),
                new Point3d(x + width, y, z),
                new Point3d(x + width, y + depth, z),
                new Point3d(x, y + depth, z),
                new Point3d(x, y, z + height),
                new Point3d(x + width, y, z + height),
                new Point3d(x + width, y + depth, z + height),
                new Point3d(x, y + depth, z + height)
            };

            for (int i = 0; i < 8; i++)
            {
                mesh.Vertices.Add(corners[i]);
            }

            mesh.Faces.AddFace(0, 1, 2, 3); // Bottom
            mesh.Faces.AddFace(4, 7, 6, 5); // Top
            mesh.Faces.AddFace(0, 4, 5, 1); // Front
            mesh.Faces.AddFace(2, 6, 7, 3); // Back
            mesh.Faces.AddFace(0, 3, 7, 4); // Left
            mesh.Faces.AddFace(1, 5, 6, 2); // Right

            return mesh;
        }

        /// <summary>
        /// Creates an L-shaped building mesh.
        /// </summary>
        public static Mesh CreateLShapedBuilding(double x, double y, double z, double width, double depth, double height)
        {
            var mesh = new Mesh();
            var mainPart = CreateBox(x, y, z, width * 0.6, depth, height);
            mesh.Append(mainPart);
            var horizontalPart = CreateBox(x + width * 0.4, y + depth * 0.4, z, width * 0.6, depth * 0.6, height);
            mesh.Append(horizontalPart);
            return mesh;
        }

        /// <summary>
        /// Creates a U-shaped building mesh.
        /// </summary>
        public static Mesh CreateUShapedBuilding(double x, double y, double z, double width, double depth, double height)
        {
            var mesh = new Mesh();
            var leftLeg = CreateBox(x, y, z, width * 0.3, depth, height);
            mesh.Append(leftLeg);
            var rightLeg = CreateBox(x + width * 0.7, y, z, width * 0.3, depth, height);
            mesh.Append(rightLeg);
            var backPart = CreateBox(x, y + depth * 0.7, z, width, depth * 0.3, height);
            mesh.Append(backPart);
            return mesh;
        }
    }
}