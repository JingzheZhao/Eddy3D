using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;

namespace Eddy
{
    public class CellSize_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the CellSize_Component class.
        /// </summary>
        public CellSize_Component()
          : base(
                "Mesh Cell Size",
                "CellSz",
                @"Calculate required mesh refinement levels for a target cell size.

Based on the blockMesh base cell size, this calculates how many 
refinement levels (halving) are needed to reach your target resolution.

" + EddyVersion.toString(),
                EddyVersion.Name,
                "1 | Wind")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter(
                "Domain", "Dom",
                "CFD Simulation Domain (Cylindrical or Box).",
                GH_ParamAccess.item);

            pManager.AddNumberParameter(
                "Base Cell Size", "Base",
                "Base mesh cell size from Domain component. Units: meters.",
                GH_ParamAccess.item);

            pManager.AddNumberParameter(
                "Target Cell Size", "Target",
                "Desired final cell size at highest refinement level. Units: meters.",
                GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter(
                "Refinement Level", "Lvl",
                "Refinement level (n) required to reach target cell size.",
                GH_ParamAccess.item);

            pManager.AddIntegerParameter(
                "Refinement Level + 1", "Lvl+1",
                "One level higher than required (finer resolution).",
                GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData("Domain", ref gobj) || gobj?.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No domain provided.");
                return;
            }

            if (!(gobj.Value is OFCylDomain || gobj.Value is OFBoxDomain))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid domain object.");
                return;
            }

            double baseSize = 0;
            if (!DA.GetData("Base Cell Size", ref baseSize)) return;

            double targetSize = 0;
            if (!DA.GetData("Target Cell Size", ref targetSize)) return;

            if (baseSize <= 0 || targetSize <= 0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Sizes must be greater than zero.");
                return;
            }

            int level = (int)Math.Max(0, Math.Ceiling(Math.Log(baseSize / targetSize, 2.0)));

            Message = $"Level: {level}";
            DA.SetData("Refinement Level", level);
            DA.SetData("Refinement Level + 1", level + 1);
        }

        protected override System.Drawing.Bitmap Icon => Resources.Eddy_resizeMesh;

        public override Guid ComponentGuid => new Guid("{BFAD64ED-FACE-4D30-8A3A-64877F1609F2}");
    }
}