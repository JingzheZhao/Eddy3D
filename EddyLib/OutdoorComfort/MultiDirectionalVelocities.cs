using Rhino.Geometry;
using System;
using System.IO;
using System.Text;

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