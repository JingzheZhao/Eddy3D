using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Rhino;
using Rhino.Geometry;

namespace EddyLib
{
    //===========================================RADIANCE FILE PROC============================================

    public class RadianceFiles
    {
        public static string DaysimInstallation = @"C:\DIVA\DaysimBinaries";

        public static string Epw2Wea(string weatherFilePath, string targetPath)
        {
            try
            {
                if (Directory.Exists(targetPath) == false)
                {
                    Directory.CreateDirectory(targetPath);
                }

                //if (Directory.GetFiles(targetPath, "*.wea").Length > 0)
                //{
                //    Array.ForEach(Directory.GetFiles(targetPath, "*.wea"), delegate (string path) { File.Delete(path); });
                //}
                string epwdatname = Path.GetFileNameWithoutExtension(weatherFilePath);

                string arguments = "\"" + Path.GetFullPath(weatherFilePath) + "\" \"" +
                                   Path.GetFullPath(Path.Combine(targetPath, epwdatname + @".wea")) + "\"";

                Debug.WriteLine(arguments);

                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    Arguments = arguments,
                    FileName = DaysimInstallation + @"\epw2wea",
                    WorkingDirectory = DaysimInstallation,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                Process p = new Process
                {
                    StartInfo = processInfo
                };

                // p.OutputDataReceived += DebugLog.CaptureOutput; p.ErrorDataReceived += DebugLog.CaptureError;

                p.Start();

                p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                       Console.WriteLine("output>>" + e.Data);
                p.BeginOutputReadLine();

                p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                    Console.WriteLine("error>>" + e.Data);
                p.BeginErrorReadLine();

                p.WaitForExit();

                Console.WriteLine("ExitCode: {0}", p.ExitCode);
                p.Close();

                Debug.WriteLine("WEA FILE EXSISTS? " + File.Exists(Path.GetFullPath(Path.Combine(targetPath, epwdatname + @".wea"))).ToString());

                return epwdatname;
            }
            catch
            {
                Debug.WriteLine("SetWeather failed");
                return "";
            }
        }

        // Radiance isn't exactly culture-aware, so we have to make everything here en-US
        private static readonly CultureInfo radianceCulture = new CultureInfo("en-US");

        private static string FormatPointAndNormal(Point3d p, Vector3d n) =>
           String.Format(radianceCulture, "{0:0.###} {1:0.###} {2:0.###} {3:0.###} {4:0.###} {5:0.###}", p.X, p.Y, p.Z, n.X, n.Y, n.Z);

        private static string FormatPoint(Point3d p) =>
        String.Format(radianceCulture, "{0:0.###} {1:0.###} {2:0.###}", p.X, p.Y, p.Z);

        public static void MeshProc(Mesh _m, string _fname, string _mat)
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(_fname);
            sw.WriteLine("#Grasshopper Eddy 2019");
            sw.WriteLine("");

            //_m.Faces.ConvertQuadsToTriangles();

            //_m.Faces.ExtractDuplicateFaces();
            _m.Faces.ConvertNonPlanarQuadsToTriangles(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAngleToleranceRadians, 0);

            // Sometimes Octrees are not written robustly

            //int fixCount = 0;
            //_m.Faces.RemoveZeroAreaFaces(ref fixCount);
            _m.Faces.CullDegenerateFaces();

