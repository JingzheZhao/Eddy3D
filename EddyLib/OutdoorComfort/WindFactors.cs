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
}
