using System;
using System.Collections.Generic;
using EddyLib;
using Rhino.Geometry;
using Xunit;

namespace RhinoPlugin.Test.Xunit.Tests
{
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
    }
}
