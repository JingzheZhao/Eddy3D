using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Helper methods for loading and manipulating geometry in tests.
    /// </summary>
    public static class GeometryHelpers
    {
        /// <summary>
        /// Loads every mesh contained in an STL, appends them into one mesh,
        /// cleans it (weld, unify normals, merge coplanar faces) and returns it.
        /// </summary>
        public static Mesh LoadMergedMesh(string solutionRelativePath,
            double weldAngleRadians = TestConstants.DefaultWeldAngleRadians,
            double coplanarTol = TestConstants.DefaultCoplanarTolerance)
        {
            var solutionRoot = GetSolutionRoot();
            var stlAbs = Path.Combine(solutionRoot, solutionRelativePath);
            if (!File.Exists(stlAbs))
                throw new FileNotFoundException($"STL not found: {stlAbs}");

            using (var doc = RhinoDoc.CreateHeadless(null))
            {
                var opts = new FileStlReadOptions()
                {
                    STLModelUnits = UnitSystem.Meters
                };
                // Try direct STL reader first, then fall back to RhinoDoc.Import
                // for environments where one path is available but the other is not.
                bool imported = FileStl.Read(stlAbs, doc, opts);
                if (!imported)
                {
                    imported = doc.Import(stlAbs, opts.ToDictionary());
                }

                if (!imported)
                {
                    throw new InvalidOperationException(
                        $"STL import failed for '{stlAbs}' (both FileStl.Read and RhinoDoc.Import returned false).");
                }

                // Duplicate every MeshObject (so they survive after Dispose)
                var pieces = new List<Mesh>();
                foreach (MeshObject mo in doc.Objects.GetObjectList(ObjectType.Mesh))
                    pieces.Add(((Mesh)mo.Geometry).DuplicateMesh());

                if (pieces.Count == 0)
                    throw new InvalidOperationException("No mesh objects in STL.");

                // Append all pieces into one mesh
                var merged = new Mesh();
                foreach (var part in pieces)
                    merged.Append(part);

                // Clean up
                merged.Vertices.CombineIdentical(true, true);
                merged.Weld(weldAngleRadians);
                merged.UnifyNormals();
                merged.Normals.ComputeNormals();
                merged.Compact();
                merged.MergeAllCoplanarFaces(coplanarTol);

                return merged;
            }
        }

        /// <summary>
        /// Finds the solution root directory by navigating up from the bin directory.
        /// </summary>
        private static string GetSolutionRoot()
        {
            var baseDir = AppContext.BaseDirectory;

            // Keep going up until we find the solution root
            var current = new DirectoryInfo(baseDir);
            while (current != null && current.Name != TestConstants.SolutionFolderName)
            {
                current = current.Parent;
            }

            if (current == null)
            {
                // Fallback to relative path from bin directory
#if DEBUG
                return Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\"));
#else
                return Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\"));
#endif
            }

            return current.FullName;
        }

        /// <summary>
        /// Converts a Rectangle3d to a simple quad mesh.
        /// </summary>
        public static Mesh RectangleToMesh(Rectangle3d rect)
        {
            Mesh mesh = new Mesh();
            mesh.Vertices.Add(rect.Corner(0));
            mesh.Vertices.Add(rect.Corner(1));
            mesh.Vertices.Add(rect.Corner(2));
            mesh.Vertices.Add(rect.Corner(3));
            mesh.Faces.AddFace(0, 1, 2, 3);
            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
        }
    }
}
