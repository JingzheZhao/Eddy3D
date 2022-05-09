using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EddyLib

{
    public static class ArrayExtensions
    {
        public static void Set<T>(this Array array, T defaultValue)
        {
            int[] indicies = new int[array.Rank];

            SetDimension<T>(array, indicies, 0, defaultValue);
        }

        private static void SetDimension<T>(Array array, int[] indicies, int dimension, T defaultValue)
        {
            for (int i = 0; i <= array.GetUpperBound(dimension); i++)
            {
                indicies[dimension] = i;

                if (dimension < array.Rank - 1)
                    SetDimension<T>(array, indicies, dimension + 1, defaultValue);
                else
                    array.SetValue(defaultValue, indicies);
            }
        }
    }

    public static class ArrayHelper
    {
        //public class Benchmark
        //{
        //    [Params(10, 100, 1000, 10000)]
        //    public int size;

        // public double[][] data;

        // [GlobalSetup] public void Setup() { var rnd = new Random();

        // data = new double[size][]; for (var i = 0; i < size; i++) { data[i] = new double[size];
        // for (var j = 0; j < size; j++) { data[i][j] = rnd.NextDouble(); } } }

        // [Benchmark] public void ComputeTo2D() { var output = To2D(data); }

        // [Benchmark] public void ComputeTo2DFast() { var output = To2DFast(data); }

        // public static T[,] To2DFast<T>(T[][] source) where T : unmanaged { var dataOut = new
        // T[source.Length, source.Length]; var assertLength = source[0].Length;

        // unsafe { for (var i = 0; i < source.Length; i++) { if (source[i].Length != assertLength) {
        // throw new InvalidOperationException("The given jagged array is not rectangular."); }

        // fixed (T* pDataIn = source[i]) { fixed (T* pDataOut = &dataOut[i, 0]) {
        // CopyBlockHelper.SmartCopy<T>(pDataOut, pDataIn, assertLength); } } } }

        // return dataOut; }

        // public static T[,] To2D<T>(T[][] source) { try { var FirstDim = source.Length; var
        // SecondDim = source.GroupBy(row => row.Length).Single() .Key; // throws
        // InvalidOperationException if source is not rectangular

        // var result = new T[FirstDim, SecondDim]; for (var i = 0; i < FirstDim; ++i) for (var j =
        // 0; j < SecondDim; ++j) result[i, j] = source[i][j];

        //            return result;
        //        }
        //        catch (InvalidOperationException)
        //        {
        //            throw new InvalidOperationException("The given jagged array is not rectangular.");
        //        }
        //    }
        //}

        //public class Programm
        //{
        //    public static void Main(string[] args)
        //    {
        //        BenchmarkRunner.Run<Benchmark>();
        //        //            var rnd = new Random();
        //        //
        //        //            var size = 100;
        //        //            var data = new double[size][];
        //        //            for (var i = 0; i < size; i++) {
        //        //                data[i] = new double[size];
        //        //                for (var j = 0; j < size; j++) {
        //        //                    data[i][j] = rnd.NextDouble();
        //        //                }
        //        //            }
        //        //
        //        //            var outSafe = Benchmark.To2D(data);
        //        //            var outFast = Benchmark.To2DFast(data);
        //        //
        //        //            for (var i = 0; i < outSafe.GetLength(0); i++) {
        //        //                for (var j = 0; j < outSafe.GetLength(1); j++) {
        //        //                    if (outSafe[i, j] != outFast[i, j]) {
        //        //                        Console.WriteLine("Error at: {0}, {1}", i, j);
        //        //                    }
        //        //                }
        //        //            }
        //        //
        //        //            Console.WriteLine("All Good!");

        // }

        //public static class ArrayExt
        //{
        //    public static T[] GetRow<T>(this T[,] array, int row)
        //    {
        //        if (!typeof(T).IsPrimitive)
        //            throw new InvalidOperationException("Not supported for managed types.");

        // if (array == null) throw new ArgumentNullException("array");

        // int cols = array.GetUpperBound(1) + 1; T[] result = new T[cols];

        // int size;

        // if (typeof(T) == typeof(bool)) size = 1; else if (typeof(T) == typeof(char)) size = 2;
        // else size = Marshal.SizeOf<T>();

        // Buffer.BlockCopy(array, row * cols * size, result, 0, cols * size);

        //        return result;
        //    }
        //}

        public static double GH_NumberToDouble(GH_Number p)
        {
            return p.Value;
        }

        public static Vector3d GH_VectorToVector3d(GH_Vector vec)
        {
            return new Vector3d(vec.Value);
        }

        public static GH_Number DoubleToGH_Number(double p)
        {
            return new GH_Number(p);
        }

        public static GH_Vector Vector3dToGH_Vector(Vector3d vec)
        {
            return new GH_Vector(vec);
        }

        public static class CustomArray<T>
        {
            public static T[] GetColumn(T[,] matrix, int columnNumber)
            {
                return Enumerable.Range(0, matrix.GetLength(0))
                        .Select(x => matrix[x, columnNumber])
                        .ToArray();
            }

            public static T[] GetRow(T[,] matrix, int rowNumber)
            {
                return Enumerable.Range(0, matrix.GetLength(1))
                        .Select(x => matrix[rowNumber, x])
                        .ToArray();
            }
        }

        // The following extension method will allow you to initialise every value in an array, regardless of the number of dimensions.
        //    Use like so:
        //int[,,] test1 = new int[3, 4, 5];
        //    test1.Set(1);
        //int[,] test2 = new int[3, 4];
        //    test2.Set(1);
        //int[] test3 = new int[3];
        //    test3.Set(1);

        public static DataTree<T> ToTree<T>(List<List<T>> list)
        {
            DataTree<T> tree = new DataTree<T>();
            int i = 0;
            foreach (List<T> innerList in list)
            {
                tree.AddRange(innerList, new GH_Path(new int[] { 0, i }));
                i++;
            }
            return tree;
        }

        public static List<List<T>> ToListOfLists<T>(DataTree<T> tree)
        {
            List<List<T>> list = new List<List<T>>();

            foreach (List<T> b in tree.Branches)
            {
                list.Add(b);
            }
            return list;
        }

        //public static T[,] To2DArray<T>(DataTree<T> tree)
        //{
        //    T[,] result = new T[tree.Branch(0).Count, tree.BranchCount];

        //    for (int i = 0; i < tree.Branch(0).Count; i++)
        //    {
        //        for (int j = 0; j < tree.BranchCount; j++)
        //        {
        //            if (tree.Branch(j).Count != tree.Branch(0).Count) throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
        //            result[i, j] = tree[new GH_Path(j), i];
        //        }
        //    }
        //    return result;
        //}

        //public static Vector3d[,] To2DArray(DataTree<Vector3d> tree)
        //{
        //    Vector3d[,] result = new Vector3d[tree.Branch(0).Count, tree.BranchCount];

        //    for (int i = 0; i < tree.Branch(0).Count; i++)
        //    {
        //        for (int j = 0; j < tree.BranchCount; j++)
        //        {
        //            if (tree.Branch(j).Count != tree.Branch(0).Count) throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
        //            result[i, j] = tree[new GH_Path(j), i];
        //        }
        //    }
        //    return result;
        //}

        public static IGH_Goo[,] To2DArrayGen(DataTree<IGH_Goo> tree)
        {
            IGH_Goo[,] result = new IGH_Goo[tree.Branches.Count, tree.Branches[0].Count];

            for (int i = 0; i < tree.Branches.Count; i++)
            {
                for (int j = 0; j < tree.Branches[0].Count; j++)
                {
                    if (tree.Branches[i].Count != tree.Branches[0].Count) throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = tree.Branches[i][j];
                }
            }
            return result;
        }

        public static double[,] To2DArrayGen(DataTree<double> tree)
        {
            double[,] result = new double[tree.Branches.Count, tree.Branches[0].Count];

            for (int i = 0; i < tree.Branches.Count; i++)
            {
                for (int j = 0; j < tree.Branches[0].Count; j++)
                {
                    if (tree.Branches[i].Count != tree.Branches[0].Count) throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = tree.Branches[i][j];
                }
            }
            return result;
        }

        public static double[,] To2DArray(DataTree<double> tree)
        {
            double[,] result = new double[tree.Branches.Count, tree.Branches[0].Count];

            for (int i = 0; i < tree.Branches.Count; i++)
            {
                for (int j = 0; j < tree.Branches[0].Count; j++)
                {
                    if (tree.Branches[i].Count != tree.Branches[0].Count) throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = tree.Branches[i][j];
                }
            }
            return result;
        }

        public static Vector3d[,] To2DArrayVec3d(GH_Structure<GH_Vector> tree)
        {
            Vector3d[,] result = new Vector3d[tree.Branches[0].Count, tree.Branches.Count];

            for (int i = 0; i < tree.Branches[0].Count; i++)
            {
                for (int j = 0; j < tree.Branches.Count; j++)
                {
                    if (tree.Branches[j].Count != tree.Branches[0].Count) throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = new Vector3d(tree[j][i].Value);   //[i];
                }
            }
            return result;
        }

        public static IGH_Goo Conversion(GH_Vector data)

        {
            return data; // easy since GH_Point already implements IGH_Goo.
        }

        public static GH_Number Conversion(double data)

        {
            return new GH_Number(data); // easy since GH_Point already implements IGH_Goo.
        }

        // Couldnt find a way to implement this such that it works not only in scripting components,
        // see above

        //public static List<List<T>> ToListOfLists<T>(GH_Structure<T> tree)
        //{
        //    List<List<T>> list = new List<List<T>>();

        //    foreach (List<T> b in tree)
        //    {
        //        list.Add(b);
        //    }
        //    return list;
        //}

        // Check which one is faster later
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

        // Check which one is faster later
        //public static T[,] To2DArray<T>(this List<List<T>> lst)
        //{
        //    if ((lst == null) || (lst.Any(subList => subList.Any() == false)))
        //        throw new ArgumentException("Input list is not properly formatted with valid data");

        // int index = 0; int subindex;

        // return

        //       lst.Aggregate(new T[lst.Count(), lst.Max(sub => sub.Count())],
        //                     (array, subList) =>
        //                     {
        //                         subindex = 0;
        //                         subList.ForEach(itm => array[index, subindex++] = itm);
        //                         ++index;
        //                         return array;
        //                     });
        //}

        public static T[,] To2D<T>(T[][] source)
        {
            try
            {
                int FirstDim = source.Length;
                int SecondDim = source.GroupBy(row => row.Length).Single().Key; // throws InvalidOperationException if source is not rectangular

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

        public static object[][] CreateJaggedMatrix(int rows, int columns)
        {
            object[][] matrix = new object[rows][];

            for (int i = 0; i < matrix.Length; i++)
            {
                matrix[i] = new object[columns];
            }

            return matrix;
        }

        public static void JaggedArray2CSV(double[][] data, string filePath)
        {
            //writing output to csv

            using (StreamWriter outfile = new StreamWriter(filePath))
            {
                for (int x = 0; x < data.Length; x++)
                {
                    string content = "";
                    for (int y = 0; y < data[x].Length; y++)
                    {
                        content += data[x][y].ToString() + ",";
                    }

                    //trying to write data to csv
                    outfile.WriteLine(content);
                }
            }
        }

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

        public static T[] Slice<T>(T[] source, int fromIdx, int toIdx)
        {
            T[] ret = new T[toIdx - fromIdx + 1];
            for (int srcIdx = fromIdx, dstIdx = 0; srcIdx <= toIdx; srcIdx++)
            {
                ret[dstIdx++] = source[srcIdx];
            }
            return ret;
        }

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

        public static object[][] CSV2JaggedArray(String filePath)
        {
            object[][] data = File.ReadLines(filePath).Select(x => x.Split(',')).ToArray();

            return data;
        }

        public static void _2DArray2CSV(double[,] data, string filePath, bool truncateDoubles, int truncateBy = 1)
        {
            //writing output to csv

            if (!truncateDoubles)
            {
                using (StreamWriter outfile = new StreamWriter(filePath))
                {
                    for (int x = 0; x <= data.GetUpperBound(0); x++)
                    {
                        string content = "";

                        for (int y = 0; y <= data.GetUpperBound(1); y++)
                        {
                            content += data[x, y].ToString() + ",";
                        }

                        //trying to write data to csv
                        outfile.WriteLine(content);
                    }
                }
            }
            else
            {
                using (StreamWriter outfile = new StreamWriter(filePath))
                {
                    for (int x = 0; x <= data.GetUpperBound(0); x++)
                    {
                        string content = "";

                        for (int y = 0; y <= data.GetUpperBound(1); y++)
                        {
                            content += Math.Round(data[x, y], truncateBy).ToString() + ",";
                        }

                        //trying to write data to csv
                        outfile.WriteLine(content);
                    }
                }
            }
        }

        public static void _1DArray2CSV(double[] data, string filePath, bool truncateDoubles = true, int truncateBy = 1)
        {
            //writing output to csv

            if (!truncateDoubles)
            {
                using (StreamWriter outfile = new StreamWriter(filePath))
                {
                    for (int x = 0; x <= data.GetUpperBound(0); x++)
                    {
                        string content = "";

                        content += data[x].ToString() + ",";

                        //trying to write data to csv
                        outfile.WriteLine(content);
                    }
                }
            }
            else
            {
                using (StreamWriter outfile = new StreamWriter(filePath))
                {
                    for (int x = 0; x <= data.GetUpperBound(0); x++)
                    {
                        string content = "";

                        content += Math.Round(data[x], truncateBy).ToString() + ",";

                        //trying to write data to csv
                        outfile.WriteLine(content);
                    }
                }
            }
        }
    }
}