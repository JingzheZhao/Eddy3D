using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EddyLib
{
    /// <summary>
    /// Helper methods for converting between Grasshopper and .NET types.
    /// </summary>
    public static class GrasshopperConversions
    {
        /// <summary>
        /// Converts a GH_Number to a double.
        /// </summary>
        public static double GH_NumberToDouble(GH_Number p)
        {
            return p.Value;
        }

        /// <summary>
        /// Converts a GH_Vector to a Vector3d.
        /// </summary>
        public static Vector3d GH_VectorToVector3d(GH_Vector vec)
        {
            return new Vector3d(vec.Value);
        }

        /// <summary>
        /// Converts a double to a GH_Number.
        /// </summary>
        public static GH_Number DoubleToGH_Number(double p)
        {
            return new GH_Number(p);
        }

        /// <summary>
        /// Converts a Vector3d to a GH_Vector.
        /// </summary>
        public static GH_Vector Vector3dToGH_Vector(Vector3d vec)
        {
            return new GH_Vector(vec);
        }

        /// <summary>
        /// Converts a GH_Vector to an IGH_Goo.
        /// </summary>
        public static IGH_Goo Conversion(GH_Vector data)
        {
            return data;
        }

        /// <summary>
        /// Converts a double to a GH_Number (IGH_Goo compatible).
        /// </summary>
        public static GH_Number Conversion(double data)
        {
            return new GH_Number(data);
        }

        /// <summary>
        /// Converts a list of lists to a DataTree.
        /// </summary>
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

        /// <summary>
        /// Converts a DataTree to a list of lists.
        /// </summary>
        public static List<List<T>> ToListOfLists<T>(DataTree<T> tree)
        {
            List<List<T>> list = new List<List<T>>();
            foreach (List<T> b in tree.Branches)
            {
                list.Add(b);
            }
            return list;
        }

        /// <summary>
        /// Converts a DataTree of IGH_Goo to a 2D array.
        /// </summary>
        public static IGH_Goo[,] To2DArrayGen(DataTree<IGH_Goo> tree)
        {
            IGH_Goo[,] result = new IGH_Goo[tree.Branches.Count, tree.Branches[0].Count];

            for (int i = 0; i < tree.Branches.Count; i++)
            {
                for (int j = 0; j < tree.Branches[0].Count; j++)
                {
                    if (tree.Branches[i].Count != tree.Branches[0].Count)
                        throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = tree.Branches[i][j];
                }
            }
            return result;
        }

        /// <summary>
        /// Converts a DataTree of doubles to a 2D array.
        /// </summary>
        public static double[,] To2DArrayGen(DataTree<double> tree)
        {
            double[,] result = new double[tree.Branches.Count, tree.Branches[0].Count];

            for (int i = 0; i < tree.Branches.Count; i++)
            {
                for (int j = 0; j < tree.Branches[0].Count; j++)
                {
                    if (tree.Branches[i].Count != tree.Branches[0].Count)
                        throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = tree.Branches[i][j];
                }
            }
            return result;
        }

        /// <summary>
        /// Converts a DataTree of doubles to a 2D array.
        /// </summary>
        public static double[,] To2DArray(DataTree<double> tree)
        {
            double[,] result = new double[tree.Branches.Count, tree.Branches[0].Count];

            for (int i = 0; i < tree.Branches.Count; i++)
            {
                for (int j = 0; j < tree.Branches[0].Count; j++)
                {
                    if (tree.Branches[i].Count != tree.Branches[0].Count)
                        throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = tree.Branches[i][j];
                }
            }
            return result;
        }

        /// <summary>
        /// Converts a GH_Structure of GH_Vector to a 2D array of Vector3d.
        /// </summary>
        public static Vector3d[,] To2DArrayVec3d(GH_Structure<GH_Vector> tree)
        {
            Vector3d[,] result = new Vector3d[tree.Branches[0].Count, tree.Branches.Count];

            for (int i = 0; i < tree.Branches[0].Count; i++)
            {
                for (int j = 0; j < tree.Branches.Count; j++)
                {
                    if (tree.Branches[j].Count != tree.Branches[0].Count)
                        throw new InvalidOperationException("The list cannot contain elements (lists) of different sizes.");
                    result[i, j] = new Vector3d(tree[j][i].Value);
                }
            }
            return result;
        }
    }
}
