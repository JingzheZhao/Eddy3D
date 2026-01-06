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
            string binWFSpatial = interpolate == false ? Path.Combine(baseWorkingDir + fileName + fileNameBinExtension) : Path.Combine(baseWorkingDir + fileName + fileNameBinExtension);

            this.SimulatedWindDirections = bcond.WindDirections;

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
                      for (int w = 0; w < numberOfWindDirs; w++)
                      {
                          // Independent of hour
                          var velSimAtProbingHeight = 0.0;

                          // Need to find the corresponding BCond for each hour.

                          bool allABL = bcond.BCs.All(item => item is ABL); // Checks if all items are of type ABL

                          if (allABL)
                          {
                              // We assume a probing height of z = 2m
                              ABL casted_bc = (ABL)bcond.BCs[w];
                              velSimAtProbingHeight = BC.ScaleABL(casted_bc.URef, casted_bc.zref, casted_bc.z0, 2);
                          }
                          else
                          {
                              ConstU casted_bc = (ConstU)bcond.BCs[w];
                              // We assume a probing height of z = 2m
                              // assume a zref of 10;
                              velSimAtProbingHeight = BC.ScaleABL(casted_bc.URef, 10, casted_bc.z0, 2);
                          }

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
}
