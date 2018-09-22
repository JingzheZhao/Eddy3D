using Rhino.Geometry;
using System;
using System.Collections.Generic;

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
        public double Ustar;
        public double z0 = 1;
        public double zref = 10;
        public double zGround = 0;
        public List<double> windDir = new List<double>();
        public List<Vector3d> flowDir = new List<Vector3d>();
        public BoundaryType btype;


        //turbulence
        public double k;
        public double epsilon;
        public double omega;


        public string weather;

        public double pinf;
        public double pref;
        public List<Vector3d> Uinf = new List<Vector3d>();
        private readonly double Tu;
        private readonly double eddy_viscosity_ratio;
        private readonly double nu;



        //public BoundaryConditions()
        //{
        //    windDir.Add(0);
        //    flowDir.Add(Vector3d.YAxis);
        //}

        public BoundaryConditions(BoundaryType type, List<double> dirs, double _uref, double _zref, double _z0, double _zground, string weather)
        {
            this.weather = weather;
            double pedestrianHeight = 1.5;
            this.btype = type;
            this.URef = _uref;
            this.zref = _zref;
            this.z0 = _z0;
            this.zGround = _zground;
            this.UPedestrianHeight = (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((pedestrianHeight + z0) / z0));
            //this.UBuildingHeight = (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((maxBuildingHeight + z0) / z0));
            this.Ustar = (0.41 * URef) / Math.Log(((zref + z0) / z0));
            //this.k = Math.Pow(this.Ustar, 2) / Math.Sqrt(0.09);
            //this.epsilon = Math.Pow(this.Ustar, 3) / (0.41 * (this.zref - this.zGround + this.z0));
            //this.omega = this.epsilon / (0.09 * this.k);

            //CFD Online

            eddy_viscosity_ratio = 10;
            Tu = 5;
            nu = 1.5e-05;

            this.k = K(this.Tu, this.URef);
            this.epsilon = Epsilon(this.k, this.eddy_viscosity_ratio, this.nu);
            this.omega = Omega(this.epsilon, this.k);

            foreach (double d in dirs)
            {
                this.windDir.Add(d);
                this.flowDir.Add(new Vector3d(Math.Sin(d * Math.PI / 180), Math.Cos(d * Math.PI / 180), 0));
            }
        }

        // This is the overload for the constantU BCond where zGround is missing

        public BoundaryConditions(BoundaryType type, List<double> dirs, double _uref, double _z0, string weather)
        {
            this.weather = weather;
            double pedestrianHeight = 1.5;
            this.btype = type;
            this.URef = _uref;
            //this.zref = _zref;
            this.z0 = _z0;
            //this.zGround = _zground;
            this.UPedestrianHeight = (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((pedestrianHeight + z0) / z0));
            //this.UBuildingHeight = (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((maxBuildingHeight + z0) / z0));
            this.Ustar = 0.41 * (URef / Math.Log((zref + z0) / z0));
            //this.k = Math.Pow(this.Ustar, 2) / Math.Sqrt(0.09);
            //this.epsilon = Math.Pow(this.Ustar, 3) / (0.41 * (this.zref - this.zGround + this.z0));
            //this.omega = this.epsilon / (0.09 * this.k);

            //CFD Online

            eddy_viscosity_ratio = 10;
            Tu = 5; //in percent
            nu = 1.5e-05;

            this.k = K(this.Tu, this.URef);
            this.epsilon = Epsilon(this.k, this.eddy_viscosity_ratio, this.nu);
            this.omega = Omega(this.epsilon, this.k);



            foreach (double d in dirs)
            {
                this.windDir.Add(d);
                this.flowDir.Add(new Vector3d(Math.Sin(d * Math.PI / 180), Math.Cos(d * Math.PI / 180), 0));
            }
        }

        public void SetUatBuildingHeightABL(double maxBuildingHeight)
        {
            this.UatBuildingHeight = (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((maxBuildingHeight + z0) / z0));
        }


        public void SetUatBuildingHeightUconst()
        {
            this.UatBuildingHeight = this.URef;
        }


        public void CalculateCPPressures(double buildingHeight)
        {
            this.pinf = 1.2 * 0.5 * Math.Pow(((((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))), 2);
            this.pref = 1.2 * 0.5 * Math.Pow(((((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))), 2);

            foreach (Vector3d d in flowDir)
            {
                this.Uinf.Add((d * (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))));
            }

        }

        public double Epsilon(double k, double eddy_viscosity_ratio, double nu)
        {
            double epsilon = 0.09 * Math.Pow(k, 2) / (nu * eddy_viscosity_ratio);
            return epsilon;
        }

        public double Omega(double epsilon, double k)
        {
            double omega = epsilon / (0.09 * k);
            return omega;
        }
        public double K(double Tu, double URef)
        {
            double k = 1.5 * Math.Pow(Tu / 100, 2) * Math.Pow(URef, 2);
            return k;
        }

    }


}
