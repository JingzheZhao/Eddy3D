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
        #region Domain Properties

        /// <summary>
        /// Bounding box of the domain.
        /// </summary>
        public Box BBox { get; protected set; }

        /// <summary>
        /// Boundary conditions collection.
        /// </summary>
        public BCCollection BCond { get; protected set; }

        /// <summary>
        /// Tree vegetation objects in the domain.
        /// </summary>
        public List<Tree> Trees { get; protected set; }

        /// <summary>
        /// Number of cells in the mesh.
        /// </summary>
        public int NumberOFCellsInMesh { get; protected set; }

        /// <summary>
        /// Recorded runtimes for profiling.
        /// </summary>
        public List<double> Runtimes { get; } = new List<double>();

        #endregion
    }
}
