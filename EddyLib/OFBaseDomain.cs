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

        public static double ProjectedBuildingArea(Vector3d windDir, Mesh buildings, double spacing, string path, out Plane newLocal , out Box box)
        {
            var up = Vector3d.ZAxis;
            var forward = windDir;
            forward.Unitize();
            var right = Vector3d.CrossProduct(forward, up);
            right.Unitize();


            Plane local = new Plane(Point3d.Origin, right, forward);


            Plane worldXY = Plane.WorldXY;
            Transform xform = Transform.ChangeBasis(worldXY, local);
            Transform xformBack = Transform.ChangeBasis(local, worldXY);


            BoundingBox empty = BoundingBox.Empty;
            BoundingBox boundingBox = buildings.GetBoundingBox(xform);
            empty.Union(boundingBox);


            Interval intervalX = new Interval(empty.Min.X, empty.Max.X);
            Interval intervalY = new Interval(empty.Min.Y, empty.Max.Y);
            Interval intervalZ = new Interval(empty.Min.Z, empty.Max.Z);
            box = new Box(local, intervalX, intervalY, intervalZ);

            Point3d newO = empty.Min;
            // Transform xformBack;
            // xform.TryGetInverse(out xformBack);
            newO.Transform(xformBack);

            newLocal = new Plane(newO, right, forward);


            int x = (int)Math.Round(intervalX.Length / spacing);
            int z = (int)Math.Round(intervalZ.Length / spacing);

            double incrX = intervalX.Length / x;
            double incrZ = intervalZ.Length / z;
            double raylen = intervalY.Length;

            List<Point3d> points = new List<Point3d>();
            List<Ray3d> rays = new List<Ray3d>();

            List<bool> hits = new List<bool>();
            int hitcount = 0;



            using (var FrontageImage = new Bitmap(x, z))

            {
                for (int zz = 0; zz < z; zz++)
                {

                    for (int xx = 0; xx < x; xx++)
                    {
                        var pt = newLocal.PointAt((0.5 * incrX) + xx * incrX, -0.1, (0.5 * incrZ) + zz * incrZ);
                     
                        points.Add(pt);

                        var ray = new Ray3d(pt, newLocal.YAxis * raylen);

                        rays.Add(ray);

                        double d = Rhino.Geometry.Intersect.Intersection.MeshRay(buildings, ray);
                        if (d > 0)
                        {
                            hitcount++;
                            hits.Add(true);

                            FrontageImage.SetPixel(xx, zz, Color.Black);

                        }
                        else
                        {
                            hits.Add(false);
                            FrontageImage.SetPixel(xx, zz, Color.White);
                        }
                    }
                }
                FrontageImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
                FrontageImage.Save(path, System.Drawing.Imaging.ImageFormat.Png);

            }



            return incrX * incrZ * hitcount;
        }
    }
}
