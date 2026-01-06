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
        #region Mesh Intersection

        /// <summary>
        /// Domain mesh intersected with terrain.
        /// </summary>
        public Mesh[] DomainMeshIntersection { get; protected set; }

        #endregion
    }
}
