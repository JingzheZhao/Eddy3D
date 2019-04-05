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
        public Point3d Center;
        public Point3d LocationInMesh;

        

        public double FrontageBuildingArea;

        public Cylinder RefinementCylinder;        
        public BoundingBox BBox;

        // 3 Main meshes
        
        public Mesh TerrainMesh;
        public Mesh DomainMesh;
        public Mesh BuildingGeometry;


        public BoundaryConditions BCond;



        public int NumberOFCellsInMesh;



        public List<double> Runtimes = new List<double>();




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


        public static double GetZMinTerrain(Mesh terrain, BoundingBox Box)
        {

            // If terrain is used, scale down Z to make sure all points are inside the domain
            // Zinter is call divisionsZ for CylDomain which is an int instead of an Interval
            double zDomain = Box.Min.Z;         

         
            BoundingBox bboxTerrain = terrain.GetBoundingBox(true);

            if (terrain.Faces.Count > 0)
            {

                if (bboxTerrain.Min.Z < zDomain)
                {
                    zDomain = bboxTerrain.Min.Z;
         
                }

            }


            return zDomain;

        }

       
    }
}
