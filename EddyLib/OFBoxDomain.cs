using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Rhino.Geometry;

namespace EddyLib
{
    public class OFBoxDomain : OFBaseDomain
    {
        //BoundingBox
        public double width;

        public double length;
        public double height;

        public double Length_BBox;
        public double Width_BBox;
        public double Height_BBox;

        public double Width_SBox;
        public double Length_SBox;
        public double Height_SBox;

        public int CellsAlongWidth;
        public int CellsAlongLength;
        public int CellsAlongHeight;

        public Mesh DomainMeshGround;
        public Mesh DomainMeshGroundPerim;
        public Box SBox;

        //public Mesh BoxWithDivs;

        public double blockDimension;

        public string info;

        public double test;

        public OFBoxDomain(Mesh BuildingGeometry, Mesh terrainMesh, BoundaryConditions BCond, double blockDimension, double length = 0, double width = 0, double height = 0)
        {
            this.BCond = BCond;
            this.BuildingGeometry = BuildingGeometry;
            this.blockDimension = blockDimension;

            // Box-shaped tunnel can only have 1 windDir which is the 1st windDir

            Vector3d windDirVector = BCond.flowDir[0];
            windDirVector.Unitize();

            // Rotate the Plane based on wind vector area

            var bb = BuildingGeometry.GetBoundingBox(true);

            var centerBottomOfBuildings = new Point3d(bb.Center.X, bb.Center.Y, bb.Min.Z);
            Plane orientedPlane = GetOrientedBasePlane(windDirVector, centerBottomOfBuildings);

            orientedPlane.Origin = centerBottomOfBuildings;

            var l = new List<Mesh>();
            l.Add(BuildingGeometry);
            this.BBox = UnionB(l, orientedPlane);

            var corners = new List<Point3d>();

            foreach (Point3d pt in BBox.GetCorners())
            {
                corners.Add(pt);
            }

            var MinHeightBBox = corners[0].Z;
            var MaxHeightBBox = corners[4].Z;
            this.MaxHeightBuilding = MaxHeightBBox;

            Length_BBox = corners[0].DistanceTo(corners[1]);
            Width_BBox = corners[0].DistanceTo(corners[3]);
            Height_BBox = corners[0].DistanceTo(corners[4]);

            Bitmap FrontageImage;
            this.MaxFrontageBuildingArea = OFBaseDomain.GetProjectedBuildingArea(BCond.windDirs[0], BuildingGeometry, out FrontageImage);
            this.FrontagePNGs[BCond.windDirs[0]] = FrontageImage;

            // Order important Z --> Y --> X

            // Z
            double scaleZ = height == 0 ? 5 * Height_BBox : height;

            // X; take blocking ratio into account

            double WidthDueToBlockingRatio = ((RequiredInletArea(MaxFrontageBuildingArea) / (6 * Height_BBox)) - (MaxFrontageBuildingArea / MaxHeightBuilding)) / 2;

            double scaleRectDomainXUser = 0;

            if (width != 0)
            {
                scaleRectDomainXUser = width / 2;
            }

            double scaleX = width == 0 ? WidthDueToBlockingRatio : scaleRectDomainXUser;

            // Y
            double scaleYDownStream = length == 0 ? 15 * Height_BBox : length / 3 * 2;
            double scaleYUpStream = length == 0 ? 5 * Height_BBox : length / 3 * 1;

            var yTransUpStr = Transform.Translation(scaleYUpStream * -windDirVector);
            var yTransDownStr = Transform.Translation(scaleYDownStream * windDirVector);

            var NormalToWindDir = Vector3d.CrossProduct(windDirVector, Vector3d.ZAxis);
            NormalToWindDir.Unitize();

            var xTransMinusX = Transform.Translation((scaleX) * -NormalToWindDir);
            var xTransPlusX = Transform.Translation((scaleX) * NormalToWindDir);

            var zTrans = Transform.Translation((scaleZ) * Vector3d.ZAxis);

            Point3d P0 = corners[0];
            Point3d P1 = corners[1];
            Point3d P2 = corners[2];
            Point3d P3 = corners[3];
            Point3d P4 = corners[4];
            Point3d P5 = corners[5];
            Point3d P6 = corners[6];
            Point3d P7 = corners[7];

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

            // Move all in z

            P4.Transform(zTrans);
            P5.Transform(zTrans);
            P6.Transform(zTrans);
            P7.Transform(zTrans);

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

                var zTransTerrain = Transform.Translation((Math.Abs(zMinTerrain - centerBottomOfBuildings.Z)) * -Vector3d.ZAxis);

                P0.Transform(zTransTerrain);
                P1.Transform(zTransTerrain);
                P2.Transform(zTransTerrain);
                P3.Transform(zTransTerrain);

                MinHeightBBox = zMinTerrain;
            }

            // Update Dimensions now that we know the terrain

            Height_BBox = MaxHeightBBox - MinHeightBBox;

            IEnumerable<Point3d> Corners = new List<Point3d> { P0, P1, P2, P3, P4, P5, P6, P7 };
            SBox = new Box(orientedPlane, Corners);

