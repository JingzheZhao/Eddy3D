using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using MathNet.Numerics;
using Rhino.Geometry;

namespace EddyLib.BCs
{
    public class ABL : BoundaryCondition
    {
        protected double Ustar;

        public double zref;

        public double zGround;

        public ABL(List<int> dirs = null, double _uref = 5, double _zref = 10.0, double _z0 = 1, double _zground = 0, string epwFilePath = "")
        {
            this.epwFilePath = epwFilePath;

            URef = _uref;
            zref = _zref;
            z0 = _z0;
            zGround = _zground;

            if (dirs == null)
            {
                dirs = new List<int> { 0 };
            }

            CalcUstar();
            CalcUPedestrianHeight();

            k = K(Tu, URef);
            epsilon = Epsilon(k, eddyViscosityRatio, nu);
            omega = Omega(epsilon, k);

            foreach (int d in dirs)
            {
                windDirs.Add(d);
                flowDir.Add(Utilities.Dir2Vec(d));
            }

            if (epwFilePath.EndsWith("epw"))
            {
                Weather weather = new Weather(epwFilePath);

                // Wind Factors

                var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = GetClosestWindDirs(weather, this);
                this.WindDirOffset = OffSet.ToArray();
                this.WindDirOffSetAverage = OffSetAverage;
                this.ClstSimDirs = ClstSimDirs.ToArray();
                this.ClstSimDirIndices = SimDirIndices.ToArray();
            }
        }

        protected void CalcUstar()
        {
            this.Ustar = Kappa * URef / Math.Log((zref + z0) / z0);
        }

        protected void CalcUPedestrianHeight()
        {
            { this.UPedestrianHeight = ((this.Ustar / Kappa) * Math.Log((pedestrianHeight + z0) / z0)); }
        }
    }

    public class ConstU : BoundaryCondition
    {
        // This is the overload for the constantU BCond where zGround is missing

        public ConstU(List<int> dirs, double _uref, double _z0, string epwFilePath)
        {
            this.epwFilePath = epwFilePath;

            URef = _uref;
            z0 = _z0;

            if (dirs == null)
            {
                dirs = new List<int> { 0 };
            }

            CalcUPedestrianHeight();

            k = K(Tu, URef);
            epsilon = Epsilon(k, eddyViscosityRatio, nu);
            omega = Omega(epsilon, k);

            foreach (int d in dirs)
            {
                windDirs.Add(d);
                flowDir.Add(Utilities.Dir2Vec(d));
            }

            if (epwFilePath.EndsWith("epw"))
            {
                Weather weather = new Weather(epwFilePath);

                // Wind Factors

                var (SimDirIndices, ClstSimDirs, OffSet, OffSetAverage) = GetClosestWindDirs(weather, this);
                this.WindDirOffset = OffSet.ToArray();
                this.WindDirOffSetAverage = OffSetAverage;
                this.ClstSimDirs = ClstSimDirs.ToArray();
                this.ClstSimDirIndices = SimDirIndices.ToArray();
            }
        }

        protected void CalcUPedestrianHeight()
        {
            { this.UPedestrianHeight = this.URef; }
        }
    }

    public class BoundaryCondition
    {
        public double URef;

        protected readonly double pedestrianHeight = 1.75;

        public double UPedestrianHeight;

        public double z0;

        public List<int> windDirs = new List<int>();

        public List<Vector3d> flowDir = new List<Vector3d>();

        protected double Cmu = 0.09;

        public double Kappa = 0.41;

        //public static double Kappa { get; set; }

        //CFD Online

        protected readonly double eddyViscosityRatio = 10;

        protected readonly double Tu = 2; // % https://www.cfd-online.com/Tools/turbulence.php Medium turbulence case

        protected readonly double nu = 1.5e-05;

        //turbulence
        public double k;

        public double epsilon;

        public double omega;

        public string epwFilePath;

        // Wind Factors

        public int[] ClstSimDirs = new int[8760];

        public int[] ClstSimDirIndices = new int[8760];

