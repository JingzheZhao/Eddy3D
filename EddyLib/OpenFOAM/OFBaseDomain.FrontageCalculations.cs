using EddyLib.BCs;
using EddyLib.Domain;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace EddyLib
{
    /// <summary>
    /// Base class for all CFD domain types.
    /// Provides common properties and methods for domain construction.
    /// </summary>
    public abstract partial class OFBaseDomain : IDomain
    {
        #region Frontage Calculations

        /// <summary>
        /// Gets the projected building area for a given wind direction.
        /// </summary>
        public static double GetProjectedBuildingArea(int windDir, Mesh buildings, out Bitmap frontageImage)
        {
            return GetProjectedBuildingAreaInternal(windDir, buildings, 10, true, out frontageImage, null);
        }

        /// <summary>
        /// Gets the projected building area with debug vectors.
        /// </summary>
        public static double GetProjectedBuildingArea(int windDir, Mesh buildings, out Bitmap frontageImage, out List<Vector3d> testVecs)
        {
            testVecs = new List<Vector3d>();
            return GetProjectedBuildingAreaInternal(windDir, buildings, 2, false, out frontageImage, testVecs);
        }

        private static double GetProjectedBuildingAreaInternal(
            int windDir,
            Mesh buildings,
            double spacing,
            bool clampDivisions,
            out Bitmap frontageImage,
            List<Vector3d> testVecs)
        {
            Vector3d windDirVec = Utilities.Dir2Vec(windDir);

            var up = Vector3d.ZAxis;
            var forward = windDirVec;
            forward.Unitize();
            var right = Vector3d.CrossProduct(forward, up);
            right.Unitize();

            var boundingBox = buildings.GetBoundingBox(true);
            var centerGround = boundingBox.Center + 0.5 * -Vector3d.ZAxis * (boundingBox.Max.Z - boundingBox.Min.Z);

            var local = new Plane(centerGround, right, forward);
            var dimX = boundingBox.Max.X - boundingBox.Min.X;

            var intervalX = new Interval(boundingBox.Min.X, boundingBox.Max.X);
            var intervalZ = new Interval(boundingBox.Min.Z, boundingBox.Max.Z);

            int x = (int)Math.Round(intervalX.Length / spacing);
            int z = (int)Math.Round(intervalZ.Length / spacing);

            if (clampDivisions)
            {
                x = Math.Max(1, x);
                z = Math.Max(1, z);
            }

            if (clampDivisions && (x > 5000 || z > 5000))
            {
                throw new ArgumentException("Simulation domain either too large or too far from the origin.");
            }

            double incrX = intervalX.Length / x;
            double incrZ = intervalZ.Length / z;

            int hitcount = 0;
            var image = new Bitmap(x, z);

            for (int zz = 0; zz < z; zz++)
            {
                for (int xx = 0; xx < x; xx++)
                {
                    var centerZ = 0.5 * incrZ;
                    var centerX = 0.5 * incrX;

                    var pt = local.PointAt(centerX + xx * incrX - 0.5 * dimX, -50, centerZ + zz * incrZ);
                    var ray = new Ray3d(pt, local.YAxis * DomainConstants.DefaultRayLength);
                    var vec = new Vector3d(local.YAxis * DomainConstants.DefaultRayLength);

                    testVecs?.Add(vec);

                    double d = Rhino.Geometry.Intersect.Intersection.MeshRay(buildings, ray);
                    if (d > 0)
                    {
                        hitcount++;
                        image.SetPixel(xx, zz, Color.Black);
                    }
                    else
                    {
                        image.SetPixel(xx, zz, Color.White);
                    }
                }
            }

            frontageImage = image;
            frontageImage.RotateFlip(RotateFlipType.Rotate180FlipX);

            return incrX * incrZ * hitcount;
        }

        #endregion
    }
}
