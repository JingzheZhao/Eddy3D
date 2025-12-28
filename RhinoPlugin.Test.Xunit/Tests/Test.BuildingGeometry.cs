using EddyLib;
using EddyLib.BCs;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class BuildingGeometryTests

    {
        private static Mesh LoadValidMesh(string resourcePath)
        {
            var mesh = GeometryHelpers.LoadMergedMesh(resourcePath);
            Assert.True(mesh.IsValid);
            return mesh;
        }

        private static void AssertTopology(Mesh mesh, int expectedVertices, int expectedFaces)
        {
            Assert.Equal(expectedVertices, mesh.Vertices.Count);
            Assert.Equal(expectedFaces, mesh.Faces.Count);
        }

        [NotWindowsServerTheory]
        [InlineData(@"RhinoPlugin.Test.Xunit\Resources\BuildingGeo_fine.stl", 12495, 22322)]
        [InlineData(@"RhinoPlugin.Test.Xunit\Resources\BuildingGeo.stl", 233, 172)]
        public void BuildingGeo_HasExpectedTopology(string resourcePath, int expectedVertices, int expectedFaces)
        {
            var mesh = LoadValidMesh(resourcePath);

            AssertTopology(mesh, expectedVertices, expectedFaces);
        }
    }
}
