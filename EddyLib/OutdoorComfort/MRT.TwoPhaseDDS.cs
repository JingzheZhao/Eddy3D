using EddyLib.Radiation;
using Rhino.Geometry;
using System;
using System.IO;
using System.Linq;

namespace EddyLib.OutdoorComfort
{
    public partial class MRT
    {
        public MRT(string baseWorkingDir, Mesh BuildingGeometry, SkyTemperatureModel sky, SkyViewFactor vf, Weather weather, MRTType type, Point3d[] probes, bool recalc, string RadianceDir = "C:\\Program Files\\Radiance\\")
        {
            var binMRT = baseWorkingDir + @"MRT.bin";

            var numberOfProbes = probes.Length;

            #region TwoPhaseDDS

            var difillFile = baseWorkingDir + @"\Output\annual_total.ill";
            var dirillFile = baseWorkingDir + @"\Output\annual_dir.ill";

            // Add other files here
            if (recalc == false && File.Exists(binMRT))
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
            else if (recalc == true)
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

                int numberOfHours = 8760;
                int numberOfSensors = probes.Length;

                var DDSTOTAL = dds.totalIll;

                this.Values = new double[numberOfHours, numberOfSensors];

                double sol_trans = 1;
                double f_bes = 0.5;

                System.Threading.Tasks.Parallel.For(0, 8760, h =>
                 {
                     for (int p = 0; p < probes.Length; p++)
                     {
                         double dMRT;
                         double ERF;

                         SolarGain.ERF(weather.SolarElevation[h], weather.SolarAzi[h], SolarGain.Posture.standing, DDSTOTAL[h][p], sol_trans, ViewFactors[p], f_bes, 0.6, out ERF, out dMRT);

                         var surfaceTempBuilding = weather.DryBulbTemp[h] * (1 - ViewFactors[p]);
                         var skyTemp = sky.Temp[h] * ViewFactors[p];

                         this.Values[h, p] = surfaceTempBuilding + dMRT + skyTemp;
                     }
                 });

                RadianceFiles.writeBin(baseWorkingDir + @"\MRT.bin", this.Values);
            }

            #endregion TwoPhaseDDS
        }

    }
}
