using Rhino.Geometry;
using System;

namespace EddyLib
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

        
        public Mesh DomainMeshGround;
        public Mesh DomainMeshGroundPerim;
        public Box DomainBox;
        //public Mesh BoxWithDivs;

        public double diameter;
        public double blockDimension;


      
        


        public OFBoxDomain(Mesh buildingGeometry, Mesh terrainMesh, BoundaryConditions bCond, double _blockDim)
        {
            this.BCond = bCond;

            this.BuildingGeometry = buildingGeometry;
                                 

            blockDimension = _blockDim;           

            BBox = buildingGeometry.GetBoundingBox(true);

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
            Cetner = BBox.Center + 0.5 * vecMinusZ * dimZ;
            LocationInMesh = Cetner + 4 * vecPlusZ * dimZ;



            var windDir = bCond.windDirs[0];

            var windDirVector = bCond.flowDir[0];


            //Vector3d vecWindDir = new Vector3d(Math.Sin(windDir * Math.PI / 180), Math.Cos(windDir * Math.PI / 180), 0);
            //Plane localCoordSystem = Plane.WorldZX;

            //Create Box Domain
            //Find frontfacing areas in wind direction

            FrontageBuildingArea = RunBlockMesh.ProjectedBuildingArea(windDirVector, buildingGeometry, 1, out Plane newLocal, out Box box);


            double scaleRectDomainZ = 6 * dimZ;

            // New Dimensions in X; take blocking ratio into account
            var scaleRectDomainXblockingRatio = FrontageBuildingArea * 100 / 3 / scaleRectDomainZ / 2;
            var scaleRectDomainXHeight = (5 * dimZ) + dimX / 2;
            var scaleRectDomainX = scaleRectDomainXblockingRatio > scaleRectDomainXHeight ? scaleRectDomainXblockingRatio : scaleRectDomainXHeight;

            //New Dimensions in Y \cite{Tominaga2008,Franke2007}

            double scaleRectDomainYUpstream = -(5.5 * dimZ + dimY);
            double scaleRectDomainYDownstream = 15.5 * dimZ + dimY;
            double scaleRectDomainYUpstreamCore = -scaleRectDomainX;
            double scaleRectDomainYDownstreamCore = scaleRectDomainX;


            Interval xInter = new Interval(-scaleRectDomainX, scaleRectDomainX);
            Interval yInter = new Interval(scaleRectDomainYUpstream, scaleRectDomainYDownstream);

            Interval yInterPerim1 = new Interval(-scaleRectDomainX, scaleRectDomainYUpstream);
            Interval yInterPerim2 = new Interval(scaleRectDomainX, scaleRectDomainYDownstream);


            Interval zInter;


            // If terrain is used, scale down Z to make sure all points are inside the domain
            this.TerrainMesh = terrainMesh;

            if (terrainMesh.DisjointMeshCount == 0)
            {
                zInter = new Interval(0, scaleRectDomainZ);
            }
            else
            {
                var bboxTerrain = terrainMesh.GetBoundingBox(true);
                zInter = new Interval(bboxTerrain.Min.Z - 0.1, scaleRectDomainZ);
            }
            
            xCells = (int)((Math.Abs(xInter.Length)) / blockDimension);
            yCells = (int)((Math.Abs(yInter.Length)) / blockDimension);
            zCells = (int)((Math.Abs(zInter.Length)) / blockDimension);
            
            var pl = new Plane(Cetner, newLocal.XAxis, newLocal.YAxis)
            {
                Origin = Cetner
            };

            //Plane newPlaneGround = new Plane()
            //newBoxDomain = box;
            this.DomainBox = new Box(pl, xInter, yInter, zInter);


            //Point3d[] cornersGroundPlane;
            //Point3d[] = cornersGroundPlane;
            Point3d[] cornersGroundPlane = DomainBox.GetCorners();

            // newMinGroundPlane1 = cornersGroundPlane[1];
            // newMaxGroundPlane2 = cornersGroundPlane[3];

            //Rectangle3d plGround = new Rectangle3d(pl, xInter, yInter);
            Rectangle3d plGroundCore = new Rectangle3d(pl, xInter, xInter);
            Rectangle3d plGroundPerim1 = new Rectangle3d(pl, xInter, yInterPerim1);
            Rectangle3d plGroundPerim2 = new Rectangle3d(pl, xInter, yInterPerim2);

            //Rectangle3d plGround = new Rectangle3d(pl, newMin, newMax);





            MeshingParameters mpGround = MeshingParameters.Default;

            // Add terrain to ground mesh if it exists

            if (terrainMesh.DisjointMeshCount == 0)
            {
                this.DomainMeshGround = Mesh.CreateFromPlanarBoundary(plGroundCore.ToNurbsCurve(), mpGround);
                this.DomainMeshGroundPerim = new Mesh();
                this.DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plGroundPerim1.ToNurbsCurve(), mpGround));
                this.DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plGroundPerim2.ToNurbsCurve(), mpGround));
            }
            else
            {
                this.DomainMeshGround = terrainMesh;
            }




            // refinement Cylinder
            //refinementCylinder = getRefinementCyl(center, geometry, 10);

            if (bCond.btype == BoundaryType.constant)
            {
                bCond.SetUatBuildingHeightUconst();
            }
            if (bCond.btype == BoundaryType.abl)
            {
                bCond.SetUatBuildingHeightABL(zMax);
            }



            bCond.CalculateCPPressures(zMax);


            this.DomainMesh = Mesh.CreateFromBox(DomainBox, xCells, yCells, zCells);
            //this.DomainMesh = Mesh.CreateFromBox(DomainBox);



        }

        public override string ToString()
        {
            return "Box Domain:\n" +
            "Dimensions in x: " + Math.Round(xCells * blockDimension, 1) + " m\n" +
            "Dimensions in y: " + Math.Round(yCells * blockDimension, 1) + " m\n" +
            "Dimensions in z: " + Math.Round(zCells * blockDimension, 1) + " m\n" +
            "Cells in x: " + xCells + "\n" +
            "Cells in y: " + xCells + "\n" +
            "Cells in z: " + zCells + "\n" +
            "Projected area: " + Math.Round(FrontageBuildingArea)


            ;
            // return base.ToString();
        }








    }
}
