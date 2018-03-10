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
       


        public static BoundingBox getRefinementBox(Plane localSystem, Mesh buildings, double padding = 10)
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

        public static Cylinder getRefinementCyl(Point3d center, Mesh buildings, double padding = 10)
        {
     
            BoundingBox bb = buildings.GetBoundingBox(true);
            var pt = new Point3d(bb.Max.X, bb.Max.Y, center.Z);
            double radi = (center - pt).Length ;
            var cyl = new Cylinder(new Circle(center, radi + padding), bb.Max.Z + padding);
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
