using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rhino;

namespace UnitTest
{
    [DeploymentItem("RhinoCommon.dll")]
    [TestClass]
    public class WindFactors
    {
        [TestMethod]
        public void CalcWindReductionArray_Direction330_ReturnDistance15Twice()
        {
            //// Arrange
            //var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
            //string epw = @"C:\DIVA\WeatherData\USA_IL_Chicago - OHare.Intl.AP.725300_TMY3.epw";
            //BoundaryConditions bcond = new BoundaryConditions(BoundaryType.abl, windDirList, 5, 1, epw);
            //Weather weather = new Weather(epw);

            //var vecs = new Vector3d[10, 8];
            //AnnualVelocities av = new AnnualVelocities(bcond.windDirs.ToArray(), vecs, @"C:\test\AnnualVelocityProbes.csv", true, true);

            //EddyLib.WindFactors target = new EddyLib.WindFactors(@"C:\test\", bcond, weather, av, 2, false, true);
            //PrivateObject obj = new PrivateObject(target);

            //// Act
            //var retVal = obj.Invoke("CalcWindReductionArray");

            //// Assert

            //Assert.AreEqual(expectedVal, retVal);
        }

        [TestMethod]
        public void DistanceBetween_0_355_Return5()
        {
            // Arrange
            var dir1 = 0;
            var dir2 = 355;

            // Act
            var res = EddyLib.WindFactors.DistanceBetweenWindDirs(dir1, dir2);

            // Assert
            Assert.AreEqual(5, res);
        }

        [TestMethod]
        public void DistanceBetween_45_90_Return45()
        {
            // Arrange
            var dir1 = 45;
            var dir2 = 90;

            // Act
            var res = EddyLib.WindFactors.DistanceBetweenWindDirs(dir1, dir2);

            // Assert
            Assert.AreEqual(45, res);
        }

        [TestMethod]
        public void ReturnNextLowerIndex_0_Return315()
        {
            // Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };

            // Act
            var res = EddyLib.WindFactors.ReturnNextLowerIndex(windDirList, 0);

            // Assert

            Assert.AreEqual(7, res);
        }
    }
}