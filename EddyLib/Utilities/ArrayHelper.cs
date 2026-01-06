using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib
{
    /// <summary>
    /// Helper methods for array operations.
    /// </summary>
    public static class ArrayHelper
    {
        /// <summary>
        /// Generic class for extracting rows and columns from 2D arrays.
        /// </summary>
        public static class CustomArray<T>
        {
            /// <summary>
            /// Gets a column from a 2D array.
            /// </summary>
            public static T[] GetColumn(T[,] matrix, int columnNumber)
            {
                return Enumerable.Range(0, matrix.GetLength(0))
                        .Select(x => matrix[x, columnNumber])
                        .ToArray();
            }

            /// <summary>
            /// Gets a row from a 2D array.
            /// </summary>
            public static T[] GetRow(T[,] matrix, int rowNumber)
            {
                return Enumerable.Range(0, matrix.GetLength(1))
                        .Select(x => matrix[rowNumber, x])
                        .ToArray();
            }
        }

        /// <summary>
        /// Converts a list of lists to a 2D array.
        /// </summary>
        public static T[,] To2dArray<T>(this List<List<T>> list)
        {
            if (list.Count == 0 || list[0].Count == 0)
                throw new ArgumentException("The list must have non-zero dimensions.");

            var result = new T[list.Count, list[0].Count];
            for (int i = 0; i < list.Count; i++)
            {
                for (int j = 0; j < list[i].Count; j++)
                {
                    if (list[i].Count != list[0].Count)
                        throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = list[i][j];
                }
            }

            return result;
        }

        /// <summary>
        /// Converts a jagged array to a 2D array.
        /// </summary>
        public static T[,] To2D<T>(T[][] source)
        {
            try
            {
                int FirstDim = source.Length;
                int SecondDim = source.GroupBy(row => row.Length).Single().Key;

                var result = new T[FirstDim, SecondDim];
                for (int i = 0; i < FirstDim; ++i)
                    for (int j = 0; j < SecondDim; ++j)
                        result[i, j] = source[i][j];

                return result;
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("The given jagged array is not rectangular.");
            }
        }

        /// <summary>
        /// Converts all elements of a 2D array using a converter function.
        /// </summary>
        public static TOutput[,] ConvertAll<TInput, TOutput>(TInput[,] array, Func<TInput, TOutput> converter)
        {
            int length0 = array.GetLength(0);
            int length1 = array.GetLength(1);

            var result = new TOutput[length0, length1];

            for (int i = 0; i < length0; i++)
                for (int j = 0; j < length1; j++)
                    result[i, j] = converter(array[i, j]);

            return result;
        }

        /// <summary>
        /// Creates an empty jagged matrix.
        /// </summary>
        public static object[][] CreateJaggedMatrix(int rows, int columns)
        {
            object[][] matrix = new object[rows][];

            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[i] = new object[columns];
            }

            return matrix;
        }

        /// <summary>
        /// Transposes rows and columns of a 2D double array.
        /// </summary>
        public static double[,] TransposeRowsAndColumns(double[,] arr)
        {
            int rowCount = arr.GetLength(0);
            int columnCount = arr.GetLength(1);
            double[,] transposed = new double[columnCount, rowCount];
            if (rowCount == columnCount)
            {
                transposed = (double[,])arr.Clone();
                for (int i = 1; i < rowCount; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        double temp = transposed[i, j];
                        transposed[i, j] = transposed[j, i];
                        transposed[j, i] = temp;
                    }
                }
            }
            else
            {
                for (int column = 0; column < columnCount; column++)
                {
                    for (int row = 0; row < rowCount; row++)
                    {
                        transposed[column, row] = arr[row, column];
                    }
                }
            }
            return transposed;
        }

        /// <summary>
        /// Extracts a slice from a 1D array.
        /// </summary>
        public static T[] Slice<T>(T[] source, int fromIdx, int toIdx)
        {
            T[] ret = new T[toIdx - fromIdx + 1];
            for (int srcIdx = fromIdx, dstIdx = 0; srcIdx <= toIdx; srcIdx++)
            {
                ret[dstIdx++] = source[srcIdx];
            }
            return ret;
        }

        /// <summary>
        /// Extracts a slice from a 2D array.
        /// </summary>
        public static T[,] Slice<T>(T[,] source, int fromIdxRank0, int toIdxRank0, int fromIdxRank1, int toIdxRank1)
        {
            T[,] ret = new T[toIdxRank0 - fromIdxRank0 + 1, toIdxRank1 - fromIdxRank1 + 1];

            for (int srcIdxRank0 = fromIdxRank0, dstIdxRank0 = 0; srcIdxRank0 <= toIdxRank0; srcIdxRank0++, dstIdxRank0++)
            {
                for (int srcIdxRank1 = fromIdxRank1, dstIdxRank1 = 0; srcIdxRank1 <= toIdxRank1; srcIdxRank1++, dstIdxRank1++)
                {
                    ret[dstIdxRank0, dstIdxRank1] = source[srcIdxRank0, srcIdxRank1];
                }
            }
            return ret;
        }

        /// <summary>
        /// Rotates an array by a specified distance using Span.
        /// </summary>
        /// <remarks>
        /// Based on: https://outcompute.com/2019/02/23/algorithm-cyclic-rotation-in-csharp/
        /// </remarks>
        public static T[] RotateBySpanCopy<T>(T[] input, int distance)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
            if (input.Length == 0) return new T[0];

            var result = new T[input.Length];
            var target = new Span<T>(result);
            var diff = distance % input.Length;
            var source1 = new Span<T>(input, 0, input.Length - diff);
            source1.CopyTo(target.Slice(diff, input.Length - diff));
            var source2 = new Span<T>(input, input.Length - diff, diff);
            source2.CopyTo(target.Slice(0, diff));

            return result;
        }
    }
}
