using EddyLib.BCs;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace EddyLib
{
    public class OFCylDomain : OFBaseDomain
    {
        //public BoundingBox BBox; //moved to BaseDomain so that is globally accessible

        public double radius;

        public double height;

        public int divisionsX = 1;

        public int divsRadial;

        public int divisionsZ;

        public int divPerim;

        public double gradingPerim;

        public double cellSizeInner;

        //Meshes from Cycl Domain
        public Mesh CylDomainMesh = new Mesh();

        public Mesh CylDomainMeshGround = new Mesh();

        public Mesh CylDomainMeshGroundPerim = new Mesh();

        public Mesh perimBottom = new Mesh();

        public Mesh coreBottom = new Mesh();

        public Mesh perimTop = new Mesh();

        public Mesh coreTop = new Mesh();

        public Mesh sides = new Mesh();

        public double sizeInnerR;

        public List<Polyline> concentricDivisions;

        public List<Circle> outerCircles;

        // Remove this later
        public Point3d[] pointsOnCircle;

        public Point3d[] pointsOnRect;

        /// <summary>
        /// Creates a cylindrical domain for CFD simulation.
        /// </summary>
        public OFCylDomain(Mesh BuildingGeometry, Mesh terrainMesh, BCCollection BCond, double coreBlockSize, double sizeInnerRect = 0, double sizeOuterCirc = 0, double sizeHeight = 0, List<Tree> Trees = null, double radialMultiplier = 2.0, int divisionsX = 1)
        {
            gradingPerim = 1.0;
            cellSizeInner = coreBlockSize;
            this.divisionsX = divisionsX;
            this.BuildingGeometry = BuildingGeometry;

            // Step 1: Initialize bounding box and dimensions
            InitializeBoundingBox(BuildingGeometry);

            // Step 2: Set up terrain if present
            InitializeTerrain(terrainMesh);

            // Step 3: Compute domain dimensions
            ComputeDomainDimensions(sizeHeight, sizeOuterCirc, sizeInnerRect, coreBlockSize, radialMultiplier);

            // Step 4: Calculate frontage areas for all wind directions
            ComputeFrontageAreas(BuildingGeometry);

            // Step 5: Create mesh and initialize boundary conditions
            MakeCircMeshPlane(CenterGround, sizeInnerR, divsRadial, radius, height, (int)coreBlockSize);
            InitializeBoundaryConditions(BCond, MaxHeightBuilding);

            // Step 6: Store trees
            this.Trees = Trees;

            ToString();
        }

        /// <summary>
        /// Initializes the bounding box from building geometry.
        /// </summary>
        private void InitializeBoundingBox(Mesh buildingGeometry)
        {
            this.BBox = BuildBoundingBox(buildingGeometry, Plane.WorldXY);
            this.MaxHeightBuilding = BBox.Z.Max;
            this.height = BBox.Z.Max - BBox.Z.Min;
        }

        /// <summary>
        /// Sets up terrain mesh and ground center point.
        /// </summary>
        private void InitializeTerrain(Mesh terrainMesh)
        {
            if (terrainMesh != null && terrainMesh.Faces.Count > 0)
            {
                this.HasTerrain = true;
                this.TerrainMesh = terrainMesh;
                double zMinTerrain = OFBaseDomain.GetZMinTerrain(terrainMesh, BBox, Plane.WorldXY);
                this.CenterGround = new Point3d(BBox.Center.X, BBox.Center.Y, zMinTerrain);
            }
            else
            {
                this.CenterGround = new Point3d(BBox.Center.X, BBox.Center.Y, BBox.Z.Min);
            }
        }

        /// <summary>
        /// Computes domain dimensions: height, radius, inner rectangle size, and radial divisions.
        /// </summary>
        private void ComputeDomainDimensions(double sizeHeight, double sizeOuterCirc, double sizeInnerRect, double coreBlockSize, double radialMultiplier)
        {
            double dimY = BBox.Y.Max - BBox.Y.Min;
            double dimZ = BBox.Z.Max - BBox.Z.Min;

            // Calculate height
            if (sizeHeight == 0)
            {
                height = 6 * dimZ + (BBox.Z.Min - CenterGround.Z);
            }
            else
            {
                height = sizeHeight;
            }

            // Calculate radius
            double scaleDomByHeight = (15.5 * dimZ) + dimY;
            double scaleCylDomainFromBlockingRatio = MaxFrontageBuildingArea * 100 / 3 / height / 2;

            if (sizeOuterCirc == 0)
            {
                radius = Math.Max(scaleCylDomainFromBlockingRatio, scaleDomByHeight);
            }
            else
            {
                radius = sizeOuterCirc;
            }
            this.radius = radius; // Ensure field is set

            // Calculate inner rectangle size
            if (sizeInnerRect == 0)
            {
                sizeInnerR = radius * 0.35;
            }
            else
            {
                sizeInnerR = sizeInnerRect;
            }

            divsRadial = RadialDivsFromBlockSize(coreBlockSize, sizeInnerR, radialMultiplier);
        }

        /// <summary>
        /// Computes projected building frontage areas for 9 wind directions.
        /// </summary>
        private void ComputeFrontageAreas(Mesh buildingGeometry)
        {
            // Sample 9 wind directions (0, 40, 80, 120, ... 320 degrees)
            for (int i = 0; i < 9; i++)
            {
                int angle = i * 40;
                Bitmap frontagePNG;
                this.FrontageBuildingAreas[angle] = OFBaseDomain.GetProjectedBuildingArea(angle, buildingGeometry, out frontagePNG);
                this.FrontagePNGs[angle] = frontagePNG;
            }
            MaxFrontageBuildingArea = FrontageBuildingAreas.Max();
        }

        private void MakeCircMeshPlane(Point3d center, double sizeInnerRect, int divsRadial, double circleRadius, double height, int coreBlockSize)
        {
            // point inside cdf domain - needed for meshing and finding the void space for fluid
            LocationInMesh = center + (Vector3d.ZAxis * (height - 0.1));

            // move into periphery
            LocationInMesh += radius * 0.6 * Vector3d.XAxis;

            Plane pl = new Plane(center, Vector3d.ZAxis);

            Interval xinter = new Interval(-sizeInnerRect, sizeInnerRect);

            // Throws exeption if 0
            if (divsRadial < 1) { divsRadial = 1; }
            coreBottom = Mesh.CreateFromPlane(pl, xinter, xinter, divsRadial, divsRadial); // creates the inner rectangle with arbitrary subdivision

            double minRad = Math.Sqrt(2 * (sizeInnerRect * sizeInnerRect)) * 1.1; // *1.1 to account for collapsing face on boundary
            double circRad = circleRadius;
            if (circleRadius < minRad)
            {
                circRad = minRad;
            }

            double cellSizeCore = 2 * (sizeInnerRect / divsRadial);
            //double cellSizeCore = 4 * (sizeInnerRect / divsRadial); //test by ilker

            //Math.Abs was just a workaround fix

            //this.cellDivisionsPerim = Math.Abs((int)Math.Round((circRad - (2 * sizeInnerRect)) / cellSizeCore));

            // divisionsZ must be min 1

            if ((int)(height / cellSizeCore) < 1)
            {
                divisionsZ = 1;
            }
            else
            {
                divisionsZ = (int)(height / cellSizeCore);
            }

            Polyline nakedEdges = coreBottom.GetNakedEdges()[0]; //returns a polygon with line segments for each mesh cell // BREAKS Rhino 8.9
            Polyline adjustedPolyline = AdjustPolylineSeamAndOrientation(nakedEdges, true); // Fix for Rhino 8.9 and newer

            SetPointsOnRect(divsRadial, adjustedPolyline);
            SetPointsOnCircle(center, circRad, adjustedPolyline);

            double blockDimensionCore = BlockDimensionCore(pointsOnRect);
            divPerim = DivisionsPerim(pointsOnRect, pointsOnCircle, blockDimensionCore);

            ////////////////////
            //Visualize divisions inside cylindrical perimeter
            ////////////////////
            // Points on inner rectangle from naked edges

            this.concentricDivisions = GetConcenctricPolyDivisions(pointsOnRect, pointsOnCircle, divPerim, height);
            this.outerCircles = GetOuterCircles(divisionsZ, circRad, CenterGround, (int)cellSizeCore);

            coreTop.Append(coreBottom);
            coreTop.Translate(Vector3d.ZAxis * height);
            perimBottom = PerimeterRing(adjustedPolyline, pointsOnCircle);
            perimTop.Append(perimBottom);
            perimTop.Translate(Vector3d.ZAxis * height);

            sides = SideWalls(pointsOnCircle, height);

            CheckAndFlipMeshNormals();
            WeldAllIndividualMeshes();

            CylDomainMeshGround.Append(coreBottom);
            CylDomainMeshGroundPerim.Append(perimBottom);
            CylDomainMesh.Append(perimBottom);
            CylDomainMesh.Append(coreBottom);
            CylDomainMesh.Append(perimTop);
            CylDomainMesh.Append(coreTop);
            CylDomainMesh.Append(sides);
            CylDomainMesh.Normals.ComputeNormals();
            CylDomainMesh.Weld(Math.PI);
            CylDomainMesh.Vertices.CombineIdentical(true, true);

            this.DomainMesh = CylDomainMesh;

            // Show only intersection of domain and terrain

            IEnumerable<Mesh> first = new Mesh[] { DomainMesh };
            IEnumerable<Mesh> second = new Mesh[] { TerrainMesh };
            this.DomainMeshIntersection = Mesh.CreateBooleanIntersection(first, second);
        }

        private Polyline AdjustPolylineSeamAndOrientation(Polyline nakedEdge, bool reverseOrientation = true)
        {
            // Convert the polyline to a NurbsCurve
            NurbsCurve curve = nakedEdge.ToNurbsCurve();

            // Get the curve's domain
            double tStart = curve.Domain.Min;
            double tEnd = curve.Domain.Max;

            // Find the first discontinuity
            double discontinuityParam;
            curve.GetNextDiscontinuity(Continuity.G1_continuous, tStart, tEnd, out discontinuityParam);

            // Move the seam to the discontinuity
            curve.ChangeClosedCurveSeam(discontinuityParam);

            // Reverse the curve orientation if required
            if (reverseOrientation)
            {
                curve.Reverse();
            }

            // Convert the adjusted NurbsCurve back to a Polyline
            Polyline newPolyline;
            if (!curve.TryGetPolyline(out newPolyline))
            {
                throw new Exception("Failed to convert curve back to polyline.");
            }

            return newPolyline;
        }

        private void WeldAllIndividualMeshes()

        {
            this.coreBottom.Weld(Math.PI);
            this.coreTop.Weld(Math.PI);
            this.perimBottom.Weld(Math.PI);
            this.perimTop.Weld(Math.PI);
            this.sides.Weld(Math.PI);
        }

        private void CheckAndFlipMeshNormals()
        {
            ////////////////
            ///Flip perim top if they don't point in the same direction
            ///

            var pbv = perimBottom.Normals[0];
            pbv.Unitize();
            var perimBottomDot = pbv * Vector3d.ZAxis;

            if (perimBottomDot > 0)
            {
                perimBottom.Flip(true, true, true);
            }

            ////////////////
            ///Flip perim top if they don't point in the same direction
            ///

            var pv = perimTop.Normals[0];
            pv.Unitize();
            var perimTopDot = pv * Vector3d.ZAxis;

            if (perimTopDot < 0)
            {
                perimTop.Flip(true, true, true);
            }

            ////////////////
            ///Flip core top if they don't point in the same direction
            ///

            var v = coreTop.Normals[0];
            v.Unitize();
            var coreTopDot = v * Vector3d.ZAxis;

            if (coreTopDot < 0)
            {
                coreTop.Flip(true, true, true);
            }

            ////////////////
            ///Flip core bottom if they don't point in the same direction
            ///

            var vv = coreBottom.Normals[0];
            vv.Unitize();
            var coreBottomDot = vv * Vector3d.ZAxis;

            if (coreBottomDot > 0)
            {
                coreBottom.Flip(true, true, true);
            }

            ////////////////
            ///

            //// Test to make sure that all vecs on side walls point outwards

            var firstIndex = sides.Faces[0].A;
            var firstPoint = sides.Vertices[firstIndex];
            var firstNormal = sides.FaceNormals[0];
            var vec = firstPoint - BBox.Center;
            double dot = vec * firstNormal;
            if (dot < 0)
            {
                sides.Flip(true, true, true);
            }
        }

        private static int DivisionsPerim(Point3d[] core, Point3d[] perim, double blockDim)
        {
            Vector3d blockDimensionPerim = new Vector3d(perim[0].X, perim[0].Y, perim[0].Z) - new Vector3d(core[0].X, core[0].Y, core[0].Z);
            int divPerim = (int)(blockDimensionPerim.Length / blockDim);
            if (divPerim == 0)
            {
                divPerim = 1;
            }

            return divPerim;
        }

        private static double BlockDimensionCore(Point3d[] core)
        {
            Vector3d blockDimensionCore = new Vector3d(core[1].X, core[1].Y, core[1].Z) - new Vector3d(core[0].X, core[0].Y, core[0].Z);

            return blockDimensionCore.Length;
        }

        private static int RadialDivsFromBlockSize(double blockSize, double sizeInnerRect, double radialMultiplier)
        {
            return (int)((sizeInnerRect / blockSize) * radialMultiplier);
            //return (int)(sizeInnerRect / blockSize); //testing by ilker
        }

        private Mesh SideWalls(Point3d[] pt, double h)
        {
            int vcount = 0;
            Mesh m = new Mesh();
            for (int i = 0; i < pt.Length - 1; i++)
            {
                m.Vertices.Add(pt[i]);
                m.Vertices.Add(pt[i] + Vector3d.ZAxis * h);

                m.Vertices.Add(pt[i + 1] + Vector3d.ZAxis * h);
                m.Vertices.Add(pt[i + 1]);

                m.Faces.AddFace(new MeshFace(vcount, vcount + 1, vcount + 2, vcount + 3));
                vcount += 4;
            }

            m.Normals.ComputeNormals();

            return m;
        }

        private List<Circle> GetOuterCircles(int divsZ, double outerRad, Point3d centerBottom, int blockSize)
        {
            var list = new List<Circle>();

            for (int i = 0; i < divsZ; i++)
            {
                Point3d center = centerBottom + (Vector3d.ZAxis * i * blockSize);
                list.Add(new Circle(center, outerRad));
            }

            return list;
        }

        private void SetPointsOnCircle(Point3d center, double circleRadius, Polyline nakedEdges)
        {
            List<Point3d> pointsOnCircle = new List<Point3d>();
            Point3d newCenter = new Point3d(center.X, center.Y, 0);
            Circle c = new Circle(newCenter, circleRadius);

            // -1 would avoid duplicates but other methods (PerimeterRing) depend on having one
            // duplicate point

            for (int i = 0; i < nakedEdges.Count; i++)
            {
                Vector3d vec = newCenter - nakedEdges[i];
                vec.Unitize();
                vec *= (circleRadius + 1);

                double t1;
                Point3d p1;
                double t2;
                Point3d p2;

                Rhino.Geometry.Intersect.LineCircleIntersection inter = Rhino.Geometry.Intersect.Intersection.LineCircle(new Line(newCenter, vec), c, out t1, out p1, out t2, out p2);

                //Move all points in one plane
                pointsOnCircle.Add(new Point3d(p1.X, p1.Y, center.Z));
            }
            this.pointsOnCircle = pointsOnCircle.ToArray();
        }

        private void SetPointsOnRect(int divisions, Polyline nakedEdges)
        {
            Point3d[] pointsOnRect;
            nakedEdges.ToNurbsCurve().DivideByCount(divisions * 4, true, out pointsOnRect);
            this.pointsOnRect = pointsOnRect;
        }

        private Mesh PerimeterRing(Polyline poly, Point3d[] pointsOnCircle)
        {
            Mesh mOutBottom = new Mesh();
            int vcount = 0;
            for (int i = 0; i < poly.Count - 1; i++)
            {
                mOutBottom.Vertices.Add(poly[i]);
                mOutBottom.Vertices.Add(pointsOnCircle[i]);
                mOutBottom.Vertices.Add(pointsOnCircle[i + 1]);
                mOutBottom.Vertices.Add(poly[i + 1]);

                mOutBottom.Faces.AddFace(new MeshFace(vcount, vcount + 1, vcount + 2, vcount + 3));
                vcount += 4;
            }

            mOutBottom.Normals.ComputeNormals();

            return mOutBottom;
        }

        private string StringifyBlocks2()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int c1 = perimBottom.Faces.Count;
            int c2 = perimBottom.Faces.Count + coreBottom.Faces.Count + perimTop.Faces.Count;

            // counter for cores and perimeters
            int c3 = perimBottom.Faces.Count + coreBottom.Faces.Count;

#if DEBUG
            sb.AppendLine("//perimeter");
#endif
            for (int i = 0; i < perimBottom.Faces.Count; i++)
            {
                //perimeter blocks
                //Changed order because we had to flip core mesh plane
                sb.AppendLine("hex (" + DomainMesh.Faces[i].A + " " + DomainMesh.Faces[i].D + " " + DomainMesh.Faces[i].C + " " + DomainMesh.Faces[i].B + " " +
                  ((DomainMesh.Faces[i + c3].A)) + " " + (DomainMesh.Faces[i + c3].B) + " " + (DomainMesh.Faces[i + c3].C) + " " +

                  (DomainMesh.Faces[i + c3].D) + ") (" + divPerim + " " + (divisionsX) + " " + divisionsZ + ") simpleGrading (1 " + gradingPerim + " 1)");

                // After coreTop and coreBottom were flipped by a code change in RhinoCommon, the (" + divisionsX + " " + (divPerim) + " " + divisionsZ + ") command changed from (" + divisionsX + " " + (divPerim) + " " + divisionsZ + ") to (" + divisionsPerim + " " + (divisionsX) + " " + divisionsZ + ");
            }
#if DEBUG
            sb.AppendLine("//core");
#endif
            for (int i = 0; i < coreBottom.Faces.Count; i++)
            {   //core blocks //Changed order because we had to flip core mesh plane
                sb.AppendLine("hex (" + DomainMesh.Faces[i + c1].A + " " + DomainMesh.Faces[i + c1].D + " " + DomainMesh.Faces[i + c1].C + " " + DomainMesh.Faces[i + c1].B + " " +
                  ((DomainMesh.Faces[i + c2].A)) + " " + (DomainMesh.Faces[i + c2].B) + " " + (DomainMesh.Faces[i + c2].C) + " " +
                  (DomainMesh.Faces[i + c2].D) + ") (" + divisionsX + " " + divisionsX + " " + divisionsZ + ") simpleGrading (1 1 1)");
            }

            return sb.ToString();
        }

        private string StringifyPatches2()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int counter = perimBottom.Faces.Count + coreBottom.Faces.Count + perimTop.Faces.Count + coreTop.Faces.Count;

            for (int i = 0; i < sides.Faces.Count; i++)
            {
                sb.AppendLine("patch" + i + @"
          {
          type patch;
          faces
          (");
                sb.AppendLine("(" + DomainMesh.Faces[i + counter].A + " " + DomainMesh.Faces[i + counter].B + " " + DomainMesh.Faces[i + counter].C + " " + DomainMesh.Faces[i + counter].D + ")");
                sb.AppendLine(@");
          }");
            }

            return sb.ToString();
        }

        private string StringyfyVertexList2()
        {
            System.Text.StringBuilder stb = new System.Text.StringBuilder();

            for (int i = 0; i < DomainMesh.Vertices.Count; i++)
            {
                stb.AppendLine("(" + Utilities.FormatPV(DomainMesh.Vertices[i]) + ")");
            }
            return stb.ToString();
        }

        private string StringifyTop2()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int c1 = perimTop.Faces.Count + coreTop.Faces.Count;
            int c2 = perimBottom.Faces.Count + coreBottom.Faces.Count + perimTop.Faces.Count;
            sb.AppendLine(@"frontAndBack
        {
        type patch;
        faces
        (");
            for (int i = 0; i < perimTop.Faces.Count; i++)
            {
                sb.AppendLine("(" + DomainMesh.Faces[i + c1].A + " " + DomainMesh.Faces[i + c1].B + " " + DomainMesh.Faces[i + c1].C + " " + DomainMesh.Faces[i + c1].D + ")");
            }
            for (int i = 0; i < coreTop.Faces.Count; i++)
            {
                sb.AppendLine("(" + DomainMesh.Faces[i + c2].A + " " + DomainMesh.Faces[i + c2].B + " " + DomainMesh.Faces[i + c2].C + " " + DomainMesh.Faces[i + c2].D + ")");
            }
            sb.AppendLine(@");
        }");
            return sb.ToString();
        }

        private string StringifyGround2()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int c1 = perimBottom.Faces.Count + coreBottom.Faces.Count;

            //int c2 = this.perim.Faces.Count + this.core.Faces.Count + this.perimTop.Faces.Count + this.coreTop.Faces.Count;

            sb.AppendLine(@"ground
        {
        type wall;
        faces
        (");
            for (int i = 0; i < c1; i++)
            {
                sb.AppendLine("(" + DomainMesh.Faces[i].A + " " + DomainMesh.Faces[i].B + " " + DomainMesh.Faces[i].C + " " + DomainMesh.Faces[i].D + ")");
            }

            sb.AppendLine(@");
        }");
            return sb.ToString();
        }

        private List<Polyline> GetConcenctricPolyDivisions(Point3d[] pointsOnRect, Point3d[] pointsOnCircle, int divPerim, double topOfDomain)
        {
            // Shift pointsOnCircle to the right by one to get correct order not necessary because
            // the GetPointsOnCircle yields one duplicate point pointsOnCircle = RightShift(pointsOnCircle);

            // Add radial polylines from divisions

            List<Polyline> radialDivisions = new List<Polyline>();
            for (int i = 0; i < pointsOnRect.Length; i++)
            {
                radialDivisions.Add(new Polyline(new Point3d[] { pointsOnRect[i], pointsOnCircle[i] }));
            }

            // Create list with arrays of all intersections This needs adaptation if grading should
            // be implemented

            List<Point3d[]> divPointsCut = new List<Point3d[]>();
            for (int i = 0; i < pointsOnRect.Length; i++)
            {
                Point3d[] ar = new Point3d[pointsOnRect.Length];
                new PolylineCurve(radialDivisions[i]).DivideByCount(divPerim, true, out ar);
                divPointsCut.Add(ar);
            }

            // Create one sequential list with all points from inside to outside

            List<Point3d> fullList = new List<Point3d>();
            foreach (Point3d[] ar in divPointsCut)
            {
                foreach (Point3d pt in ar)
                {
                    fullList.Add(pt);
                }
            }

            List<Polyline> concentricDivisionsBottom = new List<Polyline>();
            List<Polyline> concentricDivisionsTop = new List<Polyline>();
            List<Point3d> innerRadialList = new List<Point3d>();

            for (int j = 0; j < divPerim; j++)
            {
                // Go through all loops and add the vertices with the correct stepsize
                for (int i = 0; i < fullList.Count; i++)
                {
                    innerRadialList.Add(fullList[(i + j)]);
                    i += divPerim;
                }

                //Add the last vertex to close the loop
                innerRadialList.Add(fullList[j]);
            }

            // Cull duplicates from that list
            List<Point3d> innerRadialListNoDupes = new List<Point3d>();
            innerRadialListNoDupes = innerRadialList.Distinct().ToList();

            //Split up every concentric ring and add them to a final list

            List<List<Point3d>> lists = Utilities.SplitPointList(innerRadialListNoDupes, pointsOnRect.Length);

            // Close the loop for every list (add last element)

            foreach (List<Point3d> l in lists)
            {
                l.Add(l[0]);
            }

            // Copy everything to the top

            var vec = Vector3d.ZAxis * topOfDomain;
            var xf = Rhino.Geometry.Transform.Translation(vec);

            foreach (List<Point3d> l in lists)
            {
                var pl = new Polyline(l);

                // Bottom
                concentricDivisionsBottom.Add(pl);

                // Top
                pl.Transform(xf);
                concentricDivisionsTop.Add(pl);
            }

            // Merge both lists

            var concentricDivisions = concentricDivisionsBottom.Union(concentricDivisionsTop).ToList();

            return concentricDivisions;
        }

        public string StringyfyDomain2()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(@"
        /*--------------------------------*- C++ -*----------------------------------*\
        | =========                 |                                                 |
        | \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
        |  \\    /   O peration     | Version:  2.1.0                                  |
        |   \\  /    A nd           | Web:      http://www.OpenFOAM.com               |
        |    \\/     M anipulation  |                                                 |
        \*---------------------------------------------------------------------------*/
        FoamFile
        {
        version     2.0;
        format      ascii;
        class       dictionary;
        object      blockMeshDict;
        }

        // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        convertToMeters 1;

        //
        vertices
        (

        ");

            sb.AppendLine(StringyfyVertexList2());

            sb.AppendLine(@"
        );
        blocks
        (
        ");

            sb.AppendLine(StringifyBlocks2());

            sb.AppendLine(@"
        );

        edges
        (
        );
        boundary
        (

        ");

            sb.AppendLine(StringifyPatches2());
            sb.AppendLine(StringifyTop2());
            sb.AppendLine(StringifyGround2());

            sb.AppendLine(@"
        );

        mergePatchPairs
        (
        );");
            return sb.ToString();
        }

        public override string ToString()
        {
            return "Cyclic Domain:\n" +
              "Smallest cell size in center: " + cellSizeInner + " m\n" +
              "Projected area: " + Math.Round(this.MaxFrontageBuildingArea, 1)

              ;

            // return base.ToString();
        }
    }
}