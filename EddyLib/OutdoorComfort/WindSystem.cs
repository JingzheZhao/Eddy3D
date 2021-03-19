using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.OutdoorComfort
{
    public class WindSystem
    {
        public int[] ClstSimDirs = new int[8760];

        public int[] ClstSimDirIndices = new int[8760];

        public int[] WindDirOffset = new int[8760];

        public double WindDirOffSetAverage = 0;

        public WindSystem(Weather w, List<int> SimulatedWindDirections)
        {
            var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = GetClosestWindDirs(w, SimulatedWindDirections);
            this.WindDirOffset = OffSet.ToArray();
            this.WindDirOffSetAverage = OffSetAverage;
            this.ClstSimDirs = ClstSimDirs.ToArray();
            this.ClstSimDirIndices = SimDirIndices.ToArray();
        }

        public static double ScaleABL(double URefEPW, double zref, double z0, double probingHeight)
        {
            double zGround = 0;
            var Kappa = 0.41;

            var U_star = Kappa * URefEPW / (Math.Log((zref + z0) / z0));

            return U_star / Kappa * Math.Log((probingHeight - zGround + z0) / z0);
        }

        public static Tuple<List<int>, List<int>, List<int>, double> GetClosestWindDirs(Weather weather, List<int> SimulatedWindDirections)
        {
            var offSet = new List<int>();
            var clstSimIndices = new List<int>();
            var clstSimDirs = new List<int>();

            var windDirsEPW = weather.WindDirection;

            for (int h = 0; h < 8760; h++)
            {
                int weatherDir = (int)weather.WindDirection[h];
                int closestIndex = 0;
                var distance = 0;

                // Treat 360 as 0 and add that right away if it exists
                if (weatherDir == 360 && SimulatedWindDirections.Contains(0))
                {
                    closestIndex = 0;

                    distance = 0;
                    offSet.Add(distance);
                    clstSimIndices.Add(closestIndex);
                    clstSimDirs.Add(SimulatedWindDirections[closestIndex]);

                    continue;
                }

                // Check what is closest for all other cases

                if (SimulatedWindDirections.Contains(weatherDir))
                {
                    closestIndex = SimulatedWindDirections.IndexOf(weather.WindDirection[h]);
                }
                else
                {
                    var nextIndexDown = ReturnNextLowerIndex(SimulatedWindDirections, (int)windDirsEPW[h]);
                    var nextIndexUp = ReturnNextUpperIndex(SimulatedWindDirections, (int)windDirsEPW[h]);

                    var nextDirDown = SimulatedWindDirections[nextIndexDown];
                    var nextDirUp = SimulatedWindDirections[nextIndexUp];

                    double distanceToLower = Math.Abs(windDirsEPW[h] - nextDirDown);
                    double distanceToUpper = Math.Abs(windDirsEPW[h] - nextDirUp);

                    closestIndex = distanceToLower < distanceToUpper ? nextIndexDown : nextIndexUp;
                }

                distance = Math.Abs(SimulatedWindDirections[closestIndex] - weatherDir);
                offSet.Add(distance);
                clstSimIndices.Add(closestIndex);
                clstSimDirs.Add(SimulatedWindDirections[closestIndex]);
            }

            return new Tuple<List<int>, List<int>, List<int>, double>(clstSimIndices, clstSimDirs, offSet, offSet.Average());
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
    }
}