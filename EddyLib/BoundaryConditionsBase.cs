using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
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



        public BoundaryConditions()
        {
            windDir.Add(0);
            flowDir.Add(Vector3d.YAxis);
        }

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
            this.Ustar = (0.41 * URef) / Math.Log(((zref + z0) / z0));
            this.k = Math.Pow(this.Ustar, 2) / Math.Sqrt(0.09);
            this.epsilon = Math.Pow(this.Ustar, 3) / (0.41 * (this.zref - this.zGround + this.z0));
            this.omega = this.epsilon / (0.09 * this.k);

            //CFD Online
            //form.k.value = 1.5 * Math.pow(form.Tu.value / 100, 2) * Math.pow(form.u_freestream.value, 2)

            // form.epsilon.value = 0.09 * Math.pow(form.k.value, 2) / (form.nu.value * form.eddy_viscosity_ratio.value)

            // form.omega.value = form.epsilon.value / (0.09 * form.k.value)

            foreach (double d in dirs)
            {
                this.windDir.Add(d);
                this.flowDir.Add(new Vector3d(Math.Sin(d * Math.PI / 180), Math.Cos(d * Math.PI / 180), 0));
            }
        }

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
            this.Ustar = 0.41 * (URef / Math.Log((zref + z0) / z0));
            this.k = Math.Pow(this.Ustar, 2) / Math.Sqrt(0.09);
            this.epsilon = Math.Pow(this.Ustar, 3) / 0.41 * (this.zref - this.zGround + this.z0);
            this.omega = this.epsilon / 0.09 * this.k;

            foreach (double d in dirs)
            {
                this.windDir.Add(d);
                this.flowDir.Add(new Vector3d(Math.Sin(d * Math.PI / 180), Math.Cos(d * Math.PI / 180), 0));
            }
        }


        public void calculateCPPressures(double buildingHeight)
        {
            this.pinf = 1.2 * 0.5 * Math.Pow(((((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))), 2);
            this.pref = 1.2 * 0.5 * Math.Pow(((((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))), 2);

            foreach (Vector3d d in flowDir)
            {
                this.Uinf.Add((d * (((0.41 * URef) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))));
            }

        }

    }


}
