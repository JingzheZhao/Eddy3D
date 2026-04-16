using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EddyLib.BCs
{
    public class ABL : BC
    {
        public double Ustar;
        public double zref;
        public double zGround;

        public ABL(int _windDir = 0, double _uref = 5.0, double _zref = 10, double _z0 = 1, double _zground = 0)
        {
            windDir = _windDir;
            zGround = _zground;
            URef = _uref;
            zref = _zref;
            z0 = _z0;
            Ustar = Kappa * URef / Math.Log((zref + z0) / z0);

            FinalizeSetup();
        }
    }

    public class ConstU : BC
    {
        // This is the overload for the constantU BCond where zGround is missing

        public ConstU(int _windDir, double _uref, double _z0)
        {
            windDir = _windDir;
            URef = _uref;
            z0 = _z0;

            FinalizeSetup();
        }
    }

    public enum BCType
    {
        ABL,
        ConstU
    }

    public class BCCollection
    {
        public List<BC> BCs { get; }
        public List<int> WindDirections { get; }

        public string epwFilePath { get; private set; }

        // Wind Factors

        public int[] ClstSimDirs = new int[8760];
        public int[] ClstSimDirIndices = new int[8760];
        public int[] WindDirOffset = new int[8760];
        public double WindDirOffSetAverage = 0;

        public BCCollection()
        {
            BCs = new List<BC>();
            WindDirections = new List<int>();
        }

        public BCCollection(string epwFilePath) : this()
        {
            this.epwFilePath = epwFilePath;
        }

        public BCCollection(BC BoundaryCondition) : this() // Call the base constructor to ensure initialization
        {
            AddBoundaryCondition(BoundaryCondition); // Use the method to ensure consistency
        }

        public BCCollection(List<BC> BoundaryConditions) : this()
        {
            AddBoundaryConditions(BoundaryConditions);
        }

        public BCCollection(BC BoundaryCondition, string epwFilePath) : this()
        {
            this.epwFilePath = epwFilePath;
            AddBoundaryCondition(BoundaryCondition);

            CalcWindStatistic(epwFilePath);
        }

        public BCCollection(List<int> windDirections, BCType Type) : this()
        {
            SetUpWindDirections(windDirections, Type);
        }

        protected void SetUpWindDirections(List<int> windDirections, BCType Type)
        {
            WindDirections.Clear();
            BCs.Clear();

            foreach (int dir in windDirections)
            {
                if (Type == BCType.ConstU)
                {
                    AddBoundaryCondition(new ConstU(dir, 5, 1));
                }
                else if (Type == BCType.ABL)
                {
                    AddBoundaryCondition(new ABL(dir, 5, 10, 1, 0));
                }
            }
        }

        protected void CalcWindStatistic(string epwFilePath)

        {
            if (epwFilePath.EndsWith("epw"))
            {
                this.epwFilePath = epwFilePath.Trim();
                Weather weather = new Weather(this.epwFilePath);

                // Wind Factors

                var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = EddyLib.OutdoorComfort.WindSystem.GetClosestWindDirs(weather, this.WindDirections.ToArray());
                WindDirOffset = OffSet.ToArray();
                WindDirOffSetAverage = OffSetAverage;
                ClstSimDirs = ClstSimDirs.ToArray();
                ClstSimDirIndices = SimDirIndices.ToArray();
            }
        }

        public void AddBoundaryCondition(BC boundaryCondition)
        {
            if (boundaryCondition == null)
            {
                return;
            }

            BCs.Add(boundaryCondition);
            WindDirections.Add(boundaryCondition.windDir);
        }

        public void AddBoundaryConditions(IEnumerable<BC> boundaryConditions)
        {
            if (boundaryConditions == null)
            {
                return;
            }

            foreach (var boundaryCondition in boundaryConditions)
            {
                AddBoundaryCondition(boundaryCondition);
            }
        }
    }

    public class BC
    {
        protected const double DefaultPedestrianHeight = 1.75;

        protected const double DefaultCmu = 0.09;

        protected const double DefaultKappa = 0.41;

        protected const double DefaultEddyViscosityRatio = 10;

        protected const double DefaultTu = 2;

        protected const double DefaultNu = 1.5e-05;

        public double URef;

        public double UPedestrianHeight;

        public double z0;

        public int windDir;
 
        public System.Collections.Generic.List<double> SimulatedDirections { get; set; } = new System.Collections.Generic.List<double>();
 
        public string EPWPath { get; set; }
 
        public Vector3d flowDir;

        protected double Cmu = DefaultCmu;

        public double Kappa = DefaultKappa;

        //CFD Online

        protected readonly double eddyViscosityRatio = DefaultEddyViscosityRatio;

        protected readonly double Tu = DefaultTu; // % https://www.cfd-online.com/Tools/turbulence.php Medium turbulence case

        protected readonly double nu = DefaultNu;

        //turbulence
        public double k;

        public double epsilon;

        public double omega;

        public static double ScaleABL(double URefEPW, double zref, double z0, double probingHeight)
        {
            double zGround = 0;
            var uStar = DefaultKappa * URefEPW / Math.Log((zref + z0) / z0);

            return uStar / DefaultKappa * Math.Log((probingHeight - zGround + z0) / z0);
        }

        protected double Epsilon(double k, double eddy_viscosity_ratio, double nu)
        {
            // view-source:https://www.cfd-online.com/Tools/turbulence.php

            // Bolt optimization: Replace Math.Pow(k, 2) with k * k for faster computation
            epsilon = this.Cmu * (k * k) / (this.nu * this.eddyViscosityRatio);

            return epsilon;
        }

        protected double Omega(double epsilon, double k)
        {
            double omega = epsilon / (this.Cmu * k);
            return omega;
        }

        protected double K(double Tu, double URef)
        {
            // Bolt optimization: Replace Math.Pow(..., 2) with explicit multiplication for faster computation
            double tu100 = Tu / 100;
            double k = 1.5 * (tu100 * tu100) * (URef * URef);
            return k;
        }

        protected void CalcUPedestrianHeight()
        {
            if (this is ABL)
            {
                var BCNew = (ABL)this;

                { this.UPedestrianHeight = BCNew.Ustar / Kappa * Math.Log((DefaultPedestrianHeight + z0) / z0); }
            }
            else
            {
                { this.UPedestrianHeight = this.URef; }
            }
        }

        protected void FinalizeSetup()
        {
            CalcUPedestrianHeight();

            k = K(Tu, URef);
            epsilon = Epsilon(k, eddyViscosityRatio, nu);
            omega = Omega(epsilon, k);

            flowDir = Utilities.Dir2Vec(windDir);
        }
    }
}
