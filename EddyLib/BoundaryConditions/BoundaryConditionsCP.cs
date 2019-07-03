using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace EddyLib
{
    public class BoundaryConditionsCP : BoundaryConditions
    {
        public double pinf;
        public double pref;
        public List<Vector3d> Uinf = new List<Vector3d>();

        public BoundaryConditionsCP(double buildingHeight, BoundaryConditions bc) : base(bc.btype, bc.windDirs, bc.URef, bc.z0, bc.epwFilePath)
        {
            CalculateCPPressures(buildingHeight, bc.btype, bc.URef);
        }

        public void CalculateCPPressures(double buildingHeight, BoundaryType btype, double Uref)
        {
            //height < 0 gives Nan
            if (buildingHeight < 0) { buildingHeight = 0; };

            if (btype == BoundaryType.abl)
            {
                foreach (Vector3d d in flowDir)
                {
                    this.Uinf.Add((d * (((this.kappa * URef) / Math.Log((zref + z0) / z0) / this.kappa) * Math.Log((buildingHeight + z0) / z0))));
                }

                this.pinf = 1.2 * 0.5 * Math.Pow(((((this.kappa * URef) / Math.Log((zref + z0) / z0) / this.kappa) * Math.Log((buildingHeight + z0) / z0))), 2);
                this.pref = 1.2 * 0.5 * Math.Pow(((((this.kappa * URef) / Math.Log((zref + z0) / z0) / this.kappa) * Math.Log((buildingHeight + z0) / z0))), 2);
            }
            else
            {
                foreach (Vector3d d in flowDir)
                {
                    this.Uinf.Add(d * Uref);
                }

                this.pinf = 1.2 * 0.5 * Math.Pow(Uref, 2);
                this.pref = 1.2 * 0.5 * Math.Pow(Uref, 2);
            }
        }
    }
}