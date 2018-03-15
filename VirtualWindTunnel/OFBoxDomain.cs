using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Eddy
{
    public class OFBoxDomain : OFBaseDomain
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
     

        public int xCells;
        public int yCells;
        public int zCells;

        //public BoundingBox BBox;
        public Mesh newBoxGround;
        public Mesh newCylGround;
        public Box newBoxDomain;
    

        public Rectangle3d plGround;

        public double diameter;  
        public double blockDimension;
        

        public Mesh BuildingGeometry;

        //// Delete later
        //public Plane pl;
        //public Point3d center;
        //// Delete later


        public OFBoxDomain(Mesh geometry, List<BoundaryConditions> BCond,  string _workingDirectory, double _blockDim)
        {

  


            workingDirectory = _workingDirectory;
            systemDirectory = workingDirectory + @"system\";
            blockDimension = _blockDim;
            BuildingGeometry = geometry;
            //BoundingBox bb = domain.GetBoundingBox(true);





            BBox = BuildingGeometry.GetBoundingBox(true);
                   
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
            center = BBox.Center + 0.5 * vecMinusZ * dimZ;            
            locationInMesh = center + 4 * vecPlusZ * dimZ;



            Plane localCoordSystem = Plane.WorldZX;
            localCoordSystem.Origin = center;

            localCoordSystem.Translate(vecMinusY * dimY);



            //Create Box Domain
            //Find frontfacing areas in wind direction
     
            frontageBuildingArea = projectedBuildingArea(localCoordSystem, BuildingGeometry);
           

            //New Dimensions in Y \cite{Tominaga2008,Franke2007}
            double scaleRectDomainYUpstream = - (5.5 * dimZ + dimY);
            double scaleRectDomainYDownstream = 15.5 * dimZ + dimY;
            double scaleRectDomainZ = 6* dimZ;

            // New Dimensions in X; take blocking ratio into account
            var scaleRectDomainXblockingRatio  = frontageBuildingArea * 100 / 3 / scaleRectDomainZ / 2;
            var scaleRectDomainXHeight = (5* dimZ)+dimX/2;


            var scaleRectDomainX = scaleRectDomainXblockingRatio > scaleRectDomainXHeight ? scaleRectDomainXblockingRatio : scaleRectDomainXHeight;

            Interval xInter = new Interval(-scaleRectDomainX, scaleRectDomainX);
            Interval yInter = new Interval(scaleRectDomainYUpstream, scaleRectDomainYDownstream);
            Interval zInter = new Interval(0, scaleRectDomainZ);

          

            xCells = (int)((Math.Abs(xInter.Length)) / blockDimension);
            yCells = (int)((Math.Abs(yInter.Length)) / blockDimension);
            zCells = (int)((Math.Abs(zInter.Length)) / blockDimension);


            Plane pl = Plane.WorldXY;
            pl.Origin = center;

            //Plane newPlaneGround = new Plane()
            newBoxDomain = new Box(pl, xInter, yInter, zInter);

           

            //Point3d[] cornersGroundPlane;
            //Point3d[] = cornersGroundPlane;
            Point3d[] cornersGroundPlane = newBoxDomain.GetCorners();

            // newMinGroundPlane1 = cornersGroundPlane[1];
            // newMaxGroundPlane2 = cornersGroundPlane[3];

            Rectangle3d plGround = new Rectangle3d(pl, xInter, yInter);
            //Rectangle3d plGround = new Rectangle3d(pl, newMin, newMax);
            MeshingParameters mpGround = MeshingParameters.Default;
            newBoxGround = Mesh.CreateFromPlanarBoundary(plGround.ToNurbsCurve(), mpGround);
                                            
            
            
            // refinement Cylinder

            refinementCylinder = getRefinementCyl(center, geometry, 10);
            this.BCInflow = BCond;
        }

        public override string ToString()
        {
            return "Box Domain:\n" +
            "Dimensions in x: " + Math.Round(xCells * blockDimension, 1) + " m\n" +
            "Dimensions in y: " + Math.Round(yCells * blockDimension, 1) + " m\n" +
            "Dimensions in z: " + Math.Round(zCells * blockDimension, 1) + " m\n" +
            "Cells in x: " + xCells + "\n" +
            "Cells in y: " + xCells + "\n" +
            "Cells in z: " + zCells + "\n"+
            "Projected area: " + Math.Round(frontageBuildingArea)


            ;
            // return base.ToString();
        }






      

    }
}
