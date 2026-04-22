using EddyLib.Radiation;
using Rhino.Geometry;
using System;
using System.IO;
using System.Linq;

namespace EddyLib.OutdoorComfort
{
    public partial class MRT
    {
        private const int HoursPerYear = 8760;

        public MRT(string baseWorkingDir, Mesh BuildingGeometry, SkyTemperatureModel sky, SkyViewFactor vf, Weather weather, MRTType type, Point3d[] probes, bool recalc, string RadianceDir = null)
        {
            // Use provided dir or default
            RadianceDir = string.IsNullOrWhiteSpace(RadianceDir) ? DefaultDirectoriesAndPaths.RadianceDir : RadianceDir;

            // Verify Radiance
            DefaultDirectoriesAndPaths.CheckRadiance(RadianceDir);

            var binMRT = Path.Combine(baseWorkingDir, "MRT.bin");

            var numberOfProbes = probes.Length;

            #region TwoPhaseDDS

            // Add other files here
            if (!recalc && File.Exists(binMRT))
            {
                // Load radiation datasets [x][] time [][x] points

                var tempValues = RadianceFiles.loadBinD(binMRT);

                int sensorPointCountExisting = tempValues.GetLength(1);

                if (sensorPointCountExisting != numberOfProbes)
                {
                    this.wrongNumberOfProbes = true;
                    return;
                }
                else
                {
                    this.Values = tempValues;
                }
            }
            else if (recalc)
            {
                if (File.Exists(binMRT))
                {
                    File.Delete(binMRT);
                }

                //Utilities.CleanDirectory(baseWorkingDir + @"Rad\");
                //Utilities.CleanDirectory(baseWorkingDir + @"Output\");

                EddyLib.Radiation.TwoPhaseDDS dds = new EddyLib.Radiation.TwoPhaseDDS(baseWorkingDir, BuildingGeometry, probes.ToList(), weather, recalc, RadianceDir);

                this.SkyTemp = sky.Temp;

                this.ViewFactors = vf.Values;

                int numberOfHours = HoursPerYear;
                int numberOfSensors = probes.Length;

                var DDSTOTAL = dds.totalIll;

                this.Values = new double[numberOfHours, numberOfSensors];

                double sol_trans = 1;
                double f_bes = 0.5;

                System.Threading.Tasks.Parallel.For(0, HoursPerYear, h =>
                 {
                     // Bolt optimization: Cache array lookups outside the inner loop
                     // to avoid repetitive bounds checking and array indirection
                     double elev = weather.SolarElevation[h];
                     double azi = weather.SolarAzi[h];
                     double dryBulb = weather.DryBulbTemp[h];
                     double sTemp = sky.Temp[h];
                     double[] ddsTotalH = DDSTOTAL[h];

                     for (int p = 0; p < probes.Length; p++)
                     {
                         double dMRT;
                         double ERF;

                         SolarGain.ERF(elev, azi, SolarGain.Posture.standing, ddsTotalH[p], sol_trans, ViewFactors[p], f_bes, 0.6, out ERF, out dMRT);

                         var surfaceTempBuilding = dryBulb * (1 - ViewFactors[p]);
                         var skyTemp = sTemp * ViewFactors[p];

                         this.Values[h, p] = surfaceTempBuilding + dMRT + skyTemp;
                     }
                 });

                RadianceFiles.writeBin(binMRT, this.Values);
            }

            #endregion TwoPhaseDDS
        }

    }
}
