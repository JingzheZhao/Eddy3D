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
        // double zMin; public double zMax;
        public double dimX; public double dimY;

        public double dimZ;

        public int xCells;
        public int yCells;
        public int zCells;

        public Mesh DomainMeshGround;
        public Mesh DomainMeshGroundPerim;
        public Box DomainBox;
        //public Mesh BoxWithDivs;

        public double diameter;
        public double blockDimension;

        public List<Point3d> corners;
        public List<Point3d> SimDcorners;

        public Plane bbb;
        public Box bb;
        public Transform trans;

        public String info;

        public Curve test;

        public Rectangle3d plg2;
        public Rectangle3d plg1;

        public OFBoxDomain(Mesh BuildingGeometry, Mesh terrainMesh, BoundaryConditions BCond, double blockDimension, double length = 0, double width = 0, double height = 0)
        {
            this.BCond = BCond;
            this.BuildingGeometry = BuildingGeometry;
            this.blockDimension = blockDimension;

            // Box-shaped tunnel can only have 1 windDir which is the 1st windDir

            Vector3d windDirVector = BCond.flowDir[0];

            // Rotate the Plane based on wind vector area

            var bb = BuildingGeometry.GetBoundingBox(true);

            var centerBottomOfBuildings = new Point3d(bb.Center.X, bb.Center.Y, bb.Min.Z);
            Plane orientedPlane = GetOrientedBasePlane(windDirVector, centerBottomOfBuildings);

            orientedPlane.Origin = centerBottomOfBuildings;

            var l = new List<Mesh>();
            l.Add(BuildingGeometry);
            var bbbb = UnionB(l, orientedPlane);
            this.bb = bbbb;

            var BBox = bbbb;

            /*

                  var xMin = BBox..Min.X;
                  var xMax = BBox.Max.X;
                  var yMin = BBox.Min.Y;
                  var yMax = BBox.Max.Y;
                  var zMin = BBox.Min.Z;
                  var zMax = BBox.Max.Z;

                  var list = new List<Point3d >();

                  foreach( Point3d pt in BBox.GetCorners()){
                    list.Add(pt);
                  }
                  this.corners = list;
            */

            var list = new List<Point3d>();

            foreach (Point3d pt in BBox.GetCorners())
            {
                list.Add(pt);
            }
            this.corners = list;

            var xMin = BBox.X.Min;
            var xMax = BBox.X.Max;
            var yMin = BBox.Y.Min;
            var yMax = BBox.Y.Max;
            var zMin = BBox.Z.Min;
            var zMax = BBox.Z.Max;

            this.zMaxBuilding = zMax;

            dimX = xMax - xMin;
            dimY = yMax - yMin;
            dimZ = zMax - zMin;

            ////////////
            // Center ground order is not correct but it worked before by moving Plane is wrongly projected
            ////////////

            //Plane pl = new Plane(CenterGround, orientedPlane.XAxis, orientedPlane.YAxis);
            //this.pll = pl;

            // Define offsets to place the building geometry in the middle of the domain

            //double xOffset = dimX / 2;
            //double yOffset = dimY / 2;

            // Order important Z --> Y --> X

            // Z
            double scaleRectDomainZ = 0;
            if (height == 0) { scaleRectDomainZ = 6 * dimZ; }
            else { scaleRectDomainZ = height; }

            Bitmap FrontageImage;
            this.MaxFrontageBuildingArea = OFBaseDomain.GetProjectedBuildingArea(BCond.windDirs[0], BuildingGeometry, out FrontageImage);
            this.FrontagePNGs[BCond.windDirs[0]] = FrontageImage;

            // X; take blocking ratio into account
            double xOffset = 0;
            double scaleRectDomainXblockingRatio = MaxFrontageBuildingArea * 100 / 3 / scaleRectDomainZ / 2;
            double scaleRectDomainXHeight = (5 * dimZ) + dimX / 2;
            double scaleRectDomainX = scaleRectDomainXblockingRatio > scaleRectDomainXHeight ? scaleRectDomainXblockingRatio : scaleRectDomainXHeight;

            double scaleRectDomainMinusXBP = -scaleRectDomainX - xOffset;
            double scaleRectDomainPlusXBP = scaleRectDomainX - xOffset;
            double scaleRectDomainMinusX = -width / 2 - xOffset;
            double scaleRectDomainPlusX = width / 2 - xOffset;

            // Y \cite{Tominaga2008,Franke2007}
            double yOffset = dimY;
            double scaleRectDomainYUpstreamBP = -5.5 * dimZ + dimY - yOffset;
            double scaleRectDomainYDownstreamBP = 15.5 * dimZ + dimY - yOffset;
            double scaleRectDomainYUpstream = -length / 20 * 5;
            double scaleRectDomainYDownstream = length / 20 * 15;

            this.SimDcorners = corners;

            var yTransUpStr = Transform.Translation((-5.5 * dimZ + dimY - yOffset) * windDirVector);
            var yTransDownStr = Transform.Translation((15 * dimZ + dimY - yOffset) * windDirVector);

            var NormalToWindDir = Vector3d.CrossProduct(Vector3d.ZAxis, windDirVector);

            var xTransMinusX = Transform.Translation((scaleRectDomainX + dimX - xOffset) * -NormalToWindDir);
            var xTransPlusX = Transform.Translation((scaleRectDomainX + dimX - xOffset) * NormalToWindDir);

            var zTrans = Transform.Translation((6 * dimZ + dimZ) * Vector3d.ZAxis);

            Point3d P0 = this.SimDcorners[0];
            Point3d P1 = this.SimDcorners[1];
            Point3d P2 = this.SimDcorners[2];
            Point3d P3 = this.SimDcorners[3];
            Point3d P4 = this.SimDcorners[4];
            Point3d P5 = this.SimDcorners[5];
            Point3d P6 = this.SimDcorners[6];
            Point3d P7 = this.SimDcorners[7];

            // Move all in y

            P0.Transform(yTransUpStr);
            P1.Transform(yTransUpStr);
            P4.Transform(yTransUpStr);
            P5.Transform(yTransUpStr);

            P2.Transform(yTransDownStr);
            P3.Transform(yTransDownStr);
            P6.Transform(yTransDownStr);
            P7.Transform(yTransDownStr);

            // Move all in x

            P0.Transform(xTransMinusX);
            P3.Transform(xTransMinusX);
            P7.Transform(xTransMinusX);
            P4.Transform(xTransMinusX);

            P1.Transform(xTransPlusX);
            P5.Transform(xTransPlusX);
            P2.Transform(xTransPlusX);
            P6.Transform(xTransPlusX);

            // Move all in x

            P4.Transform(zTrans);
            P5.Transform(zTrans);
            P6.Transform(zTrans);
            P7.Transform(zTrans);

            //this.test = P1;

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

            this.length = Math.Round(xCells * blockDimension, 1);
            this.width = Math.Round(yCells * blockDimension, 1);
            this.height = Math.Round(zCells * blockDimension, 1);
            this.CenterGround = centerBottomOfBuildings;

            IEnumerable<Point3d> ccc = new List<Point3d> { P0, P1, P2, P3, P4, P5, P6, P7 };

            DomainBox = new Box(orientedPlane, ccc);

            // Pick location in Mesh

            var vec = new Vector3d(0, 0, 5);
            var moveUP = Transform.Translation(vec);
            var bg = BuildingGeometry.GetBoundingBox(true).GetCorners()[7];

            LocationInMesh = new Point3d(DomainBox.Center.X, DomainBox.Center.Y, bg.Z);
            LocationInMesh.Transform(moveUP);

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

                this.plg1 = new Rectangle3d(orientedPlane, corners[0], corners[2]);

                IEnumerable<Point3d> p2 = new List<Point3d> { P0, P1, corners[0], corners[1], P0 };
                var plg2 = new Rhino.Geometry.Polyline(p2);

                IEnumerable<Point3d> p3 = new List<Point3d> { P0, corners[1], corners[2], P3, P0 };
                var plg3 = new Rhino.Geometry.Polyline(p3);

                IEnumerable<Point3d> p4 = new List<Point3d> { P2, P3, corners[2], corners[3], P2 };
                var plg4 = new Rhino.Geometry.Polyline(p4);

                IEnumerable<Point3d> p5 = new List<Point3d> { corners[3], corners[0], P1, P2, corners[3] };
                var plg5 = new Rhino.Geometry.Polyline(p5);

                this.test = plg5.ToNurbsCurve();

                DomainMeshGround = Utilities.ConvertToQuads(Mesh.CreateFromPlanarBoundary(plg1.ToNurbsCurve(), mpGround, tolerance));

                DomainMeshGroundPerim = new Mesh();
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plg2.ToPolylineCurve(), mpGround, tolerance));
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plg3.ToPolylineCurve(), mpGround, tolerance));
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plg4.ToPolylineCurve(), mpGround, tolerance));
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plg5.ToPolylineCurve(), mpGround, tolerance));
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

            ToString();
        }

        private Box UnionB(List<Mesh> list, Plane pl)
        {
            Transform val = Transform.ChangeBasis(Plane.WorldXY, pl);

            Point3d val3;
            Point3d val4;
            Interval val5 = default(Interval);
            Point3d val7;
            Point3d val8;
            Interval val9 = default(Interval);
            Point3d val11;
            Point3d val12;
            Interval val13 = default(Interval);

            List<Box> list2 = new List<Box>();
            List<Box> list3 = new List<Box>();
            int num2 = list.Count - 1;
            Box item = default(Box);
            Box item2 = default(Box);
            for (int j = 0; j <= num2; j++)
            {
                if (list[j] == null)
                {
                    list2.Add(Box.Unset);
                    list3.Add(Box.Unset);
                    continue;
                }
                BoundingBox boundingBox2 = list[j].GetBoundingBox(val);
                Plane val15 = pl;
                val12 = boundingBox2.Min;
                double x2 = ((Point3d)(val12)).X;
                val11 = ((BoundingBox)(boundingBox2)).Max;
                val13 = new Interval(x2, ((Point3d)(val11)).X);
                Interval val16 = val13;
                val8 = ((BoundingBox)(boundingBox2)).Min;
                double y2 = ((Point3d)(val8)).Y;
                val7 = ((BoundingBox)(boundingBox2)).Max;
                val9 = new Interval(y2, val7.Y);
                Interval val17 = val9;
                val4 = ((BoundingBox)(boundingBox2)).Min;
                double z2 = ((Point3d)(val4)).Z;
                val3 = ((BoundingBox)(boundingBox2)).Max;
                val5 = new Interval(z2, ((Point3d)(val3)).Z);
                item = new Box(val15, val16, val17, val5);
                list2.Add(item);
                item2 = new Box(boundingBox2);
                list3.Add(item2);
            }

            return item;
        }

        public override string ToString()
        {
            return "Box Domain:\n" +
            "Width: " + Math.Round(xCells * blockDimension, 1) + " m\n" +
            "Length: " + Math.Round(yCells * blockDimension, 1) + " m\n" +
            "Height: " + Math.Round(zCells * blockDimension, 1) + " m\n" +
            "Cells in x: " + xCells + "\n" +
            "Cells in y: " + xCells + "\n" +
            "Cells in z: " + zCells + "\n" +
            "Projected area: " + Math.Round(this.MaxFrontageBuildingArea)
            ;
        }
    }
}