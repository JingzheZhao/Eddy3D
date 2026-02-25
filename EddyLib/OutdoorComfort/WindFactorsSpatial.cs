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
    public class WindFactorsSpatial : WindFactors
    {
        // This is just the ratio
        public double[,] ValuesSpatial;

        public BCCollection BCond;

        private readonly string fileName = @"WF_S";

        public List<int> SimulatedWindDirections;

        public WindFactorsSpatial(string baseWorkingDir, BCCollection bcond, MultiDirectionalVelocities velocityProbes, List<Point3d> probes, bool interpolate, bool recalc)
        {
            this.BCond = bcond;
            string binWFSpatial = BuildSpatialCachePath(baseWorkingDir, interpolate);

            this.SimulatedWindDirections = bcond.WindDirections;

            if (File.Exists(binWFSpatial) && !recalc)
            {
                try
                {
                    var cached = RadianceFiles.loadBinD(binWFSpatial);
                    if (IsCacheCompatible(cached, probes?.Count ?? 0, bcond?.WindDirections?.Count ?? 0))
                    {
                        this.ValuesSpatial = cached;
                        this.resultPrecalculated = true;
                        this.wrongNumberOfProbes = false;
                    }
                    else
                    {
                        this.ValuesSpatial = null;
                        this.resultPrecalculated = false;
                        this.wrongNumberOfProbes = true;
                    }
                }
                catch
                {
                    this.resultPrecalculated = false;
                    this.wrongNumberOfProbes = true;
                    throw;
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

        private string BuildSpatialCachePath(string baseWorkingDir, bool interpolate)
        {
            string cacheDir = DirectoryHelpers.EnsureTrailingBackslash(baseWorkingDir);
            string suffix = interpolate ? interpolationPref + fileNameBinExtension : fileNameBinExtension;
            return Path.Combine(cacheDir, fileName + suffix);
        }

        private static bool IsCacheCompatible(double[,] values, int probeCount, int windDirCount)
        {
            if (values == null)
            {
                return false;
            }

            if (probeCount <= 0 || windDirCount <= 0)
            {
                return false;
            }

            return values.GetLength(0) == probeCount && values.GetLength(1) == windDirCount;
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

            for (int w = 0; w < numberOfWindDirs; w++)
            {
                var velSimAtProbingHeight = BC.ScaleABL(Probe.Uref[w], Probe.Zref[w], Probe.Z0[w], Probe.Point.Value.Z);

                var velSimProbingPoint = Probe.U[w].Value.Length;

                // Avoid Infinity
                var velSimRatio = velSimAtProbingHeight == 0 ? 0.00000 : velSimProbingPoint / velSimAtProbingHeight;

                WFSpatial[w] = (float)velSimRatio;

                cnt++;
            }

            return WFSpatial;
        }

        private static double[,] CalcWindFactorsSpatial(Vector3d[,] MultiDirectionalVelocities, BCCollection bcond, List<Point3d> probes)
        {
            var windDirsSim = bcond.WindDirections;

            int numberOfWindDirs = windDirsSim.Count();
            int numberOfSensors = MultiDirectionalVelocities.GetLength(0);

            double[,] WFSpatial = new double[numberOfSensors, numberOfWindDirs];

            int round = 2;
            int processedSensors = 0;

            #region progressbar

            using (var progress = new ASCIIProgressBar())
            {
                #endregion progressbar

                // Create lookup table with plain vector magnitudes

                var MultiDirectionalVelMags = VectorLengths(numberOfSensors, numberOfWindDirs, MultiDirectionalVelocities);
                bool allABL = bcond.BCs.All(item => item is ABL);
                var approachingVelocityAtProbingHeight = new double[numberOfWindDirs];

                for (int w = 0; w < numberOfWindDirs; w++)
                {
                    if (allABL)
                    {
                        // We assume a probing height of z = 2m
                        var castedBc = (ABL)bcond.BCs[w];
                        approachingVelocityAtProbingHeight[w] = BC.ScaleABL(castedBc.URef, castedBc.zref, castedBc.z0, 2);
                    }
                    else
                    {
                        var castedBc = (ConstU)bcond.BCs[w];
                        // We assume a probing height of z = 2m and a zref of 10m for ConstU.
                        approachingVelocityAtProbingHeight[w] = BC.ScaleABL(castedBc.URef, 10, castedBc.z0, 2);
                    }
                }

                Console.WriteLine("Calculating: Wind reduction factors");

                Parallel.For(
                  0, numberOfSensors, p =>
                  {
                      for (int w = 0; w < numberOfWindDirs; w++)
                      {
                          var velSimAtProbingHeight = approachingVelocityAtProbingHeight[w];
                          var velSimProbingPoint = MultiDirectionalVelMags[p, w];

                          // Avoid Infinity
                          var velSimRatio = velSimAtProbingHeight == 0 ? 0.00000 : velSimProbingPoint / velSimAtProbingHeight;

                          // We need to multiply the normalized velocity with respect to the approaching flow
                          // for every probing point and multiply that with the scaled-down, measured airport velocity.

                          WFSpatial[p, w] = Math.Round(velSimRatio, round);
                      }

                      #region progressbar

                      var processed = System.Threading.Interlocked.Increment(ref processedSensors);
                      progress.Report((double)processed / numberOfSensors);

                      #endregion progressbar
                  });
            }

            return WFSpatial;
        }
    }
}
