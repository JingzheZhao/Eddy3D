//using Rhino.Compute;

using EddyLib;
using EddyLib.BCs;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class OFDomainTests

    {
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

        [Fact]
        public void CreateCylDomain_PredefinedGeometry_ReturnCorrectMesh()
        {
            // Arrange

            var mm = Setup.SetUpBuildingMesh();

            // var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
            BC bcond = new ABL(0, 5, 10, 1, 0);

            BCCollection bcColl = new BCCollection(bcond);

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            var tolerance = 1e-9; // Adjust this value as needed for your precision requirements

            Assert.Equal(DOMCYL.pointsOnCircle.First(), DOMCYL.pointsOnCircle.Last(), new Point3dComparer(tolerance));

            Assert.Equal(DOMCYL.pointsOnCircle[0], new Point3d(116.066017177982, 116.066017177982, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnCircle[1], new Point3d(103.704257133164, 127.130321416455, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnCircle[2], new Point3d(87.174363314129, 138.623938856882, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnCircle[6], new Point3d(-19.4174202707276, 157.087101353638, 0), new Point3dComparer(tolerance));

            Assert.Equal(DOMCYL.pointsOnRect[0], new Point3d(60, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[1], new Point3d(50, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[2], new Point3d(40, 60, 0), new Point3dComparer(tolerance));
            Assert.Equal(DOMCYL.pointsOnRect[9], new Point3d(-30, 60, 0), new Point3dComparer(tolerance));

            Assert.True(DOMCYL.DomainMesh.Faces.Count == 320 && DOMCYL.DomainMesh.Vertices.Count == 322);
        }

        [Fact]
        public void PassProperBoundaryConditions_Returns9DifferentWindDirs()
        {
            // Arrange

            var mm = Setup.SetUpBuildingMesh();

            var windDirList = new List<int>() { 0, 22, 45, 90, 135, 180, 225, 270, 315 };

            BCCollection bcColl = new BCCollection(windDirList, BCType.ABL);

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            Assert.True(DOMCYL.BCond.WindDirections.SequenceEqual(new List<int>() { 0, 22, 45, 90, 135, 180, 225, 270, 315 }));
        }

        [Fact]
        public void PassProperBoundaryConditions2_Returns9DifferentWindDirs()
        {
            // Arrange

            var mm = Setup.SetUpBuildingMesh();

            var windDirList = new List<int>() { 0, 22, 45, 90, 135, 180, 225, 270, 315 };

            BCCollection bcColl = new BCCollection();

            foreach (int dir in windDirList)
            {
                var bc = new ABL(dir);
                bcColl.AddBoundaryCondition(bc);
            }

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            Assert.True(DOMCYL.BCond.WindDirections.SequenceEqual(new List<int>() { 0, 22, 45, 90, 135, 180, 225, 270, 315 }));
        }

        [Fact]
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

            Assert.True(v == 3037030.6 && area1 == 151.5 && area3 == 454.6);
        }
    }
}