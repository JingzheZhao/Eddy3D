using Rhino.Geometry;
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

            // Bolt optimization: Parallelize by sensor point and iterate over wind directions in the inner loop.
            // This ensures contiguous memory access for both reading and writing (row-major),
            // eliminates false sharing between threads, and significantly improves cache locality.
            Parallel.For(0, sensorPointCount, p =>
            {
                for (int d = 0; d < numberOfWindDirs; d++)
                {
                    velocities[p, d] = vectorProbes[p, d].Length;
                }
            });

            return velocities;
        }
    }
}
