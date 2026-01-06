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
        #region Protected Methods

        /// <summary>
        /// Initializes boundary conditions and pressure coefficients.
        /// </summary>
        protected void InitializeBoundaryConditions(BCCollection bcCollection, double buildingHeight)
        {
            BCond = bcCollection;
            var pressureCoeff = new PressureCoeff(buildingHeight, bcCollection);
            pressureCoeff.SetUatBuildingHeight(buildingHeight, bcCollection);
        }

        /// <summary>
        /// Builds a bounding box for a single mesh.
        /// </summary>
        protected static Box BuildBoundingBox(Mesh mesh, Plane plane)
        {
            if (mesh == null)
            {
                return Box.Unset;
            }

            return BuildBoundingBox(new[] { mesh }, plane);
        }

        /// <summary>
        /// Builds a bounding box for multiple meshes.
        /// </summary>
        protected static Box BuildBoundingBox(IEnumerable<Mesh> meshes, Plane plane)
        {
            if (meshes == null)
            {
                return Box.Unset;
            }

            var xform = Transform.ChangeBasis(Plane.WorldXY, plane);
            var bbox = BoundingBox.Empty;

            foreach (var mesh in meshes)
            {
                if (mesh == null)
                {
                    continue;
                }

                bbox.Union(mesh.GetBoundingBox(xform));
            }

            return bbox.IsValid ? new Box(plane, bbox) : Box.Unset;
        }

        #endregion
    }
}
