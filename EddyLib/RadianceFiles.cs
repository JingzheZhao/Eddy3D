using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using Rhino.Geometry;
using System.IO;
using Rhino;
using System.Globalization;
using System.Linq;


namespace EddyLib
{


    //===========================================RADIANCE FILE PROC============================================

    public class RadianceFiles
    {
        // Radiance isn't exactly culture-aware, so we have to make everything here en-US
        private static readonly CultureInfo radianceCulture = new CultureInfo("en-US");
        private static string FormatPointAndNormal(Point3d p, Vector3d n) =>
           String.Format(radianceCulture, "{0:0.000} {1:0.000} {2:0.000} {3:0.000} {4:0.000} {5:0.000}", p.X, p.Y, p.Z, n.X, n.Y, n.Z);
        private static string FormatPoint(Point3d p) =>
        String.Format(radianceCulture, "{0:0.000} {1:0.000} {2:0.000}", p.X, p.Y, p.Z);

      
   
        public static void MeshProc(Mesh _m, string _fname, string _mat)
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(_fname);
            sw.WriteLine("#Grasshopper UrbanDaylight 2011 - Timur Dogan");
            sw.WriteLine("");
            //_m.Faces.ConvertQuadsToTriangles();

            for (int i = 0; i < _m.Faces.Count; ++i)
            {
                if (_m.Faces[i].IsTriangle)
                {
                    sw.WriteLine(_mat + " polygon " + _mat + "." + (i + 1).ToString());
                    sw.WriteLine("0");
                    sw.WriteLine("0");
                    sw.WriteLine("9");

                    int v0 = _m.Faces[i].A;
                    int v1 = _m.Faces[i].B;
                    int v2 = _m.Faces[i].C;

                    sw.WriteLine(FormatPoint(_m.Vertices[v0]));
                    sw.WriteLine(FormatPoint(_m.Vertices[v1]));
                    sw.WriteLine(FormatPoint(_m.Vertices[v2]));

                }
                else
                {
                    sw.WriteLine(_mat + " polygon " + _mat + "." + (i + 1).ToString());
                    sw.WriteLine("0");
                    sw.WriteLine("0");
                    sw.WriteLine("12");

                    int v0 = _m.Faces[i].A;
                    int v1 = _m.Faces[i].B;
                    int v2 = _m.Faces[i].C;
                    int v3 = _m.Faces[i].D;

                    sw.WriteLine(FormatPoint(_m.Vertices[v0]));
                    sw.WriteLine(FormatPoint(_m.Vertices[v1]));
                    sw.WriteLine(FormatPoint(_m.Vertices[v2]));
                    sw.WriteLine(FormatPoint(_m.Vertices[v3]));

                }

            }

            sw.Close();
        }





        public static void writePTS(string pts_path, List<Point3d> pts, List<Vector3d> pts_norm)
        {
            StringBuilder sb = new StringBuilder();
            for (int k = 0; k < pts.Count; k++)
            {
                sb.AppendLine(FormatPointAndNormal(pts[k], pts_norm[k]));
            }
            File.WriteAllText(pts_path, sb.ToString());
        }

        public static void writePTS(string pts_path, List<Point3d> pts)
        {
            StringBuilder sb = new StringBuilder();
            for (int k = 0; k < pts.Count; k++)
            {
                sb.AppendLine(FormatPointAndNormal(pts[k], Vector3d.ZAxis));
            }
            File.WriteAllText(pts_path, sb.ToString());
        }





        public static double[,] readDatFile(string path)
        {
            string[] txt = File.ReadAllLines(path);
            double[,] RGB = new double[txt.Length, 3];
            for (int i = 0; i < txt.Length; i++)
            {
                string[] ln = txt[i].Split('\t');
                RGB[i, 0] = double.Parse(ln[0]);
                RGB[i, 1] = double.Parse(ln[1]);
                RGB[i, 2] = double.Parse(ln[2]);
            }
            return RGB;
        }

        //====================================== ILLU FILE

        /// <summary>
        /// Loads an Daysim Illuminance file and converts it into a 2D double array.
        /// </summary>
        /// <returns>A list of list of doubles where [x][] is  time and [][x] are sensor points.</returns>
        /// <param name="fileName">File name.</param>
        /// <param name="start">Start.</param>
        /// <param name="stop">Stop.</param>
        public static double[][] loadILL(string fileName, int start, int stop)
        {
            //  [x][]  time
            //  [][x]  points

            double[][] values = new double[stop - start][];
            string[] lines = System.IO.File.ReadAllLines(fileName);

            for (int h = 0; h < stop; h++)
            {
                if (h >= start)
                {
                    string[] hourData = lines[h].Split(' ').Skip(4).ToArray();
                    double[] hourDataDouble = Array.ConvertAll<string, double>(hourData, Double.Parse);
                    values[h - start] = hourDataDouble;
                }

            }
            return values;
        }


        public static double[][] loadILL(string illFileName) // total illuminance data
        {
            // [x][] time
            // [][x] points
            //string[] illLines = System.IO.File.ReadAllLines(illFileName);
            //return illLines.Select(l => Array.ConvertAll<string, double>(l.Split(new[] { ' ' }).Skip(4).ToArray(), Double.Parse)).ToArray();


            string[] lines = System.IO.File.ReadAllLines(illFileName);
            double[][] values = new double[lines.Length][];
           

            for (int h = 0; h < lines.Length; h++)
            {
                
                    string[] hourData = lines[h].Split(' ').Skip(4).ToArray();
                    double[] hourDataDouble = Array.ConvertAll<string, double>(hourData, Double.Parse);
                    values[h] = hourDataDouble;
               

            }
            return values;
        }

        public static double[][] loadDIR(string dirFileName) // direct illuminance data
        {
            //string[] dirLines = System.IO.File.ReadAllLines(dirFileName);
            //return dirLines.Select(l => Array.ConvertAll<string, double>(l.Split(new[] { ' ' }).Skip(3).ToArray(), Double.Parse)).ToArray();

            string[] lines = System.IO.File.ReadAllLines(dirFileName);
            double[][] values = new double[lines.Length][];


            for (int h = 0; h < lines.Length; h++)
            {

                string[] hourData = lines[h].Split(' ').Skip(3).ToArray();
                double[] hourDataDouble = Array.ConvertAll<string, double>(hourData, Double.Parse);
                values[h] = hourDataDouble;


            }
            return values;
        }

    }
}
