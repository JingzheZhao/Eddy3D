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

        //public BoundingBox BBox;
        public Mesh newBoxGround;
        public Mesh newBoxGroundPerim;



        public Box newBoxDomain;


        public double diameter;
        public double blockDimension;


        public Mesh BuildingGeometry;

        //// Delete later
        //public Plane pl;
        //public Point3d center;
        //// Delete later


        public OFBoxDomain(Brep inputBreps, Mesh geometry, Mesh terrain, BoundaryConditions BCond, double _blockDim, int CPUs, string baseWorkingDirectory = @"C:\temp")
        {
            blockDimension = _blockDim;
            BuildingGeometry = geometry;
            this.CPUs = CPUs;

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

            var windDir = BCond.windDir[0];
            //Vector3d vecWindDir = new Vector3d(Math.Sin(windDir * Math.PI / 180), Math.Cos(windDir * Math.PI / 180), 0);


            //Plane localCoordSystem = Plane.WorldZX;
            var localCoordSystem = Plane.WorldZX;
            localCoordSystem.Rotate(((-1 * windDir) - 90) * Math.PI / 180, localCoordSystem.XAxis);
            localCoordSystem.Origin = center;

            localCoordSystem.Translate(localCoordSystem.YAxis * dimY);



            //Create Box Domain
            //Find frontfacing areas in wind direction

            frontageBuildingArea = ProjectedBuildingArea(localCoordSystem, BuildingGeometry);


            double scaleRectDomainZ = 6 * dimZ;

            // New Dimensions in X; take blocking ratio into account
            var scaleRectDomainXblockingRatio = frontageBuildingArea * 100 / 3 / scaleRectDomainZ / 2;
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

            this.terrainMesh = terrain;

            if (terrain.DisjointMeshCount == 0)
            {
                zInter = new Interval(0, scaleRectDomainZ);
            }
            else
            {
                var bboxTerrain = terrain.GetBoundingBox(true);
                zInter = new Interval(bboxTerrain.Min.Z - 0.1, scaleRectDomainZ);
            }


            // If terrain is used, scale down Z to make sure all points are inside the domain




            xCells = (int)((Math.Abs(xInter.Length)) / blockDimension);
            yCells = (int)((Math.Abs(yInter.Length)) / blockDimension);
            zCells = (int)((Math.Abs(zInter.Length)) / blockDimension);



            var pl = new Plane(center, localCoordSystem.ZAxis, -1 * localCoordSystem.YAxis)
            {
                Origin = center
            };

            //Plane newPlaneGround = new Plane()
            newBoxDomain = new Box(pl, xInter, yInter, zInter);



            //Point3d[] cornersGroundPlane;
            //Point3d[] = cornersGroundPlane;
            Point3d[] cornersGroundPlane = newBoxDomain.GetCorners();

            // newMinGroundPlane1 = cornersGroundPlane[1];
            // newMaxGroundPlane2 = cornersGroundPlane[3];

            //Rectangle3d plGround = new Rectangle3d(pl, xInter, yInter);
            Rectangle3d plGroundCore = new Rectangle3d(pl, xInter, xInter);
            Rectangle3d plGroundPerim1 = new Rectangle3d(pl, xInter, yInterPerim1);
            Rectangle3d plGroundPerim2 = new Rectangle3d(pl, xInter, yInterPerim2);

            //Rectangle3d plGround = new Rectangle3d(pl, newMin, newMax);





            MeshingParameters mpGround = MeshingParameters.Default;

            if (terrain.DisjointMeshCount == 0)
            {
                this.newBoxGround = Mesh.CreateFromPlanarBoundary(plGroundCore.ToNurbsCurve(), mpGround);
                this.newBoxGroundPerim = new Mesh();
                this.newBoxGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plGroundPerim1.ToNurbsCurve(), mpGround));
                this.newBoxGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plGroundPerim2.ToNurbsCurve(), mpGround));
            }
            else
            {
                this.newBoxGround = terrain;
            }




            // refinement Cylinder
            //refinementCylinder = getRefinementCyl(center, geometry, 10);

            if (BCond.btype == BoundaryType.constant)
            {
                BCond.SetUatBuildingHeightUconst();
            }
            if (BCond.btype == BoundaryType.abl)
            {
                BCond.SetUatBuildingHeightABL(zMax);
            }



            BCond.CalculateCPPressures(zMax);



            this.BCInflow = BCond;

            this.baseWorkingDirectory = baseWorkingDirectory;
            this.meshStlDirectory = baseWorkingDirectory + @"\mesh\constant\triSurface\";
            this.meshPolyMeshDirectory = baseWorkingDirectory + @"\mesh\constant\polyMesh\";
            this.meshSystemDirectory = baseWorkingDirectory + @"\mesh\system\";
            this.meshConstantDirectory = baseWorkingDirectory + @"\mesh\constant\";
            this.meshWorkingDirectory = baseWorkingDirectory + @"\mesh\";



            //needed for meshing purposes at this point in time
            this.iter = 1000;
            this.writeInterval = 10;
            this.keepTimeSteps = 2;

            this.inputBreps = inputBreps;
            this.autoCPUCalc = false;





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
            "Projected area: " + Math.Round(frontageBuildingArea)


            ;
            // return base.ToString();
        }








    }
}
