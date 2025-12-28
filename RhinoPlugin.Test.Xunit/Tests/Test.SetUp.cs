//using Rhino.Compute;

using Rhino.Geometry;
using System.Collections.Generic;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    [Collection("Rhino Collection")]
    public class Setup

    {
        public static Mesh SetUpBuildingMesh()
        {
            var point0 = new Point3d(0, 0, 0);
            var point1 = new Point3d(20, 0, 0);
            var point2 = new Point3d(0, 20, 0);
            var point3 = new Point3d(20, 20, 0);
            var point4 = new Point3d(0, 0, 40);
            var point5 = new Point3d(20, 0, 40);
            var point6 = new Point3d(0, 20, 40);
            var point7 = new Point3d(20, 20, 40);
            var box1 = new Box(Plane.WorldXY, new List<Point3d> { point0, point1, point2, point3, point4, point5, point6, point7 });
            var mp = new MeshingParameters();
            var m = Mesh.CreateFromBrep(box1.ToBrep(), mp);

            var mm = new Mesh();

            foreach (Mesh im in m)
            {
                mm.Append(im);
            }

            return mm;
        }

        public static Mesh SetUpBuildingMeshAppendSurface()
        {
            var mm = SetUpBuildingMesh();

            var surface = NurbsSurface.CreateFromCorners(
                new Point3d(5000, 0, 0),
                new Point3d(5000, 5000, 0),
                new Point3d(0, 5000, 0),
                new Point3d(0, 0, 0));

            surface.Translate(new Vector3d(-2500, -2050, 0));
            var mp = new MeshingParameters();
            var s = Mesh.CreateFromBrep(surface.ToBrep(), mp);
            mm.Append(s);

            return mm;
        }
    }
}
