using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindTunnel
{
    public class OFDomainBuilder
    {
       public double width;
       public double length;
       public double height;
       public double yMin;
       public double yMax;
       public double xMin;
       public double xMax;
       public double zMin;
       public double zMax;
       public int xCells;
       public int yCells;
       public int zCells;

       public BoundingBox uBox;
       public Circle circ;
       public Cylinder cylHeight;
       public Mesh boxGround;
       public Mesh cylGround;

       public double diameter;
       public string workingDirectory;


       public static List<Brep> FindFacades(Plane plane, List<Brep> volumes, out double projectedAreaTotal)
        {
            List<Brep> Facades = new List<Brep>();

            List<double> projectedAreas = new List<double>();
            foreach (Brep b in volumes)
            {
                for (int i = 0; i < b.Faces.Count; i++)
                {
                    Vector3d vSurf = b.Faces[i].NormalAt(0.5, 0.5);
                    double dot = plane.ZAxis * vSurf;
                    if (dot < 0.1) continue;


                    Brep face = b.Faces[i].DuplicateFace(false);
                    //Print(dot + "");
                    Facades.Add(face);

                    Vector3d cross = Vector3d.CrossProduct(plane.ZAxis, vSurf);
                    double norm = cross.Length;
                    double angle = Math.Atan2(norm, dot);

                    //Print((angle * 180 / Math.PI) + "");

                    double projectedArea = face.GetArea() * Math.Cos(angle);

                    projectedAreas.Add(projectedArea);
                    //Print(projectedArea + "");
                }
            }
            projectedAreaTotal = projectedAreas.Sum(x => x);

            return Facades;
        }



       public OFDomainBuilder(List<Brep> geometry, string _workingDirectory)
       {

            
            workingDirectory = _workingDirectory;
            //BoundingBox bb = domain.GetBoundingBox(true);

            uBox = new BoundingBox();

            foreach (Brep i in geometry)
            {
                BoundingBox bbb = i.GetBoundingBox(true);
                uBox.Union(bbb);
            }



            xMin = uBox.Min.X;
            xMax = uBox.Max.X;
            yMin = uBox.Min.Y;
            yMax = uBox.Max.Y;
            zMin = uBox.Min.Z;
            zMax = uBox.Max.Z;
            xCells = ((int)(uBox.Max.X) - (int)(uBox.Min.X)) / 20;
            yCells = ((int)(uBox.Max.Y) - (int)(uBox.Min.Y)) / 20;
            zCells = ((int)(uBox.Max.Z) - (int)(uBox.Min.Z)) / 20;




            var center = uBox.Center;
            center.Z = 0;

            var dimX = xMax - xMin;
            var dimY = yMax - yMin;
            var dimZ = zMax;
            var dim = dimX > dimY ? dimX : dimY;


            circ = new Circle(center, 16 * dim);
            cylHeight = new Cylinder(circ, 5 * dimZ);



            MeshingParameters mpGround = MeshingParameters.Default;
            cylGround = Mesh.CreateFromPlanarBoundary(circ.ToNurbsCurve(), mpGround);

            Vector3d vecDownstream = new Vector3d(0, 1, 0);
            Vector3d vecUpstream = new Vector3d(0, -1, 0);
            Vector3d vecLeft = new Vector3d(-1, 0, 0);
            Vector3d vecRight = new Vector3d(1, 0, 0);

            double scaleRectDomainYUpstream = 10 * (yMax - yMin) ;
            double scaleRectDomainYDownstream = 16 * (yMax - yMin);

            Point3d newMin = uBox.Min + vecUpstream * scaleRectDomainYUpstream;
            Point3d newMax = uBox.Max + vecDownstream * scaleRectDomainYDownstream;

            newMin += vecLeft * 5;
            newMax += vecRight * 5;

            Rectangle3d plGround = new Rectangle3d(Plane.WorldXY, newMin, newMax );
            boxGround = Mesh.CreateFromPlanarBoundary(plGround.ToNurbsCurve(), mpGround);

            

       }

        public override string ToString()
        {
            return "OF Domain: " + yCells;
           // return base.ToString();
        }
    }
}