            // Compute dimension of simulation domain

            Width_SBox = Math.Abs(SBox.X.Max - SBox.X.Min);
            Length_SBox = Math.Abs(SBox.Y.Max - SBox.Y.Min);
            Height_SBox = Math.Abs(SBox.Z.Max - SBox.Z.Min);

            // Pick location in Mesh

            SetLocationInMesh(BuildingGeometry, scaleZ);

            // Add terrain to ground mesh if it exists
            if (hasTerrain)
            {
                this.TerrainMesh = terrainMesh;
                this.DomainMeshGround = terrainMesh;
            }
            else
            {
                double tolerance = 0.01;
                // Create ground meshes

                MeshingParameters mpGround = MeshingParameters.Default;

                // Inner Ground Mesh

                var plg1 = new Rectangle3d(orientedPlane, corners[0], corners[2]);
                DomainMeshGround = Utilities.ConvertToQuads(Mesh.CreateFromPlanarBoundary(plg1.ToNurbsCurve(), mpGround, tolerance));

                // 4 Surrounding Ground Meshes

                IEnumerable<Point3d> p2 = new List<Point3d> { P0, P1, corners[0], corners[1], P0 };
                var plg2 = new Rhino.Geometry.Polyline(p2);

                IEnumerable<Point3d> p3 = new List<Point3d> { P0, corners[1], corners[2], P3, P0 };
                var plg3 = new Rhino.Geometry.Polyline(p3);

                IEnumerable<Point3d> p4 = new List<Point3d> { P2, P3, corners[2], corners[3], P2 };
                var plg4 = new Rhino.Geometry.Polyline(p4);

                IEnumerable<Point3d> p5 = new List<Point3d> { corners[3], corners[0], P1, P2, corners[3] };
                var plg5 = new Rhino.Geometry.Polyline(p5);

                DomainMeshGroundPerim = new Mesh();
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plg2.ToPolylineCurve(), mpGround, tolerance));
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plg3.ToPolylineCurve(), mpGround, tolerance));
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plg4.ToPolylineCurve(), mpGround, tolerance));
                DomainMeshGroundPerim.Append(Mesh.CreateFromPlanarBoundary(plg5.ToPolylineCurve(), mpGround, tolerance));
            }

            // Create final Mesh

            CellsAlongWidth = (int)((Math.Abs(Width_SBox)) / blockDimension);
            CellsAlongLength = (int)((Math.Abs(Length_SBox)) / blockDimension);
            CellsAlongHeight = (int)((Math.Abs(Height_SBox)) / blockDimension);

            this.CenterGround = centerBottomOfBuildings;

            this.DomainMesh = Mesh.CreateFromBox(SBox, CellsAlongWidth, CellsAlongLength, CellsAlongHeight);

            // Show only intersection of domain and terrain

            IEnumerable<Mesh> first = new List<Mesh>() { DomainMesh };
            IEnumerable<Mesh> second = new List<Mesh>() { TerrainMesh };
            this.DomainMeshIntersection = Mesh.CreateBooleanDifference(first, second);

            // Set up BCs
            if (BCond.btype == EddyLib.BoundaryType.constant)
            {
                BCond.SetUatBuildingHeightUconst();
            }
            if (BCond.btype == EddyLib.BoundaryType.abl)
            {
                BCond.SetUatBuildingHeightABL(MaxHeightBuilding);
            }

            ToString();
        }

        private void SetLocationInMesh(Mesh BuildingGeo, double scaleZ)
        {
            var vec = new Vector3d(0, 0, 0.1 * scaleZ);
            var moveUP = Transform.Translation(vec);
            var bg = BuildingGeo.GetBoundingBox(true).GetCorners()[7];

            LocationInMesh = new Point3d(SBox.Center.X, SBox.Center.Y, bg.Z);
            LocationInMesh.Transform(moveUP);
        }

        private double RequiredInletArea(double FrontageFacadeArea)
        {
            // Required area for 3 % blocking ratio
            return 100 / 3 * (FrontageFacadeArea);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(@"Building Geometries
        Width: " + Math.Round(this.Length_BBox, 0) + " m\n" +
              "Length: " + Math.Round(this.Width_BBox, 0) + " m\n" +
              "Height: " + Math.Round(this.Height_BBox, 0) + " m\n" + "Projected facade area: " + Math.Round(this.MaxFrontageBuildingArea) + " m^2");

            sb.AppendLine("\nBox Domain\n" +
              "Width: " + Math.Round(this.Width_SBox, 0) + " m\n" +
              "Length: " + Math.Round(this.Length_SBox, 0) + " m\n" +
              "Height: " + Math.Round(this.Height_SBox, 0) + " m\n" +
              "Cells along width: " + CellsAlongWidth + "\n" +
              "Cells along length: " + CellsAlongLength + "\n" +
              "Cells along height: " + CellsAlongHeight + "\n");

            this.info = sb.ToString();

            return sb.ToString();
        }
    }
}