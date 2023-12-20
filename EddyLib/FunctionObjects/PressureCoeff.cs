using EddyLib.BCs;
using EddyLib.FunctionObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace EddyLib
{
    public class PressureCoeff : FunctionObject
    {
        public List<double> UatBuildingHeight;
        public List<double> pinf;
        public List<double> pref;
        public List<Vector3d> Uinf = new List<Vector3d>();

        private int NumberOfWindDirections { get; set; }

        public PressureCoeff(double buildingHeight, BCCollection bc)
        {
            this.NumberOfWindDirections = bc.BCs.Count;
            this.Uinf = new List<Vector3d>();
            this.UatBuildingHeight = new List<double>();
            this.pinf = new List<double>();
            this.pref = new List<double>();

            CalculateCPPressures(buildingHeight, bc);
        }

        public void SetUatBuildingHeight(double maxBuildingHeight, BCCollection BCC)
        {
            for (int i = 0; i < NumberOfWindDirections; i++)
            {
                var currBC = BCC.BCs[i];

                if (currBC is ABL)
                {
                    ABL casted_bc = (ABL)currBC;
                    UatBuildingHeight.Add(BC.ScaleABL(casted_bc.URef, casted_bc.zref, casted_bc.z0, maxBuildingHeight));
                }
                else
                {
                    ConstU casted_bc = (ConstU)currBC;
                    UatBuildingHeight.Add(casted_bc.URef);
                }
            }
        }

        public void CalculateCPPressures(double buildingHeight, BCCollection BCC)
        {
            //height < 0 gives Nan
            if (buildingHeight < 0) { buildingHeight = 0; };

            for (int i = 0; i < NumberOfWindDirections; i++)
            {
                var currBC = BCC.BCs[i];

                if (currBC is ABL)
                {
                    ABL casted_bc = (ABL)currBC;

                    Uinf.Add(casted_bc.flowDir * (casted_bc.Kappa * casted_bc.URef / Math.Log((casted_bc.zref + casted_bc.z0) / casted_bc.z0) / casted_bc.Kappa * Math.Log((buildingHeight + casted_bc.z0) / casted_bc.z0)));

                    pinf.Add(1.2 * 0.5 * Math.Pow(casted_bc.Kappa * casted_bc.URef / Math.Log((casted_bc.zref + casted_bc.z0) / casted_bc.z0) / casted_bc.Kappa * Math.Log((buildingHeight + casted_bc.z0) / casted_bc.z0), 2));
                    pref.Add(1.2 * 0.5 * Math.Pow(casted_bc.Kappa * casted_bc.URef / Math.Log((casted_bc.zref + casted_bc.z0) / casted_bc.z0) / casted_bc.Kappa * Math.Log((buildingHeight + casted_bc.z0) / casted_bc.z0), 2));
                }
                else
                {
                    ConstU casted_bc = (ConstU)currBC;

                    Uinf.Add((casted_bc.flowDir * casted_bc.URef));

                    pinf.Add(1.2 * 0.5 * Math.Pow(casted_bc.URef, 2));
                    pref.Add(1.2 * 0.5 * Math.Pow(casted_bc.URef, 2));
                }
            }
        }
    }
}