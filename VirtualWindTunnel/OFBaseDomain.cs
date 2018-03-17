using Rhino.Geometry;
using System.Collections.Generic;


namespace Eddy
{
    // class for all common domain properties, every domain type inherits this
    public class OFBaseDomain
    {
        public Point3d center;
        public Point3d locationInMesh;

        // settings
        public string workingDirectory;
        public string systemDirectory;
        public string constantDirectory;
        public string postProcessingDirectory;
        public string stlDirectory;
                
        public int CPU;

        public double frontageBuildingArea;

        //simulation inputs

        public int iter;
        public int writeInterval;
        public int keepTimeSteps;

        public int flowDir;
        public Cylinder refinementCylinder;
        public BoundingBox refinementBox;
        public BoundingBox BBox;


        //Meshes from Cycl Domain
        public Mesh DomainMesh= new Mesh();
        public Mesh DomainMeshGround = new Mesh();
        public Mesh DomainMeshGroundPerim = new Mesh();

        
        public Mesh perim = new Mesh();
        public Mesh core = new Mesh();
        public Mesh perimTop = new Mesh();
        public Mesh coreTop = new Mesh();
        public Mesh side = new Mesh();

        public List<BoundaryConditions> BCInflow;

        public int accBuildings;
        public int accGround;
        public int accFeatures;
        public int accRefinement;
        
        public int nLayers;

        


        public static BoundingBox getRefinementBox(Plane localSystem, Mesh buildings, double padding = 0)
        {
            Plane worldXY = Plane.WorldXY;
            Transform xform = Transform.ChangeBasis(worldXY, localSystem);
            var refBox = BoundingBox.Empty;
            BoundingBox boundingBox = buildings.GetBoundingBox(xform);
            refBox.Union(boundingBox);
            refBox.Max = new Point3d(refBox.Max.X + padding, refBox.Max.Y + padding, refBox.Max.Z + padding);
            refBox.Min = new Point3d(refBox.Min.X - padding, refBox.Min.Y - padding, refBox.Min.Z);

            return boundingBox;

           // Interval refBoxinterval = new Interval(refBox.Min.X, refBox.Max.X);   // y
           // Interval refBoxinterval2 = new Interval(refBox.Min.Y, refBox.Max.Y);  // z
           // Interval refBoxinterval3 = new Interval(refBox.Min.Z, refBox.Max.Z);
           //return new Box(localSystem, refBoxinterval, refBoxinterval2, refBoxinterval3);
        }

        


        public Cylinder getRefinementCyl(Point3d center, Mesh buildings, double paddingXY = 0, double paddingZ = 0.3)
        {
     
            BoundingBox bb = buildings.GetBoundingBox(true);
            var pt = new Point3d(bb.Max.X, bb.Max.Y, center.Z);          
            var radiusRefBox = (center - pt).Length; 
            var cyl = new Cylinder(new Circle(center, radiusRefBox + paddingXY), bb.Max.Z + paddingZ);
            return cyl;

        }

        public static double projectedBuildingArea(Plane localSystem, Mesh buildings)
        {

            Plane worldXY = Plane.WorldXY;
            Transform xform = Transform.ChangeBasis(worldXY, localSystem);


            BoundingBox empty = BoundingBox.Empty;


            BoundingBox boundingBox = buildings.GetBoundingBox(xform);
            empty.Union(boundingBox);
            Interval interval = new Interval(empty.Min.X, empty.Max.X);   // y
            Interval interval2 = new Interval(empty.Min.Y, empty.Max.Y);  // z
            Interval interval3 = new Interval(empty.Min.Z, empty.Max.Z);
            Box box = new Box(localSystem, interval, interval2, interval3);



            //Todo:
            int z = 30;
            int y = 30;

            double incrY = interval.Length / y;
            double incrZ = interval2.Length / z;
            //Todo: Adapt length to actual domain dimension
            double raylen = 99999999999;

            List<Point3d> points = new List<Point3d>();
            List<Ray3d> rays = new List<Ray3d>();
            for (int i = 0; i < z; i++)
            {

                for (int j = 0; j < y; j++)
                {

                    var pt = localSystem.PointAt((0.5 * incrY) + interval.Min + j * incrY, (0.5 * incrZ) + interval2.Min + i * incrZ);
                    points.Add(pt);
                    rays.Add(new Ray3d(pt, localSystem.ZAxis * raylen));


                }
            }


            List<bool> hits = new List<bool>();
            int hitcount = 0;
            for (int i = 0; i < rays.Count; i++)
            {
                double d = Rhino.Geometry.Intersect.Intersection.MeshRay(buildings, rays[i]);
                if (d > 0)
                {
                    hitcount++;
                    hits.Add(true);
                }
                else { hits.Add(false); }
            };



          
            return incrY * incrZ * hitcount;
        }
    }
}
