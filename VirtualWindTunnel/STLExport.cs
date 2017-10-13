using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindTunnel
{
    class STLExport
    {

        public static string ExportASCI(List<Mesh> meshObjects)
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("solid OBJECT");

            foreach (Mesh m in meshObjects)
            {
                m.Faces.ConvertQuadsToTriangles();   // STL supports trinangles only!

                m.FaceNormals.ComputeFaceNormals();

                for (int i = 0; i < m.Faces.Count; i++)
                {


                    //if (m.Faces[i].IsQuad)
                    //{
                    //    var pt1 = m.Vertices[m.Faces[i].A];
                    //    var pt2 = m.Vertices[m.Faces[i].B];
                    //    var pt3 = m.Vertices[m.Faces[i].C];
                    //    var pt4 = m.Vertices[m.Faces[i].D];

                    //    sb.AppendLine("\tfacet normal " + m.FaceNormals[i].X + " " + m.FaceNormals[i].Y + " " + m.FaceNormals[i].Z);
                    //    sb.AppendLine("\t\touter loop");
                    //    sb.AppendLine("\t\t\tvertex " + pt1.X + " " + pt1.Y + " " + pt1.Z);
                    //    sb.AppendLine("\t\t\tvertex " + pt2.X + " " + pt2.Y + " " + pt2.Z);
                    //    sb.AppendLine("\t\t\tvertex " + pt3.X + " " + pt3.Y + " " + pt3.Z);
                    //    sb.AppendLine("\t\t\tvertex " + pt4.X + " " + pt4.Y + " " + pt4.Z);
                    //    sb.AppendLine("\t\tendloop");
                    //    sb.AppendLine("\tendfacet");

                    //}

                    //else {
                    var pt1 = m.Vertices[m.Faces[i].A];
                    var pt2 = m.Vertices[m.Faces[i].B];
                    var pt3 = m.Vertices[m.Faces[i].C];


                    sb.AppendLine("\tfacet normal " + m.FaceNormals[i].X + " " + m.FaceNormals[i].Y + " " + m.FaceNormals[i].Z);
                    sb.AppendLine("\t\touter loop");
                    sb.AppendLine("\t\t\tvertex " + pt1.X + " " + pt1.Y + " " + pt1.Z);
                    sb.AppendLine("\t\t\tvertex " + pt2.X + " " + pt2.Y + " " + pt2.Z);
                    sb.AppendLine("\t\t\tvertex " + pt3.X + " " + pt3.Y + " " + pt3.Z);
                    sb.AppendLine("\t\tendloop");
                    sb.AppendLine("\tendfacet");

                    //}

                }



            }

            sb.AppendLine("endsolid OBJECT");

            return sb.ToString();
        }

    }
}
