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

        public WindSystem(Weather w, int[] SimulatedWindDirections)
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

        public static Tuple<int[], int[], int[], double> GetClosestWindDirs(Weather weather, int[] SimulatedWindDirections)
        {
            var offSet = new int[8760];
            var clstSimIndices = new int[8760];
            var clstSimDirs = new int[8760];

            var windDirsEPW = weather.WindDirection;

            for (int h = 0; h < 8760; h++)
            {
                int weatherDir = (int)weather.WindDirection[h];
                int closestIndex;
                int distance;

                // Treat 360 as 0 and add that right away if it exists
                if (weatherDir == 360 && SimulatedWindDirections.Contains(0))
                {
                    closestIndex = 0;
                    distance = 0;
                    offSet[h] = 0;
                    clstSimIndices[h] = closestIndex;
                    clstSimDirs[h] = (SimulatedWindDirections[closestIndex]);
                }

                // Check what is closest for all other cases
                else if (SimulatedWindDirections.Contains(weatherDir))
                {
                    closestIndex = Array.IndexOf(SimulatedWindDirections, weather.WindDirection[h]);
                    distance = 0;
                }
                else
                {
                    var nextIndexDown = ReturnNextLowerIndex(SimulatedWindDirections, (int)windDirsEPW[h]);
                    var nextIndexUp = ReturnNextHigherIndex(SimulatedWindDirections, (int)windDirsEPW[h]);

                    var nextDirDown = SimulatedWindDirections[nextIndexDown];
                    var nextDirUp = SimulatedWindDirections[nextIndexUp];

                    double distanceToLower = DistanceBetweenWindDirs(windDirsEPW[h], nextDirDown);
                    double distanceToUpper = DistanceBetweenWindDirs(windDirsEPW[h], nextDirUp);

                    closestIndex = distanceToLower < distanceToUpper ? nextIndexDown : nextIndexUp;

                    distance = DistanceBetweenWindDirs(SimulatedWindDirections[closestIndex], weatherDir);
                }

                offSet[h] = (distance);
                clstSimIndices[h] = closestIndex;
                clstSimDirs[h] = SimulatedWindDirections[closestIndex];
            }

            return new Tuple<int[], int[], int[], double>(clstSimIndices, clstSimDirs, offSet, offSet.Average());
        }

        public static int DistanceBetweenWindDirs(int dir1, int dir2)
        {
            var vec2 = Utilities.Dir2Vec(dir1);
            var vec1 = Utilities.Dir2Vec(dir2);

            double dist = Math.Abs(Math.Round(Utilities.AngleBetweenVectors(vec1, vec2)));

            int distInt = (int)dist;

            return distInt;
        }

        public static int ReturnNextLowerIndex(int[] list, int compareTo)
        {
            int lowerIndex;

            if (compareTo <= list.Min())
            {
                lowerIndex = Array.IndexOf(list, list.Max());
            }
            else
            {
                // Take everything smaller than compare
                var smaller = list.Where(x => x < compareTo).ToArray();

                // Take the max from that selection and then take the index
                lowerIndex = Array.IndexOf(list, smaller.Max(y => y));
            }

            return lowerIndex;
        }

        public static int ReturnNextHigherIndex(int[] list, int compareTo)
        {
            // Radial approach

            int upperIndex;

            // If compareTo is larger than max of array, take the smallest wind direction: 320 --> 0
            if (compareTo >= list.Max())
            {
                upperIndex = Array.IndexOf(list, list.Min());
            }
            else // 180 --> 225
            {
                // Take everything larger than compareTo
                var larger = list.Where(x => x > compareTo).ToArray();

                // Take the min from that selection and then take the index
                upperIndex = Array.IndexOf(list, larger.Min(y => y));
            }
            return upperIndex;
        }
    }
}