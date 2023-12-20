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

        public ABL(int windDir = 0, double _uref = 5.0, double _zref = 10, double _z0 = 1, double _zground = 0, string epwFilePath = "")
        {
            this.epwFilePath = epwFilePath;

            zGround = _zground;
            URef = _uref;
            zref = _zref;
            z0 = _z0;
            Ustar = Kappa * URef / Math.Log((zref + z0) / z0);

            CalcUPedestrianHeight();

            k = K(Tu, URef);
            epsilon = Epsilon(k, eddyViscosityRatio, nu);
            omega = Omega(epsilon, k);

            flowDir = Utilities.Dir2Vec(windDir);
        }
    }

    public class ConstU : BC
    {
        // This is the overload for the constantU BCond where zGround is missing

        public ConstU(int windDir, double _uref, double _z0, string epwFilePath)
        {
            this.epwFilePath = epwFilePath;

            URef = _uref;
            z0 = _z0;

            CalcUPedestrianHeight();

            k = K(Tu, URef);
            epsilon = Epsilon(k, eddyViscosityRatio, nu);
            omega = Omega(epsilon, k);

            flowDir = Utilities.Dir2Vec(windDir);
        }
    }

    public enum BCType
    {
        ABL,
        ConstU
    }

    public class BCCollection
    {
        public List<BC> BCs = new List<BC>();

        public string epwFilePath;
        public List<int> WindDirections;

        // Wind Factors

        public int[] ClstSimDirs = new int[8760];

        public int[] ClstSimDirIndices = new int[8760];

        public int[] WindDirOffset = new int[8760];

        public double WindDirOffSetAverage = 0;

        public BCCollection()
        {
        }

        public BCCollection(BC BoundaryCondition)
        {
            this.BCs.Add(BoundaryCondition);
            this.WindDirections = new List<int>() { BoundaryCondition.windDir };
        }

        public BCCollection(BC BoundaryCondition, string epwFilePath)
        {
            this.BCs.Add(BoundaryCondition);
            this.WindDirections = new List<int>() { BoundaryCondition.windDir };

            CalcWindStatistic(epwFilePath);
        }

        public BCCollection(List<BC> BoundaryConditions)
        {
            this.BCs.AddRange(BoundaryConditions);

            foreach (var bcond in BoundaryConditions)
            {
                this.WindDirections.Add(bcond.windDir);
            }
        }

        public BCCollection(List<BC> BoundaryConditions, string epwFilePath)
        {
            this.BCs.AddRange(BoundaryConditions);

            foreach (var bcond in BoundaryConditions)
            {
                this.WindDirections.Add(bcond.windDir);
            }

            CalcWindStatistic(epwFilePath);
        }

        public BCCollection(List<int> windDirections, BCType Type)
        {
            this.WindDirections = windDirections;

            SetUpWindDirections(windDirections, Type);
        }

        public BCCollection(List<int> windDirections, BCType Type, string epwFilePath)
        {
            this.WindDirections = windDirections;

            SetUpWindDirections(windDirections, Type, epwFilePath);
            CalcWindStatistic(epwFilePath);
        }

        protected void SetUpWindDirections(List<int> windDirections, BCType Type, string EPW = "")
        {
            this.WindDirections = windDirections;

            foreach (int dir in windDirections)
            {
                if (Type == BCType.ConstU)
                {
                    this.BCs.Add(new ConstU(dir, 5, 1, EPW));
                }
                else if (Type == BCType.ABL)
                {
                    this.BCs.Add(new ABL(dir, 5, 10, 1, 0, EPW));
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
    }

    public class BC
    {
        public double URef;

        protected readonly double pedestrianHeight = 1.75;

        public double UPedestrianHeight;

        public double z0;

        public int windDir;

        public Vector3d flowDir;

        protected double Cmu = 0.09;

        public double Kappa = 0.41;

        //CFD Online

        protected readonly double eddyViscosityRatio = 10;

        protected readonly double Tu = 2; // % https://www.cfd-online.com/Tools/turbulence.php Medium turbulence case

        protected readonly double nu = 1.5e-05;

        //turbulence
        public double k;

        public double epsilon;

        public double omega;

        public string epwFilePath;

        public static double ScaleABL(double URefEPW, double zref, double z0, double probingHeight)
        {
            double zGround = 0;
            var Kappa = 0.41;

            var U_star = Kappa * URefEPW / (Math.Log((zref + z0) / z0));

            return U_star / Kappa * Math.Log((probingHeight - zGround + z0) / z0);
        }

        protected double Epsilon(double k, double eddy_viscosity_ratio, double nu)
        {
            // view-source:https://www.cfd-online.com/Tools/turbulence.php

            epsilon = this.Cmu * Math.Pow(k, 2) / (this.nu * this.eddyViscosityRatio);

            return epsilon;
        }

        protected double Omega(double epsilon, double k)
        {
            double omega = epsilon / (this.Cmu * k);
            return omega;
        }

        protected double K(double Tu, double URef)
        {
            double k = 1.5 * Math.Pow(Tu / 100, 2) * Math.Pow(URef, 2);
            return k;
        }

        protected void CalcUPedestrianHeight()
        {
            if (this is ABL)
            {
                var BCNew = (ABL)this;

                { this.UPedestrianHeight = BCNew.Ustar / Kappa * Math.Log((pedestrianHeight + z0) / z0); }
            }
            else
            {
                { this.UPedestrianHeight = this.URef; }
            }
        }
    }
}