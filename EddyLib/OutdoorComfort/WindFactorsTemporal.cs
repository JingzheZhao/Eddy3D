using EddyLib.BCs;
using EddyLib.Helpers;
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
        private const int HoursPerYear = 8760;
        private const string FileName = "WF_A";
        private const string Delimiter = "_";

        // This is the ratio times the velocity in the weather file
        public double[,] ValuesTemporalAtProbingHeight;

        public Weather weather;

        public WindFactorsTemporal(string baseWorkingDir, BCCollection bcond, Weather weather, WindFactorsSpatial wfspatial, List<Point3d> probes, bool interpolate, bool recalc)
        {
            this.weather = weather;
            string binWFTemporal = BuildTemporalCachePath(baseWorkingDir, weather, interpolate);

            if (File.Exists(binWFTemporal) && !recalc)
            {
                try
                {
                    var cached = RadianceFiles.loadBinD(binWFTemporal);
                    if (IsCacheCompatible(cached, probes?.Count ?? 0))
                    {
                        this.ValuesTemporalAtProbingHeight = cached;
                        this.resultPrecalculated = true;
                        this.wrongNumberOfProbes = false;
                    }
                    else
                    {
                        this.ValuesTemporalAtProbingHeight = null;
                        this.resultPrecalculated = false;
                        this.wrongNumberOfProbes = true;
                    }
                }
                catch (Exception)
                {
                    this.resultPrecalculated = false;
                    this.wrongNumberOfProbes = true;
                    throw;
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

        private string BuildTemporalCachePath(string baseWorkingDir, Weather weatherData, bool interpolate)
        {
            string cacheDir = DirectoryHelpers.EnsureTrailingBackslash(baseWorkingDir);
            string locationToken = SanitizeFileToken(weatherData?.Location);
            var baseFileName = FileName + Delimiter + locationToken + Delimiter;
            var fileSuffix = interpolate ? interpolationPref + fileNameBinExtension : fileNameBinExtension;
            return Path.Combine(cacheDir, baseFileName + fileSuffix);
        }

        private static string SanitizeFileToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return "UnknownLocation";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var clean = new string(token
                .Select(ch => invalidChars.Contains(ch) ? '_' : ch)
                .ToArray())
                .Trim();

            return string.IsNullOrWhiteSpace(clean) ? "UnknownLocation" : clean;
        }

        private static bool IsCacheCompatible(double[,] values, int probeCount)
        {
            if (values == null)
            {
                return false;
            }

            if (probeCount <= 0)
            {
                return false;
            }

            return values.GetLength(0) == HoursPerYear && values.GetLength(1) == probeCount;
        }

        // This returns the plain annual array

        private void CalcWindFactorsTemporal(double[,] WFSpatial, BCCollection bcond, Weather weather, List<Point3d> probes, bool interpolate)
        {
            var windDirsSim = bcond.WindDirections.ToArray();
            var windDirsEPW = weather.WindDirection;
            bool allABL = bcond.BCs.All(item => item is ABL);

            // Ensure ClstSimDirIndices is initialized for "No interpolation" mode
            // This handles cases where BCCollection was created without an EPW file path
            if (!interpolate && bcond.ClstSimDirIndices.All(idx => idx == 0) && windDirsSim.Length > 1)
            {
                var (indices, _, _, _) = WindSystem.GetClosestWindDirs(weather, windDirsSim);
                Array.Copy(indices, bcond.ClstSimDirIndices, HoursPerYear);
            }

            int numberOfSensors = WFSpatial.GetLength(0);

            int numberOfHours = HoursPerYear;
            int round = 2;

            ValuesTemporalAtProbingHeight = new double[numberOfHours, numberOfSensors];
            var closestSimDirectionIndices = bcond.ClstSimDirIndices;
            var windSpeedAtProbingHeightByHour = new double[numberOfHours];
            var lowerIndices = interpolate ? new int[numberOfHours] : null;
            var higherIndices = interpolate ? new int[numberOfHours] : null;
            var lowerWeights = interpolate ? new double[numberOfHours] : null;
            var upperWeights = interpolate ? new double[numberOfHours] : null;

            for (int h = 0; h < numberOfHours; h++)
            {
                int clstIdx = closestSimDirectionIndices[h];

                if (allABL)
                {
                    var castedBc = (ABL)bcond.BCs[clstIdx];
                    windSpeedAtProbingHeightByHour[h] = BC.ScaleABL(weather.WindSpeed[h], castedBc.zref, castedBc.z0, 2);
                }
                else
                {
                    var castedBc = (ConstU)bcond.BCs[clstIdx];
                    windSpeedAtProbingHeightByHour[h] = BC.ScaleABL(weather.WindSpeed[h], 10, castedBc.z0, 2);
                }

                if (interpolate)
                {
                    var idxBelow = WindSystem.ReturnNextLowerIndex(windDirsSim, windDirsEPW[h]);
                    var idxAbove = WindSystem.ReturnNextHigherIndex(windDirsSim, windDirsEPW[h]);
                    int dirBelow = windDirsSim[idxBelow];
                    int dirAbove = windDirsSim[idxAbove];
                    int distanceToLower = WindSystem.DistanceBetweenWindDirs(windDirsEPW[h], dirBelow);
                    int distanceToUpper = WindSystem.DistanceBetweenWindDirs(windDirsEPW[h], dirAbove);

                    lowerIndices[h] = idxBelow;
                    higherIndices[h] = idxAbove;
                    lowerWeights[h] = 1 - (distanceToLower / (distanceToLower + distanceToUpper));
                    upperWeights[h] = 1 - (distanceToUpper / (distanceToLower + distanceToUpper));
                }
            }

            int processedSensors = 0;

            #region progressbar

            using var progress = new ASCIIProgressBar();

            #endregion progressbar

            // Create lookup table with plain vector magnitudes

            Console.WriteLine("Calculating: Wind reduction factors");

            Parallel.For(0, numberOfSensors, p =>
            {
                for (int h = 0; h < numberOfHours; h++)
                {
                    // We need to multiply the normalized velocity with respect to the approaching flow
                    // for every probing point and multiply that with the scaled-down, measured airport velocity.
                    double ratioSimProbingPoint;

                    if (!interpolate)
                    {
                        ratioSimProbingPoint = WFSpatial[p, closestSimDirectionIndices[h]];
                    }
                    else
                    {
                        ratioSimProbingPoint =
                            (WFSpatial[p, lowerIndices[h]] * lowerWeights[h]) +
                            (WFSpatial[p, higherIndices[h]] * upperWeights[h]);
                    }

                    ValuesTemporalAtProbingHeight[h, p] = Math.Round(windSpeedAtProbingHeightByHour[h] * ratioSimProbingPoint, round);
                }

                var processed = System.Threading.Interlocked.Increment(ref processedSensors);

                #region progressbar

                progress.Report((double)processed / numberOfSensors);

                #endregion progressbar
            });
        }

        public static float[] CalcWindFactorsTemporalSP(Weather weather, WProbe Probe, WindSystem WS, bool interpolate)
        {
            var windDirsSim = Probe.WindDirections;
            var windDirsEPW = weather.WindDirection;

            int numberOfHours = HoursPerYear;

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
