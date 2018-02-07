using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindTunnel
{
    class STLExport
    {

        public static void ExportASCI(string filePath, List<Mesh> meshObjects)
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

            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, sb.ToString());
        }

        public static void ExportASCI(string filePath, Mesh m)
        {

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("solid OBJECT");

          
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

            sb.AppendLine("endsolid OBJECT");

            var dir = Path.GetDirectoryName(filePath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(filePath, sb.ToString());
        }

        public static void ExportBinary(string filePath, List<Mesh> meshObjects)
        {

            //triangulate, compute face normals and count facets
            int faceCnt = 0;
            foreach (Mesh m in meshObjects)
            {
                m.Faces.ConvertQuadsToTriangles();   // STL supports trinangles only!
                m.FaceNormals.ComputeFaceNormals();
                faceCnt += m.Faces.Count;
            }

            // Use using statement and File.Open.
            using (BinaryWriter b = new BinaryWriter(
                File.Open(filePath, FileMode.Create)))
            {

                byte[] header = Encoding.ASCII.GetBytes("Binary STL generated by VirtualWindTunnel v0.1");
                byte[] headerFull = new byte[80];
                Buffer.BlockCopy(header, 0, headerFull, 0, Math.Min(header.Length, headerFull.Length));

                b.Write(headerFull);
                b.Write((UInt32)faceCnt);

                UInt16 AttributeByteCount = 0; //After these follows a 2-byte ("short") unsigned integer that is the "attribute byte count" – in the standard format, this should be zero because most software does not understand anything else.

                foreach (Mesh m in meshObjects)
                {
                    for (int i = 0; i < m.Faces.Count; i++)
                    {

                        var pt1 = m.Vertices[m.Faces[i].A];
                        var pt2 = m.Vertices[m.Faces[i].B];
                        var pt3 = m.Vertices[m.Faces[i].C];

                        b.Write((float)m.FaceNormals[i].X);
                        b.Write((float)m.FaceNormals[i].Y);
                        b.Write((float)m.FaceNormals[i].Z);

                        b.Write((float)pt1.X);
                        b.Write((float)pt1.Y);
                        b.Write((float)pt1.Z);

                        b.Write((float)pt2.X);
                        b.Write((float)pt2.Y);
                        b.Write((float)pt2.Z);

                        b.Write((float)pt3.X);
                        b.Write((float)pt3.Y);
                        b.Write((float)pt3.Z);

                        b.Write(AttributeByteCount);
                    }
                }
            }



        }

        public static void ExportBinary(string filePath, Mesh m)
        {

            //triangulate, compute face normals and count facets
            int faceCnt = 0;

            m.Faces.ConvertQuadsToTriangles();   // STL supports trinangles only!
            m.FaceNormals.ComputeFaceNormals();
            faceCnt += m.Faces.Count;


            // Use using statement and File.Open.
            using (BinaryWriter b = new BinaryWriter(
                File.Open(filePath, FileMode.Create)))
            {

                byte[] header = Encoding.ASCII.GetBytes("Binary STL generated by VirtualWindTunnel v0.1");
                byte[] headerFull = new byte[80];
                Buffer.BlockCopy(header, 0, headerFull, 0, Math.Min(header.Length, headerFull.Length));

                b.Write(headerFull);
                b.Write((UInt32)faceCnt);

                UInt16 AttributeByteCount = 0; //After these follows a 2-byte ("short") unsigned integer that is the "attribute byte count" – in the standard format, this should be zero because most software does not understand anything else.


                for (int i = 0; i < m.Faces.Count; i++)
                {

                    var pt1 = m.Vertices[m.Faces[i].A];
                    var pt2 = m.Vertices[m.Faces[i].B];
                    var pt3 = m.Vertices[m.Faces[i].C];

                    b.Write((float)m.FaceNormals[i].X);
                    b.Write((float)m.FaceNormals[i].Y);
                    b.Write((float)m.FaceNormals[i].Z);

                    b.Write((float)pt1.X);
                    b.Write((float)pt1.Y);
                    b.Write((float)pt1.Z);

                    b.Write((float)pt2.X);
                    b.Write((float)pt2.Y);
                    b.Write((float)pt2.Z);

                    b.Write((float)pt3.X);
                    b.Write((float)pt3.Y);
                    b.Write((float)pt3.Z);

                    b.Write(AttributeByteCount);
                }

            }



        }


        //public static void ExportBinaryList(string filePath, List<Mesh> meshObjects, String filePrefix)
        //{

        //    //triangulate, compute face normals and count facets
        //    int faceCnt = 0;

        //    foreach (GeometryBase m in meshObjects)
        //    {
        //        if (m.ObjectType == Rhino.DocObjects.ObjectType.Brep || m.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || m.ObjectType == Rhino.DocObjects.ObjectType.Surface)
        //        {
        //            Mesh obj = (Mesh)m;
        //            meshObjects.Add(obj);

        //            foreach (Mesh mm in meshObjects)
        //            {
        //                mm.Faces.ConvertQuadsToTriangles();   // STL supports trinangles only!
        //                mm.FaceNormals.ComputeFaceNormals();
        //                faceCnt += mm.Faces.Count;
        //            }
        //        }


        //        Mesh newMesh = (Mesh)m;

        //        using (BinaryWriter b = new BinaryWriter(File.Open(filePath, FileMode.Create)))
        //        {

        //            byte[] header = Encoding.ASCII.GetBytes("Binary STL generated by VirtualWindTunnel v0.1");
        //            byte[] headerFull = new byte[80];
        //            Buffer.BlockCopy(header, 0, headerFull, 0, Math.Min(header.Length, headerFull.Length));

        //            b.Write(headerFull);
        //            b.Write((UInt32)faceCnt);

        //            UInt16 AttributeByteCount = 0; //After these follows a 2-byte ("short") unsigned integer that is the "attribute byte count" – in the standard format, this should be zero because most software does not understand anything else.


        //            for (int i = 0; i < 3; i++)
        //            {

        //                var pt1 = newMesh.Vertices[newMesh.Faces[i].A];
        //                var pt2 = newMesh.Vertices[newMesh.Faces[i].B];
        //                var pt3 = newMesh.Vertices[newMesh.Faces[i].C];

        //                b.Write((float)newMesh.FaceNormals[i].X);
        //                b.Write((float)newMesh.FaceNormals[i].Y);
        //                b.Write((float)newMesh.FaceNormals[i].Z);

        //                b.Write((float)pt1.X);
        //                b.Write((float)pt1.Y);
        //                b.Write((float)pt1.Z);

        //                b.Write((float)pt2.X);
        //                b.Write((float)pt2.Y);
        //                b.Write((float)pt2.Z);

        //                b.Write((float)pt3.X);
        //                b.Write((float)pt3.Y);
        //                b.Write((float)pt3.Z);

        //                b.Write(AttributeByteCount);
        //            }






        //        }

        //    }
        //}
    }
}

