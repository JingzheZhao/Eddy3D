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
        public Point3d CenterGround;
        public Point3d LocationInMesh;



        public double[] FrontageBuildingAreas = new double[360];
        public double MaxFrontageBuildingArea;
        public Bitmap[] FrontagePNGs = new Bitmap[360];

        public Cylinder RefinementCylinder;        
        public BoundingBox BBox;

        // 3 Main meshes
        
        public Mesh TerrainMesh;
        public Mesh DomainMesh;
        public Mesh[] DomainMeshIntersection;
        public Mesh BuildingGeometry;


        public BoundaryConditions BCond;



        public int NumberOFCellsInMesh;



        public List<double> Runtimes = new List<double>();

        public bool hasTerrain;

        public double zMaxBuilding;


        public static Plane GetOrientedBasePlane(Vector3d windDir, Mesh buildings, Point3d CenterGround)
        {

            var up = Vector3d.ZAxis;
            var forward = windDir;
            forward.Unitize();
            var right = Vector3d.CrossProduct(forward, up);
            right.Unitize();

            var boundingBox = buildings.GetBoundingBox(true);
            Point3d newO = boundingBox.Min;

            var orientedBasePlane = new Plane(CenterGround, right, forward);

            return orientedBasePlane;
        }


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


        public static double GetZMinTerrain(Mesh terrain, BoundingBox Box, Plane orientedlocalPlane)
        {

            // If terrain is used, scale down Z to make sure all points are inside the domain
            // Zinter is call divisionsZ for CylDomain which is an int instead of an Interval


            Plane worldXY = Plane.WorldXY;
            Transform xform = Transform.ChangeBasis(worldXY, orientedlocalPlane);
            var refBox = BoundingBox.Empty;
            BoundingBox bboxTerrain = terrain.GetBoundingBox(xform);


            double zDomain = Box.Min.Z;


            //BoundingBox bboxTerrain = terrain.GetBoundingBox(orientedlocalPlane);

            if (terrain.Faces.Count > 0)
            {

                if (bboxTerrain.Min.Z < zDomain)
                {
                    zDomain = bboxTerrain.Min.Z;

                }

            }


            return zDomain;

        }

        public static double GetProjectedBuildingArea(int windDir, Mesh buildings, out Bitmap FrontageImage)
        {

            // Spacing in meters between rays
            //Todo: checked this number how correct it is
            double spacing = 10;



            Vector3d windDirVec = Utilities.Dir2Vec(windDir);



            var up = Vector3d.ZAxis;
            var forward = windDirVec;
            forward.Unitize();
            var right = Vector3d.CrossProduct(forward, up);
            right.Unitize();



            var CenterGround = buildings.GetBoundingBox(true).Center + 0.5 * -Vector3d.ZAxis * (buildings.GetBoundingBox(true).Max.Z - buildings.GetBoundingBox(true).Min.Z);


            BoundingBox empty = BoundingBox.Empty;
            BoundingBox boundingBox = buildings.GetBoundingBox(true);
            empty.Union(boundingBox);

            //var startPoint = new Point3d(empty.Min.X, empty.Min.Y, empty.Min.Z);

            Plane local = new Plane(CenterGround, right, forward);



            var BBox = empty;


            var xMin = BBox.Min.X;
            var xMax = BBox.Max.X;
            var yMin = BBox.Min.Y;
            var yMax = BBox.Max.Y;
            var zMin = BBox.Min.Z;
            var zMax = BBox.Max.Z;

            var dimX = xMax - xMin;
            var dimY = yMax - yMin;
            var dimZ = zMax - zMin;


            Interval intervalX = new Interval(empty.Min.X, empty.Max.X);
            Interval intervalZ = new Interval(empty.Min.Z, empty.Max.Z);



            int x = (int)Math.Round(intervalX.Length / spacing);
            int z = (int)Math.Round(intervalZ.Length / spacing);

            double incrX = intervalX.Length / x;
            double incrZ = intervalZ.Length / z;
            double raylen = 9999;

            List<Point3d> points = new List<Point3d>();
            List<Ray3d> rays = new List<Ray3d>();
            //testVecs = new List<Vector3d>();

            List<bool> hits = new List<bool>();
            int hitcount = 0;

                                 
            using (var FI = new Bitmap(x, z))

            {               

                for (int zz = 0; zz < z; zz++)
                {

                    for (int xx = 0; xx < x; xx++)
                    {


                        var centerZ = (0.5 * incrZ);
                        var centerX = (0.5 * incrX);

                        var pt = local.PointAt(centerX + xx * incrX - 0.5 * dimX, -50, centerZ + zz * incrZ);

                        points.Add(pt);



                        var ray = new Ray3d(pt, local.YAxis * raylen);
                        var vec = new Vector3d(local.YAxis * raylen);

                        rays.Add(ray);
                        //testVecs.Add(vec);



                        double d = Rhino.Geometry.Intersect.Intersection.MeshRay(buildings, ray);
                        if (d > 0)
                        {
                            hitcount++;
                            hits.Add(true);

                            FI.SetPixel(xx, zz, Color.Black);

                        }
                        else
                        {
                            hits.Add(false);
                            FI.SetPixel(xx, zz, Color.White);
                        }


                    }
                }


                FrontageImage = FI;
                //Needs to be rotated and flipped to represend the view from the wind direction
                FrontageImage.RotateFlip(RotateFlipType.Rotate180FlipX);

            }

            return incrX * incrZ * hitcount;
        }

        public static double GetProjectedBuildingArea(int windDir, Mesh buildings, out Bitmap FrontageImage, out List<Vector3d> testVecs)
        {

            // Spacing in meters between rays
            double spacing = 2;



            Vector3d windDirVec = Utilities.Dir2Vec(windDir);



            var up = Vector3d.ZAxis;
            var forward = windDirVec;
            forward.Unitize();
            var right = Vector3d.CrossProduct(forward, up);
            right.Unitize();



            var CenterGround = buildings.GetBoundingBox(true).Center + 0.5 * -Vector3d.ZAxis * (buildings.GetBoundingBox(true).Max.Z - buildings.GetBoundingBox(true).Min.Z);


            BoundingBox empty = BoundingBox.Empty;
            BoundingBox boundingBox = buildings.GetBoundingBox(true);
            empty.Union(boundingBox);

            //var startPoint = new Point3d(empty.Min.X, empty.Min.Y, empty.Min.Z);

            Plane local = new Plane(CenterGround, right, forward);



            var BBox = empty;


            var xMin = BBox.Min.X;
            var xMax = BBox.Max.X;
            var yMin = BBox.Min.Y;
            var yMax = BBox.Max.Y;
            var zMin = BBox.Min.Z;
            var zMax = BBox.Max.Z;

            var dimX = xMax - xMin;
            var dimY = yMax - yMin;
            var dimZ = zMax - zMin;


            Interval intervalX = new Interval(empty.Min.X, empty.Max.X);
            //Interval intervalY = new Interval(empty.Min.Y, empty.Max.Y);
            Interval intervalZ = new Interval(empty.Min.Z, empty.Max.Z);



            int x = (int)Math.Round(intervalX.Length / spacing);
            int z = (int)Math.Round(intervalZ.Length / spacing);

            double incrX = intervalX.Length / x;
            double incrZ = intervalZ.Length / z;
            double raylen = 9999;

            List<Point3d> points = new List<Point3d>();
            List<Ray3d> rays = new List<Ray3d>();
            testVecs = new List<Vector3d>();

            List<bool> hits = new List<bool>();
            int hitcount = 0;





            using (var FI = new Bitmap(x, z))

            {



                for (int zz = 0; zz < z; zz++)
                {

                    for (int xx = 0; xx < x; xx++)
                    {

                        //var pt = local.PointAt((0.5 * incrX) + xx * incrX, -0.1, (0.5 * incrZ) + zz * incrZ);
                        var centerZ = (0.5 * incrZ);
                        var centerX = (0.5 * incrX);

                        var pt = local.PointAt(centerX + xx * incrX - 0.5 * dimX, -50, centerZ + zz * incrZ);

                        points.Add(pt);



                        var ray = new Ray3d(pt, local.YAxis * raylen);
                        var vec = new Vector3d(local.YAxis * raylen);

                        rays.Add(ray);
                        testVecs.Add(vec);



                        double d = Rhino.Geometry.Intersect.Intersection.MeshRay(buildings, ray);
                        if (d > 0)
                        {
                            hitcount++;
                            hits.Add(true);

                            FI.SetPixel(xx, zz, Color.Black);

                        }
                        else
                        {
                            hits.Add(false);
                            FI.SetPixel(xx, zz, Color.White);
                        }


                    }
                }


                FrontageImage = FI;
                //Needs to be rotated and flipped to represend the view from the wind direction
                FrontageImage.RotateFlip(RotateFlipType.Rotate180FlipX);

            }

            return incrX * incrZ * hitcount;
        }


    }
}
