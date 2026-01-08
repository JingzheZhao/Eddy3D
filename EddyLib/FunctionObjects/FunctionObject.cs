using Rhino.Geometry;
using System.Collections.Generic;

namespace EddyLib.FunctionObjects
{
    /// <summary>
    /// Base class for OpenFOAM function objects that can be exported to simulation files.
    /// </summary>
    public class FunctionObject
    {
        /// <summary>Full dictionary export string for OpenFOAM.</summary>
        public string FullExportString { get; set; }

        /// <summary>Unique identifier for this function object.</summary>
        public string ID { get; set; }

        /// <summary>Display name of the function object.</summary>
        public string Name { get; set; }

        /// <summary>Mesh geometry associated with this function object.</summary>
        public Mesh Geometry { get; set; }

        /// <summary>
        /// Selection mode for the function object.
        /// </summary>
        public enum selectionMode
        {
            /// <summary>Apply to entire domain.</summary>
            all,
            /// <summary>Apply to specific cell zone.</summary>
            cellZone
        }

        /// <summary>
        /// Exports all function object geometries as STL files.
        /// </summary>
        /// <param name="functionObjects">List of function objects to export.</param>
        /// <param name="meshStlDir">Directory for STL files.</param>
        public static void ExportGeometries(List<FunctionObject> functionObjects, string meshStlDir)
        {
            for (int i = 0; i < functionObjects.Count; i++)
            {
                var fo = functionObjects[i];
                if (fo.Geometry != null)
                {
                    string filename = !string.IsNullOrEmpty(fo.ID) ? fo.ID : $"FunctionObject_{i}";
                    STLExport.ExportBinary(System.IO.Path.Combine(meshStlDir, filename + ".stl"), fo.Geometry);
                }
            }
        }

        /// <summary>
        /// Exports a single function object geometry as STL.
        /// </summary>
        public void ExportGeometry(string meshStlDir)
        {
            if (Geometry != null && !string.IsNullOrEmpty(ID))
            {
                STLExport.ExportBinary(System.IO.Path.Combine(meshStlDir, ID + ".stl"), Geometry);
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility.
        /// </summary>
        public void ExportGeometry(List<FunctionObject> FO, string meshStlDir)
        {
            ExportGeometries(FO, meshStlDir);
        }
    }
}