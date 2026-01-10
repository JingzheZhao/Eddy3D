using EddyLib;
using EddyLib.BCs;
using EddyLib.OutdoorComfort;
using EddyLib.Radiation;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using static RhinoPlugin.Test.Xunit.WeatherDownload;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class OutdoorComfortTests
    {
        [Fact]
        public void SkyTemp_JFK()
        {
            string epw = DownloadEPW();

            var skytemp = new List<double>() { -17.25, -16.11, -12.57, -12.14, -15.18 };

            Weather w = new Weather(epw);

            // Tested: E+ does not use the SkyRadiation from the weather file when started through the Shoebox template with ClimateStudio
            var sky = new SkyTemperatureModel(w.DewPointTemp, w.DryBulbTemp, w.OpaqSkyCover, w.RelativeHumidity, true, SkyTemperatureModel.CalculationType.DefaultClarkAllen);

            var skyTemp = sky.Temp;

            Assert.Equal(skytemp.ToArray().Select(d => Math.Round(d, 2)).ToArray(), skyTemp.Select(d => Math.Round(d, 2)).Take(5).ToArray());
        }

        public enum MrtBcCollectionMode
        {
            AddBc,
            Single,
            WithWeather
        }

        private static void AssertRoundedEqual(double expected, double actual, int decimals)
        {
            Assert.Equal(Math.Round(expected, decimals), Math.Round(actual, decimals));
        }

        [Theory]
        [Trait("Category", "Execution")]
        [InlineData(0.5, 20.0, 5.0, MrtBcCollectionMode.AddBc)]
        [InlineData(0.6, 18.0, 5.0, MrtBcCollectionMode.Single)]
        [InlineData(0.4, 22.0, 10.0, MrtBcCollectionMode.WithWeather)]
        public void MRT_SkyBuildings_ReturnsExpected(
            double skyFactor,
            double expectedMrt,
            double uref,
            MrtBcCollectionMode bcMode)
        {
            var mrt = RunMrtCase(skyFactor, uref, bcMode);

            Assert.Equal(expectedMrt, Math.Round(mrt.Values[0, 0], 2));
        }

        private MRT RunMrtCase(double skyFactor, double uref, MrtBcCollectionMode bcMode)
        {
            var workingdir = TestFixtures.CreateTestDirectory("outdoorcomfort-mrt");

            var mm = Setup.SetUpBuildingMeshAppendSurface();
            var epw = DownloadEPW();
            var bcond = new ABL(0, uref, 10, 1, 0);
            var bcColl = CreateMrtBcCollection(bcond, epw, bcMode);
            var weather = CreateMrtWeather(epw);

            var domCyl = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);
            var points = Enumerable.Repeat(new Point3d(60, 60, 2), 100).ToArray();

            var vf = new SkyViewFactor(workingdir, domCyl.BuildingGeometry, points, true)
            {
                Values = Enumerable.Repeat(skyFactor, points.Length).ToArray()
            };

            var sky = new SkyTemperatureModel(
                weather.DewPointTemp,
                weather.DryBulbTemp,
                weather.TotalSkyCover,
                weather.RelativeHumidity,
                false,
                SkyTemperatureModel.CalculationType.DefaultClarkAllen)
            {
                Temp = Enumerable.Repeat(10.0, TestConstants.HoursPerYear).ToArray()
            };

            return new MRT(
                workingdir,
                domCyl.BuildingGeometry,
                sky,
                vf,
                weather,
                MRT.MRTType.RadianceTwoPhaseDDS,
                points,
                true,
                GetRadianceDir());
        }

        private static BCCollection CreateMrtBcCollection(ABL bcond, string epw, MrtBcCollectionMode bcMode)
        {
            switch (bcMode)
            {
                case MrtBcCollectionMode.AddBc:
                    {
                        var bcColl = new BCCollection();
                        bcColl.BCs.Add(bcond);
                        return bcColl;
                    }
                case MrtBcCollectionMode.Single:
                    return new BCCollection(bcond);

                case MrtBcCollectionMode.WithWeather:
                    return new BCCollection(bcond, epw);

                default:
                    throw new ArgumentOutOfRangeException(nameof(bcMode), bcMode, "Unknown BC collection mode.");
            }
        }

        private static Weather CreateMrtWeather(string epw)
        {
            var weather = new Weather(epw)
            {
                WindSpeed = Enumerable.Repeat(5.0, TestConstants.HoursPerYear).ToArray(),
                WindDirection = Enumerable.Repeat(0, TestConstants.HoursPerYear).ToArray(),
                DryBulbTemp = Enumerable.Repeat(30.0, TestConstants.HoursPerYear).ToArray(),
                RelativeHumidity = Enumerable.Repeat(50.0, TestConstants.HoursPerYear).ToArray(),
                DiffuseHorizontalRadiation = Enumerable.Repeat(0.0, TestConstants.HoursPerYear).ToArray(),
                DirectNormalRadiation = Enumerable.Repeat(0.0, TestConstants.HoursPerYear).ToArray()
            };

            return weather;
        }

        private static string CreateCleanWorkingDir()
        {
            return TestFixtures.CreateTestDirectory("outdoorcomfort");
        }

        private static string EnsureViewFactorsDir(string workingdir)
        {
            var viewFactorsDir = Path.Combine(workingdir, "Rad", "ViewFactors");
            if (!Directory.Exists(viewFactorsDir)) { Directory.CreateDirectory(viewFactorsDir); }

            return viewFactorsDir;
        }

        [Theory]
        [InlineData(90, 0, 700, 1.0, 0.4, 0.5, 0.7, 23.2, 97.0)]
        [InlineData(0, 120, 800, 0.5, 0.5, 0.5, 0.7, 10.3, 42.9)]
        public void SolarGain_ReturnsExpected(
            int solAlt,
            int sharp,
            double directBeam,
            double tSol,
            double fsvv,
            double fbes,
            double avgShortAbs,
            double expectedDmrt,
            double expectedErf)
        {
            // https://comfort.cbe.berkeley.edu/
            double dMRT = 0.0;
            double ERF = 0.0;

            EddyLib.Radiation.SolarGain.ERF(
                solAlt,
                sharp,
                EddyLib.Radiation.SolarGain.Posture.seating,
                directBeam,
                tSol,
                fsvv,
                fbes,
                avgShortAbs,
                out ERF,
                out dMRT);

            AssertRoundedEqual(expectedDmrt, dMRT, 1);
            AssertRoundedEqual(expectedErf, ERF, 1);

            ////             >>> from pythermalcomfort.models import solar_gain
            ////             >>> results = solar_gain(sol_altitude=0, sol_azimuth=120, sol_radiation_dir=800, sol_transmittance=0.5, f_svv=0.5, f_bes=0.5, asw=0.7, posture='seated')
            ////             >>> print(results)
            ////             {'erf': 42.9, 'delta_mrt': 10.3}
        }

        [Fact]
        [Trait("Category", "Execution")]
        public void ViewFactors()
        {
            // Arrange
            var workingdir = CreateCleanWorkingDir();

            var mm = Setup.SetUpBuildingMeshAppendSurface();

            BC bcond = new ABL(0, 5, 10, 1, 0);

            BCCollection bcColl = new BCCollection(bcond);

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            var points = Enumerable.Repeat(new Point3d(40, 40, 2), 100);

            // Act
            var vf = new SkyViewFactor(workingdir, DOMCYL.BuildingGeometry, points.ToArray(), true);

            // Assert

            Assert.Equal(0.47, Math.Round(vf.Values[0], 2));
        }

        [Fact]
        [Trait("Category", "Execution")]
        public void ViewFactors_Export()
        {
            // Arrange
            var workingdir = CreateCleanWorkingDir();

            double[] file = new double[3] { 0.47, 0.49, 0.50 };

            // Act
            var viewFactorsDir = EnsureViewFactorsDir(workingdir);
            var csvSVF = Path.Combine(viewFactorsDir, "SkyViewFactors.csv");

            CsvHelpers.Array1DToCSV(file, csvSVF);
        }

        [Fact]
        [Trait("Category", "Execution")]
        public void ViewFactors_Load()
        {
            // Arrange
            var workingdir = CreateCleanWorkingDir();

            double[] file = new double[3] { 0.47, 0.49, 0.50 };

            // Act
            var viewFactorsDir = EnsureViewFactorsDir(workingdir);
            var binSVF = Path.Combine(viewFactorsDir, "SkyViewFactors.bin");

            RadianceFiles.writeBin1D(binSVF, file);

            var tempValues = RadianceFiles.loadBin1D(binSVF);

            // Assert

            AssertRoundedEqual(0.47, tempValues[0], 2);
            AssertRoundedEqual(0.49, tempValues[1], 2);
            AssertRoundedEqual(0.50, tempValues[2], 2);
        }

        [Theory]
        [InlineData(0, 355, 5)]
        [InlineData(45, 90, 45)]
        public void WindFactors_DistanceBetween_ReturnsExpected(int dir1, int dir2, int expected)
        {
            var res = EddyLib.OutdoorComfort.WindSystem.DistanceBetweenWindDirs(dir1, dir2);

            Assert.Equal(expected, res);
        }

        [Fact]
        public void WindFactors_ReturnNextLowerIndex_0_Return315()
        {
            // Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };

            // Act
            var res = EddyLib.OutdoorComfort.WindSystem.ReturnNextLowerIndex(windDirList.ToArray(), 0);

            // Assert

            Assert.Equal(7, res);
        }

        [Theory]
        [InlineData(5.0, 10.0, 1.0, 3.0, 2.89)]
        [InlineData(2.89, 3.0, 1.0, 10.0, 5.0)]
        public void ScaleABL_ReturnsExpected(double uref, double zref, double z0, double height, double expected)
        {
            var res = EddyLib.BCs.BC.ScaleABL(uref, zref, z0, height);

            Assert.Equal(expected, Math.Round(res, 2));
        }

        [Fact]
        public void WindFactors()
        {
            //// Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };

            var z0 = 1;
            var uref = 10;
            var zref = 10;

            var bcond = new ABL(0, uref, zref, z0, 0);

            BCCollection bcColl = new BCCollection(bcond);

            string epw = DownloadEPW();
            Weather weather = new Weather(epw);
            weather.WindSpeed = Enumerable.Repeat(5.0, TestConstants.HoursPerYear).ToArray();
            weather.WindDirection = Enumerable.Repeat(0, TestConstants.HoursPerYear).ToArray();

            Vector3d[,] vecs = new Vector3d[100, 8];
            for (int j = 0; j < 100; j++)
            {
                for (int i = 0; i < windDirList.Count; i++)
                {
                    vecs[j, i] = new Vector3d(0, 2, 0);
                }
            }
                ;

            var workingdir = TestFixtures.CreateTestDirectory("outdoorcomfort-windfactors");
            var points = Enumerable.Repeat(new Point3d(0, 0, 2), 100);
            var mdv = new MultiDirectionalVelocities(workingdir, windDirList.ToArray(), vecs, true, true);

            //// Act

            var wfs = new WindFactorsSpatial(workingdir, bcColl, mdv, points.ToList(), false, true);

            //// Assert
            /// We would expect a Uref of 4.58 m/s at 2 m height --> this leads to a WFS of 44 % for an assumption of 2 m/s probes

            Assert.Equal(0.44, Math.Round(wfs.ValuesSpatial[0, 0], 2));

            var wft = new WindFactorsTemporal(workingdir, bcColl, weather, wfs, points.ToList(), false, true);

            // Here, we would expect 44 % of 2.29 m/s which is the ABl velocity at 5 m height.

            Assert.Equal(1.01, Math.Round(wft.ValuesTemporalAtProbingHeight[0, 0], 2));
        }

        private readonly string RadiancePath = @"C:\Program Files\Radiance\bin"; // Replace with your folder path
        private readonly string EddyRadiancePath = @"C:\Eddy3D\Common\Radiance\bin";

        private readonly string[] RadianceExecutables = new string[]
        {
            "rfluxmtx.exe",
            "epw2wea.exe",
            "gendaymtx.exe",
            "dctimestep.exe",
            "oconv.exe",
            "rcontrib.exe",
            "rmtxop.exe"
        };

        [Fact]
        public void IsRadianceInstalled()
        {
            foreach (var exe in RadianceExecutables)
            {
                string defaultFilePath = Path.Combine(RadiancePath, exe);
                string Eddy3DFilePath = Path.Combine(EddyRadiancePath, exe);
                Assert.True(File.Exists(defaultFilePath) || File.Exists(Eddy3DFilePath), $"Executable not found: {defaultFilePath}");
            }
        }

        [Fact]
        public void UTCIBounds()
        {
            //// Arrange

            var outOfBounds = new bool[10];
            var utci = new double[10];

            var mrt = new double[] {10
,20
,30
,40
,50
,10
,20
,30
,40
,50
 };

            var tamb = new double[] {30
,35
,40
,25
,10
,5
,15
,20
,25
,20
 };

            var rh = new double[] {
        30
,40
,75
,95
,10
,30
,20
,45
,55
,60
};

            var wsp = new double[] {                0
,1
,5
,4
,10
,15
,7
,5
,3
,8
 };

            //// Act

            for (int i = 0; i < mrt.Length; i++)
            {
                bool ob;
                var val = EddyLib.UTCI.CalcUTCICorrectBounds(tamb[i], rh[i], wsp[i], mrt[i], out ob);
                outOfBounds[i] = ob;

                utci[i] = val;
            }

            Assert.Equal(Math.Round(23.36079, 3), Math.Round(utci[0], 3));
            Assert.Equal(Math.Round(31.826409, 3), Math.Round(utci[1], 3));
            Assert.Equal(Math.Round(52.290443, 3), Math.Round(utci[2], 3));
            Assert.Equal(Math.Round(27.897167, 3), Math.Round(utci[3], 3));
            Assert.Equal(Math.Round(2.689566, 3), Math.Round(utci[4], 3));
            Assert.Equal(Math.Round(-25.777969, 3), Math.Round(utci[5], 3));
            Assert.Equal(Math.Round(3.610271, 3), Math.Round(utci[6], 3));
            Assert.Equal(Math.Round(15.771004, 3), Math.Round(utci[7], 3));
            Assert.Equal(Math.Round(26.627918, 3), Math.Round(utci[8], 3));
            Assert.Equal(Math.Round(19.105775, 3), Math.Round(utci[9], 3));
        }

        private string GetRadianceDir()
        {
            string foundPath = string.Empty;

            // Check if all executables exist in the default Radiance path
            if (AllExecutablesExist(RadiancePath))
            {
                foundPath = RadiancePath;
            }
            // Check if all executables exist in the Eddy3D Radiance path
            else if (AllExecutablesExist(EddyRadiancePath))
            {
                foundPath = EddyRadiancePath;
            }

            if (!string.IsNullOrEmpty(foundPath))
            {
                // Use DirectoryInfo to navigate to the parent directory
                DirectoryInfo directoryInfo = new DirectoryInfo(foundPath);
                DirectoryInfo parentDir = directoryInfo.Parent;
                if (parentDir != null)
                {
                    return parentDir.FullName;
                }
                else
                {
                    throw new InvalidOperationException("No parent directory exists for the found path.");
                }
            }

            // If no directory contains all executables, throw an exception
            throw new InvalidOperationException("Radiance executables not found in the specified directories.");
        }

        private bool AllExecutablesExist(string directoryPath)
        {
            foreach (var exe in RadianceExecutables)
            {
                if (!File.Exists(Path.Combine(directoryPath, exe)))
                {
                    return false; // If any executable is missing, return false
                }
            }
            return true; // All executables were found
        }

        [Fact]
        [Trait("Category", "Execution")]
        public void UTCI()
        {
            //// Arrange
            ///
            string epw = DownloadEPW();

            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };

            var z0 = 1;
            var uref = 10;
            var zref = 10;

            var bcond = new ABL(0, uref, zref, z0, 0);

            BCCollection bcColl = new BCCollection(bcond);

            Weather weather = new Weather(epw);
            weather.WindSpeed = Enumerable.Repeat(5.0, TestConstants.HoursPerYear).ToArray();
            weather.WindDirection = Enumerable.Repeat(0, TestConstants.HoursPerYear).ToArray();
            weather.DryBulbTemp = Enumerable.Repeat(20.0, TestConstants.HoursPerYear).ToArray();
            weather.RelativeHumidity = Enumerable.Repeat(50.0, TestConstants.HoursPerYear).ToArray();

            Vector3d[,] vecs = new Vector3d[100, 8];
            for (int j = 0; j < 100; j++)
            {
                for (int i = 0; i < windDirList.Count; i++)
                {
                    vecs[j, i] = new Vector3d(0, 2, 0);
                }
            }
        ;

            var workingdir = TestFixtures.CreateTestDirectory("outdoorcomfort-utci");
            var points = Enumerable.Repeat(new Point3d(0, 0, 2), 100);
            var mdv = new MultiDirectionalVelocities(workingdir, windDirList.ToArray(), vecs, true, true);
            var wfs = new WindFactorsSpatial(workingdir, bcColl, mdv, points.ToList(), false, true);
            var wft = new WindFactorsTemporal(workingdir, bcColl, weather, wfs, points.ToList(), false, true);

            //// Act

            var mrtdir = Path.Combine(workingdir, "Rad");
            var outputdir = Path.Combine(workingdir, "Output");
            if (!Directory.Exists(outputdir)) { Directory.CreateDirectory(outputdir); }
            ;
            if (!Directory.Exists(mrtdir)) { Directory.CreateDirectory(mrtdir); }
            ;

            var box = new BoundingBox(new Point3d(0, 0, 0), new Point3d(20, 20, 20));
            var box2 = new Box(Plane.WorldXY, box);

            var vf = new SkyViewFactor(workingdir, Mesh.CreateFromBox(box2, 100, 100, 100), points.ToArray(), true);

            var sky = new SkyTemperatureModel(weather.DewPointTemp, weather.DryBulbTemp, weather.TotalSkyCover, weather.RelativeHumidity, true, SkyTemperatureModel.CalculationType.DefaultClarkAllen);

            var mrt = new MRT(workingdir, Mesh.CreateFromBox(box2, 100, 100, 100), sky, vf, weather, MRT.MRTType.RadianceTwoPhaseDDS, points.ToArray(), true)
            {
                Values = new double[TestConstants.HoursPerYear, 100]
            };

            for (int j = 0; j < TestConstants.HoursPerYear; j++)
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

        #region UTCI Subroutine Tests

        /// <summary>
        /// Tests the At10Meters wind speed conversion formula.
        /// Formula: va_10m = va_h * log(10/0.01) / log(h/0.01)
        /// </summary>
        [Theory]
        [InlineData(2.0, 2.0, 2.61)]   // 2 m/s at 2m height -> ~2.61 m/s at 10m  
        [InlineData(5.0, 10.0, 5.0)]   // 5 m/s at 10m height -> 5 m/s at 10m (identity)
        [InlineData(3.0, 1.5, 4.14)]   // 3 m/s at 1.5m height -> ~4.14 m/s at 10m
        [InlineData(10.0, 5.0, 11.12)] // 10 m/s at 5m height -> ~11.12 m/s at 10m
        public void UTCI_At10Meters(double windSpeedAtHeight, double height, double expectedAt10m)
        {
            double result = EddyLib.UTCI.At10Meters(windSpeedAtHeight, height);
            Assert.Equal(expectedAt10m, Math.Round(result, 2));
        }

        /// <summary>
        /// Tests the UTCI calculation for known reference conditions.
        /// TODO: These expected values need to be verified against reference implementation.
        /// </summary>
        //[Theory]
        //[InlineData(20.0, 50.0, 0.5, 20.0, 19.1)]  // Neutral conditions
        //[InlineData(30.0, 50.0, 0.5, 30.0, 29.4)]  // Warm conditions
        //[InlineData(10.0, 50.0, 0.5, 10.0, 7.7)]   // Cool conditions
        //[InlineData(35.0, 80.0, 1.0, 40.0, 40.6)]  // Hot humid with higher MRT
        //public void UTCI_CalcUTCI_SingleValue(double ta, double rh, double windSpeed10m, double mrt, double expectedUTCI)
        //{
        //    double result = EddyLib.UTCI.CalcUTCI(ta, rh, windSpeed10m, mrt);
        //    Assert.Equal(expectedUTCI, Math.Round(result, 1));
        //}

        /// <summary>
        /// Tests the Binning method that categorizes UTCI values into stress categories.
        /// Categories: -5(extreme cold) to +5(extreme heat), 0 = no stress
        /// </summary>
        [Fact]
        public void UTCI_Binning_Categories()
        {
            // Create a list with known categories
            var vals = new List<double>
            {
                0, 0, 0, 0, 0,  // 5x no stress (category 0)
                1, 1,           // 2x slight heat (category 1)
                -1, -1, -1      // 3x slight cold (category -1)
            };

            double extrCold = 0, vryStrngCold = 0, strngCold = 0, mdrtCold = 0, slgtCold = 0;
            double noStress = 0, slgtHeat = 0, mdrtHeat = 0, strngHeat = 0, vryStrngHeat = 0, extrHeat = 0;

            EddyLib.UTCI.Binning(vals, 
                ref extrCold, ref vryStrngCold, ref strngCold, ref mdrtCold, ref slgtCold,
                ref noStress, ref slgtHeat, ref mdrtHeat, ref strngHeat, ref vryStrngHeat, ref extrHeat);

            Assert.Equal(0.5, noStress);    // 5/10 = 50%
            Assert.Equal(0.2, slgtHeat);    // 2/10 = 20%
            Assert.Equal(0.3, slgtCold);    // 3/10 = 30%
        }

        #endregion
    }
}
