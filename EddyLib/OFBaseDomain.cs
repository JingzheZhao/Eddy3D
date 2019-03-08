using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace EddyLib
{
    // class for all common domain properties, every domain type inherits this
    public class OFBaseDomain
    {
        public Point3d center;
        public Point3d locationInMesh;

        

        public double frontageBuildingArea;

        public Cylinder refinementCylinder;
        public BoundingBox refinementBox;
        public BoundingBox BBox;

        //Building Meshes for probes component
        public Brep inputBreps;


        //Meshes from Cycl Domain
        public Mesh DomainMesh = new Mesh();
        public Mesh DomainMeshGround = new Mesh();
        public Mesh DomainMeshGroundPerim = new Mesh();
        public Mesh TerrainMesh = new Mesh();
        public Mesh CombinedMesh; // TODO: What is this??

        public Mesh perimBottom = new Mesh();
        public Mesh coreBottom = new Mesh();
        public Mesh perimTop = new Mesh();
        public Mesh coreTop = new Mesh();
        public Mesh sides = new Mesh();

        public BoundaryConditions BCond;



        public int numberOfCellsInMesh;



        public List<double> runtimes = new List<double>();




        public static BoundingBox GetRefinementBox(Plane localSystem, Mesh buildings, double padding = 0)
        {
            Plane worldXY = Plane.WorldXY;
            Transform xform = Transform.ChangeBasis(worldXY, localSystem);
            var refBox = BoundingBox.Empty;
            BoundingBox boundingBox = buildings.GetBoundingBox(xform);
            refBox.Union(boundingBox);
            refBox.Max = new Point3d(refBox.Max.X + padding, refBox.Max.Y + padding, refBox.Max.Z + padding);
            refBox.Min = new Point3d(refBox.Min.X - padding, refBox.Min.Y - padding, refBox.Min.Z);

            return boundingBox;
        }

               


        public Cylinder GetRefinementCyl(Point3d center, Mesh buildings, double paddingXY = 0, double paddingZ = 0.3)
        {

            BoundingBox bb = buildings.GetBoundingBox(true);
            var pt = new Point3d(bb.Max.X, bb.Max.Y, center.Z);
            var radiusRefBox = (center - pt).Length;
            var cyl = new Cylinder(new Circle(center, radiusRefBox + paddingXY), bb.Max.Z + paddingZ);
            return cyl;

        }

       
    }
}
