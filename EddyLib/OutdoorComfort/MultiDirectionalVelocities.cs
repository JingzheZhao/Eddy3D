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

namespace EddyLib
{
    // This is a post-processing class
    public class WindComfort
    {
        public double[] ValuesPedestrianWindComfort;

        public WindComfort(WindFactorsAnnual wf, PCIdx pcidxx)
        {
            this.ValuesPedestrianWindComfort = CalcPedestrianComfort(wf.ValuesTemporal, pcidxx);
        }

        public enum PCIdx
        {
            LawsonGeneral,

            LawsonLDDC,

            Lawson2001,

            Davenport,

            NEN8100,
        };

        private double[] CalcPedestrianComfort(double[,] ValuesWindFactors, WindComfort.PCIdx cmftidx)
        {
            int sensorPointCount = ValuesWindFactors.GetLength(1);

            var PedestrianWindComfort = new double[sensorPointCount];

            for (int probe = 0; probe < sensorPointCount; probe++)
            {
                // column is all hours of the year
                var column = ArrayHelper.CustomArray<double>.GetColumn(ValuesWindFactors, probe);

                if (cmftidx == EddyLib.WindComfort.PCIdx.Davenport)
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcDavenportComfort(column);
                }
                else if (cmftidx == EddyLib.WindComfort.PCIdx.LawsonGeneral)
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcLawsonGeneralComfort(column);
                }
                else if (cmftidx == EddyLib.WindComfort.PCIdx.LawsonLDDC)
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcLawsonLDDCComfort(column);
                }
                else if (cmftidx == EddyLib.WindComfort.PCIdx.Lawson2001)
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcLawson2001Comfort(column);
                }
                else
                {
                    PedestrianWindComfort[probe] = WindComfortMetrics.CalcNEN8100Comfort(column);
                }
            }

            return PedestrianWindComfort;
        }
    }

    // All this does is placing the U Datatree into an array and writing it to disk
    public class MultiDirectionalVelocities
    {
        //[probes, windDirs]  Vector3d[,] Probes;

        public Vector3d[,] Values;

        public int[] WindDirs;

        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        public bool infValues;

        public MultiDirectionalVelocities(string workingDir, int[] windDirs, Vector3d[,] vectors, bool truncateDoubles, bool recalc, int truncateTo = 1)

        {
            var csvAnnualVelProbes = workingDir + "MultiDirectionalVelocities.csv";
            var binAnnualVelProbes = workingDir + "MultiDirectionalVelocities.bin";

            this.infValues = CheckForInfValues(vectors);

            if (File.Exists(binAnnualVelProbes) && !recalc)
            {
                var temp = RadianceFiles.loadBinDVectors(binAnnualVelProbes, out windDirs);

                if (temp.GetLength(0) == vectors.GetLength(0))
                {
                    this.Values = temp;
                    this.WindDirs = windDirs;
                    this.resultPrecalculated = true;
                    this.wrongNumberOfProbes = false;
                }
                else
                {
                    this.wrongNumberOfProbes = true;
                    this.resultPrecalculated = false;
                }
            }
            if (recalc)
            {
                if (File.Exists(csvAnnualVelProbes))
                {
                    File.Delete(csvAnnualVelProbes);
                }
                if (File.Exists(binAnnualVelProbes))
                {
                    File.Delete(binAnnualVelProbes);
                }

                this.Values = vectors;
                this.WindDirs = windDirs;

                RadianceFiles.writeBinVectors(binAnnualVelProbes, vectors, windDirs);
                Write2CSV(windDirs, vectors, csvAnnualVelProbes, truncateDoubles, truncateTo);

                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        private bool CheckForInfValues(Vector3d[,] vectors)
        {
            bool infValues = false;
            foreach (Vector3d vec in vectors)
            {
                if (vec.Length > 10000) { infValues = true; }
            }
            return infValues;
        }

        private void Write2CSV(int[] windDirs, Vector3d[,] vectors, string filePath, bool truncateDoubles, int truncateBy = 1)
        {
            //writing output to csv

            StringBuilder sb = new StringBuilder();

            string header = "";

            for (int d = 0; d <= vectors.GetUpperBound(1); d++)
            {
                header += windDirs[d] + ", , ,";
            }
            sb.AppendLine(header);

            if (!truncateDoubles)
            {
                truncateBy = 3;
            }
            else
            {
                truncateBy = 1;
            }

            for (int p = 0; p <= vectors.GetUpperBound(0); p++)
            {
                string content = "";
                for (int d = 0; d <= vectors.GetUpperBound(1); d++)
                {
                    // Filter extreme values
                    if (vectors[p, d].Length > 10000)
                    {
                        content += "0 , 0 , 0 , ";
                        continue;
                    }
                    else
                    {
                        var X = Math.Round(vectors[p, d].X, truncateBy).ToString();
                        var Y = Math.Round(vectors[p, d].Y, truncateBy).ToString();
                        var Z = Math.Round(vectors[p, d].Z, truncateBy).ToString();
                        content += X + "," + Y + "," + Z + ",";
                    }
                }
                sb.AppendLine(content);
            }
            File.WriteAllText(filePath, sb.ToString());
        }
    }

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