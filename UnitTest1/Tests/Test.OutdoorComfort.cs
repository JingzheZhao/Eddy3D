using System;

using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Types;

using Xunit;
using EddyLib;
using System.Collections.Generic;

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
        public void ScaleABL_1m_Returns_2_89()
        {
            // Arrange

            var z0 = 1;
            var uref = 10;

            var bcond = new BoundaryConditions(BoundaryType.abl, new List<int> { 0 }, uref, z0, "");

            // Act
            var res = EddyLib.WindFactors.ScaleABL(10, bcond, 1);

            // Assert

            Assert.Equal(2.89, Math.Round(res, 2));
        }

        [Fact]
        public void CalcWindReductionArray_Direction330_ReturnDistance15Twice()
        {
            //// Arrange
            //var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
            //string epw = @"C:\DIVA\WeatherData\USA_IL_Chicago - OHare.Intl.AP.725300_TMY3.epw";
            //BoundaryConditions bcond = new BoundaryConditions(BoundaryType.abl, windDirList, 5, 1, epw);
            //Weather weather = new Weather(epw);

            //var vecs = new Vector3d[10, 8];
            //PedestrianComfort av = new PedestrianComfort(bcond.windDirs.ToArray(), vecs, @"C:\test\AnnualVelocityProbes.csv", true, true);

            //EddyLib.WindFactors target = new EddyLib.WindFactors(@"C:\test\", bcond, weather, av, 2, false, true);
            //PrivateObject obj = new PrivateObject(target);

            //// Act
            //var retVal = obj.Invoke("CalcWindReductionArray");

            //// Assert

            //Assert.Equal(expectedVal, retVal);
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