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
        //BoundingBox
        public double width;
        public double length;
        public double height;
        public double yMin;
        public double yMax;
        public double xMin;
        public double xMax;
        public double zMin;
        public double zMax;
        public double dimX;
        public double dimY;
        public double dimZ;
        public double dim;

        //Calculated boundary
        public Point3d newMin;
        public Point3d newMax;
        public Point3d centerGroundBBox;
        //public double newDimX;
        //public double newDimY;
        //public double newDimZ;


        public int xCells;
        public int yCells;
        public int zCells;

        public BoundingBox BBox;
        public Circle circ;
        public Cylinder newCylindricalDomain;
        public Mesh newBoxGround;
        public Mesh newCylGround;
        public Box newBoxDomain;
        public Point3d locationInMesh;

        public Rectangle3d plGround;

        public double diameter;
        public string workingDirectory;
        public double baseMesh;
        
        public int CPU;

        public Point3d newMinGroundPlane1;
        public Point3d newMaxGroundPlane2;

       


        public OFDomainBuilder(List<Brep> geometry, string _workingDirectory, double _baseMesh)
        {
            workingDirectory = _workingDirectory;
            baseMesh = _baseMesh;
            
            //BoundingBox bb = domain.GetBoundingBox(true);
            
            BBox = geometry[0].GetBoundingBox(true);
            if (geometry.Count > 1)
            {

                for (int i = 1; i < geometry.Count; i++)
                {
                    BoundingBox bbb = geometry[i].GetBoundingBox(true);
                    BBox.Union(bbb);
                }
            }



            xMin = BBox.Min.X;
            xMax = BBox.Max.X;
            yMin = BBox.Min.Y;
            yMax = BBox.Max.Y;
            zMin = BBox.Min.Z;
            zMax = BBox.Max.Z;
            
            
            Vector3d vecPlusY = new Vector3d(0, 1, 0);
            Vector3d vecMinusY = new Vector3d(0, -1, 0);
            Vector3d vecMinusX = new Vector3d(-1, 0, 0);
            Vector3d vecPlusX = new Vector3d(1, 0, 0);
            Vector3d vecMinusZ = new Vector3d(0, 0, -1);
            Vector3d vecPlusZ = new Vector3d(0, 0, 1);

            
            dimX = xMax - xMin;
            dimY = yMax - yMin;
            dimZ = zMax - zMin;

            

            //Create ground plane of BBox
            centerGroundBBox = BBox.Center + 0.5 * vecMinusZ * dimZ;

            
            locationInMesh = centerGroundBBox + 4 * vecPlusZ * dimZ;

            //Create Circular Domain
            dim = dimX > dimY ? dimX : dimY;
            circ = new Circle(centerGroundBBox, 16.5 * dim);
            newCylindricalDomain = new Cylinder(circ, 6* dimZ);
            MeshingParameters mpGround = MeshingParameters.Default;
            newCylGround = Mesh.CreateFromPlanarBoundary(circ.ToNurbsCurve(), mpGround);



            //Create Box Domain
            //Find frontfacing areas in wind direction
            double pj;
            double projectedAreaZX = FindFacades(Plane.WorldZX, geometry, out pj);

            //New Dimensions in Y
            double scaleRectDomainYUpstream = - 10.5 * dimY;
            double scaleRectDomainYDownstream = 16.5 * dimY;
            double scaleRectDomainZ = 6* dimZ;

            // New Dimensions in X; take blocking ratio into account
            var scaleRectDomainX  = projectedAreaZX * 100 / 3 / scaleRectDomainZ / 2;

         


            Interval xInter = new Interval(-scaleRectDomainX, scaleRectDomainX);
            Interval yInter = new Interval(scaleRectDomainYUpstream, scaleRectDomainYDownstream);
            Interval zInter = new Interval(0, scaleRectDomainZ);

          

            xCells = (int)((Math.Abs(xInter.Length)) / baseMesh);
            yCells = (int)((Math.Abs(yInter.Length)) / baseMesh);
            zCells = (int)((Math.Abs(zInter.Length)) / baseMesh);


            var pl = Plane.WorldXY;
            pl.Origin = centerGroundBBox;

            //Plane newPlaneGround = new Plane()
            newBoxDomain = new Box(pl, xInter, yInter, zInter);

           

            //Point3d[] cornersGroundPlane;
            //Point3d[] = cornersGroundPlane;
            Point3d[] cornersGroundPlane = newBoxDomain.GetCorners();

            // newMinGroundPlane1 = cornersGroundPlane[1];
            // newMaxGroundPlane2 = cornersGroundPlane[3];

            Rectangle3d plGround = new Rectangle3d(pl, xInter, yInter);
            //Rectangle3d plGround = new Rectangle3d(pl, newMin, newMax);
            newBoxGround = Mesh.CreateFromPlanarBoundary(plGround.ToNurbsCurve(), mpGround);

        }

        public override string ToString()
        {
            return "OF Domain:\n" +
            "Dimensions in x: " + Math.Round(xCells * baseMesh) + " m\n" +
            "Dimensions in y: " + Math.Round(yCells * baseMesh) + " m\n" +
            "Dimensions in z: " + Math.Round(zCells * baseMesh) + " m\n" +
            "Cells in x: " + xCells + "\n" +
            "Cells in y: " + xCells + "\n" +
            "Cells in z: " + zCells + "\n"


            ;
            // return base.ToString();
        }






        public static double FindFacades(Plane plane, List<Brep> volumes, out double projectedAreaTotal)
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

            //return Facades;

            return projectedAreaTotal;
        }

    }
}
