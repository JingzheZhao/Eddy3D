using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;


namespace EddyLib
{
    public class OFCylDomain : OFBaseDomain
    {
        //public BoundingBox BBox; //moved to BaseDomain so that is globally accessible

        public double radius;
        public double height;

        private readonly List<string> MeshFaceLabel = new List<string>();
        private readonly List<int> topFaceID = new List<int>();
        private readonly List<int> bottomFaceID = new List<int>();
        private readonly List<int> outletFaceID = new List<int>();
        private readonly List<int> inletFaceID = new List<int>();
        public List<Point3d> ListOfAllPointsInMagicOrder;


        public int divisionsX = 1;
        public int divsRadial;
        public int divisionsZ;
        public int divPerim;
        public double gradingPerim;


        public double cellSizeInner;
        public double cellSizeOuter;
        public double distanceInnerOuter;
        public int equalDivisions;


        //Meshes from Cycl Domain
        public Mesh CylDomainMesh = new Mesh();
        public Mesh CylDomainMeshGround = new Mesh();
        public Mesh CylDomainMeshGroundPerim = new Mesh();
        public Mesh CylCombinedMesh; // TODO: What is this??

        public Mesh perimBottom = new Mesh();
        public Mesh coreBottom = new Mesh();
        public Mesh perimTop = new Mesh();
        public Mesh coreTop = new Mesh();
        public Mesh sides = new Mesh();

        public double sizeInnerR;

        public List<Polyline> concentricDivisions;


      


        // Remove this later
        public Point3d[] pointsOnCircle;
        public Point3d[] pointsOnRect;



        public OFCylDomain(Mesh BuildingGeometry, Mesh terrainMesh, BoundaryConditions BCond, double coreBlockSize, double sizeInnerRect = 0, double sizeOuterCirc = 0, double sizeHeight = 0)
        {
            gradingPerim = 1.0;

            this.BCond = BCond;
            this.BuildingGeometry = BuildingGeometry;

            var BBoxCrude = BuildingGeometry.GetBoundingBox(true);




            // Box-shaped tunnel can only have 1 windDir which is the 1st windDir

            Vector3d windDirVector = BCond.flowDir[0];

            // Rotate the Plane based on wind vector area  


            Plane orientedPlane = GetOrientedBasePlane(windDirVector, BuildingGeometry, BBoxCrude.Center);

            // Create BBox with respect to new plane (new coordinates)
            BBox = BuildingGeometry.GetBoundingBox(Plane.WorldXY);


            var xMin = BBox.Min.X;
            var xMax = BBox.Max.X;
            var yMin = BBox.Min.Y;
            var yMax = BBox.Max.Y;
            var zMin = BBox.Min.Z;
            var zMax = BBox.Max.Z;

            var dimX = xMax - xMin;
            var dimY = yMax - yMin;
            var dimZ = zMax - zMin;



            // If terrain is used, scale down Z to make sure all points are inside the domain
            // Zinter is call divisionsZ for CylDomain which is an int instead of an Interval




            if (terrainMesh.Faces.Count > 0)
            {
                this.hasTerrain = true;
            }


            if (hasTerrain)
            {
                this.TerrainMesh = terrainMesh;
                double zMinTerrain = OFBaseDomain.GetZMinTerrain(terrainMesh, BBox, Plane.WorldXY);
                this.CenterGround = new Point3d(BBox.Center.X, BBox.Center.Y, zMinTerrain);

            }
            else
            {
                this.CenterGround = new Point3d(BBox.Center.X, BBox.Center.Y, zMin);

            }

            //var CenterGround = BBoxCrude.Center + 0.9 * -Vector3d.ZAxis * dimZ;




            //Create ground plane of BBox
            //center needs dimZ to stay at ground level but also respect terrain if its being used; 0.1 = safety factor
            //center = (BBox.Center + 0.5 * -Vector3d.ZAxis * dimZ) + zTerrainScaling * Vector3d.ZAxis;




            // Check standard inputs for height

            if (sizeHeight == 0)
            {
                height = 6 * dimZ + (BBox.Min.Z - CenterGround.Z);
            }
            else
            {
                height = sizeHeight;
            }




            double scaleDomByHeight = (15.5 * dimZ) + dimY;
            //var scaleCyclDomainHeight = height > dimY ? height : dimY;


            //Plane localSystem = new Plane(center, Vector3d.XAxis, Vector3d.ZAxis);
            //// localSystem.Origin = center;
            //localSystem.Translate(-Vector3d.YAxis * dimY);


            for (int i = 0; i < 72; i++)
            {
                Vector3d localCopy = Vector3d.YAxis;
                localCopy.Rotate(5 * i * Math.PI / 180, Vector3d.ZAxis);


                Bitmap FI;
                this.FrontageBuildingAreas[i * 5] = OFBaseDomain.GetProjectedBuildingArea(i * 5, BuildingGeometry, out FI);
                this.FrontagePNGs[i * 5] = FI;

                //projAreaList.Add(RunBlockMesh.GetProjectedBuildingAreas(BCond.flowDir[0], orientedPlane, BuildingGeometry));


            }

            MaxFrontageBuildingArea = FrontageBuildingAreas.Max();


            // New Dimensions in X; take blocking ratio into account
            double scaleCylDomainFromBlockingRatio = MaxFrontageBuildingArea * 100 / 3 / height / 2;


            // Check standard inputs for radius

            if (sizeOuterCirc == 0)
            {
                radius = scaleCylDomainFromBlockingRatio > scaleDomByHeight ? scaleCylDomainFromBlockingRatio : scaleDomByHeight;
            }
            else
            {
                // Radius, not Durchmesser
                radius = sizeOuterCirc / 2;
            }


            //old domain
            //var allPoints = MakeCylMeshPoints5deg(center, radius, height, scaleFactorInnerRect);
            //MakeCylMesh(allPoints, divisionsX, divisionsY, divisionsZ, windDir);

            if (sizeInnerRect == 0)
            {
                //sizeInnerR = BBox.Diagonal.Length / Math.Sqrt(2);
                sizeInnerR = radius * 0.35;
            }
            else
            {
                sizeInnerR = sizeInnerRect;
            }





            divsRadial = RadialDivsFromBlockSize(coreBlockSize, sizeInnerR);
            //divisionsZ = _divisionsZ;


            MakeCircMeshPlane(CenterGround, sizeInnerR, divsRadial, radius, height);


            BCond.CalculateCPPressures(zMax, BCond.btype, BCond.URef);


            if (BCond.btype == BoundaryType.constant)
            {
                BCond.SetUatBuildingHeightUconst();
            }
            if (BCond.btype == BoundaryType.abl)
            {
                BCond.SetUatBuildingHeightABL(zMax);
            }

            base.BCond = BCond;

            // refinement Cylinder
            //refinementCylinder = getRefinementCyl(center, geometry, 0.3, 0.3);
            //refinementBox = getRefinementBox(localSystem, geometry, 0.3);





        }




