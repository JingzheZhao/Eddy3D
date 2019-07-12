using System;
using System.Collections.Generic;
using System.Drawing;
using Rhino.Geometry;

namespace EddyLib
{
    public class OFBoxDomain : OFBaseDomain
    {
        //BoundingBox
        public double width;

        public double length;
        public double height;
        // public double yMin; public double yMax; public double xMin; public double xMax; public
        // double zMin; public double zMax; public double dimX; public double dimY; public double
        // dimZ; public double dim;

        public int xCells;
        public int yCells;
        public int zCells;

        public Mesh DomainMeshGround;
        public Mesh DomainMeshGroundPerim;
        public Box DomainBox;
        //public Mesh BoxWithDivs;

        public double diameter;
        public double blockDimension;

        public OFBoxDomain(Mesh BuildingGeometry, Mesh terrainMesh, BoundaryConditions BCond, double blockDimension, double length = 0, double width = 0, double height = 0)
        {
            this.BCond = BCond;
            this.BuildingGeometry = BuildingGeometry;
            this.blockDimension = blockDimension;

            var BBoxCrude = BuildingGeometry.GetBoundingBox(true);

            // Box-shaped tunnel can only have 1 windDir which is the 1st windDir

            Vector3d windDirVector = BCond.flowDir[0];

            // Rotate the Plane based on wind vector area

            Plane orientedPlane = GetOrientedBasePlane(windDirVector, BuildingGeometry, BBoxCrude.Center);

            // Create BBox with respect to new plane (new coordinates)
            BBox = BuildingGeometry.GetBoundingBox(orientedPlane);

            var xMin = BBox.Min.X;
            var xMax = BBox.Max.X;
            var yMin = BBox.Min.Y;
            var yMax = BBox.Max.Y;
            var zMin = BBox.Min.Z;
            var zMax = BBox.Max.Z;
            this.zMaxBuilding = zMax;

            var dimX = xMax - xMin;
            var dimY = yMax - yMin;
            var dimZ = zMax - zMin;

            ////////////
            // Center ground order is not correct but it worked before by moving Plane is wrongly projected
            ////////////

            //Plane pl = new Plane(CenterGround, orientedPlane.XAxis, orientedPlane.YAxis);
            //this.pll = pl;

            // Define offsets to place the building geometry in the middle of the domain

            //double xOffset = dimX / 2;
            //double yOffset = dimY / 2;
            double xOffset = 0;
            double yOffset = dimY;

            // Order important Z --> Y --> X

            // Z
            double scaleRectDomainZ = 0;
            if (height == 0) { scaleRectDomainZ = 6 * dimZ; }
            else { scaleRectDomainZ = height; }

            // X; take blocking ratio into account

            Bitmap FrontageImage;
            this.MaxFrontageBuildingArea = OFBaseDomain.GetProjectedBuildingArea(BCond.windDirs[0], BuildingGeometry, out FrontageImage);
            this.FrontagePNGs[BCond.windDirs[0]] = FrontageImage;

            double scaleRectDomainXblockingRatio = MaxFrontageBuildingArea * 100 / 3 / scaleRectDomainZ / 2;
            double scaleRectDomainXHeight = (5 * dimZ) + dimX / 2;
            double scaleRectDomainX = scaleRectDomainXblockingRatio > scaleRectDomainXHeight ? scaleRectDomainXblockingRatio : scaleRectDomainXHeight;

            double scaleRectDomainMinusXBP = -scaleRectDomainX - xOffset;
            double scaleRectDomainPlusXBP = scaleRectDomainX - xOffset;
            double scaleRectDomainMinusX = -width / 2 - xOffset;
            double scaleRectDomainPlusX = width / 2 - xOffset;

            // Y \cite{Tominaga2008,Franke2007}
            double scaleRectDomainYUpstreamBP = -5.5 * dimZ + dimY - yOffset;
            double scaleRectDomainYDownstreamBP = 15.5 * dimZ + dimY - yOffset;
            double scaleRectDomainYUpstream = -length / 20 * 5;
            double scaleRectDomainYDownstream = length / 20 * 15;

            Interval xInter;
            Interval yInter;
            Interval zInter;

            // X
            if (width == 0)
            {
                xInter = new Interval(scaleRectDomainMinusXBP, scaleRectDomainPlusXBP);
            }
            else
            {
                xInter = new Interval(scaleRectDomainMinusX, scaleRectDomainPlusX);
            }

            // Y
            if (length == 0)
            {
                yInter = new Interval(scaleRectDomainYUpstreamBP, scaleRectDomainYDownstreamBP);
            }
            else
            {
                yInter = new Interval(scaleRectDomainYUpstream, scaleRectDomainYDownstream);
            }

            // Z If terrain is used, scale down Z to make sure all points are inside the domain
            // Zinter is call divisionsZ for CylDomain which is an int instead of an Interval

            if (terrainMesh.Faces.Count > 0)
            {
                this.hasTerrain = true;
            }

            // zInter is with respect to the CenterGround of the Plane
            if (hasTerrain)
            {
                double zMinTerrain = OFBaseDomain.GetZMinTerrain(terrainMesh, BBox, orientedPlane);
                zInter = new Interval(zMinTerrain, zMin + scaleRectDomainZ);
            }
            else
            {
                zInter = new Interval(zMin, zMin + scaleRectDomainZ);
            }

            // Create the new Domain from 8 minmax points Doesn't work combined with rotating the domain
            /*
            Point3d p1 = new Point3d(xInter.T0, yInter.T0, zInter.T0);
            Point3d p2 = new Point3d(xInter.T1, yInter.T0, zInter.T0);
            Point3d p3 = new Point3d(xInter.T1, yInter.T1, zInter.T0);
            Point3d p4 = new Point3d(xInter.T0, yInter.T1, zInter.T0);

            Point3d p5 = new Point3d(xInter.T0, yInter.T0, zInter.T1);
            Point3d p6 = new Point3d(xInter.T1, yInter.T0, zInter.T1);
            Point3d p7 = new Point3d(xInter.T1, yInter.T1, zInter.T1);
            Point3d p8 = new Point3d(xInter.T0, yInter.T1, zInter.T1);

            IEnumerable < Point3d > MinMaxPoints = new List<Point3d>() {p1,p2,p3,p4,p5,p6,p7,p8};
            this.minmax = MinMaxPoints;
            */

            DomainBox = new Box(orientedPlane, xInter, yInter, zInter);

            // Pick location in Mesh

            Point3d maxPoint = DomainBox.GetCorners()[7];
            LocationInMesh = new Point3d(maxPoint.X - 1, maxPoint.Y - 1, maxPoint.Z - 1);

            // Create ground meshes

            MeshingParameters mpGround = MeshingParameters.Default;

            // Add terrain to ground mesh if it exists
            if (hasTerrain)
            {
                this.TerrainMesh = terrainMesh;
                this.DomainMeshGround = terrainMesh;
            }
            else
            {
                double tolerance = 0.01;

                Interval yInterPerim1 = new Interval(-xInter.T0, yInter.T0);
                Interval yInterPerim2 = new Interval(xInter.T0, yInter.T1);

                Rectangle3d plGroundCore = new Rectangle3d(orientedPlane, xInter, xInter);
                Rectangle3d plGroundPerim1 = new Rectangle3d(orientedPlane, xInter, yInterPerim1);
                Rectangle3d plGroundPerim2 = new Rectangle3d(orientedPlane, xInter, yInterPerim2);

                DomainMeshGround = Mesh.CreateFromPlanarBoundary(plGroundCore.ToNurbsCurve(), mpGround, tolerance);
                DomainMeshGroundPerim = new Mesh();
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plGroundPerim1.ToNurbsCurve(), mpGround, tolerance));
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plGroundPerim2.ToNurbsCurve(), mpGround, tolerance));
            }

            // Set up BCs
            if (BCond.btype == EddyLib.BoundaryType.constant)
            {
                BCond.SetUatBuildingHeightUconst();
            }
            if (BCond.btype == EddyLib.BoundaryType.abl)
            {
                BCond.SetUatBuildingHeightABL(zMax);
            }

            // Create final Mesh

            xCells = (int)((Math.Abs(xInter.Length)) / blockDimension);
            yCells = (int)((Math.Abs(yInter.Length)) / blockDimension);
            zCells = (int)((Math.Abs(zInter.Length)) / blockDimension);

            this.DomainMesh = Mesh.CreateFromBox(DomainBox, xCells, yCells, zCells);

            // Show only intersection of domain and terrain

            IEnumerable<Mesh> first = new List<Mesh>() { DomainMesh };
            IEnumerable<Mesh> second = new List<Mesh>() { TerrainMesh };
            this.DomainMeshIntersection = Mesh.CreateBooleanIntersection(first, second);
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
            "Projected area: " + Math.Round(this.MaxFrontageBuildingArea)
            ;
        }
    }
}