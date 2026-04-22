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
        [RhinoRequiredFact]
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

        [NotWindowsServerTheory]
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

        [Fact]
        public void MRT_GetMRTForPointViaKessling_MatchesLegacyMathPowBaseline()
        {
            var cases = new[]
            {
                (Tair: 27.240514702802940, RelHum: 75.073871946698389, SolarElev: 12.749954696930402, Wst: 42.671512064480488, Hst: 13.137875527817734, BodyA: 0.808056224722471, GrRef: 0.253798160993097, DiffRad: 113.243731879183557, DirRad: 812.845779060884411, ExpectedMrt: 57.264728321438668, ExpectedIr: 24.239529086964410),
                (Tair: 36.036657430805590, RelHum: 45.733564850574318, SolarElev: 53.299046505024563, Wst: 10.531354708588545, Hst: 35.372543229457555, BodyA: 0.618951360026051, GrRef: 0.222203028303200, DiffRad: 64.570927835711302, DirRad: 595.753116837384709, ExpectedMrt: 53.147079253777633, ExpectedIr: 35.599126174224352),
                (Tair: 9.117578189470944, RelHum: 18.190013904180514, SolarElev: 80.113461407013432, Wst: 34.114285730729200, Hst: 15.391218899770896, BodyA: 0.524609344020181, GrRef: 0.346155647813346, DiffRad: 27.581738562679885, DirRad: 548.053032142749544, ExpectedMrt: 22.891836294753546, ExpectedIr: 2.117356651724094),
                (Tair: -1.950129318721551, RelHum: 23.466206671099382, SolarElev: 81.067848307998972, Wst: 38.608405795262684, Hst: 34.050106138113051, BodyA: 0.475781649274238, GrRef: 0.336227900147605, DiffRad: 292.688892248290585, DirRad: 62.941394963394096, ExpectedMrt: 14.304405777662339, ExpectedIr: -6.211456631636224),
                (Tair: 8.691273131140321, RelHum: 11.423708164384257, SolarElev: 39.037297649599665, Wst: 9.065014316658285, Hst: 36.898577890428761, BodyA: 0.499333866322132, GrRef: 0.120909566798192, DiffRad: 18.699259855124055, DirRad: 578.047625903573930, ExpectedMrt: 21.458738041572019, ExpectedIr: 7.660843857725411),
                (Tair: 9.583797924633991, RelHum: 59.260843076072796, SolarElev: 71.128732537681003, Wst: 38.947669108609709, Hst: 42.706980843219817, BodyA: 0.598438845823702, GrRef: 0.221622952771288, DiffRad: 9.739984442189831, DirRad: 477.377607804337003, ExpectedMrt: 21.670370409404086, ExpectedIr: 6.898951659139072),
                (Tair: 35.504624084546265, RelHum: 87.950368385021932, SolarElev: 84.279773532566139, Wst: 40.279216147621710, Hst: 43.447341387778600, BodyA: 0.502880720531137, GrRef: 0.131396381756549, DiffRad: 135.372856391778043, DirRad: 141.169798849181234, ExpectedMrt: 42.023806797025543, ExpectedIr: 35.366752794946024),
                (Tair: 26.194483446953370, RelHum: 43.826949362617619, SolarElev: 47.884550992345559, Wst: 26.952194175662946, Hst: 43.898633086806285, BodyA: 0.464930726432912, GrRef: 0.212678418019434, DiffRad: 103.872191703517245, DirRad: 476.956093843437827, ExpectedMrt: 38.650036653398615, ExpectedIr: 24.764850185258240),
                (Tair: 5.199309782941572, RelHum: 29.662489859539466, SolarElev: 37.429655314379730, Wst: 24.280180416932062, Hst: 10.508249817947210, BodyA: 0.845632187302119, GrRef: 0.370801267391077, DiffRad: 47.055633689484857, DirRad: 357.536369300146646, ExpectedMrt: 24.166658953155206, ExpectedIr: -1.778443244569928),
                (Tair: 25.748829667638880, RelHum: 37.685213504031381, SolarElev: 66.718757151777595, Wst: 44.340750674140857, Hst: 28.790387331885260, BodyA: 0.592594523913868, GrRef: 0.332257922729166, DiffRad: 269.305308813070440, DirRad: 155.615772614018141, ExpectedMrt: 42.865034717855906, ExpectedIr: 22.236114768226400),
                (Tair: 5.014345150503296, RelHum: 29.641051334380048, SolarElev: 21.292524782824870, Wst: 11.633108987638977, Hst: 20.462429421619547, BodyA: 0.420827461519315, GrRef: 0.246457376702346, DiffRad: 178.205438155646220, DirRad: 575.583824071897766, ExpectedMrt: 23.918497467519444, ExpectedIr: 2.832839086165848),
                (Tair: 16.753819746507709, RelHum: 28.769365296863363, SolarElev: 55.485058179296345, Wst: 11.454559080764859, Hst: 26.461110897574567, BodyA: 0.482300235463430, GrRef: 0.214758371573616, DiffRad: 197.248830624316525, DirRad: 783.407812265213124, ExpectedMrt: 40.672641666454467, ExpectedIr: 15.265909811409870),
                (Tair: 11.723768127182140, RelHum: 61.369377763032766, SolarElev: 9.955885029110057, Wst: 19.245047051412524, Hst: 22.524389482799648, BodyA: 0.453461728532043, GrRef: 0.293373576116172, DiffRad: 254.209667449827492, DirRad: 938.012352036776747, ExpectedMrt: 40.136753625196377, ExpectedIr: 9.272420090622518),
                (Tair: 30.723498572283340, RelHum: 80.619012928058012, SolarElev: 58.517255308108055, Wst: 48.418365997668332, Hst: 10.285814003714794, BodyA: 0.517775174787783, GrRef: 0.126017127492620, DiffRad: 264.142365101805922, DirRad: 569.788053515546721, ExpectedMrt: 47.523888321844424, ExpectedIr: 28.514572125537654),
                (Tair: 5.795124723998118, RelHum: 48.680989538309774, SolarElev: 48.678095961986202, Wst: 23.436714409788546, Hst: 38.851594055103398, BodyA: 0.654897772606376, GrRef: 0.341896722760301, DiffRad: 66.005537018511021, DirRad: 209.785779573431228, ExpectedMrt: 18.510607431666244, ExpectedIr: 3.750964089565628),
                (Tair: 30.713974068585841, RelHum: 78.824287558675380, SolarElev: 31.070724828786123, Wst: 33.004821443293309, Hst: 19.692698320505681, BodyA: 0.505615319947435, GrRef: 0.160147951799249, DiffRad: 265.131685682839873, DirRad: 279.887774967279086, ExpectedMrt: 44.851165239115346, ExpectedIr: 29.326977667181950),
                (Tair: 31.549402593186400, RelHum: 89.053557230158887, SolarElev: 37.759802352568379, Wst: 21.669158163345625, Hst: 44.403590070932161, BodyA: 0.767617895761838, GrRef: 0.305350855941382, DiffRad: 158.542698679102898, DirRad: 91.491375865774472, ExpectedMrt: 45.550078367141737, ExpectedIr: 31.250317859250515),
                (Tair: 17.189582327838608, RelHum: 71.186137066005443, SolarElev: 11.504534052118368, Wst: 32.581141856048248, Hst: 40.659313492203943, BodyA: 0.458209657646018, GrRef: 0.331062657219152, DiffRad: 246.851934831670974, DirRad: 316.300436166128122, ExpectedMrt: 34.129687703790864, ExpectedIr: 15.341530714753503),
                (Tair: 23.494745838033417, RelHum: 79.419837959956340, SolarElev: 5.883146927936905, Wst: 21.357176525432809, Hst: 4.412231359059397, BodyA: 0.491680433738709, GrRef: 0.189731533616846, DiffRad: 161.293677071406108, DirRad: 378.164603487731029, ExpectedMrt: 33.669163215273329, ExpectedIr: 19.200576015434763),
                (Tair: 8.967789713689083, RelHum: 39.069999572374449, SolarElev: 50.886184896759701, Wst: 20.850384950986054, Hst: 52.902686947647354, BodyA: 0.434189176273467, GrRef: 0.193899854646306, DiffRad: 249.142990181534373, DirRad: 751.223784196874021, ExpectedMrt: 32.986713743003236, ExpectedIr: 7.607011369428051),
                (Tair: 11.225771103556465, RelHum: 94.137668783316457, SolarElev: 63.534343891818857, Wst: 18.445381073791992, Hst: 11.772218437188712, BodyA: 0.684782928687627, GrRef: 0.181611684479397, DiffRad: 200.881480728694783, DirRad: 793.024734705593005, ExpectedMrt: 42.043701966917354, ExpectedIr: 7.716281787427135),
                (Tair: -14.692292653548508, RelHum: 44.399150282638502, SolarElev: 19.938179092107148, Wst: 38.579115422516836, Hst: 11.237784497598236, BodyA: 0.696829339516338, GrRef: 0.224343963158390, DiffRad: 288.914825326725861, DirRad: 723.833486832597373, ExpectedMrt: 29.117267349532142, ExpectedIr: -23.468124410543908),
                (Tair: 39.164905444153156, RelHum: 84.827426964435517, SolarElev: 67.869321778954614, Wst: 38.803957640012463, Hst: 6.940249504593541, BodyA: 0.646477466728073, GrRef: 0.159753756327363, DiffRad: 146.452953701114637, DirRad: 298.018741105326683, ExpectedMrt: 52.180274450529680, ExpectedIr: 39.762457594621651),
                (Tair: 33.426612832349257, RelHum: 73.421650606903540, SolarElev: 24.091520733273477, Wst: 39.731300587371749, Hst: 56.937218235413773, BodyA: 0.761681279852631, GrRef: 0.369954764128102, DiffRad: 179.215194818654282, DirRad: 450.921216214941069, ExpectedMrt: 60.388062897544046, ExpectedIr: 32.882788991152381),
                (Tair: 4.550316618949672, RelHum: 56.151901423558627, SolarElev: 10.742991191200442, Wst: 31.153062511656216, Hst: 16.230100955866188, BodyA: 0.642259458194283, GrRef: 0.165123901825281, DiffRad: 101.873498855148043, DirRad: 545.992576133203897, ExpectedMrt: 22.735200700954977, ExpectedIr: -0.880716511217429),
                (Tair: -13.395131319253096, RelHum: 37.415739627169359, SolarElev: 10.521675061326338, Wst: 27.895014930091364, Hst: 32.413358509824441, BodyA: 0.542131212012728, GrRef: 0.284126695712427, DiffRad: 339.807844520198955, DirRad: 709.508875839654593, ExpectedMrt: 27.025553075991297, ExpectedIr: -16.704494974895454),
                (Tair: -11.159034887007264, RelHum: 15.863648205105200, SolarElev: 40.189160681255352, Wst: 29.766213076989423, Hst: 15.654555363796828, BodyA: 0.752295565861379, GrRef: 0.332567670753447, DiffRad: 305.304785442648893, DirRad: 67.698457475383094, ExpectedMrt: 17.543237346406727, ExpectedIr: -17.796787355973009),
                (Tair: 1.329747973074909, RelHum: 28.554839799592301, SolarElev: 78.154879811614251, Wst: 21.427018545822197, Hst: 32.902877430864791, BodyA: 0.592857614465030, GrRef: 0.375224466908180, DiffRad: 210.627982132863139, DirRad: 25.773295491463927, ExpectedMrt: 16.572043696184551, ExpectedIr: -1.174111474754682),
                (Tair: -3.894240921766061, RelHum: 93.935083676500142, SolarElev: 49.402949476084686, Wst: 47.951311631313708, Hst: 24.189168830225775, BodyA: 0.656617840902026, GrRef: 0.324403963926371, DiffRad: 202.442461521507767, DirRad: 236.193491666070969, ExpectedMrt: 18.893622240279569, ExpectedIr: -9.315680468816765),
                (Tair: -1.447876263722026, RelHum: 10.796893936654461, SolarElev: 45.047177691321394, Wst: 42.395385084131412, Hst: 23.123308556859779, BodyA: 0.593216467196377, GrRef: 0.347380925086193, DiffRad: 143.337274532767907, DirRad: 715.645148520177827, ExpectedMrt: 31.355053122615232, ExpectedIr: -7.989631253558457)
            };

            for (int i = 0; i < cases.Length; i++)
            {
                var c = cases[i];
                var weather = new Weather
                {
                    DryBulbTemp = new[] { c.Tair },
                    RelativeHumidity = new[] { c.RelHum },
                    SolarElevation = new List<double> { c.SolarElev },
                    Wst = c.Wst,
                    Hst = c.Hst,
                    BodyA = c.BodyA,
                    GrRef = c.GrRef
                };

                var result = MRT.GetMRTForPointViaKessling(weather, 0, c.DiffRad, c.DirRad);

                Assert.True(Math.Abs(result[0] - c.ExpectedMrt) < 1e-9,
                    $"Case {i + 1}: MRT mismatch. Expected {c.ExpectedMrt}, got {result[0]}.");
                Assert.True(Math.Abs(result[1] - c.ExpectedIr) < 1e-9,
                    $"Case {i + 1}: IR mismatch. Expected {c.ExpectedIr}, got {result[1]}.");
            }
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

        [RhinoRequiredTheory]
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

        [NotWindowsServerFact]
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

        [RhinoRequiredFact]
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

        [RhinoRequiredFact]
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

        [RhinoRequiredTheory]
        [InlineData(0, 355, 5)]
        [InlineData(45, 90, 45)]
        public void WindFactors_DistanceBetween_ReturnsExpected(int dir1, int dir2, int expected)
        {
            var res = EddyLib.OutdoorComfort.WindSystem.DistanceBetweenWindDirs(dir1, dir2);

            Assert.Equal(expected, res);
        }

        [RhinoRequiredFact]
        public void WindFactors_ReturnNextLowerIndex_0_Return315()
        {
            // Arrange
            var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };

            // Act
            var res = EddyLib.OutdoorComfort.WindSystem.ReturnNextLowerIndex(windDirList.ToArray(), 0);

            // Assert

            Assert.Equal(7, res);
        }

        [RhinoRequiredTheory]
        [InlineData(5.0, 10.0, 1.0, 3.0, 2.89)]
        [InlineData(2.89, 3.0, 1.0, 10.0, 5.0)]
        public void ScaleABL_ReturnsExpected(double uref, double zref, double z0, double height, double expected)
        {
            var res = EddyLib.BCs.BC.ScaleABL(uref, zref, z0, height);

            Assert.Equal(expected, Math.Round(res, 2));
        }

        [RhinoRequiredFact]
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

        private readonly string RadianceBinPath = DefaultDirectoriesAndPaths.RadianceBinDir;
        private readonly string RadianceLibPath = DefaultDirectoriesAndPaths.RadianceLibDir;

        private static readonly string[] RadianceExecutableNames = new string[]
        {
            "rfluxmtx",
            "epw2wea",
            "gendaymtx",
            "dctimestep",
            "oconv",
            "rcontrib",
            "rmtxop"
        };

        [RhinoRequiredFact]
        public void IsRadianceInstalled()
        {
            // Skip if Radiance is not installed (don't fail on machines without it)
            if (!Directory.Exists(DefaultDirectoriesAndPaths.RadianceDir))
            {
                return; // Radiance not installed — nothing to verify
            }

            // Verify the installation is complete
            DefaultDirectoriesAndPaths.CheckRadiance();

            string ext = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows) ? ".exe" : "";

            foreach (var name in RadianceExecutableNames)
            {
                string filePath = Path.Combine(RadianceBinPath, name + ext);
                Assert.True(File.Exists(filePath), $"Executable not found: {filePath}");
            }
        }

        [RhinoRequiredFact]
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
            // Verify and return the base directory (parent of bin)
            DefaultDirectoriesAndPaths.CheckRadiance();
            return Path.GetDirectoryName(DefaultDirectoriesAndPaths.RadianceBinDir);
        }



        [NotWindowsServerFact]
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

            var mrt = new MRT(workingdir, Mesh.CreateFromBox(box2, 100, 100, 100), sky, vf, weather, MRT.MRTType.RadianceTwoPhaseDDS, points.ToArray(), true, GetRadianceDir())
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
        [RhinoRequiredTheory]
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
        //[RhinoRequiredTheory]
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
        [RhinoRequiredFact]
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

        /// <summary>
        /// Regression baseline for UTCI CalcUTCI polynomial.
        /// Expected values were computed from the legacy Math.Pow implementation on the dev branch.
        /// Any refactoring of the UTCI equation must reproduce these values within 1e-9 tolerance.
        /// </summary>
        [Fact]
        public void UTCI_CalcUTCI_MatchesLegacyMathPowBaseline()
        {
            var cases = new[]
            {
                (TaC: 20.0, RH: 50.0, Wsp: 1.0, MRT: 20.0, Expected: 19.380148093259553),
                (TaC: 22.0, RH: 40.0, Wsp: 0.5, MRT: 25.0, Expected: 22.212925694438137),
                (TaC: 18.0, RH: 60.0, Wsp: 2.0, MRT: 18.0, Expected: 16.188539036894525),
                (TaC: 35.0, RH: 30.0, Wsp: 1.0, MRT: 50.0, Expected: 38.34701370096742),
                (TaC: 40.0, RH: 80.0, Wsp: 0.5, MRT: 55.0, Expected: 62.66754326664106),
                (TaC: 30.0, RH: 70.0, Wsp: 3.0, MRT: 40.0, Expected: 32.68223605321507),
                (TaC: 38.0, RH: 20.0, Wsp: 5.0, MRT: 60.0, Expected: 41.36734003961241),
                (TaC: 33.0, RH: 50.0, Wsp: 2.0, MRT: 45.0, Expected: 36.20848596526606),
                (TaC: -5.0, RH: 80.0, Wsp: 3.0, MRT: -5.0, Expected: -13.711170585778499),
                (TaC: -15.0, RH: 50.0, Wsp: 5.0, MRT: -10.0, Expected: -32.10060687529533),
                (TaC: 0.0, RH: 30.0, Wsp: 1.0, MRT: 0.0, Expected: -1.3365241775851404),
                (TaC: 5.0, RH: 90.0, Wsp: 7.0, MRT: 5.0, Expected: -11.334700930783931),
                (TaC: -10.0, RH: 20.0, Wsp: 2.0, MRT: -5.0, Expected: -14.61129068428623),
                (TaC: 25.0, RH: 50.0, Wsp: 10.0, MRT: 30.0, Expected: 18.803142884399882),
                (TaC: 15.0, RH: 40.0, Wsp: 15.0, MRT: 20.0, Expected: -6.091491860660974),
                (TaC: 30.0, RH: 60.0, Wsp: 17.0, MRT: 35.0, Expected: 24.622038458380803),
                (TaC: 20.0, RH: 50.0, Wsp: 1.0, MRT: 60.0, Expected: 31.22884304267038),
                (TaC: 10.0, RH: 50.0, Wsp: 1.0, MRT: 50.0, Expected: 23.662373808259968),
                (TaC: 25.0, RH: 50.0, Wsp: 2.0, MRT: 70.0, Expected: 35.5518031550996),
                (TaC: -5.0, RH: 50.0, Wsp: 1.0, MRT: 30.0, Expected: 7.314357383521779),
                (TaC: 0.0, RH: 95.0, Wsp: 0.5, MRT: 10.0, Expected: 5.402900682077781),
                (TaC: 40.0, RH: 10.0, Wsp: 0.5, MRT: 40.0, Expected: 38.17783043752492),
                (TaC: 10.0, RH: 50.0, Wsp: 0.5, MRT: 10.0, Expected: 10.617561929501276),
                (TaC: 28.0, RH: 85.0, Wsp: 1.0, MRT: 35.0, Expected: 32.85194958301161),
                (TaC: 15.0, RH: 30.0, Wsp: 8.0, MRT: 25.0, Expected: 4.290809042087781),
            };

            for (int i = 0; i < cases.Length; i++)
            {
                var c = cases[i];
                double actual = EddyLib.UTCI.CalcUTCI(c.TaC, c.RH, c.Wsp, c.MRT);
                Assert.True(Math.Abs(actual - c.Expected) < 1e-9,
                    $"Case {i + 1}: UTCI mismatch (TaC={c.TaC}, RH={c.RH}, Wsp={c.Wsp}, MRT={c.MRT}). Expected {c.Expected}, got {actual}.");
            }
        }

        #endregion

        #region DDS File Parsing Tests

        /// <summary>
        /// Tests that LoadDDSIll correctly parses illumination files with leading whitespace
        /// and tab-separated values. This test verifies the fix for a regression where
        /// Skip(1) incorrectly skipped the first data value after RemoveEmptyEntries.
        /// </summary>
        [RhinoRequiredFact]
        public void LoadDDSIll_ParsesCorrectNumberOfColumns()
        {
            // Arrange - Create a test .ill file with the Radiance format
            var workingDir = TestFixtures.CreateTestDirectory("dds-parsing-test");
            var radDir = Path.Combine(workingDir, "Rad", "output");
            Directory.CreateDirectory(radDir);

            var illFilePath = Path.Combine(radDir, "test.ill");
            var illContent = @"#?RADIANCE
NROWS=3
NCOLS=5
FORMAT=ascii

 1.0	2.0	3.0	4.0	5.0	
 6.0	7.0	8.0	9.0	10.0	
 11.0	12.0	13.0	14.0	15.0	
";
            File.WriteAllText(illFilePath, illContent);

            // Act - Parse using reflection since LoadDDSIll is private
            var radiationSystemType = typeof(EddyLib.Radiation.RadiationSystem);
            var loadDDSIllMethod = radiationSystemType.GetMethod("LoadDDSIll",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.NotNull(loadDDSIllMethod);

            var result = (float[][])loadDDSIllMethod.Invoke(null, new object[] { illFilePath });

            // Assert
            Assert.Equal(3, result.Length); // 3 rows
            Assert.Equal(5, result[0].Length); // 5 columns per row
            Assert.Equal(1.0f, result[0][0]);
            Assert.Equal(5.0f, result[0][4]);
            Assert.Equal(15.0f, result[2][4]);

            // Cleanup
            Directory.Delete(workingDir, true);
        }

        #endregion
    }
}
