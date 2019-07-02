using System;
using System.Linq;
using System.Runtime.InteropServices;


namespace ArrayHelper

{
    //public class Benchmark
    //{
    //    [Params(10, 100, 1000, 10000)]
    //    public int size;

    //    public double[][] data;

    //    [GlobalSetup]
    //    public void Setup()
    //    {
    //        var rnd = new Random();

    //        data = new double[size][];
    //        for (var i = 0; i < size; i++)
    //        {
    //            data[i] = new double[size];
    //            for (var j = 0; j < size; j++)
    //            {
    //                data[i][j] = rnd.NextDouble();
    //            }
    //        }
    //    }

    //    [Benchmark]
    //    public void ComputeTo2D()
    //    {
    //        var output = To2D(data);
    //    }

    //    [Benchmark]
    //    public void ComputeTo2DFast()
    //    {
    //        var output = To2DFast(data);
    //    }

    //    public static T[,] To2DFast<T>(T[][] source) where T : unmanaged
    //    {
    //        var dataOut = new T[source.Length, source.Length];
    //        var assertLength = source[0].Length;

    //        unsafe
    //        {
    //            for (var i = 0; i < source.Length; i++)
    //            {
    //                if (source[i].Length != assertLength)
    //                {
    //                    throw new InvalidOperationException("The given jagged array is not rectangular.");
    //                }

    //                fixed (T* pDataIn = source[i])
    //                {
    //                    fixed (T* pDataOut = &dataOut[i, 0])
    //                    {
    //                        CopyBlockHelper.SmartCopy<T>(pDataOut, pDataIn, assertLength);
    //                    }
    //                }
    //            }
    //        }

    //        return dataOut;
    //    }

    //    public static T[,] To2D<T>(T[][] source)
    //    {
    //        try
    //        {
    //            var FirstDim = source.Length;
    //            var SecondDim =
    //                source.GroupBy(row => row.Length).Single()
    //                    .Key; // throws InvalidOperationException if source is not rectangular

    //            var result = new T[FirstDim, SecondDim];
    //            for (var i = 0; i < FirstDim; ++i)
    //                for (var j = 0; j < SecondDim; ++j)
    //                    result[i, j] = source[i][j];

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

    //    }


    //public static class ArrayExt
    //{
    //    public static T[] GetRow<T>(this T[,] array, int row)
    //    {
    //        if (!typeof(T).IsPrimitive)
    //            throw new InvalidOperationException("Not supported for managed types.");

    //        if (array == null)
    //            throw new ArgumentNullException("array");

    //        int cols = array.GetUpperBound(1) + 1;
    //        T[] result = new T[cols];

    //        int size;

    //        if (typeof(T) == typeof(bool))
    //            size = 1;
    //        else if (typeof(T) == typeof(char))
    //            size = 2;
    //        else
    //            size = Marshal.SizeOf<T>();

    //        Buffer.BlockCopy(array, row * cols * size, result, 0, cols * size);

    //        return result;
    //    }
    //}

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
}