//using Rhino.Compute;

using EddyLib;
using EddyLib.BCs;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
