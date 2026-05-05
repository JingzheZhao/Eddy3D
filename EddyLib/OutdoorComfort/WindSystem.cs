using System;
using System.Linq;

namespace EddyLib.OutdoorComfort
{
    /// <summary>
    /// Wind direction mapping between EPW weather data and CFD simulation directions.
    /// </summary>
    public class WindSystem
    {
        #region Constants

        /// <summary>Hours in a year.</summary>
        private const int HoursPerYear = 8760;

        /// <summary>Von Karman constant for ABL calculations.</summary>
        private const double VonKarmanConstant = 0.41;

        #endregion

        #region Properties

        /// <summary>Closest simulated wind direction for each hour.</summary>
        public int[] ClosestSimulatedDirections { get; }

        /// <summary>Index into simulated directions array for each hour.</summary>
        public int[] ClosestSimulatedDirectionIndices { get; }

        /// <summary>Angular offset between EPW direction and closest simulated direction.</summary>
        public int[] WindDirectionOffset { get; }

        /// <summary>Average angular offset across all hours.</summary>
        public double WindDirectionOffsetAverage { get; }

        #endregion

        #region Backward Compatibility

        /// <summary>Legacy property - use ClosestSimulatedDirections.</summary>
        public int[] ClstSimDirs => ClosestSimulatedDirections;

        /// <summary>Legacy property - use ClosestSimulatedDirectionIndices.</summary>
        public int[] ClstSimDirIndices => ClosestSimulatedDirectionIndices;

        /// <summary>Legacy property - use WindDirectionOffset.</summary>
        public int[] WindDirOffset => WindDirectionOffset;

        /// <summary>Legacy property - use WindDirectionOffsetAverage.</summary>
        public double WindDirOffSetAverage => WindDirectionOffsetAverage;

        #endregion

        /// <summary>
        /// Creates a wind system mapping between EPW and simulation directions.
        /// </summary>
        public WindSystem(Weather weather, int[] simulatedWindDirections)
        {
            var result = MapWindDirections(weather, simulatedWindDirections);
            ClosestSimulatedDirectionIndices = result.Indices;
            ClosestSimulatedDirections = result.Directions;
            WindDirectionOffset = result.Offsets;
            WindDirectionOffsetAverage = result.AverageOffset;
        }

        #region ABL Scaling

        /// <summary>
        /// Scales wind speed using atmospheric boundary layer log-law profile.
        /// </summary>
        /// <param name="refVelocity">Reference velocity at reference height.</param>
        /// <param name="refHeight">Reference measurement height (m).</param>
        /// <param name="roughnessLength">Surface roughness length z0 (m).</param>
        /// <param name="targetHeight">Target height for velocity calculation (m).</param>
        public static double ScaleABL(double refVelocity, double refHeight, double roughnessLength, double targetHeight)
        {
            double uStar = VonKarmanConstant * refVelocity / Math.Log((refHeight + roughnessLength) / roughnessLength);
            return uStar / VonKarmanConstant * Math.Log((targetHeight + roughnessLength) / roughnessLength);
        }

        #endregion

        #region Wind Direction Mapping

        /// <summary>
        /// Maps EPW wind directions to closest simulated directions.
        /// </summary>
        private static (int[] Indices, int[] Directions, int[] Offsets, double AverageOffset) MapWindDirections(
            Weather weather, int[] simulatedDirections)
        {
            var offsets = new int[HoursPerYear];
            var indices = new int[HoursPerYear];
            var directions = new int[HoursPerYear];

            // Bolt optimization: Cache results for all 360 possible wind directions
            // to avoid redundant FindClosestDirection calls during 8760 hour loop.
            var cache = new (int Index, int Offset)[360];
            var isCached = new bool[360];

            for (int h = 0; h < HoursPerYear; h++)
            {
                int weatherDir = (int)weather.WindDirection[h];

                // Normalize direction to 0-359 range
                int normalizedDir = ((weatherDir % 360) + 360) % 360;

                if (!isCached[normalizedDir])
                {
                    cache[normalizedDir] = FindClosestDirection(normalizedDir, simulatedDirections);
                    isCached[normalizedDir] = true;
                }

                var (closestIndex, offset) = cache[normalizedDir];

                offsets[h] = offset;
                indices[h] = closestIndex;
                directions[h] = simulatedDirections[closestIndex];
            }

            return (indices, directions, offsets, offsets.Average());
        }

