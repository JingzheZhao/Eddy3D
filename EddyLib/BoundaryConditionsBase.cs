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
        ablList,
        constantList
    }

    public class BoundaryConditions
    {
        public double U;
        public double z0;
        public double zref;
        public double zGround;
        //public Vector3d flowDir;
        public List<double> windDir = new List<double>();
        public List<Vector3d> flowDir = new List<Vector3d>();
        public BoundaryType btype = BoundaryType.abl;
        public double pinf;
        public double pref;
        public List<Vector3d> Uinf = new List<Vector3d>();


        public void calculateCPPressures(double buildingHeight)
        {


            this.pinf = 1.2 * 0.5 * Math.Pow(((((0.41 * U) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))), 2);
            this.pref = 1.2 * 0.5 * Math.Pow(((((0.41 * U) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))), 2);

            foreach (Vector3d d in flowDir)
            {
                this.Uinf.Add((d  *(((0.41 * U) / Math.Log((zref + z0) / z0) / 0.41) * Math.Log((buildingHeight + z0) / z0))));
            }

        }

    }


}
