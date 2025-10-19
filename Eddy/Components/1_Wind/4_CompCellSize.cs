using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;

namespace Eddy
{
    public class CellSize : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.quarternary; } // keep your original exposure
        }

        public CellSize()
          : base(
                "Cell Size",
                "Cell Size",
                @"Calculate the mesh accuracy (levels) needed for a desired cell size.

        Property     | Description
        Dom          | Simulation domain (OFCylDomain or OFBoxDomain).
        BS           | Base cell size in meters (blockMesh).
        DC           | Desired cell size in meters.

" + EddyVersion.toString(),
                EddyVersion.Name,
                "1 | Wind")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Simulation Domain", "Dom", "Simulation domain (OFCylDomain or OFBoxDomain).", GH_ParamAccess.item);
            pManager.AddNumberParameter("BlockMesh Cell Size", "BS", "Base cell size in meters (blockMesh).", GH_ParamAccess.item);
            pManager.AddNumberParameter("Desired Cell Size", "DC", "Desired cell size in meters.", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Accuracy", "Acc", "Level of accuracy (refinement levels) needed.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Accuracy+1", "Acc+1", "One higher refinement level.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- 1) Read and validate inputs
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj) || gobj == null || gobj.Value == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No domain provided.");
                return;
            }

            double blockMeshCellSize = 0.0;
            if (!DA.GetData(1, ref blockMeshCellSize))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to read BlockMesh Cell Size.");
                return;
            }

            double desiredCellSize = 0.0;
            if (!DA.GetData(2, ref desiredCellSize))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to read Desired Cell Size.");
                return;
            }

            if (blockMeshCellSize <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "BlockMesh Cell Size must be > 0.");
                return;
            }

            if (desiredCellSize <= 0.0)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Desired Cell Size must be > 0.");
                return;
            }

            // --- 2) Ensure domain type is valid (and cast if needed)
            // We don't actually use the domain below, but we validate type to match your original intent.
            if (gobj.Value is OFCylDomain _)
            {
                // OK (cylindrical domain)
            }
            else if (gobj.Value is OFBoxDomain _)
            {
                // OK (box domain)
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid domain object (OFCylDomain or OFBoxDomain).");
                return;
            }

            // --- 3) Compute refinement levels
            // Each level halves the cell size: size(level L) = blockMeshCellSize / 2^L
            // Find smallest integer L with size(L) <= desiredCellSize:
            //   blockMeshCellSize / 2^L <= desiredCellSize  =>  2^L >= blockMeshCellSize / desiredCellSize
            //   L >= log2(blockMeshCellSize / desiredCellSize)
            double ratio = blockMeshCellSize / desiredCellSize;
            int acc = (int)Math.Ceiling(Math.Log(ratio, 2.0));
            if (acc < 0) acc = 0; // if desired >= blockMesh size, zero refinement is enough

            int accPlus1 = acc + 1;

            // --- 4) Output
            DA.SetData(0, acc);
            DA.SetData(1, accPlus1);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Resources.Eddy_resizeMesh; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{BFAD64ED-FACE-4D30-8A3A-64877F1609F2}"); }
        }
    }
}