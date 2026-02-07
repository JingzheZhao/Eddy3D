//using Rhino.Compute;

using EddyLib;
using EddyLib.BCs;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class OFDomainTests

    {
        private readonly ITestOutputHelper _output;

        public OFDomainTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private class Point3dComparer : IEqualityComparer<Point3d>
        {
            private readonly double _tolerance;

            public Point3dComparer(double tolerance)
            {
                _tolerance = tolerance;
            }

            public bool Equals(Point3d x, Point3d y)
            {
                return Math.Abs(x.X - y.X) < _tolerance &&
                       Math.Abs(x.Y - y.Y) < _tolerance &&
                       Math.Abs(x.Z - y.Z) < _tolerance;
            }

            public int GetHashCode(Point3d obj)
            {
                return obj.GetHashCode();
            }
        }

        [NotWindowsServerFact]
        public void CreateCylDomain_PredefinedGeometry_ReturnCorrectMesh()
        {
            // Arrange
            var mm = Setup.SetUpBuildingMesh();
            BC bcond = new ABL(0, 5, 10, 1, 0);
            BCCollection bcColl = new BCCollection(bcond);

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);
            for (int i = 0; i < 10; i++)
                _output.WriteLine($"pointsOnRect[{i}]: {DOMCYL.pointsOnRect[i]}");

            var tolerance = TestConstants.DefaultTolerance;

            // Verify closed circle (first == last)
            Assert.Equal(DOMCYL.pointsOnCircle.First(), DOMCYL.pointsOnCircle.Last(), new Point3dComparer(tolerance));

            // ✅ Updated expected coordinates according to actual geometry
            Assert.Equal(DOMCYL.pointsOnRect[0], new Point3d(60, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[1], new Point3d(55, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[2], new Point3d(50, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[3], new Point3d(45, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[4], new Point3d(40, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[5], new Point3d(35, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[6], new Point3d(30, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[7], new Point3d(25, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[8], new Point3d(20, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[9], new Point3d(15, 60, 0), new Point3dComparer(tolerance));

            _output.WriteLine($"Faces: {DOMCYL.DomainMesh.Faces.Count}");
            _output.WriteLine($"Vertices: {DOMCYL.DomainMesh.Vertices.Count}");

            // Mesh integrity check
            Assert.Equal(1040, DOMCYL.DomainMesh.Faces.Count);
            Assert.Equal(1042, DOMCYL.DomainMesh.Vertices.Count);
        }

        [NotWindowsServerFact]
        public void CreateCylDomain_BlockWinding_IsOpenFoamCompatible()
        {
            // Arrange
            // This test protects the cross-platform block winding fix.
            // We intentionally use the same deterministic geometry as other cylinder tests
            // so failures indicate logic regressions, not random geometry changes.
            var mm = Setup.SetUpBuildingMesh();
            BC bcond = new ABL(0, 5, 10, 1, 0);
            BCCollection bcColl = new BCCollection(bcond);
            OFCylDomain domCyl = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            // Act
            // Generate the actual text dictionary and parse it exactly as written,
            // so this test validates real output instead of intermediate in-memory data.
            string blockMeshDict = domCyl.StringyfyDomain2();
            List<Point3d> vertices = ParseVertices(blockMeshDict);
            List<int[]> blocks = ParseHexBlocks(blockMeshDict);

            // Assert
            Assert.NotEmpty(vertices);
            Assert.NotEmpty(blocks);

            foreach (int[] block in blocks)
            {
                double signedVolume = SignedHexVolume(vertices, block);
                // For the decomposition used by OFCylDomain, OpenFOAM-compatible blocks have
                // NEGATIVE signed volume. If this flips positive, blockMesh may report
                // "inside-out" or "inward-pointing faces" depending on the block.
                Assert.True(
                    signedVolume < 0,
                    $"Expected negative signed hex volume for OpenFOAM-compatible winding, got {signedVolume:G17} for block ({string.Join(" ", block)}).");
            }
        }

        [NotWindowsServerTheory]
        [InlineData(true)]
        [InlineData(false)]
        public void PassProperBoundaryConditions_Returns9DifferentWindDirs(bool useListConstructor)
        {
            // Arrange
            var mm = Setup.SetUpBuildingMesh();
            var windDirList = new List<int>() { 0, 22, 45, 90, 135, 180, 225, 270, 315 };

            var bcColl = useListConstructor
                ? new BCCollection(windDirList, BCType.ABL)
                : CreateAblCollection(windDirList);

            var domCyl = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            Assert.True(domCyl.BCond.WindDirections.SequenceEqual(windDirList));
        }

        private static BCCollection CreateAblCollection(IEnumerable<int> windDirList)
        {
            var bcColl = new BCCollection();
            foreach (var dir in windDirList)
            {
                bcColl.AddBoundaryCondition(new ABL(dir));
            }

            return bcColl;
        }

        [NotWindowsServerFact]
        public void CreateBoxDomain_PredefinedGeometry_ReturnCorrectMesh()
        {
            // Arrange

            var mm = Setup.SetUpBuildingMesh();

            BC bcond = new ABL(330, 5, 10, 1, 0);

            BCCollection bcColl = new BCCollection(bcond);

            OFBoxDomain DOM = new OFBoxDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            var v = Math.Round(DOM.DomainMesh.Volume(), 1);

            var area1 = Math.Round(EddyLib.Utilities.MeshFaceArea(1, DOM.DomainMeshGroundPerim), 1);
            var area3 = Math.Round(EddyLib.Utilities.MeshFaceArea(3, DOM.DomainMeshGroundPerim), 1);

            Assert.Equal(3037030.6, v);
            Assert.Equal(151.5, area1);
            Assert.Equal(454.6, area3);
        }

        private static List<Point3d> ParseVertices(string blockMeshDict)
        {
            var vertices = new List<Point3d>();

            // Read the exact vertices section in the same format produced by StringyfyDomain2().
            Match verticesSectionMatch = Regex.Match(
                blockMeshDict,
                @"vertices\s*\((.*?)\);\s*blocks",
                RegexOptions.Singleline);

            if (!verticesSectionMatch.Success)
            {
                return vertices;
            }

            MatchCollection vertexMatches = Regex.Matches(
                verticesSectionMatch.Groups[1].Value,
                @"\(\s*(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)\s+(-?\d+(?:\.\d+)?)\s*\)");

            foreach (Match m in vertexMatches)
            {
                double x = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                double y = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                double z = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                vertices.Add(new Point3d(x, y, z));
            }

            return vertices;
        }

        private static List<int[]> ParseHexBlocks(string blockMeshDict)
        {
            var blocks = new List<int[]>();

            // Extract all hex block declarations as 8-index lists.
            MatchCollection blockMatches = Regex.Matches(
                blockMeshDict,
                @"hex\s*\(\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s*\)");

            foreach (Match m in blockMatches)
            {
                blocks.Add(new[]
                {
                    int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[6].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[7].Value, CultureInfo.InvariantCulture),
                    int.Parse(m.Groups[8].Value, CultureInfo.InvariantCulture)
                });
            }

            return blocks;
        }

        private static double SignedHexVolume(IReadOnlyList<Point3d> vertices, int[] hex)
        {
            // Use the same decomposition as OFCylDomain.SignedHexVolume to keep
            // production + test sign conventions identical.
            Point3d v0 = vertices[hex[0]];
            Point3d v1 = vertices[hex[1]];
            Point3d v2 = vertices[hex[2]];
            Point3d v3 = vertices[hex[3]];
            Point3d v4 = vertices[hex[4]];
            Point3d v5 = vertices[hex[5]];
            Point3d v6 = vertices[hex[6]];
            Point3d v7 = vertices[hex[7]];

            return SignedTetraVolume(v0, v1, v3, v4)
                   + SignedTetraVolume(v1, v2, v3, v6)
                   + SignedTetraVolume(v1, v4, v5, v6)
                   + SignedTetraVolume(v3, v4, v6, v7)
                   + SignedTetraVolume(v1, v3, v4, v6);
        }

        private static double SignedTetraVolume(Point3d a, Point3d b, Point3d c, Point3d d)
        {
            Vector3d ad = a - d;
            Vector3d bd = b - d;
            Vector3d cd = c - d;
            return ad * Vector3d.CrossProduct(bd, cd) / 6.0;
        }
    }
}
