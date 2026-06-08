using System;
using System.Collections.Generic;
using EddyLib;
using Rhino.Geometry;
using Xunit;

namespace RhinoPlugin.Test.Xunit.Tests
{
    [Collection("Rhino Collection")]
    public class GeometryHelpersTests
    {
        [Fact]
        public void CalculatePolygonArea_Correctness()
        {
            // Square 10x10
            var square = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(0, 10, 0)
            };

            double area = EddyLib.GeometryHelpers.CalculatePolygonArea(square);
            Assert.Equal(100.0, area, 1e-6);

            // Triangle base 10, height 10
            var triangle = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(5, 10, 0)
            };

            area = EddyLib.GeometryHelpers.CalculatePolygonArea(triangle);
            Assert.Equal(50.0, area, 1e-6);

            // Concave "V" shape
            // (0,0) -> (10,0) -> (10,10) -> (5,5) -> (0,10)
            // Area = Square(10x10) - Triangle(base=10, height=5)
            //      = 100 - (0.5 * 10 * 5) = 100 - 25 = 75
            var concave = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(5, 5, 0),
                new Point3d(0, 10, 0)
            };

            area = EddyLib.GeometryHelpers.CalculatePolygonArea(concave);
            Assert.Equal(75.0, area, 1e-6);
        }

        [Fact]
        public void CalculatePolygonArea_RunsWithoutNativeDependencies()
        {
            // This test confirms that the method does not rely on native Rhino libraries (rhcommon_c),
            // which are often missing in CI/Docker environments.
            var polygon = new List<Point3d>
            {
                new Point3d(0, 0, 0),
                new Point3d(10, 0, 0),
                new Point3d(10, 10, 0),
                new Point3d(5, 15, 0),
                new Point3d(0, 10, 0)
            };

            // Should not throw DllNotFoundException
            double area = EddyLib.GeometryHelpers.CalculatePolygonArea(polygon);
            Assert.True(area > 0);
        }

        [Fact]
        public void CalculatePolygonArea_HandlesSmallPolygons()
        {
            // Empty
            Assert.Equal(0, EddyLib.GeometryHelpers.CalculatePolygonArea(new List<Point3d>()), 1e-6);

            // 1 point
            Assert.Equal(0, EddyLib.GeometryHelpers.CalculatePolygonArea(new List<Point3d> { new Point3d(0, 0, 0) }), 1e-6);

            // 2 points
            Assert.Equal(0, EddyLib.GeometryHelpers.CalculatePolygonArea(new List<Point3d> { new Point3d(0, 0, 0), new Point3d(10, 0, 0) }), 1e-6);
        }

        [Fact]
        public void GetDimensionsArray_NullMesh_ReturnsZeroDimensions()
        {
            double[] dims = EddyLib.GeometryHelpers.GetDimensionsArray(null);

            Assert.NotNull(dims);
            Assert.Equal(3, dims.Length);
            Assert.Equal(0.0, dims[0], 1e-6);
            Assert.Equal(0.0, dims[1], 1e-6);
            Assert.Equal(0.0, dims[2], 1e-6);
        }

        [RhinoRequiredFact]
        public void GetDimensionsArray_ValidMesh_ReturnsCorrectDimensions()
        {
            // Cube 10x10x10
            Mesh mesh = new Mesh();
            mesh.Vertices.Add(0, 0, 0);
            mesh.Vertices.Add(10, 0, 0);
            mesh.Vertices.Add(10, 10, 0);
            mesh.Vertices.Add(0, 10, 0);
            mesh.Vertices.Add(0, 0, 10);
            mesh.Vertices.Add(10, 0, 10);
            mesh.Vertices.Add(10, 10, 10);
            mesh.Vertices.Add(0, 10, 10);

            // Just need bounding box, no need for faces, but we add them to make it valid
            mesh.Faces.AddFace(0, 1, 5, 4);
            mesh.Faces.AddFace(1, 2, 6, 5);
            mesh.Faces.AddFace(2, 3, 7, 6);
            mesh.Faces.AddFace(3, 0, 4, 7);
            mesh.Faces.AddFace(4, 5, 6, 7);
            mesh.Faces.AddFace(0, 3, 2, 1);

            double[] dims = EddyLib.GeometryHelpers.GetDimensionsArray(mesh);

            Assert.NotNull(dims);
            Assert.Equal(3, dims.Length);
            Assert.Equal(10.0, dims[0], 1e-6);
            Assert.Equal(10.0, dims[1], 1e-6);
            Assert.Equal(10.0, dims[2], 1e-6);
        }

        [RhinoRequiredFact]
        public void GetDimensionsArray_FlatMesh_ReturnsZeroThickness()
        {
            // Flat square 10x10 in XY plane
            Mesh mesh = new Mesh();
            mesh.Vertices.Add(0, 0, 0);
            mesh.Vertices.Add(10, 0, 0);
            mesh.Vertices.Add(10, 10, 0);
            mesh.Vertices.Add(0, 10, 0);
            mesh.Faces.AddFace(0, 1, 2, 3);

            double[] dims = EddyLib.GeometryHelpers.GetDimensionsArray(mesh);

            Assert.NotNull(dims);
            Assert.Equal(3, dims.Length);
            Assert.Equal(10.0, dims[0], 1e-6);
            Assert.Equal(10.0, dims[1], 1e-6);
            Assert.Equal(0.0, dims[2], 1e-6);
        }
    }
}
