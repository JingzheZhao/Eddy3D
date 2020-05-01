using System;

using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Types;

using Xunit;
using EddyLib;
using System.Collections.Generic;
using System.Linq;
using Rhino.Display;
using System.IO;

namespace RhinoPlugin.Tests.Xunit
{
    [Collection("Rhino Collection")]
    public class OutdoorComfort
    {
        ///// <summary>
        ///// Xunit Test to Transform a brep using a translation
        ///// </summary>
        //[Fact]
        //public void Brep_Translation()
        //{
        //    // Arrange
        //    var bb = new BoundingBox(new Point3d(0, 0, 0), new Point3d(100, 100, 100));
        //    var brep = bb.ToBrep();
        //    var t = Transform.Translation(new Vector3d(30, 40, 50));

        //    // Act
        //    brep.Transform(t);

        //    // Assert
        //    Assert.Equal(brep.GetBoundingBox(true).Center, new Point3d(80, 90, 100));
        //}

        ///// <summary>
        ///// Xunit Test to Intersect sphere with a plane to generate a circle
        ///// </summary>
        //[Fact]
        //public void Brep_Intersection()
        //{
        //    // Arrange
        //    var radius = 4.0;
        //    var brep = Brep.CreateFromSphere(new Sphere(new Point3d(), radius));
        //    var cuttingPlane = Plane.WorldXY;

        //    // Act
        //    Rhino.Geometry.Intersect.Intersection.BrepPlane(brep, cuttingPlane, 0.001, out var curves, out var points);

        //    // Assert
        //    Assert.Single(curves);
        //    Assert.Equal(2 * Math.PI * radius, curves[0].GetLength());
        //}

        ///// <summary>
        ///// Xunit Test to ensure Centroid of GH_Box outputs a GH_Point
        ///// </summary>
        //[Fact]
        //public void GHBox_Centroid_ReturnsGHPoint()
        //{
        //    // Arrange
        //    var myBox = new GH_Box(new Box());

        //    // Act
        //    var result = myBox.Boundingbox.Center;

        //    // Assert
        //    Assert.IsType<Point3d>(result);
        //}

        [Fact]
        public void ScaleABL_3m_Returns_2_89()
        {
            // Arrange

            var z0 = 1;
            var uref = 5;

            var bcond = new BoundaryConditions(BoundaryType.abl, new List<int> { 0 }, uref, z0, "");

            // Act
            var res = EddyLib.WindFactors.ScaleABL(uref, bcond, 3);

            // Assert

            Assert.Equal(2.89, Math.Round(res, 2));
        }

        [Fact]
        public void WindFactors()
        {
            //// Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
            string epw = @"C:\ladybug\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997.epw";
            BoundaryConditions bcond = new BoundaryConditions(BoundaryType.abl, windDirList, 10, 1, epw);

            Weather weather = new Weather(epw);
            weather.WindSpeed = Enumerable.Repeat(5.0, 8760).ToArray();
            weather.WindDirection = Enumerable.Repeat(0, 8760).ToArray();

            Vector3d[,] vecs = new Vector3d[100, 8];
            for (int j = 0; j < 100; j++)
            {
                for (int i = 0; i < windDirList.Count; i++)
                {
                    vecs[j, i] = new Vector3d(0, 2, 0);
                }
            }
                ;

            var workingdir = @"C:\Testing\";
            if (!Directory.Exists(workingdir)) { Directory.CreateDirectory(workingdir); };
            var points = Enumerable.Repeat(new Point3d(0, 0, 2), 100);
            var mdv = new MultiDirectionalVelocities(workingdir, windDirList.ToArray(), vecs, true, true);

            //// Act

            var wfs = new WindFactorsSpatial(workingdir, bcond, mdv, points.ToList(), false, true);

            //// Assert
            /// We would expect a Uref of 4.58 m/s at 2 m height --> 44 % for 2 m/s

            Assert.Equal(0.44, Math.Round(wfs.ValuesSpatial[0, 0], 2));

            var wft = new WindFactorsTemporal(workingdir, bcond, weather, wfs, points.ToList(), false, true);

            // Here we would expect 44 % of 2.29 m/s which is the ABl velocity at 5 m height.

            Assert.Equal(1.01, Math.Round(wft.ValuesTemporal[0, 0], 2));
        }

        [Fact]
        public void DistanceBetween_0_355_Return5()
        {
            // Arrange
            var dir1 = 0;
            var dir2 = 355;

            // Act
            var res = EddyLib.WindFactors.DistanceBetweenWindDirs(dir1, dir2);

            // Assert
            Assert.Equal(5, res);
        }

        [Fact]
        public void DistanceBetween_45_90_Return45()
        {
            // Arrange
            var dir1 = 45;
            var dir2 = 90;

            // Act
            var res = EddyLib.WindFactors.DistanceBetweenWindDirs(dir1, dir2);

            // Assert
            Assert.Equal(45, res);
        }

        [Fact]
        public void ReturnNextLowerIndex_0_Return315()
        {
            // Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };

            // Act
            var res = EddyLib.WindFactors.ReturnNextLowerIndex(windDirList, 0);

            // Assert

            Assert.Equal(7, res);
        }
    }
}