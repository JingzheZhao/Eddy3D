//using Rhino.Compute;

namespace UnitTest
{
    //[TestClass]
    //public class CylDomain
    //{
    //    [TestMethod]
    //    public void CreateCylDomain_PredefinedGeometry_ReturnCorrectMesh()
    //    {
    //        // Arrange

    //        ComputeServer.AuthToken = "patrick.kastner@tum.de";

    //        var point0 = new Rhino.Geometry.Point3d(0, 0, 0);
    //        var point1 = new Rhino.Geometry.Point3d(20, 0, 0);
    //        var point2 = new Rhino.Geometry.Point3d(0, 20, 0);
    //        var point3 = new Rhino.Geometry.Point3d(20, 20, 0);
    //        var point4 = new Rhino.Geometry.Point3d(0, 0, 40);
    //        var point5 = new Rhino.Geometry.Point3d(20, 0, 40);
    //        var point6 = new Rhino.Geometry.Point3d(0, 20, 40);
    //        var point7 = new Rhino.Geometry.Point3d(20, 20, 40);
    //        Rhino.Geometry.Box box1 = new Rhino.Geometry.Box(Rhino.Geometry.Plane.WorldXY, new List<Rhino.Geometry.Point3d>() { point0, point1, point2, point3, point4, point5, point6, point7 });
    //        Rhino.Geometry.MeshingParameters mp = new Rhino.Geometry.MeshingParameters();
    //        var m = Rhino.Compute.MeshCompute.CreateFromBrep(box1.ToBrep(), mp);

    //        Rhino.Geometry.Mesh mm = new Rhino.Geometry.Mesh();

    //        foreach (Rhino.Geometry.Mesh im in m)
    //        {
    //            mm.Append(im);
    //        }

    //        var windDirList = new List<int>() { 0, 45, 90, 135, 180, 225, 270, 315 };
    //        BoundaryConditions bcond = new BoundaryConditions(BoundaryType.abl, windDirList, 5, 1, "");

    //        // Act OFCylDomain DOMCYL = new OFCylDomain(mm, null, bcond, 5, 50, 300, 80);

    //        // Assert.IsTrue(DOMCYL.DomainMesh.Faces.Count == 320 && DOMCYL.DomainMesh.Vertices.Count
    //        // == 322);
    //    }
    //}
}