            for (int i = 0; i < _m.Faces.Count; ++i)
            {
                var area = Utilities.MeshFaceArea(i, _m);
                if (area < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    continue;
                }

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

        public static void MeshProc(Mesh _m, string _fname, string _mat, string _matLib)
        {
            System.IO.StreamWriter sw = new System.IO.StreamWriter(_fname);
            sw.WriteLine("#Grasshopper Eddy 2019");
            sw.WriteLine("");
            sw.WriteLine(_matLib);
            sw.WriteLine("");

            //_m.Faces.ConvertQuadsToTriangles();

            //_m.Faces.ExtractDuplicateFaces();
            _m.Faces.ConvertNonPlanarQuadsToTriangles(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAngleToleranceRadians, 0);

            // Sometimes Octrees are not written robustly

            //int fixCount = 0;
            //_m.Faces.RemoveZeroAreaFaces(ref fixCount);
            _m.Faces.CullDegenerateFaces();

            for (int i = 0; i < _m.Faces.Count; ++i)
            {
                var area = Utilities.MeshFaceArea(i, _m);
                if (area < RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)
                {
                    continue;
                }

                if (_m.Faces[i].IsTriangle)
                {
                    // Change this material for 2Phase method

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

        public static double[][] readPTS(string pts_path)
        {
            var lines = File.ReadAllLines(pts_path);

            double[][] points = new double[lines.Length][];

            for (int k = 0; k < lines.Length; k++)
            {
                string[] ptsString = lines[k].Split(' ').Take(3).ToArray();
                double[] pts = Array.ConvertAll<string, double>(ptsString, Double.Parse);
                points[k] = pts;
            }

            return points;
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

        public static double[,] readCSVFile(string path)
        {
            string[] txt = File.ReadAllLines(path);
            double[,] data = new double[txt.Length, txt[0].Trim(',').Split(',').Length];
            for (int i = 0; i < txt.Length; i++)
            {
                string[] ln = txt[i].Trim(',').Split(',');
                for (int j = 0; j < ln.Length; j++)
                {
                    if (string.IsNullOrWhiteSpace(ln[j])) continue;
                    data[i, j] = double.Parse(ln[j]);
                }
            }
            return data;
        }

        //====================================== ILLU FILE

        /// <summary>
        /// Loads an Daysim Illuminance file and converts it into a 2D double array.
        /// </summary>
        /// <returns>A list of list of doubles where [x][] is time and [][x] are sensor points.</returns>
        /// <param name="fileName">File name.</param>
        /// <param name="start">Start.</param>
        /// <param name="stop">Stop.</param>
        public static double[][] loadILL(string fileName, int start, int stop)
        {
            // [x][] time [][x] points

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

        public static void saveILLBin(string illFileName) // total illuminance data
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

            writeBin(illFileName + ".bin", values);
        }

        public static float[,] loadBin(string filename)
        {
            // [i, time j] points

            float[,] data;

            int iDim;
            int jDim;

            //reading from the file
            // 1.
            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                // 2. Position and length variables.
                int pos = 0;

                // 2A. Use BaseStream.
                int length = (int)b.BaseStream.Length;

                iDim = b.ReadInt32();
                jDim = b.ReadInt32();
                data = new float[iDim, jDim];
                pos += sizeof(int);
                pos += sizeof(int);

                int i = 0;
                int j = 0;
                while (pos < length)
                {
                    float v = b.ReadSingle();
                    data[i, j] = (v);

                    pos += sizeof(float);

                    j++;
                    if (j == jDim) { j = 0; i++; }
                }
            }

            return data;
        }

        public static double[,] loadBinD(string filename)
        {
            // [i, time j] points

            double[,] data;

            int iDim;
            int jDim;

            //reading from the file
            // 1.
            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                // 2. Position and length variables.
                int pos = 0;

                // 2A. Use BaseStream.
                int length = (int)b.BaseStream.Length;

                iDim = b.ReadInt32();
                jDim = b.ReadInt32();
                data = new double[iDim, jDim];
                pos += sizeof(int);
                pos += sizeof(int);

                int i = 0;
                int j = 0;
                while (pos < length)
                {
                    float v = b.ReadSingle();
                    data[i, j] = (v);

                    pos += sizeof(float);

                    j++;
                    if (j == jDim) { j = 0; i++; }
                }
            }

            return data;
        }

        public static float[][] loadBinJagged(string filename)
        {
            // [i][ time j] points

            float[][] data;

            int iDim;
            int jDim;

            //reading from the file
            // 1.
            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                // 2. Position and length variables.
                int pos = 0;

                // 2A. Use BaseStream.
                int length = (int)b.BaseStream.Length;

                iDim = b.ReadInt32();
                jDim = b.ReadInt32();
                data = new float[iDim][]; //jDim

                for (int ii = 0; ii < iDim; ii++)
                {
                    data[ii] = new float[jDim];
                }

                pos += sizeof(int);
                pos += sizeof(int);

                int i = 0;
                int j = 0;
                while (pos < length)
                {
                    float v = b.ReadSingle();
                    data[i][j] = (v);

                    pos += sizeof(float);

                    j++;
                    if (j == jDim) { j = 0; i++; }
                }
            }

            return data;
        }

        public static void writeBin(string fileName, double[][] values)
        {
            // [i, time j] points

            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                bw.Write((int)values.Length);
                bw.Write((int)values[0].Length);

                for (int i = 0; i < values.Length; i++)
                {
                    for (int j = 0; j < values[i].Length; j++)
                    {
                        float fval = (float)values[i][j];

                        bw.Write(fval);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        public static void writeBin(string fileName, double[,] values)
        {
            // [i, time j] points

            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                bw.Write((Int32)values.GetLength(0));
                bw.Write((Int32)values.GetLength(1));

                for (int i = 0; i < values.GetLength(0); i++)
                {
                    for (int j = 0; j < values.GetLength(1); j++)
                    {
                        float fval = (float)values[i, j];
                        bw.Write(fval);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        public static Vector3d[,] loadBinDVectors(string filename, out int[] windDirs)//, out int iDim, out int jDim)
        {
            // [i, time j] points

            Vector3d[,] data;

            int numberOfWindDirs;

            //reading from the file
            // 1.
            using (BinaryReader b = new BinaryReader(
                File.Open(filename, FileMode.Open)))
            {
                // 2. Position and length variables.
                int pos = 0;

                // 2A. Use BaseStream.
                int length = (int)b.BaseStream.Length;

                int iDim = b.ReadInt32();
                int jDim = b.ReadInt32();
                data = new Vector3d[iDim, jDim];
                pos += sizeof(int);
                pos += sizeof(int);

                // Read wind dirs
                numberOfWindDirs = b.ReadInt32();
                pos += sizeof(int);

                windDirs = new int[numberOfWindDirs];

                for (int wd = 0; wd < numberOfWindDirs; wd++)
                {
                    windDirs[wd] = b.ReadInt32();
                    pos += sizeof(int);
                }

                int i = 0;
                int j = 0;
                while (pos < length)
                {
                    float x = b.ReadSingle();
                    data[i, j].X = (x);

                    float y = b.ReadSingle();
                    data[i, j].Y = (y);

                    float z = b.ReadSingle();
                    data[i, j].Z = (z);

                    pos += sizeof(float);
                    pos += sizeof(float);
                    pos += sizeof(float);

                    j++;
                    if (j == jDim) { j = 0; i++; }
                }
            }

            return data;
        }

        public static void writeBinVectors(string fileName, Vector3d[,] values, int[] windDirs)
        {
            // [i, time j] points

            BinaryWriter bw;

            //create the file
            try
            {
                bw = new BinaryWriter(new FileStream(fileName, FileMode.Create));
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot create file.");
                return;
            }

            //writing into the file
            try
            {
                // Write array dimensions
                bw.Write((Int32)values.GetLength(0));
                bw.Write((Int32)values.GetLength(1));

                // Write wind directions
                bw.Write(windDirs.Length);
                for (int w = 0; w < windDirs.Length; w++)
                {
                    int dir = windDirs[w];
                    bw.Write(dir);
                }

                for (int i = 0; i < values.GetLength(0); i++)
                {
                    for (int j = 0; j < values.GetLength(1); j++)
                    {
                        float fvalX = (float)values[i, j].X;
                        float fvalY = (float)values[i, j].Y;
                        float fvalZ = (float)values[i, j].Z;

                        bw.Write(fvalX);
                        bw.Write(fvalY);
                        bw.Write(fvalZ);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message + "\n Cannot write to file.");
                return;
            }
            bw.Close();
        }

        public static double[][] loadDC(string file) // total illuminance data
        {
            // [x][] lines [][x] coeffs

            string[] lines = System.IO.File.ReadAllLines(file).Where(x => !x.Trim().StartsWith("#")).ToArray();
            double[][] values = new double[lines.Length][];

            for (int h = 0; h < lines.Length; h++)
            {
                string[] data = lines[h].Trim().Split('\t').ToArray();
                double[] dataDouble = Array.ConvertAll<string, double>(data, Double.Parse);
                values[h] = dataDouble;
            }
            return values;
        }

        public static void writeDC(string file, double[][] dif, double[][] dir) // total illuminance data
        {
            // [x][] lines [][x] coeffs
            var sb = new StringBuilder();

            for (int h = 0; h < dif.Length; h++)
            {
                for (int c = 0; c < dif[h].Length; c++)
                {
                    sb.Append(dif[h][c].ToString());
                    sb.Append('\t');
                }
                for (int c = 0; c < dir[h].Length; c++)
                {
                    sb.Append(dir[h][c].ToString());
                    sb.Append('\t');
                }
                sb.AppendLine("");
            }
            File.WriteAllText(file, sb.ToString());
        }

        public static void writeDC_DIF(string file, double[][] dif, double[][] dir) // total illuminance data
        {
            // [x][] lines [][x] coeffs
            var sb = new StringBuilder();

            for (int h = 0; h < dif.Length; h++)
            {
                for (int c = 0; c < dif[h].Length; c++)
                {
                    sb.Append(dif[h][c].ToString());
                    sb.Append('\t');
                }
                for (int c = 0; c < dir[h].Length; c++)
                {
                    sb.Append("0");
                    sb.Append('\t');
                }
                sb.AppendLine("");
            }
            File.WriteAllText(file, sb.ToString());
        }

        public static void writeDC_DIR(string file, double[][] dif, double[][] dir) // total illuminance data
        {
            // [x][] lines [][x] coeffs
            var sb = new StringBuilder();

            for (int h = 0; h < dif.Length; h++)
            {
                for (int c = 0; c < dif[h].Length; c++)
                {
                    sb.Append("0");
                    sb.Append('\t');
                }
                for (int c = 0; c < dir[h].Length; c++)
                {
                    sb.Append(dir[h][c].ToString());
                    sb.Append('\t');
                }
                sb.AppendLine("");
            }
            File.WriteAllText(file, sb.ToString());
        }
    }
}