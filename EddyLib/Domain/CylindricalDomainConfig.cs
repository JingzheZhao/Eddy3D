namespace EddyLib.Domain
{
    /// <summary>
    /// Configuration for cylindrical domain generation.
    /// </summary>
    public class CylindricalDomainConfig
    {
        /// <summary>
        /// Block size in the core region (meters).
        /// </summary>
        public double CoreBlockSize { get; }

        /// <summary>
        /// Size of the inner rectangular region. 0 = auto-calculate.
        /// </summary>
        public double InnerRectSize { get; }

        /// <summary>
        /// Outer circle radius. 0 = auto-calculate based on blocking ratio.
        /// </summary>
        public double OuterCircleRadius { get; }

        /// <summary>
        /// Domain height. 0 = auto-calculate based on building height.
        /// </summary>
        public double Height { get; }

        /// <summary>
        /// Radial multiplier for mesh divisions.
        /// </summary>
        public double RadialMultiplier { get; }

        /// <summary>
        /// Number of divisions in X direction.
        /// </summary>
        public int DivisionsX { get; }

        /// <summary>
        /// Creates a new cylindrical domain configuration.
        /// </summary>
        public CylindricalDomainConfig(
            double coreBlockSize,
            double innerRectSize = 0,
            double outerCircleRadius = 0,
            double height = 0,
            double radialMultiplier = DomainConstants.DefaultRadialMultiplier,
            int divisionsX = 1)
        {
            CoreBlockSize = coreBlockSize;
            InnerRectSize = innerRectSize;
            OuterCircleRadius = outerCircleRadius;
            Height = height;
            RadialMultiplier = radialMultiplier;
            DivisionsX = divisionsX;
        }
    }
}
