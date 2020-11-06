using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Eddy.Components.Indoor.Params;
using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Eddy.Components.Indoor
{
    public class IndoorDomain_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the IndoorDomain class.
        /// </summary>
        public IndoorDomain_Component() : base("IndoorDomain", "IDom", "IndoorDomain" + EddyVersion.toString(), EddyVersion.Name, "9 | Indoor")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //0
            pManager.AddParameter(new Param_IndoorBC_Wall(), "Geo", "Geo", "Indoor CFD Objects", GH_ParamAccess.list);
            //1
            pManager.AddParameter(new Param_IndoorBC_Inlet(), "Inlet", "In", "Indoor CFD Objects", GH_ParamAccess.list);
            //2
            pManager.AddParameter(new Param_IndoorBC_Outlet(), "Outlet", "Out", "Indoor CFD Objects", GH_ParamAccess.list);
            //3
            pManager.AddParameter(new Param_VolumetricHeatSource(), "Volumetric Heat Source", "VHS", "Indoor CFD Objects", GH_ParamAccess.list);

            //4
            pManager.AddTextParameter("Directory", "Dir", "Working Directory", GH_ParamAccess.item, @"C:\Temp\EddyProject");
            //5
            pManager.AddPointParameter("Point Inside", "PInside", "Point inside domain.", GH_ParamAccess.item);
            //6
            pManager.AddNumberParameter("CellSize", "Cs", "Cell Size", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new Param_IndoorDomain(), "Domain", "Dom", "Indoor CFD Domain", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var WallGoos = new List<IndoorWallGoo>();
            var InletGoos = new List<IndoorInletGoo>();
            var OutletGoos = new List<IndoorOutletGoo>();
            var VolumetricHeatSourceGoos = new List<VolumetricHeatSourceGoo>();

            var Walls = new List<IndoorBC.Wall>();
            var Inlets = new List<IndoorBC.Inlet>();
            var Outlets = new List<IndoorBC.Outlet>();
            var VolumetricHeatSources = new List<VolumetricHeatSource>();

            DA.GetDataList(0, WallGoos);
            DA.GetDataList(1, InletGoos);
            DA.GetDataList(2, OutletGoos);
            DA.GetDataList(3, VolumetricHeatSourceGoos);

            foreach (var o in WallGoos)
            {
                Walls.Add(o.Value);
            }
            foreach (var o in InletGoos)
            {
                Inlets.Add(o.Value);
            }
            foreach (var o in OutletGoos)
            {
                Outlets.Add(o.Value);
            }
            foreach (var o in VolumetricHeatSourceGoos)
            {
                VolumetricHeatSources.Add(o.Value);
            }

            string dir = "";
            DA.GetData(4, ref dir);
            Point3d pointInsideDomain = new Point3d();
            DA.GetData(5, ref pointInsideDomain);
            double cellSize = 1;
            DA.GetData(6, ref cellSize);

            var dom = new IndoorDomain(dir, cellSize, pointInsideDomain, Walls, Inlets, Outlets, VolumetricHeatSources);
            var domGoo = new IndoorDomaingGoo(dom);
            DA.SetData(0, domGoo);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return Resources.Eddy_Indoor_Domain;

                // return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f275e34c-c8ba-4b2b-8a2f-917b01ed028f"); }
        }
    }
}