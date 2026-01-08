using EddyLib.BCs;
using EddyLib.FunctionObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EddyLib
{
    /// <summary>
    /// Calculates pressure coefficients for wind simulations.
    /// </summary>
    public class PressureCoeff : FunctionObject
    {
        /// <summary>Velocity magnitude at building height for each wind direction.</summary>
        public List<double> UatBuildingHeight { get; } = new List<double>();

        /// <summary>Dynamic pressure at infinity for each wind direction.</summary>
        public List<double> Pinf { get; } = new List<double>();
        
        /// <summary>Dynamic pressure at infinity (lowercase alias for backward compatibility).</summary>
        public List<double> pinf => Pinf;

        /// <summary>Reference dynamic pressure for each wind direction.</summary>
        public List<double> Pref { get; } = new List<double>();
        
        /// <summary>Reference dynamic pressure (lowercase alias for backward compatibility).</summary>
        public List<double> pref => Pref;

        /// <summary>Velocity vector at infinity for each wind direction.</summary>
        public List<Vector3d> Uinf { get; } = new List<Vector3d>();

        private int WindDirectionCount { get; }

        /// <summary>
        /// Creates pressure coefficient calculator for given building height and boundary conditions.
        /// </summary>
        /// <param name="buildingHeight">Maximum building height (m).</param>
        /// <param name="boundaryConditions">Collection of boundary conditions for each wind direction.</param>
        public PressureCoeff(double buildingHeight, BCCollection boundaryConditions)
        {
            WindDirectionCount = boundaryConditions.BCs.Count;
            CalculatePressures(Math.Max(0, buildingHeight), boundaryConditions);
        }

        /// <summary>
        /// Sets velocity at building height for each wind direction.
        /// </summary>
        public void SetUatBuildingHeight(double maxBuildingHeight, BCCollection boundaryConditions)
        {
            foreach (var bc in boundaryConditions.BCs)
            {
                if (bc is ABL abl)
                {
                    UatBuildingHeight.Add(BC.ScaleABL(abl.URef, abl.zref, abl.z0, maxBuildingHeight));
                }
                else if (bc is ConstU constU)
                {
                    UatBuildingHeight.Add(constU.URef);
                }
            }
        }

        /// <summary>
        /// Calculates velocity and pressure values for all wind directions.
        /// </summary>
        private void CalculatePressures(double buildingHeight, BCCollection boundaryConditions)
        {
            foreach (var bc in boundaryConditions.BCs)
            {
                if (bc is ABL abl)
                {
                    double velocity = CalculateABLVelocity(abl, buildingHeight);
                    double dynamicPressure = CalculateDynamicPressure(velocity);

                    Uinf.Add(abl.flowDir * velocity);
                    Pinf.Add(dynamicPressure);
                    Pref.Add(dynamicPressure);
                }
                else if (bc is ConstU constU)
                {
                    double dynamicPressure = CalculateDynamicPressure(constU.URef);

                    Uinf.Add(constU.flowDir * constU.URef);
                    Pinf.Add(dynamicPressure);
                    Pref.Add(dynamicPressure);
                }
            }
        }

        /// <summary>
        /// Calculates velocity at a given height using the atmospheric boundary layer log-law profile.
        /// U(z) = (u* / kappa) * ln((z + z0) / z0)
        /// </summary>
        private static double CalculateABLVelocity(ABL abl, double height)
        {
            // u* (friction velocity) from reference: u* = kappa * Uref / ln((zref + z0) / z0)
            double frictionVelocity = abl.Kappa * abl.URef / Math.Log((abl.zref + abl.z0) / abl.z0);
            
            // U(z) = (u* / kappa) * ln((z + z0) / z0)
            return (frictionVelocity / abl.Kappa) * Math.Log((height + abl.z0) / abl.z0);
        }

        /// <summary>
        /// Calculates dynamic pressure: p = 0.5 * rho * U^2
        /// </summary>
        private static double CalculateDynamicPressure(double velocity)
        {
            return 0.5 * AirProperties.Rho * velocity * velocity;
        }
    }
}