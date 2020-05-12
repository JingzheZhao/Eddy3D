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
using EddyLib.BCs;
using EddyLib.Radiance;
using static EddyLib.MRT;

namespace RhinoPlugin.Tests.Xunit
{
    [Collection("Rhino Collection")]
    public class OutdoorComfort
    {
        [Fact]
        public void MRT_50Sky_50Buildings_Returns_20()
        {
            // Arrange
            var workingdir = @"C:\Testing\";

            var point0 = new Rhino.Geometry.Point3d(0, 0, 0);
            var point1 = new Rhino.Geometry.Point3d(20, 0, 0);
            var point2 = new Rhino.Geometry.Point3d(0, 20, 0);
            var point3 = new Rhino.Geometry.Point3d(20, 20, 0);
            var point4 = new Rhino.Geometry.Point3d(0, 0, 40);
            var point5 = new Rhino.Geometry.Point3d(20, 0, 40);
            var point6 = new Rhino.Geometry.Point3d(0, 20, 40);
            var point7 = new Rhino.Geometry.Point3d(20, 20, 40);

            var surface = NurbsSurface.CreateFromCorners(
              new Point3d(5000, 0, 0),
              new Point3d(5000, 5000, 0),
              new Point3d(0, 5000, 0),
              new Point3d(0, 0, 0));

            surface.Translate(new Vector3d(-2500, -2050, 0));

            Rhino.Geometry.Box box1 = new Rhino.Geometry.Box(Rhino.Geometry.Plane.WorldXY, new List<Rhino.Geometry.Point3d>() { point0, point1, point2, point3, point4, point5, point6, point7 });
            Rhino.Geometry.MeshingParameters mp = new Rhino.Geometry.MeshingParameters();
            var m = Mesh.CreateFromBrep(box1.ToBrep(), mp);
            var s = Mesh.CreateFromBrep(surface.ToBrep(), mp);

            Rhino.Geometry.Mesh mm = new Rhino.Geometry.Mesh();

            foreach (Rhino.Geometry.Mesh im in m)
            {
                mm.Append(im);
            }
            mm.Append(s);

            string epw = @"C:\ladybug\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997.epw";

            var z0 = 1;
            var uref = 10;
            var zref = 10;

            var windDirList = new List<int>() { 0 };

            var bcond = new ABL(windDirList, uref, zref, z0, 0, epw);

            Weather weather = new Weather(epw);

            weather.WindSpeed = Enumerable.Repeat(5.0, 8760).ToArray();
            weather.WindDirection = Enumerable.Repeat(0, 8760).ToArray();
            weather.DryBulbTemp = Enumerable.Repeat(30.0, 8760).ToArray();
            weather.RelativeHumidity = Enumerable.Repeat(50.0, 8760).ToArray();
            weather.DiffuseHorizontalRadiation = Enumerable.Repeat(0.0, 8760).ToArray();
            weather.DirectNormalRadiation = Enumerable.Repeat(0.0, 8760).ToArray();

            Vector3d[,] vecs = new Vector3d[100, 8];
            for (int j = 0; j < 100; j++)
            {
                for (int i = 0; i < windDirList.Count; i++)
                {
                    vecs[j, i] = new Vector3d(0, 2, 0);
                }
            }
                ;

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcond, 5, 50, 300, 80);

            var points = Enumerable.Repeat(new Point3d(60, 60, 2), 100);

            // Act

            var vf = new SkyViewFactor(workingdir, DOMCYL.BuildingGeometry, points.ToArray(), true);

            // Set the view factors to 50 % sky and 50 % buildings
            vf.Values = Enumerable.Repeat(0.5, 100).ToArray();

            var sky = new Sky(weather.DewPointTemp, weather.DryBulbTemp, weather.SkyCover, weather.RelativeHumidity);

            // Set the Skytemp to 10°C
            sky.Temp = Enumerable.Repeat(10.0, 8760).ToArray();

            var mrt = new MRT(workingdir, DOMCYL.BuildingGeometry, sky, vf, weather, EddyLib.MRT.MRTType.RadianceTwoPhaseDDS, points.ToArray(), true);

            // Assert

            Assert.Equal(20, Math.Round(mrt.Values[0, 0], 2));
        }

