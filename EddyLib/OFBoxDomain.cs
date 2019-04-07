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

        
        public OFBoxDomain(Mesh buildingGeometry, Mesh terrainMesh, BoundaryConditions bCond, double _blockDim, double length = 0, double width = 0, double height = 0)
        {
            BCond = bCond;

            BuildingGeometry = buildingGeometry;


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




            int windDir = bCond.windDirs[0];

            Vector3d windDirVector = bCond.flowDir[0];


            //Vector3d vecWindDir = new Vector3d(Math.Sin(windDir * Math.PI / 180), Math.Cos(windDir * Math.PI / 180), 0);
            //Plane localCoordSystem = Plane.WorldZX;

            //Create Box Domain
            //Find frontfacing areas in wind direction

            FrontageBuildingArea = RunBlockMesh.ProjectedBuildingArea(windDirVector, buildingGeometry, 10, out Plane newLocal, out Box box);

            double scaleRectDomainZ = 0;

            if (height == 0) { scaleRectDomainZ = 6 * dimZ; }
            else { scaleRectDomainZ = height; }



            // New Dimensions in X; take blocking ratio into account
            double scaleRectDomainXblockingRatio = FrontageBuildingArea * 100 / 3 / scaleRectDomainZ / 2;
            double scaleRectDomainXHeight = (5 * dimZ) + dimX / 2;
            double scaleRectDomainX = scaleRectDomainXblockingRatio > scaleRectDomainXHeight ? scaleRectDomainXblockingRatio : scaleRectDomainXHeight;

            //New Dimensions in Y \cite{Tominaga2008,Franke2007}

            double scaleRectDomainYUpstream = -(5.5 * dimZ + dimY);
            double scaleRectDomainYDownstream = 15.5 * dimZ + dimY;

            Interval xInter = new Interval(0, 0);
            if (width == 0)
            {
                xInter = new Interval(-scaleRectDomainX, scaleRectDomainX);
            }
            else
            {
                xInter = new Interval(-width / 2, width / 2);
            }

            Interval yInter = new Interval(0, 0);
            if (length == 0)
            {
                yInter = new Interval(scaleRectDomainYUpstream, scaleRectDomainYDownstream);
            }
            else
            {
                yInter = new Interval(length / 22 * -5.5, length / 22 * 15.5);
            }


            Interval yInterPerim1 = new Interval(-xInter.T0, yInter.T0);
            Interval yInterPerim2 = new Interval(xInter.T0, yInter.T1);

                                          
            // If terrain is used, scale down Z to make sure all points are inside the domain
            Interval zInter;


            // If terrain is used, scale down Z to make sure all points are inside the domain
            // Zinter is call divisionsZ for CylDomain which is an int instead of an Interval
            

            double zDomain = OFBaseDomain.GetZMinTerrain(terrainMesh, BBox);

            zInter = new Interval(zDomain, scaleRectDomainZ + Math.Abs(zDomain));


            xCells = (int)((Math.Abs(xInter.Length)) / blockDimension);
            yCells = (int)((Math.Abs(yInter.Length)) / blockDimension);
            zCells = (int)((Math.Abs(zInter.Length)) / blockDimension);


            //Create ground plane of BBox
            CenterGround = BBox.Center + 0.5 * vecMinusZ;
            LocationInMesh = BBox.Center + 1 * vecPlusZ * dimZ;


            Plane pl = new Plane(CenterGround, newLocal.XAxis, newLocal.YAxis)
            {
                Origin = CenterGround
            };

            // Create the new Domain
            DomainBox = new Box(pl, xInter, yInter, zInter);
            _ = DomainBox.GetCorners();

            Rectangle3d plGroundCore = new Rectangle3d(pl, xInter, xInter);
            Rectangle3d plGroundPerim1 = new Rectangle3d(pl, xInter, yInterPerim1);
            Rectangle3d plGroundPerim2 = new Rectangle3d(pl, xInter, yInterPerim2);
                                 

            MeshingParameters mpGround = MeshingParameters.Default;

            // Add terrain to ground mesh if it exists

            if (terrainMesh.Faces.Count == 0)
            {
                double tolerance = 0.01;
                DomainMeshGround = Mesh.CreateFromPlanarBoundary(plGroundCore.ToNurbsCurve(), mpGround, tolerance); 
                DomainMeshGroundPerim = new Mesh();
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plGroundPerim1.ToNurbsCurve(), mpGround, tolerance));
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plGroundPerim2.ToNurbsCurve(), mpGround, tolerance));
            }
            else
            {
                this.hasTerrain = true;
                this.TerrainMesh = terrainMesh;
                this.DomainMeshGround = terrainMesh;
            }

            


            if (bCond.btype == BoundaryType.constant)
            {
                bCond.SetUatBuildingHeightUconst();
            }
            if (bCond.btype == BoundaryType.abl)
            {
                bCond.SetUatBuildingHeightABL(zMax);
            }



            bCond.CalculateCPPressures(zMax, bCond.btype, bCond.URef);


            this.DomainMesh = Mesh.CreateFromBox(DomainBox, xCells, yCells, zCells);          



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
            "Projected area: " + Math.Round(this.FrontageBuildingArea)
            ;

        }








    }
}