        public int[] WindDirOffset = new int[8760];

        public double WindDirOffSetAverage = 0;

        public static double ScaleABL(double URefEPW, double zref, double z0, double probingHeight)
        {
            double zGround = 0;
            var Kappa = 0.41;

            var U_star = Kappa * URefEPW / (Math.Log((zref + z0) / z0));

            return U_star / Kappa * Math.Log((probingHeight - zGround + z0) / z0);
        }

        public static Tuple<List<int>, List<int>, List<int>, double> GetClosestWindDirs(Weather weather, BoundaryCondition bcond)
        {
            var offSet = new List<int>();
            var clstSimIndices = new List<int>();
            var clstSimDirs = new List<int>();

            var windDirsEPW = weather.WindDirection;
            var windDirSim = bcond.windDirs;

            for (int h = 0; h < 8760; h++)
            {
                int weatherDir = (int)weather.WindDirection[h];
                int closestIndex = 0;
                var distance = 0;

                // Treat 360 as 0 and add that right away if it exists
                if (weatherDir == 360 && bcond.windDirs.Contains(0))
                {
                    closestIndex = 0;

                    distance = 0;
                    offSet.Add(distance);
                    clstSimIndices.Add(closestIndex);
                    clstSimDirs.Add(bcond.windDirs[closestIndex]);

                    continue;
                }

                // Check what is closest for all other cases

                if (bcond.windDirs.Contains(weatherDir))
                {
                    closestIndex = windDirSim.IndexOf(weather.WindDirection[h]);
                }
                else
                {
                    var nextIndexDown = ReturnNextLowerIndex(windDirSim, (int)windDirsEPW[h]);
                    var nextIndexUp = ReturnNextUpperIndex(windDirSim, (int)windDirsEPW[h]);

                    var nextDirDown = windDirSim[nextIndexDown];
                    var nextDirUp = windDirSim[nextIndexUp];

                    double distanceToLower = Math.Abs(windDirsEPW[h] - nextDirDown);
                    double distanceToUpper = Math.Abs(windDirsEPW[h] - nextDirUp);

                    closestIndex = distanceToLower < distanceToUpper ? nextIndexDown : nextIndexUp;
                }

                distance = Math.Abs(bcond.windDirs[closestIndex] - weatherDir);
                offSet.Add(distance);
                clstSimIndices.Add(closestIndex);
                clstSimDirs.Add(bcond.windDirs[closestIndex]);
            }

            return new Tuple<List<int>, List<int>, List<int>, double>(clstSimIndices, clstSimDirs, offSet, offSet.Average());
        }

        public static int DistanceBetweenWindDirs(int dir1, int dir2)
        {
            var vec2 = Utilities.Dir2Vec(dir1);
            var vec1 = Utilities.Dir2Vec(dir2);

            return (int)Math.Abs(Utilities.AngleBetweenVectors(vec1, vec2));
        }

        public static int ReturnNextLowerIndex(List<int> list, int compareTo)
        {
            int lowerIndex;

            if (compareTo <= list.Min())
            {
                lowerIndex = list.IndexOf(list.Max());
            }
            else
            {
                // Take everything smaller than compare
                var smaller = list.Where(x => x < compareTo);

                // Take the max from that selection and then take the index
                lowerIndex = list.IndexOf(smaller.Max(y => y));
            }

            return lowerIndex;
        }

        public static int ReturnNextUpperIndex(List<int> list, int compareTo)
        {
            // If values to compare if larger than everything in the list, return the first in the
            // list which is usually 0

            int upperIndex;

            if (compareTo >= list.Max())
            {
                upperIndex = list.IndexOf(list.Min());
            }
            else
            {
                // Take everything larger than compare
                var larger = list.Where(x => x > compareTo);

                // Take the min from that selection and then take the index
                upperIndex = list.IndexOf(larger.Min(y => y));
            }
            return upperIndex;
        }

        protected double Epsilon(double k, double eddy_viscosity_ratio, double nu)
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
    }
}