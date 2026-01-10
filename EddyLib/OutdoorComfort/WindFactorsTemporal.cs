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
        private const int HoursPerYear = 8760;
        private const string FileName = "WF_A";
        private const string Delimiter = "_";

        // This is the ratio times the velocity in the weather file
        public double[,] ValuesTemporalAtProbingHeight;

        public Weather weather;

        public WindFactorsTemporal(string baseWorkingDir, BCCollection bcond, Weather weather, WindFactorsSpatial wfspatial, List<Point3d> probes, bool interpolate, bool recalc)
        {
            this.weather = weather;
            var baseFileName = FileName + Delimiter + weather.Location + Delimiter;
            var fileSuffix = interpolate ? interpolationPref + fileNameBinExtension : fileNameBinExtension;
            string binWFTemporal = Path.Combine(baseWorkingDir, baseFileName + fileSuffix);

            if (File.Exists(binWFTemporal) && !recalc)
            {
                try
                {
                    this.ValuesTemporalAtProbingHeight = RadianceFiles.loadBinD(binWFTemporal);
                    this.resultPrecalculated = true;
                    this.wrongNumberOfProbes = false;
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

        // This returns the plain annual array

        private void CalcWindFactorsTemporal(double[,] WFSpatial, BCCollection bcond, Weather weather, List<Point3d> probes, bool interpolate)
        {
            var windDirsSim = bcond.WindDirections.ToArray();
            var windDirsEPW = weather.WindDirection;
            bool allABL = bcond.BCs.All(item => item is ABL);

            int numberOfSensors = WFSpatial.GetLength(0);

            int numberOfHours = HoursPerYear;
            int round = 2;

            ValuesTemporalAtProbingHeight = new double[numberOfHours, numberOfSensors];

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
                    // We assume a probing height of z = 2m
                    double velEPWAtProbingHeight;
                    var clstIdx = bcond.ClstSimDirIndices[h];

                    if (allABL)
                    {
                        var casted_bc = (ABL)bcond.BCs[clstIdx];
                        velEPWAtProbingHeight = BC.ScaleABL(weather.WindSpeed[h], casted_bc.zref, casted_bc.z0, 2);
                    }
                    else
                    {
                        var casted_bc = (ConstU)bcond.BCs[clstIdx];

                        // assume a zref of 10;
                        velEPWAtProbingHeight = BC.ScaleABL(weather.WindSpeed[h], 10, casted_bc.z0, 2);
                    }

                    // We need to multiply the normalized velocity with respect to the approaching flow
                    // for every probing point and multiply that with the scaled-down, measured airport velocity.
                    double ratioSimProbingPoint;

                    if (!interpolate)
                    {
                        ratioSimProbingPoint = WFSpatial[p, clstIdx];
                    }
                    else
                    {
                        #region Interpolation

                        var idxBelow = WindSystem.ReturnNextLowerIndex(windDirsSim, (int)windDirsEPW[h]);
                        var idxAbove = WindSystem.ReturnNextHigherIndex(windDirsSim, (int)windDirsEPW[h]);
                        int dirBelow = windDirsSim[idxBelow];
                        int dirAbove = windDirsSim[idxAbove];
                        var distanceToLower = WindSystem.DistanceBetweenWindDirs(windDirsEPW[h], dirBelow);
                        var distanceToUpper = WindSystem.DistanceBetweenWindDirs(windDirsEPW[h], dirAbove);

                        var weightingDown = 1 - (distanceToLower / (distanceToLower + distanceToUpper));
                        var weightingUp = 1 - (distanceToUpper / (distanceToLower + distanceToUpper));
                        var nextVelocityDown = WFSpatial[p, idxBelow];
                        var nextVelocityUp = WFSpatial[p, idxAbove];

                        ratioSimProbingPoint = (nextVelocityDown * weightingDown) + (nextVelocityUp * weightingUp);

                        #endregion Interpolation
                    }

                    ValuesTemporalAtProbingHeight[h, p] = Math.Round(velEPWAtProbingHeight * ratioSimProbingPoint, round);
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
