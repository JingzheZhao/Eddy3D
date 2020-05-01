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

namespace EddyLib
{
    // This is a post-processing class
    public class WindComfort
    {
        public double[] ValuesPedestrianWindComfort;

        public WindComfort(WindFactorsTemporal wf, PCIdx pcidxx)
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
        public static double ScaleABL(double URefEPW, BoundaryConditions bcond, double probingHeight)
        {
            var zref = bcond.zref;
            var z0 = bcond.z0;
            var Kappa = 0.41;

            var U_star = Kappa * URefEPW / (Math.Log((zref + z0) / z0));

            return U_star / Kappa * Math.Log((probingHeight - 0 + z0) / z0);
        }

        public static int DistanceBetweenWindDirs(int dir1, int dir2)
        {
            var vec2 = Utilities.Dir2Vec(dir1);
            var vec1 = Utilities.Dir2Vec(dir2);

            return (int)Math.Abs(Utilities.AngleBetweenVectors(vec1, vec2));
        }

        public static int ReturnNextLowerIndex(List<int> list, int compareTo)
        {
            int lowerIndex;

            if (compareTo <= list.Min())
            {
                lowerIndex = list.IndexOf(list.Max());
            }
            else
            {
                // Take everything smaller than compare
                var smaller = list.Where(x => x < compareTo);

                // Take the max from that selection and then take the index
                lowerIndex = list.IndexOf(smaller.Max(y => y));
            }

            return lowerIndex;
        }

        public static int ReturnNextUpperIndex(List<int> list, int compareTo)
        {
            // If values to compare if larger than everything in the list, return the first in the
            // list which is usually 0

            int upperIndex;

            if (compareTo >= list.Max())
            {
                upperIndex = list.IndexOf(list.Min());
            }
            else
            {
                // Take everything larger than compare
                var larger = list.Where(x => x > compareTo);

                // Take the min from that selection and then take the index
                upperIndex = list.IndexOf(larger.Min(y => y));
            }
            return upperIndex;
        }

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

        public WindFactorsSpatial(string baseWorkingDir, BoundaryConditions bcond, MultiDirectionalVelocities velocityProbes, List<Point3d> probes, bool interpolate, bool recalc)
        {
            string csvWFSpatial = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + fileNameCSVExtension) : Path.Combine(baseWorkingDir + fileName + del + fileNameCSVExtension);
            string binWFSpatial = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + fileNameBinExtension) : Path.Combine(baseWorkingDir + fileName + del + fileNameBinExtension);

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

        private static double[,] CalcWindFactorsSpatial(Vector3d[,] MultiDirectionalVelocities, BoundaryConditions bcond, List<Point3d> probes)
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
                      var velSimAtProbingHeight = ScaleABL(bcond.URef, bcond, probes[p].Z);

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
        public int[] ClstSimDirs { get; set; }

        public int[] Indices { get; set; }

        public int[] OffSet { get; set; }

        public double OffSetAverage { get; set; }

        // This is the ration times the velocity in the weather file
        public double[,] ValuesTemporal;

        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        private readonly string fileName = @"WF_A";

        private readonly string fileNameCSVExtension = ".csv";

        private readonly string fileNameBinExtension = ".bin";

        private readonly string interpolationPref = "ip";

        private readonly string del = "_";

