using EddyLib.Helpers;
using Rhino.Geometry;
using System.IO;
using System.Linq;

namespace EddyLib
{
    // All this does is placing the U Datatree into an array and writing it to disk
    public class MultiDirectionalVelocities
    {
        //[probes, windDirs]  Vector3d[,] Probes;

        public Vector3d[,] Values;

        public int[] WindDirs;

        public bool resultPrecalculated;

        public bool wrongNumberOfProbes;

        public bool infValues;

        // [WindDirections, Probes]

        public MultiDirectionalVelocities(string workingDir, int[] windDirs, Vector3d[,] vectors, bool truncateDoubles, bool recalc, int truncateTo = 1)

        {
            var binAnnualVelProbes = DirectoryHelpers.EnsureTrailingBackslash(workingDir) + "MultiDirectionalVelocities.bin";

            this.infValues = CheckForInfValues(vectors);

            if (File.Exists(binAnnualVelProbes) && !recalc)
            {
                var cachedVectors = RadianceFiles.loadBinDVectors(binAnnualVelProbes, out int[] cachedWindDirs);

                if (IsCacheCompatible(cachedVectors, vectors, cachedWindDirs, windDirs))
                {
                    this.Values = cachedVectors;
                    this.WindDirs = cachedWindDirs;
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
                if (File.Exists(binAnnualVelProbes))
                {
                    File.Delete(binAnnualVelProbes);
                }

                this.Values = vectors;
                this.WindDirs = windDirs;

                RadianceFiles.writeBinVectors(binAnnualVelProbes, vectors, windDirs);

                this.resultPrecalculated = false;
                this.wrongNumberOfProbes = false;
            }
        }

        private static bool IsCacheCompatible(
            Vector3d[,] cachedVectors,
            Vector3d[,] currentVectors,
            int[] cachedWindDirs,
            int[] currentWindDirs)
        {
            if (cachedVectors == null || currentVectors == null)
            {
                return false;
            }

            if (cachedVectors.GetLength(0) != currentVectors.GetLength(0))
            {
                return false;
            }

            if (cachedVectors.GetLength(1) != currentVectors.GetLength(1))
            {
                return false;
            }

            if (cachedWindDirs == null || currentWindDirs == null)
            {
                return false;
            }

            if (cachedWindDirs.Length != currentWindDirs.Length)
            {
                return false;
            }

            return cachedWindDirs.SequenceEqual(currentWindDirs);
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
    }
}
