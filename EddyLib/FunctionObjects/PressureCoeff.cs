using EddyLib.BCs;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EddyLib
{
    public class PressureCoeff
    {
        public double UatBuildingHeight;

        public double pinf;

        public double pref;

        public List<Vector3d> Uinf = new List<Vector3d>();

        public PressureCoeff(double buildingHeight, BoundaryCondition bc)
        {
            CalculateCPPressures(buildingHeight, bc);
        }

        public void SetUatBuildingHeight(double maxBuildingHeight, BoundaryCondition bc)
        {
            if (bc is ABL)
            {
                ABL casted_bc = (ABL)bc;
                UatBuildingHeight = BoundaryCondition.ScaleABL(casted_bc.URef, casted_bc.zref, casted_bc.z0, maxBuildingHeight);
            }
            else
            {
                ConstU casted_bc = (ConstU)bc;
                UatBuildingHeight = casted_bc.URef;
            }
        }

        public void CalculateCPPressures(double buildingHeight, BoundaryCondition bc)
        {
            //height < 0 gives Nan
            if (buildingHeight < 0) { buildingHeight = 0; };

            if (bc is ABL)
            {
                ABL casted_bc = (ABL)bc;

                foreach (Vector3d d in casted_bc.flowDir)
                {
                    this.Uinf.Add(d * (casted_bc.Kappa * casted_bc.URef / Math.Log((casted_bc.zref + casted_bc.z0) / casted_bc.z0) / casted_bc.Kappa * Math.Log((buildingHeight + casted_bc.z0) / casted_bc.z0)));
                }

                this.pinf = 1.2 * 0.5 * Math.Pow(casted_bc.Kappa * casted_bc.URef / Math.Log((casted_bc.zref + casted_bc.z0) / casted_bc.z0) / casted_bc.Kappa * Math.Log((buildingHeight + casted_bc.z0) / casted_bc.z0), 2);
                this.pref = 1.2 * 0.5 * Math.Pow(casted_bc.Kappa * casted_bc.URef / Math.Log((casted_bc.zref + casted_bc.z0) / casted_bc.z0) / casted_bc.Kappa * Math.Log((buildingHeight + casted_bc.z0) / casted_bc.z0), 2);
            }
            else
            {
                ConstU casted_bc = (ConstU)bc;

                foreach (Vector3d d in casted_bc.flowDir)
                {
                    this.Uinf.Add(d * casted_bc.URef);
                }

                this.pinf = 1.2 * 0.5 * Math.Pow(casted_bc.URef, 2);
                this.pref = 1.2 * 0.5 * Math.Pow(casted_bc.URef, 2);
            }
        }
    }
}

//   if (bc is ABL)
//            {
//                ABL casted_bc = (ABL)bc;

//             }
//            else
//            {
//                ConstU casted_bc = (ConstU)bc;

//             }
//}