        public WindFactorsTemporal(string baseWorkingDir, BoundaryConditions bcond, Weather weather, WindFactorsSpatial wfspatial, List<Point3d> probes, bool interpolate, bool recalc)
        {
            string csvWFTemporal = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + fileNameCSVExtension) : Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + interpolationPref + fileNameCSVExtension);
            string binWFTemporal = interpolate == false ? Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + fileNameBinExtension) : Path.Combine(baseWorkingDir + fileName + del + weather.Location + del + interpolationPref + fileNameBinExtension);

            var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = GetClosestWindDirs(weather, bcond);
            this.OffSet = OffSet.ToArray();
            this.OffSetAverage = OffSetAverage;
            this.ClstSimDirs = ClstSimDirs.ToArray();
            this.Indices = SimDirIndices.ToArray();

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

                this.ValuesTemporal = CalcWindFactorsTemporal(wfspatial.ValuesSpatial, Indices, bcond, weather, probes, interpolate);

                RadianceFiles.writeBin(binWFTemporal, this.ValuesTemporal);
                ArrayHelper._2DArray2CSV(this.ValuesTemporal, csvWFTemporal, true, 1);

                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        // This returns the plain annual array

        private Tuple<List<int>, List<int>, List<int>, double> GetClosestWindDirs(Weather weather, BoundaryConditions bcond)
        {
            var offSet = new List<int>();
            var Indices = new List<int>();
            var clstSimDirs = new List<int>();

            var windDirsEPW = weather.WindDirection;
            var windDirSim = bcond.windDirs;

            for (int h = 0; h < 8760; h++)
            {
                int weatherDir = (int)weather.WindDirection[h];
                int closestIndex = 0;
                var distance = 0;

                // Treat 360 as 0 and add that right away if it exists
                if (weatherDir == 360 && bcond.windDirs.Contains(0))
                {
                    closestIndex = 0;

                    distance = 0;
                    offSet.Add(distance);
                    Indices.Add(closestIndex);
                    clstSimDirs.Add(bcond.windDirs[closestIndex]);

                    continue;
                }

                // Check what is closest for all other cases

                if (bcond.windDirs.Contains(weatherDir))
                {
                    closestIndex = windDirSim.IndexOf(weather.WindDirection[h]);
                }
                else
                {
                    var nextIndexDown = ReturnNextLowerIndex(windDirSim, (int)windDirsEPW[h]);
                    var nextIndexUp = ReturnNextUpperIndex(windDirSim, (int)windDirsEPW[h]);

                    var nextDirDown = windDirSim[nextIndexDown];
                    var nextDirUp = windDirSim[nextIndexUp];

                    double distanceToLower = Math.Abs(windDirsEPW[h] - nextDirDown);
                    double distanceToUpper = Math.Abs(windDirsEPW[h] - nextDirUp);

                    closestIndex = distanceToLower < distanceToUpper ? nextIndexDown : nextIndexUp;
                }

                distance = Math.Abs(bcond.windDirs[closestIndex] - weatherDir);
                offSet.Add(distance);
                Indices.Add(closestIndex);
                clstSimDirs.Add(bcond.windDirs[closestIndex]);
            }

            return new Tuple<List<int>, List<int>, List<int>, double>(Indices, clstSimDirs, offSet, offSet.Average());
        }

        private static double[,] CalcWindFactorsTemporal(double[,] WFSpatial, int[] clstSimDirIdx, BoundaryConditions bcond, Weather weather, List<Point3d> probes, bool interpolate)
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
                          var velEPWAtProbingHeight = ScaleABL(weather.WindSpeed[h], bcond, probes[p].Z);

                          var ratioSimProbingPoint = WFSpatial[p, clstSimDirIdx[h]];

                          // We need to multiply the normalized velocity with respect to the approaching flow
                          // for every probing point and multiply that with the scaled-down, measured airport velocity.

                          if (!interpolate)
                          {
                              WFTemporal[h, p] = Math.Round(velEPWAtProbingHeight * ratioSimProbingPoint, round);
                          }
                          else
                          {
                              #region Interpolation

                              var IdxBelow = ReturnNextLowerIndex(windDirsSim, (int)windDirsEPW[h]);
                              var IdxAbove = ReturnNextUpperIndex(windDirsSim, (int)windDirsEPW[h]);
                              int dirBelow = windDirsSim[IdxBelow];
                              int dirAbove = windDirsSim[IdxAbove];
                              var distanceToLower = DistanceBetweenWindDirs(windDirsEPW[h], dirBelow);
                              var distanceToUpper = DistanceBetweenWindDirs(windDirsEPW[h], dirAbove);

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