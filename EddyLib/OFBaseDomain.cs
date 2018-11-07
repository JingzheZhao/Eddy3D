using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace EddyLib
{
    // class for all common domain properties, every domain type inherits this
    public class OFBaseDomain
    {
        public Point3d center;
        public Point3d locationInMesh;

        // settings
        public string meshPolyMeshDirectory;
        public string meshWorkingDirectory;
        public string meshSystemDirectory;
        public string meshConstantDirectory;
        public string meshStlDirectory;
        public string baseWorkingDirectory;

        //directories for OF call
        public string OFmeshWorkingDirectory;
        public string OFmeshPolyMeshDirectory;
        public string OFmeshSystemDirectory;
        public string OFmeshConstantDirectory;
        public string OFmeshStlDirectory;
        public string OFbaseWorkingDirectory;

        public int CPUs;

        public double frontageBuildingArea;

        //simulation inputs

        public int iter;
        public int writeInterval;
        public int keepTimeSteps;


        public Cylinder refinementCylinder;
        public BoundingBox refinementBox;
        public BoundingBox BBox;

        //Building Meshes for probes component
        //public Mesh combinedMeshes;
        public Brep inputBreps;


        //Meshes from Cycl Domain
        public Mesh DomainMesh = new Mesh();
        public Mesh DomainMeshGround = new Mesh();
        public Mesh DomainMeshGroundPerim = new Mesh();
        public Mesh terrainMesh;


        public Mesh perim = new Mesh();
        public Mesh core = new Mesh();
        public Mesh perimTop = new Mesh();
        public Mesh coreTop = new Mesh();
        public Mesh side = new Mesh();

        public BoundaryConditions BCInflow;

        public int accBuildings;
        public int accGround;
        public int accFeatures;
        public int accRefinement;
        public int meshingMode;
        public int nLayers;

        public bool autoCPUCalc;
        public int numberOfCellsInMesh;

        public int turbulenceModel;

        public bool IsWindows7 = false;

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

            // Interval refBoxinterval = new Interval(refBox.Min.X, refBox.Max.X);   // y
            // Interval refBoxinterval2 = new Interval(refBox.Min.Y, refBox.Max.Y);  // z
            // Interval refBoxinterval3 = new Interval(refBox.Min.Z, refBox.Max.Z);
            //return new Box(localSystem, refBoxinterval, refBoxinterval2, refBoxinterval3);
        }


     



        public Cylinder GetRefinementCyl(Point3d center, Mesh buildings, double paddingXY = 0, double paddingZ = 0.3)
        {

            BoundingBox bb = buildings.GetBoundingBox(true);
            var pt = new Point3d(bb.Max.X, bb.Max.Y, center.Z);
            var radiusRefBox = (center - pt).Length;
            var cyl = new Cylinder(new Circle(center, radiusRefBox + paddingXY), bb.Max.Z + paddingZ);
            return cyl;

        }

        public static double ProjectedBuildingArea(Plane localSystem, Mesh buildings, double spacing , string path)
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


            int y = (int)Math.Round(interval.Length / spacing);
            int z = (int)Math.Round(interval2.Length / spacing);
           


            double incrY = interval.Length / y;
            double incrZ = interval2.Length / z;

            //Todo: Adapt length to actual domain dimension
            double raylen = 99999999999;

            List<Point3d> points = new List<Point3d>();
            List<Ray3d> rays = new List<Ray3d>();

             List<bool> hits = new List<bool>();
            int hitcount = 0;



 using (var FrontageImage = new Bitmap(y, z))

            { 
            using (Graphics graph = Graphics.FromImage(FrontageImage))
            {
                Rectangle ImageSize = new Rectangle(0, 0, y, z);
                graph.FillRectangle(Brushes.White, ImageSize);
            }


            for (int i = 0; i < z; i++)
            {

                for (int j = 0; j < y; j++)
                {

                    var pt = localSystem.PointAt((0.5 * incrY) + interval.Min + j * incrY, (0.5 * incrZ) + interval2.Min + i * incrZ);
                    points.Add(pt);

                    var ray = new Ray3d(pt, localSystem.ZAxis * raylen);

                    rays.Add(ray);

                    double d = Rhino.Geometry.Intersect.Intersection.MeshRay(buildings, ray);
                     if (d > 0)
                {
                    hitcount++;
                    hits.Add(true);

                        FrontageImage.SetPixel(j, i, Color.Black);

                }
                else { hits.Add(false);
                        FrontageImage.SetPixel(j, i, Color.White);
                    }
                }
            }

                FrontageImage.Save(path, System.Drawing.Imaging.ImageFormat.Png);

}

            

            return incrY * incrZ * hitcount;
        }
    }
}
