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
    public class WindFactors
    {
        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        public string fileNameBinExtension = ".bin";

        public string interpolationPref = "ip";

        public static double[,] VectorLengths(int sensorPointCount, int numberOfWindDirs, Vector3d[,] vectorProbes)
        {
            var velocities = new double[sensorPointCount, numberOfWindDirs];

            Parallel.For(0, numberOfWindDirs, d =>
            {
                for (int p = 0; p < sensorPointCount; p++)
                {
                    velocities[p, d] = vectorProbes[p, d].Length;
                }
            });

            return velocities;
        }
    }

    public class WindFactorsSpatial : WindFactors
    {
        // This is just the ratio
        public double[,] ValuesSpatial;

        public BoundaryCondition BCond;

        private readonly string fileName = @"WF_S";

        public List<int> SimulatedWindDirections;

        public WindFactorsSpatial(string baseWorkingDir, BoundaryCondition bcond, MultiDirectionalVelocities velocityProbes, List<Point3d> probes, bool interpolate, bool recalc)
        {
            this.BCond = bcond;
            string binWFSpatial = interpolate == false ? Path.Combine(baseWorkingDir + fileName + fileNameBinExtension) : Path.Combine(baseWorkingDir + fileName + fileNameBinExtension);

            this.SimulatedWindDirections = bcond.windDirs;

            if (File.Exists(binWFSpatial) && !recalc)
            {
                try
                {
                    this.ValuesSpatial = RadianceFiles.loadBinD(binWFSpatial);
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
                if (File.Exists(binWFSpatial))
                {
                    File.Delete(binWFSpatial);
                }

                this.ValuesSpatial = CalcWindFactorsSpatial(velocityProbes.Values, bcond, probes);

                RadianceFiles.writeBin(binWFSpatial, this.ValuesSpatial);

                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        // This returns the plain annual array

        public static float[] CalcWindFactorsSpatialSP(WProbe Probe)
        {
            var windDirsSim = Probe.WindDirections;

            int numberOfWindDirs = windDirsSim.Count();

            float[] WFSpatial = new float[numberOfWindDirs];

            int cnt = 0;

            Console.WriteLine("Calculating: Wind reduction factors");

            // Independent of hour

            var velSimAtProbingHeight = BoundaryCondition.ScaleABL(Probe.Uref, Probe.Zref, Probe.Z0, Probe.Point.Value.Z);

            for (int w = 0; w < numberOfWindDirs; w++)
            {
                var velSimProbingPoint = Probe.U[w].Value.Length;

                // Avoid Infinity
                var velSimRatio = velSimAtProbingHeight == 0 ? 0.00000 : velSimProbingPoint / velSimAtProbingHeight;

                WFSpatial[w] = (float)velSimRatio;

                cnt++;
            }

            return WFSpatial;
        }

        private static double[,] CalcWindFactorsSpatial(Vector3d[,] MultiDirectionalVelocities, BoundaryCondition bcond, List<Point3d> probes)
        {
            var windDirsSim = bcond.windDirs;

            int numberOfWindDirs = windDirsSim.Count();
            int numberOfSensors = MultiDirectionalVelocities.GetLength(0);

            double[,] WFSpatial = new double[numberOfSensors, numberOfWindDirs];

            int round = 2;

            int cnt = 0;

            #region progressbar

            using (var progress = new ASCIIProgressBar())
            {
                #endregion progressbar

                // Create lookup table with plain vector magnitudes

                var MultiDirectionalVelMags = VectorLengths(numberOfSensors, numberOfWindDirs, MultiDirectionalVelocities);

                Console.WriteLine("Calculating: Wind reduction factors");

                Parallel.For(
                  0, numberOfSensors, p =>
                  {
                      // Independent of hour
                      var velSimAtProbingHeight = 0.0;
                      if (bcond is ABL)
                      {
                          ABL casted_bc = (ABL)bcond;
                          velSimAtProbingHeight = BoundaryCondition.ScaleABL(casted_bc.URef, casted_bc.zref, casted_bc.z0, probes[p].Z);
                      }
                      else
                      {
                          ConstU casted_bc = (ConstU)bcond;

                          // assume a zref of 10;
                          velSimAtProbingHeight = BoundaryCondition.ScaleABL(casted_bc.URef, 10, bcond.z0, probes[p].Z);
                      }

                      for (int w = 0; w < numberOfWindDirs; w++)
                      {
                          var velSimProbingPoint = MultiDirectionalVelMags[p, w];

                          // Avoid Infinity
                          var velSimRatio = velSimAtProbingHeight == 0 ? 0.00000 : velSimProbingPoint / velSimAtProbingHeight;

                          // We need to multiply the normalized velocity with respect to the approaching flow
                          // for every probing point and multiply that with the scaled-down, measured airport velocity.

                          WFSpatial[p, w] = Math.Round(velSimRatio, round);

                          cnt++;

                          #region progressbar

                          progress.Report((double)cnt / numberOfSensors);

                          #endregion progressbar
                      }
                  });
            }

            return WFSpatial;
        }
    }

    public class WindFactorsTemporal : WindFactors
    {
        // This is the ration times the velocity in the weather file
        public double[,] ValuesTemporalAtProbingHeight;

        private readonly string fileName = @"WF_A";

        private readonly string del = "_";

        public Weather weather;

        public WindFactorsTemporal(string baseWorkingDir, BoundaryCondition bcond, Weather weather, WindFactorsSpatial wfspatial, List<Point3d> probes, bool interpolate, bool recalc)
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

        private void CalcWindFactorsTemporal(double[,] WFSpatial, BoundaryCondition bcond, Weather weather, List<Point3d> probes, bool interpolate)
        {
            var windDirsSim = bcond.windDirs;
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
                      var velEPWAtProbingHeight = 0.0;
                      if (bcond is ABL)
                      {
                          ABL casted_bc = (ABL)bcond;
                          velEPWAtProbingHeight = BoundaryCondition.ScaleABL(weather.WindSpeed[h], casted_bc.zref, casted_bc.z0, probes[p].Z);
                      }
                      else
                      {
                          ConstU casted_bc = (ConstU)bcond;

                          // assume a zref of 10;
                          velEPWAtProbingHeight = BoundaryCondition.ScaleABL(weather.WindSpeed[h], 10, bcond.z0, probes[p].Z);
                      }

                      var ratioSimProbingPoint = WFSpatial[p, bcond.ClstSimDirIndices[h]];

                      // We need to multiply the normalized velocity with respect to the approaching flow
                      // for every probing point and multiply that with the scaled-down, measured airport velocity.

                      if (!interpolate)
                      {
                          ValuesTemporalAtProbingHeight[h, p] = Math.Round(velEPWAtProbingHeight * ratioSimProbingPoint, round);
                      }
                      else
                      {
                          #region Interpolation

                          var IdxBelow = BoundaryCondition.ReturnNextLowerIndex(windDirsSim, (int)windDirsEPW[h]);
                          var IdxAbove = BoundaryCondition.ReturnNextUpperIndex(windDirsSim, (int)windDirsEPW[h]);
                          int dirBelow = windDirsSim[IdxBelow];
                          int dirAbove = windDirsSim[IdxAbove];
                          var distanceToLower = BoundaryCondition.DistanceBetweenWindDirs(windDirsEPW[h], dirBelow);
                          var distanceToUpper = BoundaryCondition.DistanceBetweenWindDirs(windDirsEPW[h], dirAbove);

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
            var windDirsSim = Probe.WindDirections.ToList();
            var windDirsEPW = weather.WindDirection;

            int numberOfHours = 8760;

            var WindFactorsTemporal = new float[numberOfHours];

            // Create lookup table with plain vector magnitudes

           // Console.WriteLine("Calculating: Wind reduction factors");

            for (int h = 0; h < numberOfHours; h++)
            {
                var velEPWAtProbingHeight = 0.0;

                velEPWAtProbingHeight = WindSystem.ScaleABL(weather.WindSpeed[h], Probe.Zref, Probe.Z0, Probe.Point.Value.Z);

                var ratioSimProbingPoint = Probe.WindFactorsSpatial[WS.ClstSimDirIndices[h]];

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
                    var IdxAbove = WindSystem.ReturnNextUpperIndex(windDirsSim, (int)windDirsEPW[h]);
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