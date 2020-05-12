using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.BCs;
using EddyLib.OutdoorComfort;

using System;
using System.Collections.Generic;

using System.Globalization;
using System.IO;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

using EddyLib.OutdoorComfort.Metrics;
using Eto.Drawing;
using Rhino.Geometry;

using EddyLib.BCs;

namespace EddyLib.OutdoorComfort
{
    public class WindFactors
    {
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

        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        private readonly string fileName = @"WF_S";

        private readonly string fileNameCSVExtension = ".csv";

        private readonly string fileNameBinExtension = ".bin";

        private readonly string del = "_";

        public List<int> SimulatedWindDirections;

        public WindFactorsSpatial(string baseWorkingDir, BoundaryCondition bcond, MultiDirectionalVelocities velocityProbes, List<Point3d> probes, bool interpolate, bool recalc)
        {
            string csvWFSpatial = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + fileNameCSVExtension) : Path.Combine(baseWorkingDir + fileName + del + fileNameCSVExtension);
            string binWFSpatial = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + fileNameBinExtension) : Path.Combine(baseWorkingDir + fileName + del + fileNameBinExtension);

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
                var files = new List<string>()
                {
                     csvWFSpatial,
                     binWFSpatial
                };

                foreach (string s in files)
                {
                    if (File.Exists(s))
                    {
                        File.Delete(s);
                    }
                }

                this.ValuesSpatial = CalcWindFactorsSpatial(velocityProbes.Values, bcond, probes);

                RadianceFiles.writeBin(binWFSpatial, this.ValuesSpatial);
                ArrayHelper._2DArray2CSV(this.ValuesSpatial, csvWFSpatial, true, 1);

                RadianceFiles.writeBin(binWFSpatial, this.ValuesSpatial);
                ArrayHelper._2DArray2CSV(this.ValuesSpatial, csvWFSpatial, true, 1);

                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        // This returns the plain annual array

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

    public class WindFactorsAnnual : WindFactors
    {
        // This is the ration times the velocity in the weather file
        public double[,] ValuesTemporal;

        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        private readonly string fileName = @"WF_A";

        private readonly string fileNameCSVExtension = ".csv";

        private readonly string fileNameBinExtension = ".bin";

        private readonly string interpolationPref = "ip";

        private readonly string del = "_";

        public WindFactorsAnnual(string baseWorkingDir, BoundaryCondition bcond, Weather weather, WindFactorsSpatial wfspatial, List<Point3d> probes, bool interpolate, bool recalc)
        {
            string csvWFTemporal = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + fileNameCSVExtension) : Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + interpolationPref + fileNameCSVExtension);
            string binWFTemporal = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + fileNameBinExtension) : Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + interpolationPref + fileNameBinExtension);

            if (File.Exists(binWFTemporal) && !recalc)
            {
                try
                {
                    this.ValuesTemporal = RadianceFiles.loadBinD(binWFTemporal);
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
                var files = new List<string>()
                {
                     csvWFTemporal,
                     binWFTemporal,
                };

                foreach (string s in files)
                {
                    if (File.Exists(s))
                    {
                        File.Delete(s);
                    }
                }

                this.ValuesTemporal = CalcWindFactorsTemporal(wfspatial.ValuesSpatial, bcond, weather, probes, interpolate);

                RadianceFiles.writeBin(binWFTemporal, this.ValuesTemporal);
                ArrayHelper._2DArray2CSV(this.ValuesTemporal, csvWFTemporal, true, 1);

                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        // This returns the plain annual array

        private static double[,] CalcWindFactorsTemporal(double[,] WFSpatial, BoundaryCondition bcond, Weather weather, List<Point3d> probes, bool interpolate)
        {
            var windDirsSim = bcond.windDirs;
            var windDirsEPW = weather.WindDirection;

            int numberOfWindDirs = windDirsSim.Count();
            int numberOfSensors = WFSpatial.GetLength(0);

            int numberOfHours = 8760;
            int round = 2;

            double[,] WFTemporal = new double[numberOfHours, numberOfSensors];

            int cnt = 0;

            #region progressbar

            using (var progress = new ASCIIProgressBar())
            {
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
                              WFTemporal[h, p] = Math.Round(velEPWAtProbingHeight * ratioSimProbingPoint, round);
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

                              WFTemporal[h, p] = Math.Round(velEPWAtProbingHeight * weightedRatioSimProbingPoint, round);
                          }

                          cnt++;

                          #region progressbar

                          progress.Report((double)cnt / numberOfSensors);

                          #endregion progressbar
                      }
                  });
            }

            return WFTemporal;
        }
    }
}