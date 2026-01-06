using EddyLib.BCs;
using EddyLib.Radiation;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EddyLib.OutdoorComfort
{
    public class WindFactorsTemporal : WindFactors
    {
        // This is the ration times the velocity in the weather file
        public double[,] ValuesTemporalAtProbingHeight;

        private readonly string fileName = @"WF_A";

        private readonly string del = "_";

        public Weather weather;

        public WindFactorsTemporal(string baseWorkingDir, BCCollection bcond, Weather weather, WindFactorsSpatial wfspatial, List<Point3d> probes, bool interpolate, bool recalc)
        {
            this.weather = weather;
            string binWFTemporal = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + fileNameBinExtension) : Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + interpolationPref + fileNameBinExtension);

            if (File.Exists(binWFTemporal) && !recalc)
            {
                try
                {
                    this.ValuesTemporalAtProbingHeight = RadianceFiles.loadBinD(binWFTemporal);
                    this.resultPrecalculated = true;
                    this.wrongNumberOfProbes = false;
                }
                catch (Exception e)
                {
                    this.resultPrecalculated = false;
                    this.wrongNumberOfProbes = true;
                    throw e;
                }
            }
            else
            {
                if (File.Exists(binWFTemporal))
                {
                    File.Delete(binWFTemporal);
                }

                CalcWindFactorsTemporal(wfspatial.ValuesSpatial, bcond, weather, probes, interpolate);

                RadianceFiles.writeBin(binWFTemporal, this.ValuesTemporalAtProbingHeight);

                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        // This returns the plain annual array

        private void CalcWindFactorsTemporal(double[,] WFSpatial, BCCollection bcond, Weather weather, List<Point3d> probes, bool interpolate)
        {
            var windDirsSim = bcond.WindDirections.ToArray();
            var windDirsEPW = weather.WindDirection;

            int numberOfWindDirs = windDirsSim.Count();
            int numberOfSensors = WFSpatial.GetLength(0);

            int numberOfHours = 8760;
            int round = 2;

            ValuesTemporalAtProbingHeight = new double[numberOfHours, numberOfSensors];

            int cnt = 0;

            #region progressbar

            using var progress = new ASCIIProgressBar();

            #endregion progressbar

            // Create lookup table with plain vector magnitudes

            Console.WriteLine("Calculating: Wind reduction factors");

            Parallel.For(
              0, numberOfSensors, p =>
              {
                  for (int h = 0; h < numberOfHours; h++)
                  {
                      // We assume a probing height of z = 2m
                      var velEPWAtProbingHeight = 0.0;

                      // Need to find the corresponding BCond for each hour.

                      bool allABL = bcond.BCs.All(item => item is ABL); // Checks if all items are of type ABL
                      var clstIdx = bcond.ClstSimDirIndices[h];

                      if (allABL)
                      {
                          ABL casted_bc = (ABL)bcond.BCs[clstIdx];
                          velEPWAtProbingHeight = BC.ScaleABL(weather.WindSpeed[h], casted_bc.zref, casted_bc.z0, 2);
                      }
                      else
                      {
                          ConstU casted_bc = (ConstU)bcond.BCs[clstIdx];

                          // assume a zref of 10;
                          velEPWAtProbingHeight = BC.ScaleABL(weather.WindSpeed[h], 10, casted_bc.z0, 2);
                      }

                      var ratioSimProbingPoint = WFSpatial[p, clstIdx];

                      // We need to multiply the normalized velocity with respect to the approaching flow
                      // for every probing point and multiply that with the scaled-down, measured airport velocity.

                      if (!interpolate)
                      {
                          ValuesTemporalAtProbingHeight[h, p] = Math.Round(velEPWAtProbingHeight * ratioSimProbingPoint, round);
                      }
                      else
                      {
                          #region Interpolation

                          var IdxBelow = WindSystem.ReturnNextLowerIndex(windDirsSim, (int)windDirsEPW[h]);
                          var IdxAbove = WindSystem.ReturnNextHigherIndex(windDirsSim, (int)windDirsEPW[h]);
                          int dirBelow = windDirsSim[IdxBelow];
                          int dirAbove = windDirsSim[IdxAbove];
                          var distanceToLower = WindSystem.DistanceBetweenWindDirs(windDirsEPW[h], dirBelow);
                          var distanceToUpper = WindSystem.DistanceBetweenWindDirs(windDirsEPW[h], dirAbove);

                          var weightingDown = 1 - (distanceToLower / (distanceToLower + distanceToUpper));
                          var weightingUp = 1 - (distanceToUpper / (distanceToLower + distanceToUpper));
                          var nextVelocityDown = WFSpatial[p, IdxBelow];
                          var nextVelocityUp = WFSpatial[p, IdxAbove];

                          double weightedRatioSimProbingPoint = (nextVelocityDown * weightingDown) + (nextVelocityUp * weightingUp);

                          #endregion Interpolation

                          ValuesTemporalAtProbingHeight[h, p] = Math.Round(velEPWAtProbingHeight * weightedRatioSimProbingPoint, round);
                      }

                      cnt++;

                      #region progressbar

                      progress.Report((double)cnt / numberOfSensors);

                      #endregion progressbar
                  }
              });
        }

        public static float[] CalcWindFactorsTemporalSP(Weather weather, WProbe Probe, WindSystem WS, bool interpolate)
        {
            var windDirsSim = Probe.WindDirections;
            var windDirsEPW = weather.WindDirection;

            int numberOfHours = 8760;

            var WindFactorsTemporal = new float[numberOfHours];

            // Create lookup table with plain vector magnitudes

            // Console.WriteLine("Calculating: Wind reduction factors");

            for (int h = 0; h < numberOfHours; h++)
            {
                var clstIdx = WS.ClstSimDirIndices[h];

                // We assume a probing height of z = 2m
                var velEPWAtProbingHeight = WindSystem.ScaleABL(weather.WindSpeed[h], Probe.Zref[clstIdx], Probe.Z0[clstIdx], 2);

                var ratioSimProbingPoint = Probe.WindFactorsSpatial[clstIdx];

                // We need to multiply the normalized velocity with respect to the approaching flow
                // for every probing point and multiply that with the scaled-down, measured airport velocity.

                if (!interpolate)
                {
                    WindFactorsTemporal[h] = (float)velEPWAtProbingHeight * ratioSimProbingPoint;
                }
                else
                {
                    #region Interpolation

                    var IdxBelow = WindSystem.ReturnNextLowerIndex(windDirsSim, (int)windDirsEPW[h]);
                    var IdxAbove = WindSystem.ReturnNextHigherIndex(windDirsSim, (int)windDirsEPW[h]);
                    int dirBelow = windDirsSim[IdxBelow];
                    int dirAbove = windDirsSim[IdxAbove];
                    var distanceToLower = WindSystem.DistanceBetweenWindDirs(windDirsEPW[h], dirBelow);
                    var distanceToUpper = WindSystem.DistanceBetweenWindDirs(windDirsEPW[h], dirAbove);

                    var weightingDown = 1 - (distanceToLower / (distanceToLower + distanceToUpper));
                    var weightingUp = 1 - (distanceToUpper / (distanceToLower + distanceToUpper));
                    var nextVelocityDown = Probe.WindFactorsSpatial[IdxBelow];
                    var nextVelocityUp = Probe.WindFactorsSpatial[IdxAbove];

                    double weightedRatioSimProbingPoint = (nextVelocityDown * weightingDown) + (nextVelocityUp * weightingUp);

                    #endregion Interpolation

                    WindFactorsTemporal[h] = (float)(velEPWAtProbingHeight * weightedRatioSimProbingPoint);
                }
            }

            return WindFactorsTemporal;
        }
    }
}
