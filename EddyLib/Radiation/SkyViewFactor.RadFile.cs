using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib.Radiation
{
    public partial class SkyViewFactor
    {
        #region 2. RadFile

        public static void RadFile(List<GeometryBase> Geo, string Path)// RadFilePath
        {
            var matlist = new HashSet<string>();

            StringBuilder finalFile = new StringBuilder();

            StringBuilder radFileString = new StringBuilder();

            int id = 0;

            foreach (GeometryBase g in Geo)
            {
                string mat = g.UserDictionary["RadMat"].ToString().Trim();
                matlist.Add(mat);

                string matName = mat.Split(' ')[2];

                //Print(matName);

                Mesh m = (Mesh)g;

                radFileString.AppendLine(Mesh2Rad(m, matName, id.ToString()));
                id++;
            }

            foreach (string s in matlist)
            {
                finalFile.AppendLine(s);
            }
            finalFile.AppendLine("");
            finalFile.AppendLine(radFileString.ToString());

            File.WriteAllText(Path, finalFile.ToString());

            // A = "Final File Length: " + finalFile.Length;
        }

        public static string Mesh2Rad(Mesh m, string RadianceMaterial, string id)
        {
            StringBuilder s = new StringBuilder();

            // string s = "";
            try
            {
                for (int i = 0; i < m.Faces.Count; ++i)
                {
                    if (m.Faces[i].IsTriangle)
                    {
                        s.AppendLine(RadianceMaterial + " polygon " + id + "_" + i);
                        s.AppendLine("0");
                        s.AppendLine("0");
                        s.AppendLine("9");

                        int v0 = m.Faces[i].A;
                        int v1 = m.Faces[i].B;
                        int v2 = m.Faces[i].C;

                        s.AppendLine(FormatPoint(m.Vertices[v0]));
                        s.AppendLine(FormatPoint(m.Vertices[v1]));
                        s.AppendLine(FormatPoint(m.Vertices[v2]));
                        s.AppendLine();
                    }
                    else
                    {
                        s.AppendLine(RadianceMaterial + " polygon " + id + "_" + i);
                        s.AppendLine("0");
                        s.AppendLine("0");
                        s.AppendLine("12");

                        int v0 = m.Faces[i].A;
                        int v1 = m.Faces[i].B;
                        int v2 = m.Faces[i].C;
                        int v3 = m.Faces[i].D;

                        s.AppendLine(FormatPoint(m.Vertices[v0]));
                        s.AppendLine(FormatPoint(m.Vertices[v1]));
                        s.AppendLine(FormatPoint(m.Vertices[v2]));
                        s.AppendLine(FormatPoint(m.Vertices[v3]));
                        s.AppendLine();
                    }
                }
            }
            catch (Exception ex) { RhinoApp.WriteLine("Mesh2Rad failed " + ex.Message); }
            return s.ToString();
        }

        private static readonly CultureInfo radianceCulture = new CultureInfo("en-US");

        private static string FormatPoint(Point3f p)
        {
            return String.Format(radianceCulture, "{0:0.000} {1:0.000} {2:0.000}", p.X, p.Y, p.Z);
        }

        #endregion 2. RadFile
    }
}
