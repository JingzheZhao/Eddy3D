using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace EddyLib
{
    public class RunBlockMesh
    {



        public static void RunCyl(OFCylDomain DOMCYL, OFMeshSettings MeshSettings, OFRunSettings RunSettings, string workDir)
        {




            if (!Directory.Exists(MeshSettings.meshStlDir))
            {
                Directory.CreateDirectory(MeshSettings.meshStlDir);
            }


            STLExport.ExportBinary(MeshSettings.meshStlFilenameBuildings, DOMCYL.BuildingGeometry);




            if (DOMCYL.TerrainMesh.Faces.Count > 0)
            {
                //No perim if we use a terrain                    
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMCYL.TerrainMesh);
            }
            else
            {
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMCYL.CylDomainMeshGround);
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGroundPerim, DOMCYL.CylDomainMeshGroundPerim);
            }




            if (!Directory.Exists(MeshSettings.meshSystemDir))
            {
                Directory.CreateDirectory(MeshSettings.meshSystemDir);
            }
            if (!Directory.Exists(MeshSettings.meshConstantDir))
            {
                Directory.CreateDirectory(MeshSettings.meshConstantDir);
            }
            if (!Directory.Exists(MeshSettings.meshBoundaryConditionsDirectory))
            {
                Directory.CreateDirectory(MeshSettings.meshBoundaryConditionsDirectory);
            }


            File.WriteAllText(MeshSettings.meshSystemDir + @"\blockMeshDict", DOMCYL.StringyfyDomain2());
            File.WriteAllText(MeshSettings.baseWorkingDir + @"\mesh\case.foam", "");
            File.WriteAllText(MeshSettings.meshSystemDir + @"\controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(RunSettings, DOMCYL, null, 0));




            if (!File.Exists(workDir + @"\mesh\log"))
            {
                File.WriteAllText(workDir + @"\mesh\log", "");
            }





            //TODO: Move Daysim related code into its own class


            //export RAD for DAYSIM
            if (!Directory.Exists(MeshSettings.baseWorkingDir + @"Rad\"))
            {
                Directory.CreateDirectory(MeshSettings.baseWorkingDir + @"Rad\");
            }



            string radMat = @"
void plastic Generic_20
0
0
5 0.2 0.2 0.2 0 0 
";
            Mesh daysimMesh = new Mesh();
            daysimMesh.Append(DOMCYL.BuildingGeometry);
            // Todo: add ground plane to the above mesh

            File.WriteAllText(MeshSettings.baseWorkingDir + @"Rad\materials.rad", radMat);
            RadianceFiles.MeshProc(daysimMesh, MeshSettings.baseWorkingDir + @"Rad\scene.rad", "Generic_20");




        }

        public static void RunBox(OFBoxDomain DOMBOX, OFMeshSettings MeshSettings, OFRunSettings RunSettings, string workDir)
        {




            if (!Directory.Exists(workDir))
            {
                Directory.CreateDirectory(workDir);
            }


            if (!Directory.Exists(MeshSettings.meshStlDir))
            {
                Directory.CreateDirectory(MeshSettings.meshStlDir);
            }


            // STL export

            STLExport.ExportBinary(MeshSettings.meshStlFilenameBuildings, DOMBOX.BuildingGeometry);




            if (DOMBOX.TerrainMesh.Faces.Count > 0)
            {
                //No perim if we use a terrain
                DOMBOX.DomainMeshGround.Translate(Vector3d.ZAxis * 0.001);
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMBOX.DomainMeshGround);
            }
            else
            {
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGround, DOMBOX.DomainMeshGround);
                STLExport.ExportBinary(MeshSettings.meshStlFilenameGroundPerim, DOMBOX.DomainMeshGroundPerim);
            }





            if (!Directory.Exists(MeshSettings.meshSystemDir))
            {
                Directory.CreateDirectory(MeshSettings.meshSystemDir);
            }
            if (!Directory.Exists(MeshSettings.meshConstantDir))
            {
                Directory.CreateDirectory(MeshSettings.meshConstantDir);
            }
            if (!Directory.Exists(MeshSettings.meshBoundaryConditionsDirectory))
            {
                Directory.CreateDirectory(MeshSettings.meshBoundaryConditionsDirectory);
            }





            File.WriteAllText(MeshSettings.meshSystemDir + @"\blockMeshDict", EddyLib.StrTemp.OFExecDicts.BlockMeshDict(DOMBOX));
            File.WriteAllText(MeshSettings.baseWorkingDir + @"\mesh\case.foam", "");
            File.WriteAllText(MeshSettings.meshSystemDir + @"\controlDict", EddyLib.StrTemp.OFExecDicts.ControlDict(RunSettings, DOMBOX, null, 0));

            if (!File.Exists(workDir + @"\mesh\log"))
            {
                File.WriteAllText(workDir + @"\mesh\log", "");
            }



            //export RAD for DAYSIM
            if (!Directory.Exists(MeshSettings.baseWorkingDir + @"Rad\"))
            {
                Directory.CreateDirectory(MeshSettings.baseWorkingDir + @"Rad\");
            }
            string radMat = @"
void plastic Generic_20
0
0
5 0.2 0.2 0.2 0 0 
";
            Mesh daysimMesh = new Mesh();
            daysimMesh.Append(DOMBOX.BuildingGeometry);
            // Todo: add ground plane to the above mesh

            File.WriteAllText(MeshSettings.baseWorkingDir + @"Rad\materials.rad", radMat);
            RadianceFiles.MeshProc(daysimMesh, MeshSettings.baseWorkingDir + @"Rad\scene.rad", "Generic_20");


            string logFile = "";

            using (FileStream stream = File.Open(workDir + @"\mesh\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    logFile = reader.ReadToEnd();
                    //while (!reader.EndOfStream)
                    //{

                    //}

                }
            }
        }

        public static double ProjectedBuildingArea(Vector3d windDir, Mesh buildings, double spacing, out Plane newLocal, out Box box)
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



            ////using (var FrontageImage = new Bitmap(x, z))

            //{
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

                        //FrontageImage.SetPixel(xx, zz, Color.Black);

                    }
                    else
                    {
                        hits.Add(false);
                        //FrontageImage.SetPixel(xx, zz, Color.White);
                    }
                }
            }
            //FrontageImage.RotateFlip(RotateFlipType.RotateNoneFlipY);
            //FrontageImage.Save(pathToSavePNGs, System.Drawing.Imaging.ImageFormat.Png);

            //}



            return incrX * incrZ * hitcount;
        }


        public static double ProjectedBuildingArea(Vector3d windDir, Mesh buildings, double spacing, string pathToSavePNGs, string baseWorkingDir, out Plane newLocal, out Box box)
        {
            if (!Directory.Exists(baseWorkingDir + @"\FrontageImages\"))
            {
                Directory.CreateDirectory(baseWorkingDir + @"\FrontageImages\");
            }


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
                FrontageImage.Save(pathToSavePNGs, System.Drawing.Imaging.ImageFormat.Png);

            }



            return incrX * incrZ * hitcount;
        }

    }

}
