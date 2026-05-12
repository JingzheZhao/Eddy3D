using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EddyLib
{
    /// <summary>
    /// Shared geometry utilities for mesh conversion and bounding box operations.
    /// </summary>
    public static class GeometryHelpers
    {
        /// <summary>
        /// Converts any GeometryBase to a Mesh.
        /// </summary>
        /// <param name="geometry">Input geometry (Mesh, Brep, Extrusion, or Surface).</param>
        /// <returns>Converted mesh, or null if conversion fails.</returns>
        public static Mesh ToMesh(GeometryBase geometry)
        {
            if (geometry == null) return null;

            MeshingParameters mp = new MeshingParameters();
            Mesh result = new Mesh();

            if (geometry.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
            {
                result.Append((Mesh)geometry);
            }
            else if (geometry.ObjectType == Rhino.DocObjects.ObjectType.Brep ||
                     geometry.ObjectType == Rhino.DocObjects.ObjectType.Extrusion ||
                     geometry.ObjectType == Rhino.DocObjects.ObjectType.Surface)
            {
                Brep brep = geometry as Brep;
                if (brep == null && geometry is Extrusion extrusion)
                {
                    brep = extrusion.ToBrep();
                }

                if (brep != null)
                {
                    var meshes = Mesh.CreateFromBrep(brep, mp);
                    if (meshes != null)
                    {
                        result.Append(meshes);
                    }
                }
            }

            return result.Faces.Count > 0 ? result : null;
        }

        /// <summary>
        /// Calculates dimensions from a mesh's bounding box.
        /// </summary>
        /// <param name="mesh">Input mesh.</param>
        /// <returns>Tuple of (DimX, DimY, DimZ).</returns>
        public static (double X, double Y, double Z) GetDimensions(Mesh mesh)
        {
            if (mesh == null) return (0, 0, 0);

            BoundingBox bbox = mesh.GetBoundingBox(true);
            return (
                bbox.Max.X - bbox.Min.X,
                bbox.Max.Y - bbox.Min.Y,
                bbox.Max.Z - bbox.Min.Z
            );
        }

        /// <summary>
        /// Gets dimensions as an array [X, Y, Z].
        /// </summary>
        public static double[] GetDimensionsArray(Mesh mesh)
        {
            var dims = GetDimensions(mesh);
            return new double[] { dims.X, dims.Y, dims.Z };
        }

        /// <summary>
        /// Generates a ground plane mesh based on the bounding box of the input building geometry.
        /// </summary>
        public static Mesh GenerateGroundPlane(Mesh buildingGeometry)
        {
            BoundingBox bbox = buildingGeometry.GetBoundingBox(true);
            Point3d centerBottom = new Point3d(bbox.Center.X, bbox.Center.Y, bbox.Min.Z);
            double size = Math.Max(bbox.Diagonal.Length * 5, 1000);
            Interval interval = new Interval(-size / 2, size / 2);
            Plane plane = new Plane(centerBottom, Vector3d.ZAxis);
            return Mesh.CreateFromPlane(plane, interval, interval, 2, 2);
        }

        /// <summary>
        /// Calculates the area of a polygon defined by a list of points.
        /// </summary>
        public static double CalculatePolygonArea(List<Point3d> vertices)
        {
            if (vertices == null || vertices.Count < 3) return 0;

            double totalX = 0;
            double totalY = 0;
            double totalZ = 0;

            // Cache p0 coordinates to avoid repeated property access
            var p0 = vertices[0];
            double p0x = p0.X;
            double p0y = p0.Y;
            double p0z = p0.Z;

            // Pre-calculate vector v1 for the first iteration (p1 - p0)
            var p1 = vertices[1];
            double v1x = p1.X - p0x;
            double v1y = p1.Y - p0y;
            double v1z = p1.Z - p0z;

            int count = vertices.Count;
            for (int i = 1; i < count - 1; i++)
            {
                var p2 = vertices[i + 1];

                // Vector 2: p2 - p0
                double v2x = p2.X - p0x;
                double v2y = p2.Y - p0y;
                double v2z = p2.Z - p0z;

                // Cross product: v1 x v2
                double cx = v1y * v2z - v1z * v2y;
                double cy = v1z * v2x - v1x * v2z;
                double cz = v1x * v2y - v1y * v2x;

                totalX += cx;
                totalY += cy;
                totalZ += cz;

                // Shift v2 to v1 for next iteration to reuse calculation
                v1x = v2x;
                v1y = v2y;
                v1z = v2z;
            }

            return 0.5 * Math.Sqrt(totalX * totalX + totalY * totalY + totalZ * totalZ);
        }
    }

    /// <summary>
    /// Utilities for serializing objects to dictionaries and JSON.
    /// </summary>
    public static class SerializationHelpers
    {
        /// <summary>
        /// Converts an object's public properties to a dictionary.
        /// </summary>
        public static Dictionary<string, object> ToDictionary(object obj)
        {
            if (obj == null) return new Dictionary<string, object>();

            Type t = obj.GetType();
            PropertyInfo[] props = t.GetProperties();
            var dict = new Dictionary<string, object>();

            foreach (PropertyInfo prop in props)
            {
                try
                {
                    object value = prop.GetValue(obj, null);
                    dict.Add(prop.Name, value);
                }
                catch
                {
                    // Skip properties that can't be read
                }
            }

            return dict;
        }

        /// <summary>
        /// Serializes an object's properties to JSON.
        /// </summary>
        public static string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(ToDictionary(obj));
        }
    }
}
