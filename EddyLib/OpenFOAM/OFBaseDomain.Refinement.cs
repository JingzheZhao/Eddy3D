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
        #region Refinement

        /// <summary>
        /// Refinement cylinder around buildings.
        /// </summary>
        public Cylinder RefinementCylinder { get; protected set; }

        #endregion
    }
}
