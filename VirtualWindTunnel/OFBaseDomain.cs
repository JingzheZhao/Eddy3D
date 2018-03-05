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
