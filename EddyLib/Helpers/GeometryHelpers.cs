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
                    foreach (Mesh m in meshes)
                    {
                        result.Append(m);
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
        /// Calculates the area of a polygon defined by a list of points.
        /// </summary>
        public static double CalculatePolygonArea(List<Point3d> vertices)
        {
            if (vertices == null || vertices.Count < 3) return 0;

            // Use Rhino's AreaMassProperties for accuracy with planar polygons
            var polyline = new Polyline(vertices);
            if (!polyline.IsClosed) polyline.Add(polyline[0]);
            
            var amp = AreaMassProperties.Compute(polyline.ToNurbsCurve());
            return amp != null ? amp.Area : 0;
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
