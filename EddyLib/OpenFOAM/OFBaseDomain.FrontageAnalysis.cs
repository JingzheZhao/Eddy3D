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
        #region Frontage Analysis (Optional - for UI)

        /// <summary>
        /// Building frontage areas per wind direction.
        /// </summary>
        public double[] FrontageBuildingAreas { get; } = new double[360];

        /// <summary>
        /// Maximum frontage building area across all directions.
        /// </summary>
        public double MaxFrontageBuildingArea { get; protected set; }

        /// <summary>
        /// Frontage visualization images per wind direction.
        /// Note: This is optional and may be null in portable configurations.
        /// </summary>
        public Bitmap[] FrontagePNGs { get; } = new Bitmap[360];

        #endregion
    }
}