        [Fact]
        public void MRT_60Sky_40Buildings_Returns_18()
        {
            // Arrange
            var workingdir = @"C:\Testing\";

            var point0 = new Rhino.Geometry.Point3d(0, 0, 0);
            var point1 = new Rhino.Geometry.Point3d(20, 0, 0);
            var point2 = new Rhino.Geometry.Point3d(0, 20, 0);
            var point3 = new Rhino.Geometry.Point3d(20, 20, 0);
            var point4 = new Rhino.Geometry.Point3d(0, 0, 40);
            var point5 = new Rhino.Geometry.Point3d(20, 0, 40);
            var point6 = new Rhino.Geometry.Point3d(0, 20, 40);
            var point7 = new Rhino.Geometry.Point3d(20, 20, 40);

            var surface = NurbsSurface.CreateFromCorners(
              new Point3d(5000, 0, 0),
              new Point3d(5000, 5000, 0),
              new Point3d(0, 5000, 0),
              new Point3d(0, 0, 0));

            surface.Translate(new Vector3d(-2500, -2050, 0));

            Rhino.Geometry.Box box1 = new Rhino.Geometry.Box(Rhino.Geometry.Plane.WorldXY, new List<Rhino.Geometry.Point3d>() { point0, point1, point2, point3, point4, point5, point6, point7 });
            Rhino.Geometry.MeshingParameters mp = new Rhino.Geometry.MeshingParameters();
            var m = Mesh.CreateFromBrep(box1.ToBrep(), mp);
            var s = Mesh.CreateFromBrep(surface.ToBrep(), mp);

            Rhino.Geometry.Mesh mm = new Rhino.Geometry.Mesh();

            foreach (Rhino.Geometry.Mesh im in m)
            {
                mm.Append(im);
            }
            mm.Append(s);

            string epw = @"C:\ladybug\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997.epw";

            var z0 = 1;
            var uref = 10;
            var zref = 10;

            var windDirList = new List<int>() { 0 };

            var bcond = new ABL(windDirList, uref, zref, z0, 0, epw);

            Weather weather = new Weather(epw);

            weather.WindSpeed = Enumerable.Repeat(5.0, 8760).ToArray();
            weather.WindDirection = Enumerable.Repeat(0, 8760).ToArray();
            weather.DryBulbTemp = Enumerable.Repeat(30.0, 8760).ToArray();
            weather.RelativeHumidity = Enumerable.Repeat(50.0, 8760).ToArray();
            weather.DiffuseHorizontalRadiation = Enumerable.Repeat(0.0, 8760).ToArray();
            weather.DirectNormalRadiation = Enumerable.Repeat(0.0, 8760).ToArray();

            Vector3d[,] vecs = new Vector3d[100, 8];
            for (int j = 0; j < 100; j++)
            {
                for (int i = 0; i < windDirList.Count; i++)
                {
                    vecs[j, i] = new Vector3d(0, 2, 0);
                }
            }
               ;

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcond, 5, 50, 300, 80);

            var points = Enumerable.Repeat(new Point3d(60, 60, 2), 100);

            // Act

            var vf = new SkyViewFactor(workingdir, DOMCYL.BuildingGeometry, points.ToArray(), true)
            {
                // Set the view factors to 60 % sky and 40 % buildings
                Values = Enumerable.Repeat(0.6, 100).ToArray()
            };

            var sky = new Sky(weather.DewPointTemp, weather.DryBulbTemp, weather.SkyCover, weather.RelativeHumidity)
            {
                // Set the Skytemp to 10°C
                Temp = Enumerable.Repeat(10.0, 8760).ToArray()
            };

            var mrt = new MRT(workingdir, DOMCYL.BuildingGeometry, sky, vf, weather, EddyLib.MRT.MRTType.RadianceTwoPhaseDDS, points.ToArray(), true);

            // Assert