        /// <summary>
        /// Finds the closest simulated direction to a target direction.
        /// </summary>
        private static (int Index, int Offset) FindClosestDirection(int targetDirection, int[] simulatedDirections)
        {
            // Exact match
            int exactIndex = Array.IndexOf(simulatedDirections, targetDirection);
            if (exactIndex >= 0)
            {
                return (exactIndex, 0);
            }

            // Find bracketing directions
            int lowerIndex = FindNextLowerIndex(simulatedDirections, targetDirection);
            int upperIndex = FindNextHigherIndex(simulatedDirections, targetDirection);

            int lowerDir = simulatedDirections[lowerIndex];
            int upperDir = simulatedDirections[upperIndex];

            int distToLower = AngularDistance(targetDirection, lowerDir);
            int distToUpper = AngularDistance(targetDirection, upperDir);

            int closestIndex = distToLower <= distToUpper ? lowerIndex : upperIndex;
            int offset = AngularDistance(targetDirection, simulatedDirections[closestIndex]);

            return (closestIndex, offset);
        }

        /// <summary>
        /// Calculates the angular distance between two wind directions.
        /// </summary>
        public static int DistanceBetweenWindDirs(int dir1, int dir2)
        {
            return AngularDistance(dir1, dir2);
        }

        /// <summary>
        /// Calculates the angular distance between two directions (0-180°).
        /// </summary>
        private static int AngularDistance(int dir1, int dir2)
        {
            // Bolt optimization: Replace vector-based trig with modular arithmetic
            // avoids expensive Math.Sin, Math.Cos, and Math.Atan2 calls and allocations.
            int diff = Math.Abs(dir1 - dir2) % 360;
            return diff > 180 ? 360 - diff : diff;
        }

        /// <summary>
        /// Returns the index of the next lower direction in a circular sense.
        /// </summary>
        public static int ReturnNextLowerIndex(int[] list, int compareTo)
        {
            return FindNextLowerIndex(list, compareTo);
        }

        private static int FindNextLowerIndex(int[] list, int compareTo)
        {
            // Bolt optimization: Replaced O(N) LINQ with O(N) single-pass loop avoiding array allocations
            int max = list[0];
            int maxIdx = 0;
            int best = int.MinValue;
            int bestIdx = -1;

            for (int i = 0; i < list.Length; i++)
            {
                int val = list[i];
                if (val > max) { max = val; maxIdx = i; }
                if (val < compareTo && val > best) { best = val; bestIdx = i; }
            }

            return bestIdx != -1 ? bestIdx : maxIdx;
        }

        /// <summary>
        /// Returns the index of the next higher direction in a circular sense.
        /// </summary>
        public static int ReturnNextHigherIndex(int[] list, int compareTo)
        {
            return FindNextHigherIndex(list, compareTo);
        }

        private static int FindNextHigherIndex(int[] list, int compareTo)
        {
            // Bolt optimization: Replaced O(N) LINQ with O(N) single-pass loop avoiding array allocations
            int min = list[0];
            int minIdx = 0;
            int best = int.MaxValue;
            int bestIdx = -1;

            for (int i = 0; i < list.Length; i++)
            {
                int val = list[i];
                if (val < min) { min = val; minIdx = i; }
                if (val > compareTo && val < best) { best = val; bestIdx = i; }
            }

            return bestIdx != -1 ? bestIdx : minIdx;
        }

        #endregion

        #region Legacy Method

        /// <summary>
        /// Legacy method - use constructor instead.
        /// </summary>
        public static Tuple<int[], int[], int[], double> GetClosestWindDirs(Weather weather, int[] simulatedDirections)
        {
            var result = MapWindDirections(weather, simulatedDirections);
            return new Tuple<int[], int[], int[], double>(result.Indices, result.Directions, result.Offsets, result.AverageOffset);
        }

        #endregion
    }
}