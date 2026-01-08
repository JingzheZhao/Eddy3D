using EddyLib.FunctionObjects;
using Rhino.Geometry;
using System.Linq;

namespace EddyLib.Indoor
{
    /// <summary>
    /// Represents a porous zone for momentum sink calculations (Darcy-Forchheimer).
    /// </summary>
    public class MomentumSink : FunctionObject
    {
        #region Darcy-Forchheimer Coefficients
        
        /// <summary>Viscous resistance coefficients (d) [1/m²].</summary>
        public double[] D { get; set; } = new double[3];

        /// <summary>Inertial resistance coefficients (f) [1/m].</summary>
        public double[] F { get; set; } = new double[3];

        #endregion

        #region Dimensions
        
        /// <summary>Bounding box dimension in X.</summary>
        public double DimX { get; set; }

        /// <summary>Bounding box dimension in Y.</summary>
        public double DimY { get; set; }

        /// <summary>Bounding box dimension in Z.</summary>
        public double DimZ { get; set; }

        /// <summary>Dimensions as array [X, Y, Z].</summary>
        public double[] DimXYZ { get; set; } = new double[3];

        #endregion

        /// <summary>JSON serialization of all properties.</summary>
        public string AllProperties { get; set; }

        /// <summary>
        /// Creates a momentum sink from pressure drop coefficients.
        /// </summary>
        /// <param name="geometry">Geometry defining the porous zone.</param>
        /// <param name="B">Inertial pressure drop coefficients (u²).</param>
        /// <param name="A">Viscous pressure drop coefficients (u).</param>
        /// <param name="name">Zone name.</param>
        public MomentumSink(GeometryBase geometry, double[] B, double[] A, string name)
        {
            Name = name;
            Geometry = GeometryHelpers.ToMesh(geometry);
            CalculateDimensions();
            CalculateCoefficients(A, B);
            AllProperties = SerializationHelpers.ToJson(this);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MomentumSink() { }

        /// <summary>
        /// Calculates bounding box dimensions.
        /// </summary>
        protected void CalculateDimensions()
        {
            if (Geometry == null) return;

            var dims = GeometryHelpers.GetDimensions(Geometry);
            DimX = dims.X;
            DimY = dims.Y;
            DimZ = dims.Z;
            DimXYZ = new double[] { DimX, DimY, DimZ };
        }

        /// <summary>
        /// Calculates Darcy-Forchheimer coefficients from pressure drop data.
        /// </summary>
        protected void CalculateCoefficients(double[] A, double[] B)
        {
            for (int i = 0; i < 3; i++)
            {
                // d = A / L / mu (viscous term)
                D[i] = A[i] / DimXYZ[i] / AirProperties.Mu;
                // f = B / L * 2 / rho (inertial term)
                F[i] = B[i] / DimXYZ[i] * 2 / AirProperties.Rho;
            }
        }

        #region Nested Tree Class

        /// <summary>
        /// Tree as a porous zone using LAI (Leaf Area Index).
        /// </summary>
        public class Tree : MomentumSink
        {
            /// <summary>Drag coefficient for vegetation.</summary>
            private const double Cd = 0.2;

            /// <summary>Leaf Area Index (total leaf area / ground area).</summary>
            public double LAI { get; set; }

            /// <summary>Leaf Area Density (leaf area / volume).</summary>
            public double LAD { get; set; }

            /// <summary>
            /// Creates a tree from pressure drop coefficients.
            /// </summary>
            public Tree(GeometryBase geometry, double[] B, double[] A, string name) 
                : base(geometry, B, A, name)
            {
                LAD = B.Average() / (AirProperties.Rho * Cd);
                LAI = LAD * DimZ;
            }

            /// <summary>
            /// Creates a tree from LAI value.
            /// </summary>
            public Tree(GeometryBase geometry, double lai, string name)
            {
                Name = name;
                Geometry = GeometryHelpers.ToMesh(geometry);
                CalculateDimensions();

                LAI = lai;
                LAD = lai / DimZ;

                // Calculate B from LAI: B = rho * LAD * Cd
                double b = AirProperties.Rho * LAD * Cd;
                double[] B = { b, b, b };

                // f = B * 2 / rho, d = 0 (no viscous term for vegetation)
                D = new double[] { 0, 0, 0 };
                F = B.Select(x => x * 2 / AirProperties.Rho).ToArray();

                AllProperties = SerializationHelpers.ToJson(this);
            }
        }

        #endregion
    }
}