            Assert.Equal(18, Math.Round(mrt.Values[0, 0], 2));
        }

        [Fact]
        public void MRT_40Sky_60Buildings_Returns_22()
        {
            // Arrange
            var workingdir = @"C:\Testing\";

            var point0 = new Rhino.Geometry.Point3d(0, 0, 0);
            var point1 = new Rhino.Geometry.Point3d(20, 0, 0);
            var point2 = new Rhino.Geometry.Point3d(0, 20, 0);
            var point3 = new Rhino.Geometry.Point3d(20, 20, 0);
            var point4 = new Rhino.Geometry.Point3d(0, 0, 40);
            var point5 = new Rhino.Geometry.Point3d(20, 0, 40);
            var point6 = new Rhino.Geometry.Point3d(0, 20, 40);
            var point7 = new Rhino.Geometry.Point3d(20, 20, 40);

            var surface = NurbsSurface.CreateFromCorners(
              new Point3d(5000, 0, 0),
              new Point3d(5000, 5000, 0),
              new Point3d(0, 5000, 0),
              new Point3d(0, 0, 0));

            surface.Translate(new Vector3d(-2500, -2050, 0));

            Rhino.Geometry.Box box1 = new Rhino.Geometry.Box(Rhino.Geometry.Plane.WorldXY, new List<Rhino.Geometry.Point3d>() { point0, point1, point2, point3, point4, point5, point6, point7 });
            Rhino.Geometry.MeshingParameters mp = new Rhino.Geometry.MeshingParameters();
            var m = Mesh.CreateFromBrep(box1.ToBrep(), mp);
            var s = Mesh.CreateFromBrep(surface.ToBrep(), mp);

            Rhino.Geometry.Mesh mm = new Rhino.Geometry.Mesh();

            foreach (Rhino.Geometry.Mesh im in m)
            {
                mm.Append(im);
            }
            mm.Append(s);

            string epw = @"C:\ladybug\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997.epw";

            var z0 = 1;
            var uref = 10;
            var zref = 10;

            var windDirList = new List<int>() { 0 };

            var bcond = new ABL(windDirList, uref, zref, z0, 0, epw);

            Weather weather = new Weather(epw);

            weather.WindSpeed = Enumerable.Repeat(5.0, 8760).ToArray();
            weather.WindDirection = Enumerable.Repeat(0, 8760).ToArray();
            weather.DryBulbTemp = Enumerable.Repeat(30.0, 8760).ToArray();
            weather.RelativeHumidity = Enumerable.Repeat(50.0, 8760).ToArray();
            weather.DiffuseHorizontalRadiation = Enumerable.Repeat(0.0, 8760).ToArray();
            weather.DirectNormalRadiation = Enumerable.Repeat(0.0, 8760).ToArray();

            Vector3d[,] vecs = new Vector3d[100, 8];
            for (int j = 0; j < 100; j++)
            {
                for (int i = 0; i < windDirList.Count; i++)
                {
                    vecs[j, i] = new Vector3d(0, 2, 0);
                }
            }
       ;

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcond, 5, 50, 300, 80);

            var points = Enumerable.Repeat(new Point3d(60, 60, 2), 100);

            // Act

            var vf = new SkyViewFactor(workingdir, DOMCYL.BuildingGeometry, points.ToArray(), true);

            // Set the view factors to 40 % sky and 60 % buildings
            vf.Values = Enumerable.Repeat(0.4, 100).ToArray();

            var sky = new Sky(weather.DewPointTemp, weather.DryBulbTemp, weather.SkyCover, weather.RelativeHumidity);

            // Set the Skytemp to 10°C
            sky.Temp = Enumerable.Repeat(10.0, 8760).ToArray();

            var mrt = new MRT(workingdir, DOMCYL.BuildingGeometry, sky, vf, weather, EddyLib.MRT.MRTType.RadianceTwoPhaseDDS, points.ToArray(), true);

            // Assert

            Assert.Equal(22, Math.Round(mrt.Values[0, 0], 2));
        }

        [Fact]
        public void ViewFactors()
        {
            // Arrange
            var workingdir = @"C:\Testing\";

            var point0 = new Rhino.Geometry.Point3d(0, 0, 0);
            var point1 = new Rhino.Geometry.Point3d(20, 0, 0);
            var point2 = new Rhino.Geometry.Point3d(0, 20, 0);
            var point3 = new Rhino.Geometry.Point3d(20, 20, 0);
            var point4 = new Rhino.Geometry.Point3d(0, 0, 40);
            var point5 = new Rhino.Geometry.Point3d(20, 0, 40);
            var point6 = new Rhino.Geometry.Point3d(0, 20, 40);
            var point7 = new Rhino.Geometry.Point3d(20, 20, 40);

            var surface = NurbsSurface.CreateFromCorners(
              new Point3d(5000, 0, 0),
              new Point3d(5000, 5000, 0),
              new Point3d(0, 5000, 0),
              new Point3d(0, 0, 0));

            surface.Translate(new Vector3d(-2500, -2050, 0));

            Rhino.Geometry.Box box1 = new Rhino.Geometry.Box(Rhino.Geometry.Plane.WorldXY, new List<Rhino.Geometry.Point3d>() { point0, point1, point2, point3, point4, point5, point6, point7 });
            Rhino.Geometry.MeshingParameters mp = new Rhino.Geometry.MeshingParameters();
            var m = Mesh.CreateFromBrep(box1.ToBrep(), mp);
            var s = Mesh.CreateFromBrep(surface.ToBrep(), mp);

            Rhino.Geometry.Mesh mm = new Rhino.Geometry.Mesh();

            foreach (Rhino.Geometry.Mesh im in m)
            {
                mm.Append(im);
            }
            mm.Append(s);

            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
            BoundaryCondition bcond = new ABL(windDirList, 5, 10, 1, 0, "");

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcond, 5, 50, 300, 80);

            var points = Enumerable.Repeat(new Point3d(40, 40, 2), 100);

            // Act
            var vf = new SkyViewFactor(workingdir, DOMCYL.BuildingGeometry, points.ToArray(), true);

            // Assert

            Assert.Equal(0.47, Math.Round(vf.Values[0], 2));
        }

        [Fact]
        public void ScaleABL_3m_Returns_2_89()
        {
            // Arrange

            var z0 = 1;
            var uref = 5;
            var zref = 10;

            // Act
            var res = EddyLib.BCs.BoundaryCondition.ScaleABL(uref, zref, z0, 3);

            // Assert

            Assert.Equal(2.89, Math.Round(res, 2));
        }

        [Fact]
        public void WindFactors()
        {
            //// Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
            string epw = @"C:\ladybug\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997.epw";

            var z0 = 1;
            var uref = 10;
            var zref = 10;

            var bcond = new ABL(new List<int> { 0 }, uref, zref, z0, 0, "");

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
            /// We would expect a Uref of 4.58 m/s at 2 m height --> this leads to a WFS of 44 % for an assumption of 2 m/s probes

            Assert.Equal(0.44, Math.Round(wfs.ValuesSpatial[0, 0], 2));

            var wft = new WindFactorsAnnual(workingdir, bcond, weather, wfs, points.ToList(), false, true);

            // Here, we would expect 44 % of 2.29 m/s which is the ABl velocity at 5 m height.

            Assert.Equal(1.01, Math.Round(wft.ValuesTemporal[0, 0], 2));
        }

        [Fact]
        public void UTCI()
        {
            //// Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
            string epw = @"C:\ladybug\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997\New_York_J_F_Kennedy_IntL_Ar_NY_USA_1997.epw";

            var z0 = 1;
            var uref = 10;
            var zref = 10;

            var bcond = new ABL(windDirList, uref, zref, z0, 0, "");

            Weather weather = new Weather(epw);
            weather.WindSpeed = Enumerable.Repeat(5.0, 8760).ToArray();
            weather.WindDirection = Enumerable.Repeat(0, 8760).ToArray();
            weather.DryBulbTemp = Enumerable.Repeat(20.0, 8760).ToArray();
            weather.RelativeHumidity = Enumerable.Repeat(50.0, 8760).ToArray();

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
            var wfs = new WindFactorsSpatial(workingdir, bcond, mdv, points.ToList(), false, true);
            var wft = new WindFactorsAnnual(workingdir, bcond, weather, wfs, points.ToList(), false, true);

            //// Act

            var mrtdir = @"C:\Testing\Rad\";
            var outputdir = @"C:\Testing\Output\";
            if (!Directory.Exists(outputdir)) { Directory.CreateDirectory(outputdir); };
            if (!Directory.Exists(mrtdir)) { Directory.CreateDirectory(mrtdir); };

            var box = new BoundingBox(new Point3d(0, 0, 0), new Point3d(20, 20, 20));
            var box2 = new Box(Plane.WorldXY, box);

            var vf = new SkyViewFactor(workingdir, Mesh.CreateFromBox(box2, 100, 100, 100), points.ToArray(), true);

            var sky = new Sky(weather.DewPointTemp, weather.DryBulbTemp, weather.SkyCover, weather.RelativeHumidity);

            var mrt = new MRT(workingdir, Mesh.CreateFromBox(box2, 100, 100, 100), sky, vf, weather, MRTType.RadianceTwoPhaseDDS, points.ToArray(), true)
            {
                Values = new double[8760, 100]
            };

            for (int j = 0; j < 8760; j++)
            {
                for (int i = 0; i < points.Count(); i++)
                {
                    mrt.Values[j, i] = 25.0;
                }
            }

            // At 10 m, this leads to a wind velocity of 1.31 m/s which leads to a UTCI of 20.6
            var utci = new UTCI(points.ToArray(), wft, weather, mrt, workingdir, true, 2);

            Assert.Equal(20.6, utci.ValuesUTCI[0, 0]);
        }

        [Fact]
        public void DistanceBetween_0_355_Return5()
        {
            // Arrange
            var dir1 = 0;
            var dir2 = 355;

            // Act
            var res = BoundaryCondition.DistanceBetweenWindDirs(dir1, dir2);

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
            var res = BoundaryCondition.DistanceBetweenWindDirs(dir1, dir2);

            // Assert
            Assert.Equal(45, res);
        }

        [Fact]
        public void ReturnNextLowerIndex_0_Return315()
        {
            // Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };

            // Act
            var res = BoundaryCondition.ReturnNextLowerIndex(windDirList, 0);

            // Assert

            Assert.Equal(7, res);
        }
    }
}