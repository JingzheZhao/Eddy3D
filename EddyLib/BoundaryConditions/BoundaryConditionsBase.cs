using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace EddyLib
{
    public enum BoundaryType
    {
        constant,
        abl,
        //ablList,
        //constantList
    }

    public class BoundaryConditions
    {
        public double URef = 5;
        public double UPedestrianHeight;
        public double UatBuildingHeight;
        private double Ustar;
        public double z0 = 1;
        public double zref = 10;
        public double zGround = 0;
        public readonly List<int> windDirs = new List<int>();
        public List<Vector3d> flowDir = new List<Vector3d>();
        public BoundaryType btype;
        public double Cmu = 0.09;
        public double kappa = 0.41;

        //CFD Online

        private double eddyViscosityRatio = 10;
        private double Tu = 2; // % https://www.cfd-online.com/Tools/turbulence.php Medium turbulence case
        private double nu = 1.5e-05;

        //turbulence
        public double k;

        public double epsilon;
        public double omega;

        public string epwFilePath;

        public BoundaryConditions(BoundaryType type, List<int> dirs, double _uref, double _zref, double _z0, double _zground, string epwFilePath)
        {
            this.epwFilePath = epwFilePath;
            double pedestrianHeight = 1.5;
            btype = type;
            URef = _uref;
            zref = _zref;
            z0 = _z0;
            zGround = _zground;

            this.Ustar = this.kappa * URef / Math.Log((zref + z0) / z0);

            UPedestrianHeight = ((this.Ustar / this.kappa) * Math.Log((pedestrianHeight + z0) / z0));
            //this.UBuildingHeight = (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((maxBuildingHeight + z0) / z0));

            //this.k = Math.Pow(this.Ustar, 2) / Math.Sqrt(0.09);
            //this.epsilon = Math.Pow(this.Ustar, 3) / (0.41 * (this.zref - this.zGround + this.z0));
            //this.omega = this.epsilon / (0.09 * this.k);

            k = K(Tu, URef);
            epsilon = Epsilon(btype, k, eddyViscosityRatio, nu);
            omega = Omega(epsilon, k);

            foreach (int d in dirs)
            {
                windDirs.Add(d);
                flowDir.Add(new Vector3d(-1 * Math.Sin(d * Math.PI / 180), -1 * Math.Cos(d * Math.PI / 180), 0));
            }
        }

        // This is the overload for the constantU BCond where zGround is missing

        public BoundaryConditions(BoundaryType type, List<int> dirs, double _uref, double _z0, string epwFilePath)
        {
            this.epwFilePath = epwFilePath;
            double pedestrianHeight = 1.5;
            btype = type;
            URef = _uref;
            //this.zref = _zref;
            z0 = _z0;
            //this.zGround = _zground;

            Ustar = this.kappa * URef / Math.Log((zref + z0) / z0);

            UPedestrianHeight = ((this.Ustar / this.kappa) * Math.Log((pedestrianHeight + z0) / z0));
            //this.UBuildingHeight = (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((maxBuildingHeight + z0) / z0));

            //this.k = Math.Pow(this.Ustar, 2) / Math.Sqrt(0.09);
            //this.epsilon = Math.Pow(this.Ustar, 3) / (0.41 * (this.zref - this.zGround + this.z0));
            //this.omega = this.epsilon / (0.09 * this.k);

            k = K(Tu, URef);
            epsilon = Epsilon(btype, k, eddyViscosityRatio, nu);
            omega = Omega(epsilon, k);

            foreach (int d in dirs)
            {
                windDirs.Add(d);
                flowDir.Add(Utilities.Dir2Vec(d));
            }
        }

        public void SetUatBuildingHeightABL(double maxBuildingHeight)
        {
            UatBuildingHeight = (((this.kappa * URef) / Math.Log((zref + z0) / z0) / this.kappa) * Math.Log((maxBuildingHeight + z0) / z0));
        }

        public void SetUatBuildingHeightUconst()
        {
            UatBuildingHeight = URef;
        }

        public double Epsilon(BoundaryType btype, double k, double eddy_viscosity_ratio, double nu)
        {
            // view-source:https://www.cfd-online.com/Tools/turbulence.php

            //if (btype == BoundaryType.abl)
            //{
            //epsilon = this.Cmu * Math.Pow(k, 2) / (nu * eddy_viscosity_ratio);
            epsilon = this.Cmu * Math.Pow(k, 2) / (this.nu * this.eddyViscosityRatio);
            //}
            //else
            //{
            //    // from openfoam testcase
            //    int L = 10;
            //    epsilon = Math.Pow(this.Cmu, 0.75) * Math.Pow(k, 1.5) / L;
            //}

            return epsilon;
        }

        public double Omega(double epsilon, double k)
        {
            double omega = epsilon / (this.Cmu * k);
            return omega;
        }

        public double K(double Tu, double URef)
        {
            double k = 1.5 * Math.Pow(Tu / 100, 2) * Math.Pow(URef, 2);
            return k;
        }
    }
}