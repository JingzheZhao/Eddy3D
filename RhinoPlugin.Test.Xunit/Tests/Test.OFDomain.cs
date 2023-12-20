//using Rhino.Compute;

using EddyLib;
using EddyLib.BCs;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Tests.Xunit
{
    [Collection("Rhino Collection")]
    public class OFDomainTests
    {
        [Fact]
        public void CreateCylDomain_PredefinedGeometry_ReturnCorrectMesh()
        {
            // Arrange

            var point0 = new Rhino.Geometry.Point3d(0, 0, 0);
            var point1 = new Rhino.Geometry.Point3d(20, 0, 0);
            var point2 = new Rhino.Geometry.Point3d(0, 20, 0);
            var point3 = new Rhino.Geometry.Point3d(20, 20, 0);
            var point4 = new Rhino.Geometry.Point3d(0, 0, 40);
            var point5 = new Rhino.Geometry.Point3d(20, 0, 40);
            var point6 = new Rhino.Geometry.Point3d(0, 20, 40);
            var point7 = new Rhino.Geometry.Point3d(20, 20, 40);
            Rhino.Geometry.Box box1 = new Rhino.Geometry.Box(Rhino.Geometry.Plane.WorldXY, new List<Rhino.Geometry.Point3d>() { point0, point1, point2, point3, point4, point5, point6, point7 });
            Rhino.Geometry.MeshingParameters mp = new Rhino.Geometry.MeshingParameters();
            var m = Mesh.CreateFromBrep(box1.ToBrep(), mp);

            Rhino.Geometry.Mesh mm = new Rhino.Geometry.Mesh();

            foreach (Rhino.Geometry.Mesh im in m)
            {
                mm.Append(im);
            }

            // var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
            BC bcond = new ABL(0, 5, 10, 1, 0, "");

            BCCollection bcColl = new BCCollection(bcond);

            OFCylDomain DOMCYL = new OFCylDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            Assert.True(DOMCYL.DomainMesh.Faces.Count == 1040 && DOMCYL.DomainMesh.Vertices.Count == 1042);
        }

        [Fact]
        public void CreateBoxDomain_PredefinedGeometry_ReturnCorrectMesh()
        {
            // Arrange

            var point0 = new Rhino.Geometry.Point3d(0, 0, 0);
            var point1 = new Rhino.Geometry.Point3d(20, 0, 0);
            var point2 = new Rhino.Geometry.Point3d(0, 20, 0);
            var point3 = new Rhino.Geometry.Point3d(20, 20, 0);
            var point4 = new Rhino.Geometry.Point3d(0, 0, 40);
            var point5 = new Rhino.Geometry.Point3d(20, 0, 40);
            var point6 = new Rhino.Geometry.Point3d(0, 20, 40);
            var point7 = new Rhino.Geometry.Point3d(20, 20, 40);
            Rhino.Geometry.Box box1 = new Rhino.Geometry.Box(Rhino.Geometry.Plane.WorldXY, new List<Rhino.Geometry.Point3d>() { point0, point1, point2, point3, point4, point5, point6, point7 });
            Rhino.Geometry.MeshingParameters mp = new Rhino.Geometry.MeshingParameters();
            var m = Mesh.CreateFromBrep(box1.ToBrep(), mp);

            Rhino.Geometry.Mesh mm = new Rhino.Geometry.Mesh();

            foreach (Rhino.Geometry.Mesh im in m)
            {
                mm.Append(im);
            }

            BC bcond = new ABL(330, 5, 10, 1, 0, "");

            BCCollection bcColl = new BCCollection(bcond);

            OFBoxDomain DOM = new OFBoxDomain(mm, new Mesh(), bcColl, 5, 50, 300, 80);

            var v = Math.Round(DOM.DomainMesh.Volume(), 1);

            var area1 = Math.Round(EddyLib.Utilities.MeshFaceArea(1, DOM.DomainMeshGroundPerim), 1);
            var area3 = Math.Round(EddyLib.Utilities.MeshFaceArea(3, DOM.DomainMeshGroundPerim), 1);

            Assert.True(v == 3037030.6 && area1 == 151.5 && area3 == 454.6);
        }
    }
}