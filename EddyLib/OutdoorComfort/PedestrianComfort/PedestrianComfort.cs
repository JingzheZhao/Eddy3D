using EddyLib.OutdoorComfort.PedestrianComfort;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib
{
    public class PedestrianComfort
    {
        //[probes, windDirs]  Vector3d[,] Probes;

        public Vector3d[,] Values;

        public int[] WindDirs;

        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        public bool infValues;

        public enum PedestrianComfortIdx
        {
            LawsonGeneral,

            LawsonLDDC,

            Lawson2001,

            Davenport,

            NEN8100,
        };

        //        public Dictionary<String, List<String>> PedestrianComfortIndex = new Dictionary<string, List<string>>()
        //        // name, nickname, description
        //        {  { "Lawson", new List<string>(){ "Lawson Pedestrian Comfort", "LPC", @"Lawson Pedestrian Comfort

        //4: > 4 m/s ""Sitting"" Light breezes desired for outdoor restaurants and seating areas where one can read a paper of comfortably sit for long periods.
        //6: > 6 m/s ""Standing"" Gentle breezes suitable for main buildings entrances, pick-up/drop off points and bus stops.
        //8: > 8 m/s ""Leisure Walking or Strolling"" Moderate breezes that would be appropriate for walking down a city centre street, park or plaza.
        //10: > 10 m/s ""Business Walking"" Relatively high speeds that can be tolerated if ones objective is to walk, run or cycle without lingering.
        //12: > 12 m/s ""Uncomfortable"" Winds of this magnitude are considered a nuisance for most activities, and wind mitigation is typically recommended." } },

        //            { "Davenport", new List<string>(){ "Davenport Pedestrian Comfort", "DPC", @"Davenport Pedestrian Comfort

        //1 - A > 3.6 m/s < 1.5 % Sitting Long
        //2 - B > 5.3 m/s < 1.5 % Sitting Short
        //3 - C > 7.6 m/s < 1.5 % Walking Leisurely
        //4 - D > 9.8 m/s  < 1.5 % Walking Fast
        //5 - E > 9.8 m/s >= 1.5 % Uncomfortable
        //6 - S > 15.1 m/s >= 0.01 % Dangerous" } },

        //            { "NEN8100", new List<string>(){ "NEN 8100 Pedestrian Comfort", "NPC", @"NEN 8100 Pedestrian Comfort

        //1- A > 5 m/s < 2.5 % Sitting Long
        //2 - B > 5 m/s < 5 % Sitting Short
        //3 - C > 5 m/s < 10 % Walking Leisurely
        //4 - D > 5 m/s < 20 % Walking Fast
        //5 - E > 5 m/s > 20 % Uncomfortable
        //6 - S > 15 m/s > 0.05 % Dangerous" } }
        //        };

        public PedestrianComfort(int[] windDirs, Vector3d[,] vectors, string csvFilePath, bool truncateDoubles, bool recalc, int truncateBy = 1)
        {
            this.infValues = CheckForInfValues(vectors);

            if (File.Exists(csvFilePath) && !recalc)
            {
                var temp = ReadAnnualVelocitiesFromCSV(csvFilePath);

                if (temp.Item2.GetLength(0) == vectors.GetLength(0))
                {
                    this.Values = temp.Item2;
                    this.WindDirs = temp.Item1;
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
                if (File.Exists(csvFilePath))
                {
                    File.Delete(csvFilePath);
                }

                WriteAnnualVel2CSV(windDirs, vectors, csvFilePath, truncateDoubles, truncateBy);
                this.Values = ReadAnnualVelocitiesFromCSV(csvFilePath).Item2;
                this.WindDirs = ReadAnnualVelocitiesFromCSV(csvFilePath).Item1;
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

        public static Tuple<int[], Vector3d[,]> ReadAnnualVelocitiesFromCSV(string filePath)
        {
            // Todo: slow, fix later

            // data.GetUpperBound(1) wind dirs data.GetUpperBound(0) probes

            var data = RadianceFiles.readCSVFile(filePath);

            // Extract Header

            var WindDirs = new List<int>();

            for (int d = 0; d < data.GetUpperBound(1); d += 3)
            {
                WindDirs.Add((int)data[0, d]);
            }

            // data.GetUpperBound(1)+1/3 because last line always has an additional comma and we have
            // 3 vector components

            var Probes = new Vector3d[data.GetUpperBound(0), (data.GetUpperBound(1) + 1) / 3];

            // int p = 1 --> Skip header

            Parallel.For(1, data.GetUpperBound(0) + 1, p =>
            {
                // int d = 0;
                int cnt = 0;
                for (int d = 0; d < (data.GetUpperBound(1) + 1) / 3; d++)
                {
                    // p-1 because we want to start with 0
                    Probes[p - 1, d] = new Vector3d(data[p, cnt], data[p, cnt + 1], data[p, cnt + 2]);
                    cnt += 3;
                }
            });

            // Vector3d[,] res = new Vector3d[,]{{ new Vector3d(1, 1, 1)}};

            return new Tuple<int[], Vector3d[,]>(WindDirs.ToArray(), Probes);
        }

        public static void WriteAnnualVel2CSV(int[] windDirs, Vector3d[,] vectors, string filePath, bool truncateDoubles, int truncateBy = 1)
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
        public int[] ClstSimDirs { get; set; }

        public int[] Indices;

        public int[] offSet;

        public double offSetAverage;

        public double[,] ValuesWindFactors;

        public double[] ValuesPedestrianWindComfort;

        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        private string fileNameCSV = @"WindFactors";

        private string fileNameCSVExtension = ".csv";

        private string interpolationPref = "lp";

        private string del = "_";

        public WindFactors(string baseWorkingDir, BoundaryConditions bcond, Weather weather, PedestrianComfort velocityProbes, List<Point3d> probes, bool interpolate, bool recalc, PedestrianComfort.PedestrianComfortIdx cmftidx)
        {
            string csvWindFactors = interpolate == false ? Path.Combine(baseWorkingDir + fileNameCSV + del + weather.Location + del + fileNameCSVExtension) : Path.Combine(baseWorkingDir + fileNameCSV + del + weather.Location + del + interpolationPref + del + fileNameCSVExtension);

            if (File.Exists(csvWindFactors) && !recalc)
            {
                try
                {
                    this.ValuesWindFactors = ReadWindReductionArrayFromCSV(csvWindFactors);
                    this.resultPrecalculated = true;
                    this.wrongNumberOfProbes = false;

                    var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = GetClosestWindDirs(weather, bcond);
                    this.offSet = OffSet.ToArray();
                    this.offSetAverage = OffSet.Average();
                    this.ClstSimDirs = ClstSimDirs.ToArray();
                    this.Indices = SimDirIndices.ToArray();
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
                var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = GetClosestWindDirs(weather, bcond);
                this.offSet = OffSet.ToArray();
                this.offSetAverage = OffSet.Average();
                this.ClstSimDirs = ClstSimDirs.ToArray();
                this.Indices = SimDirIndices.ToArray();

                this.ValuesWindFactors = CalcWindReductionArray(velocityProbes.Values, Indices, bcond, weather, probes, interpolate);
                ArrayHelper._2DArray2CSV(this.ValuesWindFactors, csvWindFactors, true, 1);
                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }

            //var tempComfort = CalcPedestrianComfort(this.ValuesWindFactors);

            this.ValuesPedestrianWindComfort = CalcPedestrianComfort(this.ValuesWindFactors, cmftidx);
        }

        private double[] CalcPedestrianComfort(double[,] ValuesWindFactors, PedestrianComfort.PedestrianComfortIdx cmftidx)
        {
            int sensorPointCount = ValuesWindFactors.GetLength(1);

            var PedestrianWindComfort = new double[sensorPointCount];

            for (int probe = 0; probe < sensorPointCount; probe++)
            {
                // column is all hours of the year
                var column = ArrayHelper.CustomArray<double>.GetColumn(ValuesWindFactors, probe);

                if (cmftidx == PedestrianComfort.PedestrianComfortIdx.Davenport)
                {
                    PedestrianWindComfort[probe] = Metrics.CalcDavenportComfort(column);
                }
                else if (cmftidx == PedestrianComfort.PedestrianComfortIdx.LawsonGeneral)
                {
                    PedestrianWindComfort[probe] = Metrics.CalcLawsonGeneralComfort(column);
                }
                else if (cmftidx == PedestrianComfort.PedestrianComfortIdx.LawsonLDDC)
                {
                    PedestrianWindComfort[probe] = Metrics.CalcLawsonLDDCComfort(column);
                }
                else if (cmftidx == PedestrianComfort.PedestrianComfortIdx.Lawson2001)
                {
                    PedestrianWindComfort[probe] = Metrics.CalcLawson2001Comfort(column);
                }
                else
                {
                    PedestrianWindComfort[probe] = Metrics.CalcNEN8100Comfort(column);
                }
            }

            return PedestrianWindComfort;
        }

        private static double[,] VectorLengths(int sensorPointCount, int numberOfWindDirs, Vector3d[,] vectorProbes)
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

        public static double GetVelocityAtProbingHeightFromABL(double URefEPW, BoundaryConditions bcond, double probingHeight)
        {
            var zref = bcond.zref;
            var z0 = bcond.z0;

            var UAtProbingHeightFromEPW = ((0.41 * URefEPW) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((probingHeight + z0) / z0);
            return UAtProbingHeightFromEPW;
        }

        // This returns the plain annual array

        public static double[,] ReadWindReductionArrayFromCSV(string filePath)
        {
            return RadianceFiles.readCSVFile(filePath);
        }

        public Tuple<List<int>, List<int>, List<int>, double> GetClosestWindDirs(Weather weather, BoundaryConditions bcond)
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

        protected static int ReturnNextUpperIndex(List<int> list, int compareTo)
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

        private static double[,] CalcWindReductionArray(Vector3d[,] annualVecProbes, int[] clstSimDirIdx, BoundaryConditions bcond, Weather weather, List<Point3d> probes, bool interpolate)
        {
            var windDirSim = bcond.windDirs;
            var windDirsEPW = weather.WindDirection;

            int numberOfWindDirs = windDirSim.Count();
            int numberOfSensors = annualVecProbes.GetLength(0);

            int numberOfHours = 8760;
            int rounding = 1;

            double[,] WF = new double[numberOfHours, numberOfSensors];

            int cntReduction = 0;
            using (var progress = new ASCIIProgressBar())
            {
                // Create lookup table with vector lengths

                var annualVelocities = VectorLengths(numberOfSensors, numberOfWindDirs, annualVecProbes);

                Console.WriteLine("Calculating: Wind reduction factors");

                // Calculate indices first

                int[] nextIndexDown = new int[8760];
                int[] nextIndexUp = new int[8760];

                Parallel.For(0, numberOfHours, h =>
                {
                    nextIndexDown[h] = ReturnNextLowerIndex(windDirSim, (int)windDirsEPW[h]);
                    nextIndexUp[h] = ReturnNextUpperIndex(windDirSim, (int)windDirsEPW[h]);
                });

                Parallel.For(0, numberOfHours, h =>
        {
            for (int p = 0; p < numberOfSensors; p++)
            {
                int nextDirDown = windDirSim[nextIndexDown[h]];
                int nextDirUp = windDirSim[nextIndexUp[h]];

                var distanceToLower = DistanceBetweenWindDirs(windDirsEPW[h], nextDirDown);
                var distanceToUpper = DistanceBetweenWindDirs(windDirsEPW[h], nextDirUp);

                var velAtProbHeightEPW = GetVelocityAtProbingHeightFromABL(weather.WindSpeed[h], bcond, probes[p].Z);
                var velSim = annualVelocities[p, clstSimDirIdx[h]];
                var velApproaching = GetVelocityAtProbingHeightFromABL(bcond.URef, bcond, probes[p].Z);

                // Avoid Infinity

                var ratio = velApproaching == 0 ? 0.00000 : velSim / velApproaching;

                // We need to multiply the normalized velocity with respect to the approaching flow
                // for every probiing point and multiply that with the scaled-down, measured airport velocity.

                if (!interpolate)
                {
                    WF[h, p] = Math.Round(velAtProbHeightEPW * ratio, rounding);
                }
                else
                {
                    var weightingDown = 1 - (distanceToLower / (distanceToLower + distanceToUpper));
                    var weightingUp = 1 - (distanceToUpper / (distanceToLower + distanceToUpper));
                    var nextVelocityDown = annualVelocities[p, nextIndexDown[h]];
                    var nextVelocityUp = annualVelocities[p, nextIndexUp[h]];

                    double weightedVelSim = (nextVelocityDown * weightingDown) + (nextVelocityUp * weightingUp);

                    // Avoid Infinity
                    var ratioWeighted = velApproaching == 0 ? 0.00000 : weightedVelSim / velApproaching;

                    WF[h, p] = Math.Round(velAtProbHeightEPW * ratioWeighted, rounding);
                }

                cntReduction++;
                progress.Report((double)cntReduction / numberOfSensors);
            }
        });
            }

            return WF;
        }
    }
}