        public void MakeCircMeshPlane(Point3d center, double sizeInnerRect, int divsRadial, double circleRadius, double height)
        {



            // point inside cdf domain - needed for meshing and finding the void space for fluid
            LocationInMesh = center + (Vector3d.ZAxis * (height - 0.1));
            // move into periphery
            LocationInMesh += radius * 0.6 * Vector3d.XAxis;




            Plane pl = new Plane(center, Vector3d.ZAxis);

            Interval xinter = new Interval(-sizeInnerRect, sizeInnerRect);

            // Throws exeption if 0
            if (divsRadial < 1) { divsRadial = 1; }
            Mesh m = Mesh.CreateFromPlane(pl, xinter, xinter, divsRadial, divsRadial); // creates the inner rectangle with arbitrary subdivision
            coreBottom.Append(m);
            coreBottom.Flip(true, true, true);

            double minRad = Math.Sqrt(2 * (sizeInnerRect * sizeInnerRect)) * 1.1; // *1.1 to account for collapsing face on boundary
            double circRad = circleRadius;
            if (circleRadius < minRad)
            {
                circRad = minRad;
            }

            double cellSizeCore = 2 * (sizeInnerRect / divsRadial);
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


            Circle c = new Circle(center, circRad);

            Polyline poly = coreBottom.GetNakedEdges()[0]; //returns a polygon with line segments for each mesh cell






            Point3d[] pointsOnRect = GetPointsOnRect(divsRadial, m);
            Point3d[] pointsOnCircle = GetPointsOnCircle(center, circRad, poly);

            double blockDimensionCore = BlockDimensionCore(pointsOnRect);
            divPerim = DivisionsPerim(pointsOnRect, pointsOnCircle, blockDimensionCore);




            ////////////////////
            //Visualize divisions inside cylindrical perimeter
            ////////////////////
            // Points on inner rectangle from naked edges

            this.concentricDivisions = GetConcenctricPolyDivisions(pointsOnRect, pointsOnCircle, divPerim, height );

            ////////////////
            ///




            coreTop.Append(m);
            coreTop.Translate(Vector3d.ZAxis * height);


            perimBottom = PerimeterRing(poly, pointsOnCircle);
            //perimTop = new Mesh();
            perimTop.Append(perimBottom);
            perimTop.Translate(Vector3d.ZAxis * height);

            perimTop.Flip(true, true, true);


            sides = SideWalls(pointsOnCircle, height);
            sides.Normals.ComputeNormals();


            //// Test to make sure that all vecs point outwards

            //var firstIndex = sides.Faces[0].A;
            //var firstPoint = sides.Vertices[firstIndex];
            //var firstNormal = sides.FaceNormals[0];
            //var vec = firstPoint - center;
            //double dot = vec * firstNormal;
            //if (dot < 0)
            //{
            //    sides.Flip(true, true, true);
            //}

            // Order is important!!! for stringifyDomain
            //this.DomainMeshGround.Append(perim);
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

        private static int DivisionsPerim(Point3d[] core, Point3d[] perim, double blockDim)
        {

            Vector3d blockDimensionPerim = new Vector3d(perim[0].X, perim[0].Y, perim[0].Z) - new Vector3d(core[0].X, core[0].Y, core[0].Z);
            int divPerim = (int)(blockDimensionPerim.Length / blockDim);
            if (divPerim == 0)
            {
                divPerim = 1;
            }

            return divPerim;
            //return (int)(blockDimensionPerim.Length / blockDim / 1.41);
        }
        private static double BlockDimensionCore(Point3d[] core)
        {

            Vector3d blockDimensionCore = new Vector3d(core[1].X, core[1].Y, core[1].Z) - new Vector3d(core[0].X, core[0].Y, core[0].Z);

            return blockDimensionCore.Length;
        }

        private static int RadialDivsFromBlockSize(double blockSize, double sizeInnerRect)
        {

            int radialDivs = 0;

            radialDivs = (int)(sizeInnerRect / blockSize);

            return radialDivs;
        }




        public string StringyfyDomain()
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

            sb.AppendLine(StringyfyOFVertexList(DomainMesh.Vertices.ToPoint3dArray()));


            sb.AppendLine(@"
); 
blocks          
(
");


            sb.AppendLine(StringyfyBlocks(DomainMesh, inputGroundVertices, inputTopVertices, divisionsX, divsRadial, divisionsZ));


            sb.AppendLine(@"
);
 
 edges           
 (
 );
boundary
(

");



            sb.AppendLine(StringyfyBoundaries(DomainMesh, outletFaceID, inletFaceID, topFaceID, bottomFaceID));



            sb.AppendLine(@"
 );

 
mergePatchPairs 
(
);");
            return sb.ToString();


        }



        private static string StringyfyBoundaries(Mesh m, List<int> outletFaceID, List<int> inletFaceID, List<int> topFaceID, List<int> bottomFaceID)
        {

            // make some text
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("inlet");
            sb.AppendLine("{");
            sb.AppendLine("type patch;");
            sb.AppendLine("faces");
            sb.AppendLine("(");
            foreach (int i in inletFaceID)
            {
                MeshFace mf = m.Faces[i];
                sb.AppendLine("(" + mf.A + " " + mf.B + " " + mf.C + " " + mf.D + ")");

            }
            sb.AppendLine(");");
            sb.AppendLine("}");

            sb.AppendLine("outlet");
            sb.AppendLine("{");
            sb.AppendLine("type patch;");
            sb.AppendLine("faces");
            sb.AppendLine("(");
            foreach (int i in outletFaceID)
            {
                MeshFace mf = m.Faces[i];
                sb.AppendLine("(" + mf.A + " " + mf.B + " " + mf.C + " " + mf.D + ")");

            }
            sb.AppendLine(");");
            sb.AppendLine("}");


            sb.AppendLine("frontAndBack");
            sb.AppendLine("{");
            sb.AppendLine("type patch;");
            sb.AppendLine("faces");
            sb.AppendLine("(");
            foreach (int i in topFaceID)
            {
                MeshFace mf = m.Faces[i];
                sb.AppendLine("(" + mf.A + " " + mf.B + " " + mf.C + " " + mf.D + ")");

            }
            sb.AppendLine(");");
            sb.AppendLine("}");

            sb.AppendLine("ground");
            sb.AppendLine("{");
            sb.AppendLine("type wall;");
            sb.AppendLine("faces");
            sb.AppendLine("(");
            foreach (int i in bottomFaceID)
            {
                MeshFace mf = m.Faces[i];
                sb.AppendLine("(" + mf.A + " " + mf.B + " " + mf.C + " " + mf.D + ")");

            }
            sb.AppendLine(");");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string StringyfyBlocks(Mesh m, int[] inputGroundFaces, int[] inputTopFaces, int divisionsX, int divisionsY, int divisionsZ)
        {

            List<Point3d> fullList = m.Vertices.ToPoint3dArray().ToList();

            List<string> blocksFromArcsA = new List<string>();
            List<string> blocksFromArcsB = new List<string>();

            for (int i = 0; i < fullList.Count / 12; i++)
            {
                blocksFromArcsA.Add(" hex (" + m.Faces[i].A + " " + m.Faces[i].B + " " + m.Faces[i].C + " " + m.Faces[i].D + " ");
            }
            //
            for (int i = fullList.Count / 12; i < fullList.Count / 6; i++)
            {
                blocksFromArcsB.Add(m.Faces[i].A + " " + m.Faces[i].B + " " + m.Faces[i].C + " " + m.Faces[i].D + ") (" + divisionsX + " " + divisionsY + " " + divisionsZ + ") simpleGrading (1 1 1) ");
            }


            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < blocksFromArcsA.Count; i++)
            {

                sb.AppendLine(blocksFromArcsA[i] + blocksFromArcsB[i]);
            }


            ///------
            ///------


            for (int i = 0; i < inputGroundFaces.Length - 3; i = i + 4)
            {

                sb.AppendLine("hex (" +

                    inputGroundFaces[i] + " " + inputGroundFaces[i + 1] + " " + inputGroundFaces[i + 2] + " " + inputGroundFaces[i + 3] + " " +
                    inputTopFaces[i] + " " + inputTopFaces[i + 1] + " " + inputTopFaces[i + 2] + " " + inputTopFaces[i + 3] + ") (1 1 " + divisionsZ + ") simpleGrading (1 1 1)  ");
            }



            return sb.ToString();
        }

        private static string StringyfyOFVertexList(Point3d[] L)
        {
            StringBuilder stb = new StringBuilder();

            for (int i = 0; i < L.Length; i++)
            {
                stb.AppendLine("(" + L[i].X + " " + +L[i].Y + " " + +L[i].Z + ")");
            }
            return stb.ToString();
        }

        private static readonly int[] inputGroundVertices = { 72, 73, 145, 144, 73, 74, 146, 145, 74, 75, 147, 146, 75, 76, 148, 147, 76, 77, 149, 148, 77, 78, 150, 149, 78, 79, 151, 150, 79, 80, 152, 151, 80, 81, 82, 152, 82, 83, 169, 152, 83, 84, 186, 169, 84, 85, 203, 186, 85, 86, 220, 203, 86, 87, 237, 220, 87, 88, 254, 237, 88, 89, 271, 254, 89, 90, 288, 271, 90, 91, 305, 288, 91, 92, 322, 305, 92, 93, 339, 322, 93, 94, 356, 339, 94, 95, 373, 356, 95, 96, 390, 373, 96, 97, 407, 390, 97, 98, 424, 407, 98, 99, 100, 424, 100, 101, 423, 424, 101, 102, 422, 423, 102, 103, 421, 422, 103, 104, 420, 421, 104, 105, 419, 420, 105, 106, 418, 419, 106, 107, 417, 418, 107, 108, 416, 417, 108, 109, 425, 416, 109, 110, 426, 425, 110, 111, 427, 426, 111, 112, 428, 427, 112, 113, 429, 428, 113, 114, 430, 429, 114, 115, 431, 430, 115, 116, 432, 431, 116, 117, 118, 432, 118, 119, 415, 432, 119, 120, 398, 415, 120, 121, 381, 398, 121, 122, 364, 381, 122, 123, 347, 364, 123, 124, 330, 347, 124, 125, 313, 330, 125, 126, 296, 313, 126, 127, 279, 296, 127, 128, 262, 279, 128, 129, 245, 262, 129, 130, 228, 245, 130, 131, 211, 228, 131, 132, 194, 211, 132, 133, 177, 194, 133, 134, 160, 177, 134, 135, 136, 160, 136, 137, 159, 160, 137, 138, 158, 159, 138, 139, 157, 158, 139, 140, 156, 157, 140, 141, 155, 156, 141, 142, 154, 155, 142, 143, 153, 154, 143, 72, 144, 153, 160, 159, 176, 177, 159, 158, 175, 176, 158, 157, 174, 175, 157, 156, 173, 174, 156, 155, 172, 173, 155, 154, 171, 172, 154, 153, 170, 171, 153, 144, 161, 170, 144, 145, 162, 161, 145, 146, 163, 162, 146, 147, 164, 163, 147, 148, 165, 164, 148, 149, 166, 165, 149, 150, 167, 166, 150, 151, 168, 167, 151, 152, 169, 168, 177, 176, 193, 194, 176, 175, 192, 193, 175, 174, 191, 192, 174, 173, 190, 191, 173, 172, 189, 190, 172, 171, 188, 189, 171, 170, 187, 188, 170, 161, 178, 187, 161, 162, 179, 178, 162, 163, 180, 179, 163, 164, 181, 180, 164, 165, 182, 181, 165, 166, 183, 182, 166, 167, 184, 183, 167, 168, 185, 184, 168, 169, 186, 185, 194, 193, 210, 211, 193, 192, 209, 210, 192, 191, 208, 209, 191, 190, 207, 208, 190, 189, 206, 207, 189, 188, 205, 206, 188, 187, 204, 205, 187, 178, 195, 204, 178, 179, 196, 195, 179, 180, 197, 196, 180, 181, 198, 197, 181, 182, 199, 198, 182, 183, 200, 199, 183, 184, 201, 200, 184, 185, 202, 201, 185, 186, 203, 202, 211, 210, 227, 228, 210, 209, 226, 227, 209, 208, 225, 226, 208, 207, 224, 225, 207, 206, 223, 224, 206, 205, 222, 223, 205, 204, 221, 222, 204, 195, 212, 221, 195, 196, 213, 212, 196, 197, 214, 213, 197, 198, 215, 214, 198, 199, 216, 215, 199, 200, 217, 216, 200, 201, 218, 217, 201, 202, 219, 218, 202, 203, 220, 219, 228, 227, 244, 245, 227, 226, 243, 244, 226, 225, 242, 243, 225, 224, 241, 242, 224, 223, 240, 241, 223, 222, 239, 240, 222, 221, 238, 239, 221, 212, 229, 238, 212, 213, 230, 229, 213, 214, 231, 230, 214, 215, 232, 231, 215, 216, 233, 232, 216, 217, 234, 233, 217, 218, 235, 234, 218, 219, 236, 235, 219, 220, 237, 236, 245, 244, 261, 262, 244, 243, 260, 261, 243, 242, 259, 260, 242, 241, 258, 259, 241, 240, 257, 258, 240, 239, 256, 257, 239, 238, 255, 256, 238, 229, 246, 255, 229, 230, 247, 246, 230, 231, 248, 247, 231, 232, 249, 248, 232, 233, 250, 249, 233, 234, 251, 250, 234, 235, 252, 251, 235, 236, 253, 252, 236, 237, 254, 253, 262, 261, 278, 279, 261, 260, 277, 278, 260, 259, 276, 277, 259, 258, 275, 276, 258, 257, 274, 275, 257, 256, 273, 274, 256, 255, 272, 273, 255, 246, 263, 272, 246, 247, 264, 263, 247, 248, 265, 264, 248, 249, 266, 265, 249, 250, 267, 266, 250, 251, 268, 267, 251, 252, 269, 268, 252, 253, 270, 269, 253, 254, 271, 270, 279, 278, 295, 296, 278, 277, 294, 295, 277, 276, 293, 294, 276, 275, 292, 293, 275, 274, 291, 292, 274, 273, 290, 291, 273, 272, 289, 290, 272, 263, 280, 289, 263, 264, 281, 280, 264, 265, 282, 281, 265, 266, 283, 282, 266, 267, 284, 283, 267, 268, 285, 284, 268, 269, 286, 285, 269, 270, 287, 286, 270, 271, 288, 287, 296, 295, 312, 313, 295, 294, 311, 312, 294, 293, 310, 311, 293, 292, 309, 310, 292, 291, 308, 309, 291, 290, 307, 308, 290, 289, 306, 307, 289, 280, 297, 306, 280, 281, 298, 297, 281, 282, 299, 298, 282, 283, 300, 299, 283, 284, 301, 300, 284, 285, 302, 301, 285, 286, 303, 302, 286, 287, 304, 303, 287, 288, 305, 304, 313, 312, 329, 330, 312, 311, 328, 329, 311, 310, 327, 328, 310, 309, 326, 327, 309, 308, 325, 326, 308, 307, 324, 325, 307, 306, 323, 324, 306, 297, 314, 323, 297, 298, 315, 314, 298, 299, 316, 315, 299, 300, 317, 316, 300, 301, 318, 317, 301, 302, 319, 318, 302, 303, 320, 319, 303, 304, 321, 320, 304, 305, 322, 321, 330, 329, 346, 347, 329, 328, 345, 346, 328, 327, 344, 345, 327, 326, 343, 344, 326, 325, 342, 343, 325, 324, 341, 342, 324, 323, 340, 341, 323, 314, 331, 340, 314, 315, 332, 331, 315, 316, 333, 332, 316, 317, 334, 333, 317, 318, 335, 334, 318, 319, 336, 335, 319, 320, 337, 336, 320, 321, 338, 337, 321, 322, 339, 338, 347, 346, 363, 364, 346, 345, 362, 363, 345, 344, 361, 362, 344, 343, 360, 361, 343, 342, 359, 360, 342, 341, 358, 359, 341, 340, 357, 358, 340, 331, 348, 357, 331, 332, 349, 348, 332, 333, 350, 349, 333, 334, 351, 350, 334, 335, 352, 351, 335, 336, 353, 352, 336, 337, 354, 353, 337, 338, 355, 354, 338, 339, 356, 355, 364, 363, 380, 381, 363, 362, 379, 380, 362, 361, 378, 379, 361, 360, 377, 378, 360, 359, 376, 377, 359, 358, 375, 376, 358, 357, 374, 375, 357, 348, 365, 374, 348, 349, 366, 365, 349, 350, 367, 366, 350, 351, 368, 367, 351, 352, 369, 368, 352, 353, 370, 369, 353, 354, 371, 370, 354, 355, 372, 371, 355, 356, 373, 372, 381, 380, 397, 398, 380, 379, 396, 397, 379, 378, 395, 396, 378, 377, 394, 395, 377, 376, 393, 394, 376, 375, 392, 393, 375, 374, 391, 392, 374, 365, 382, 391, 365, 366, 383, 382, 366, 367, 384, 383, 367, 368, 385, 384, 368, 369, 386, 385, 369, 370, 387, 386, 370, 371, 388, 387, 371, 372, 389, 388, 372, 373, 390, 389, 398, 397, 414, 415, 397, 396, 413, 414, 396, 395, 412, 413, 395, 394, 411, 412, 394, 393, 410, 411, 393, 392, 409, 410, 392, 391, 408, 409, 391, 382, 399, 408, 382, 383, 400, 399, 383, 384, 401, 400, 384, 385, 402, 401, 385, 386, 403, 402, 386, 387, 404, 403, 387, 388, 405, 404, 388, 389, 406, 405, 389, 390, 407, 406, 415, 414, 431, 432, 414, 413, 430, 431, 413, 412, 429, 430, 412, 411, 428, 429, 411, 410, 427, 428, 410, 409, 426, 427, 409, 408, 425, 426, 408, 399, 416, 425, 399, 400, 417, 416, 400, 401, 418, 417, 401, 402, 419, 418, 402, 403, 420, 419, 403, 404, 421, 420, 404, 405, 422, 421, 405, 406, 423, 422, 406, 407, 424, 423 };

        private static readonly int[] inputTopVertices = { 505, 506, 578, 577, 506, 507, 579, 578, 507, 508, 580, 579, 508, 509, 581, 580, 509, 510, 582, 581, 510, 511, 583, 582, 511, 512, 584, 583, 512, 513, 585, 584, 513, 514, 515, 585, 515, 516, 602, 585, 516, 517, 619, 602, 517, 518, 636, 619, 518, 519, 653, 636, 519, 520, 670, 653, 520, 521, 687, 670, 521, 522, 704, 687, 522, 523, 721, 704, 523, 524, 738, 721, 524, 525, 755, 738, 525, 526, 772, 755, 526, 527, 789, 772, 527, 528, 806, 789, 528, 529, 823, 806, 529, 530, 840, 823, 530, 531, 857, 840, 531, 532, 533, 857, 533, 534, 856, 857, 534, 535, 855, 856, 535, 536, 854, 855, 536, 537, 853, 854, 537, 538, 852, 853, 538, 539, 851, 852, 539, 540, 850, 851, 540, 541, 849, 850, 541, 542, 858, 849, 542, 543, 859, 858, 543, 544, 860, 859, 544, 545, 861, 860, 545, 546, 862, 861, 546, 547, 863, 862, 547, 548, 864, 863, 548, 549, 865, 864, 549, 550, 551, 865, 551, 552, 848, 865, 552, 553, 831, 848, 553, 554, 814, 831, 554, 555, 797, 814, 555, 556, 780, 797, 556, 557, 763, 780, 557, 558, 746, 763, 558, 559, 729, 746, 559, 560, 712, 729, 560, 561, 695, 712, 561, 562, 678, 695, 562, 563, 661, 678, 563, 564, 644, 661, 564, 565, 627, 644, 565, 566, 610, 627, 566, 567, 593, 610, 567, 568, 569, 593, 569, 570, 592, 593, 570, 571, 591, 592, 571, 572, 590, 591, 572, 573, 589, 590, 573, 574, 588, 589, 574, 575, 587, 588, 575, 576, 586, 587, 576, 505, 577, 586, 593, 592, 609, 610, 592, 591, 608, 609, 591, 590, 607, 608, 590, 589, 606, 607, 589, 588, 605, 606, 588, 587, 604, 605, 587, 586, 603, 604, 586, 577, 594, 603, 577, 578, 595, 594, 578, 579, 596, 595, 579, 580, 597, 596, 580, 581, 598, 597, 581, 582, 599, 598, 582, 583, 600, 599, 583, 584, 601, 600, 584, 585, 602, 601, 610, 609, 626, 627, 609, 608, 625, 626, 608, 607, 624, 625, 607, 606, 623, 624, 606, 605, 622, 623, 605, 604, 621, 622, 604, 603, 620, 621, 603, 594, 611, 620, 594, 595, 612, 611, 595, 596, 613, 612, 596, 597, 614, 613, 597, 598, 615, 614, 598, 599, 616, 615, 599, 600, 617, 616, 600, 601, 618, 617, 601, 602, 619, 618, 627, 626, 643, 644, 626, 625, 642, 643, 625, 624, 641, 642, 624, 623, 640, 641, 623, 622, 639, 640, 622, 621, 638, 639, 621, 620, 637, 638, 620, 611, 628, 637, 611, 612, 629, 628, 612, 613, 630, 629, 613, 614, 631, 630, 614, 615, 632, 631, 615, 616, 633, 632, 616, 617, 634, 633, 617, 618, 635, 634, 618, 619, 636, 635, 644, 643, 660, 661, 643, 642, 659, 660, 642, 641, 658, 659, 641, 640, 657, 658, 640, 639, 656, 657, 639, 638, 655, 656, 638, 637, 654, 655, 637, 628, 645, 654, 628, 629, 646, 645, 629, 630, 647, 646, 630, 631, 648, 647, 631, 632, 649, 648, 632, 633, 650, 649, 633, 634, 651, 650, 634, 635, 652, 651, 635, 636, 653, 652, 661, 660, 677, 678, 660, 659, 676, 677, 659, 658, 675, 676, 658, 657, 674, 675, 657, 656, 673, 674, 656, 655, 672, 673, 655, 654, 671, 672, 654, 645, 662, 671, 645, 646, 663, 662, 646, 647, 664, 663, 647, 648, 665, 664, 648, 649, 666, 665, 649, 650, 667, 666, 650, 651, 668, 667, 651, 652, 669, 668, 652, 653, 670, 669, 678, 677, 694, 695, 677, 676, 693, 694, 676, 675, 692, 693, 675, 674, 691, 692, 674, 673, 690, 691, 673, 672, 689, 690, 672, 671, 688, 689, 671, 662, 679, 688, 662, 663, 680, 679, 663, 664, 681, 680, 664, 665, 682, 681, 665, 666, 683, 682, 666, 667, 684, 683, 667, 668, 685, 684, 668, 669, 686, 685, 669, 670, 687, 686, 695, 694, 711, 712, 694, 693, 710, 711, 693, 692, 709, 710, 692, 691, 708, 709, 691, 690, 707, 708, 690, 689, 706, 707, 689, 688, 705, 706, 688, 679, 696, 705, 679, 680, 697, 696, 680, 681, 698, 697, 681, 682, 699, 698, 682, 683, 700, 699, 683, 684, 701, 700, 684, 685, 702, 701, 685, 686, 703, 702, 686, 687, 704, 703, 712, 711, 728, 729, 711, 710, 727, 728, 710, 709, 726, 727, 709, 708, 725, 726, 708, 707, 724, 725, 707, 706, 723, 724, 706, 705, 722, 723, 705, 696, 713, 722, 696, 697, 714, 713, 697, 698, 715, 714, 698, 699, 716, 715, 699, 700, 717, 716, 700, 701, 718, 717, 701, 702, 719, 718, 702, 703, 720, 719, 703, 704, 721, 720, 729, 728, 745, 746, 728, 727, 744, 745, 727, 726, 743, 744, 726, 725, 742, 743, 725, 724, 741, 742, 724, 723, 740, 741, 723, 722, 739, 740, 722, 713, 730, 739, 713, 714, 731, 730, 714, 715, 732, 731, 715, 716, 733, 732, 716, 717, 734, 733, 717, 718, 735, 734, 718, 719, 736, 735, 719, 720, 737, 736, 720, 721, 738, 737, 746, 745, 762, 763, 745, 744, 761, 762, 744, 743, 760, 761, 743, 742, 759, 760, 742, 741, 758, 759, 741, 740, 757, 758, 740, 739, 756, 757, 739, 730, 747, 756, 730, 731, 748, 747, 731, 732, 749, 748, 732, 733, 750, 749, 733, 734, 751, 750, 734, 735, 752, 751, 735, 736, 753, 752, 736, 737, 754, 753, 737, 738, 755, 754, 763, 762, 779, 780, 762, 761, 778, 779, 761, 760, 777, 778, 760, 759, 776, 777, 759, 758, 775, 776, 758, 757, 774, 775, 757, 756, 773, 774, 756, 747, 764, 773, 747, 748, 765, 764, 748, 749, 766, 765, 749, 750, 767, 766, 750, 751, 768, 767, 751, 752, 769, 768, 752, 753, 770, 769, 753, 754, 771, 770, 754, 755, 772, 771, 780, 779, 796, 797, 779, 778, 795, 796, 778, 777, 794, 795, 777, 776, 793, 794, 776, 775, 792, 793, 775, 774, 791, 792, 774, 773, 790, 791, 773, 764, 781, 790, 764, 765, 782, 781, 765, 766, 783, 782, 766, 767, 784, 783, 767, 768, 785, 784, 768, 769, 786, 785, 769, 770, 787, 786, 770, 771, 788, 787, 771, 772, 789, 788, 797, 796, 813, 814, 796, 795, 812, 813, 795, 794, 811, 812, 794, 793, 810, 811, 793, 792, 809, 810, 792, 791, 808, 809, 791, 790, 807, 808, 790, 781, 798, 807, 781, 782, 799, 798, 782, 783, 800, 799, 783, 784, 801, 800, 784, 785, 802, 801, 785, 786, 803, 802, 786, 787, 804, 803, 787, 788, 805, 804, 788, 789, 806, 805, 814, 813, 830, 831, 813, 812, 829, 830, 812, 811, 828, 829, 811, 810, 827, 828, 810, 809, 826, 827, 809, 808, 825, 826, 808, 807, 824, 825, 807, 798, 815, 824, 798, 799, 816, 815, 799, 800, 817, 816, 800, 801, 818, 817, 801, 802, 819, 818, 802, 803, 820, 819, 803, 804, 821, 820, 804, 805, 822, 821, 805, 806, 823, 822, 831, 830, 847, 848, 830, 829, 846, 847, 829, 828, 845, 846, 828, 827, 844, 845, 827, 826, 843, 844, 826, 825, 842, 843, 825, 824, 841, 842, 824, 815, 832, 841, 815, 816, 833, 832, 816, 817, 834, 833, 817, 818, 835, 834, 818, 819, 836, 835, 819, 820, 837, 836, 820, 821, 838, 837, 821, 822, 839, 838, 822, 823, 840, 839, 848, 847, 864, 865, 847, 846, 863, 864, 846, 845, 862, 863, 845, 844, 861, 862, 844, 843, 860, 861, 843, 842, 859, 860, 842, 841, 858, 859, 841, 832, 849, 858, 832, 833, 850, 849, 833, 834, 851, 850, 834, 835, 852, 851, 835, 836, 853, 852, 836, 837, 854, 853, 837, 838, 855, 854, 838, 839, 856, 855, 839, 840, 857, 856 };

        private int RoundToNearest5(double _Knob)
        {
            double Knob = _Knob;
            if (Knob < 0)
            {
                Knob = 0;
            }

            if (Knob > 359)
            {
                Knob = 359;
            }

            int flowDir = 0;
            while (Knob < 358)
            {
                if (Knob % 5 == 0)
                {
                    flowDir = (int)Knob;
                }
                if (Knob % 5 == 1)
                {
                    flowDir = (int)Knob - 1;
                }
                if (Knob % 5 == 2)
                {
                    flowDir = (int)Knob - 2;
                }
                if (Knob % 5 == 3)
                {
                    flowDir = (int)Knob + 2;
                }
                if (Knob % 5 == 4)
                {
                    flowDir = (int)Knob + 1;
                }
                break;
            }
            if (Knob == 358 | Knob == 359)
            {
                flowDir = 0;
            }



            return flowDir;
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
            return m;
        }

        private Point3d[] GetPointsOnCircle(Point3d center, double circleRadius, Polyline poly)
        {
            List<Point3d> pointsOnCircle = new List<Point3d>();
            Point3d newCenter = new Point3d(center.X, center.Y, 0);
            Circle c = new Circle(newCenter, circleRadius);

            // -1 would avoid duplicates but other methods (PerimeterRing) depend on having one duplicate point

            for (int i = 0; i < poly.Count; i++)
            {
                Vector3d vec = newCenter - poly[i];
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
            return pointsOnCircle.ToArray();
        }


        private Point3d[] GetPointsOnRect(int divisions, Mesh m)
        {
            Point3d[] pointsOnRect;
            m.GetNakedEdges()[0].ToNurbsCurve().DivideByCount(divisions * 4, true, out pointsOnRect);
            this.pointsOnRect = pointsOnRect;
            return pointsOnRect;
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
            return mOutBottom;
        }

        private string StringifyBlocks2()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int c1 = perimBottom.Faces.Count;
            int c2 = perimBottom.Faces.Count + coreBottom.Faces.Count + perimTop.Faces.Count;
            // counter for cores and perimeters
            int c3 = perimBottom.Faces.Count + coreBottom.Faces.Count;

            for (int i = 0; i < perimBottom.Faces.Count; i++)
            {
                //perimeter blocks
                //Changed order because we had to flip core mesh plane
                sb.AppendLine("hex (" + DomainMesh.Faces[i].A + " " + DomainMesh.Faces[i].D + " " + DomainMesh.Faces[i].C + " " + DomainMesh.Faces[i].B + " " +
                    ((DomainMesh.Faces[i + c3].A)) + " " + (DomainMesh.Faces[i + c3].B) + " " + (DomainMesh.Faces[i + c3].C) + " " +
                    (DomainMesh.Faces[i + c3].D) + ") (" + divisionsX + " " + (divPerim) + " " + divisionsZ + ") simpleGrading (1 " + gradingPerim + " 1)");

            }
            sb.AppendLine("//core");
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
                stb.AppendLine("(" + DomainMesh.Vertices[i].X + " " + +DomainMesh.Vertices[i].Y + " " + +DomainMesh.Vertices[i].Z + ")");
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




            // Shift pointsOnCircle to the right by one to get correct order
            // not necessary because the GetPointsOnCircle yields one duplicate point
            //  pointsOnCircle = RightShift(pointsOnCircle);


            // Add radial polylines from divisions

            List<Polyline> radialDivisions = new List<Polyline>();
            for (int i = 0; i < pointsOnRect.Length; i++)
            {
                radialDivisions.Add(new Polyline(new Point3d[] { pointsOnRect[i], pointsOnCircle[i] }));
            }


            // Create list with arrays of all intersections
            // This needs adaptation if grading should be implemented


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

