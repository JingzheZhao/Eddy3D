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
        #region IDomain Implementation

        /// <inheritdoc/>
        public Point3d CenterGround { get; protected set; }

        /// <inheritdoc/>
        public Point3d LocationInMesh { get; protected set; }

        /// <inheritdoc/>
        public Mesh TerrainMesh { get; protected set; }

        /// <inheritdoc/>
        public Mesh DomainMesh { get; protected set; }

        /// <inheritdoc/>
        public Mesh BuildingGeometry { get; protected set; }

        /// <inheritdoc/>
        public double MaxHeightBuilding { get; protected set; }

        /// <inheritdoc/>
        public bool HasTerrain { get; protected set; }

        /// <inheritdoc/>
        Box IDomain.BoundingBox => BBox;

        #endregion
    }
}
