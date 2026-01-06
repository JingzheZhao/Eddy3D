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
        #region Static Helper Methods

        /// <summary>
        /// Gets a base plane oriented to the wind direction.
        /// </summary>
        public static Plane GetOrientedBasePlane(Vector3d windDir, Point3d centerGround)
        {
            var up = Vector3d.ZAxis;
            var forward = windDir;
            forward.Unitize();
            var right = Vector3d.CrossProduct(forward, up);
            right.Unitize();

            return new Plane(centerGround, right, forward);
        }

        /// <summary>
        /// Gets a refinement bounding box around buildings.
        /// </summary>
        public static BoundingBox GetRefinementBox(Plane localSystem, Mesh buildings, double padding = 0)
        {
            var xform = Transform.ChangeBasis(Plane.WorldXY, localSystem);
            var refBox = BoundingBox.Empty;
            var boundingBox = buildings.GetBoundingBox(xform);
            refBox.Union(boundingBox);
            refBox.Max = new Point3d(refBox.Max.X + padding, refBox.Max.Y + padding, refBox.Max.Z + padding);
            refBox.Min = new Point3d(refBox.Min.X - padding, refBox.Min.Y - padding, refBox.Min.Z);

            return boundingBox;
        }

        /// <summary>
        /// Gets a refinement cylinder around buildings.
        /// </summary>
        public Cylinder GetRefinementCyl(Point3d center, Mesh buildings, double paddingXY = 0, double paddingZ = DomainConstants.RefinementZPadding)
        {
            BoundingBox bb = buildings.GetBoundingBox(true);
            var pt = new Point3d(bb.Max.X, bb.Max.Y, center.Z);
            var radiusRefBox = (center - pt).Length;
            return new Cylinder(new Circle(center, radiusRefBox + paddingXY), bb.Max.Z + paddingZ);
        }

        /// <summary>
        /// Gets the minimum Z coordinate of terrain.
        /// </summary>
        public static double GetZMinTerrain(Mesh terrain, BoundingBox box, Plane orientedPlane)
        {
            return GetZMinTerrainInternal(terrain, box.Min.Z, orientedPlane);
        }

        /// <summary>
        /// Gets the minimum Z coordinate of terrain.
        /// </summary>
        public static double GetZMinTerrain(Mesh terrain, Box box, Plane orientedPlane)
        {
            return GetZMinTerrainInternal(terrain, box.Z.Min, orientedPlane);
        }

        private static double GetZMinTerrainInternal(Mesh terrain, double defaultZ, Plane orientedPlane)
        {
            if (terrain == null)
            {
                return defaultZ;
            }

            var xform = Transform.ChangeBasis(Plane.WorldXY, orientedPlane);
            var bboxTerrain = terrain.GetBoundingBox(xform);

            double zDomain = defaultZ;
            if (terrain.Faces.Count > 0 && bboxTerrain.Min.Z < zDomain)
            {
                zDomain = bboxTerrain.Min.Z;
            }

            return zDomain;
        }

        /// <summary>
        /// Computes the union bounding box of multiple meshes.
        /// </summary>
        public Box UnionB(List<Mesh> list, Plane pl)
        {
            return BuildBoundingBox(list, pl);
        }

        #endregion
    }
}
