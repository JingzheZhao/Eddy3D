using EddyLib;
using EddyLib.BCs;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class BlockMeshBox : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public BlockMeshBox()
          : base("Box-shaped Domain", "DomainBox", "Box-shaped Domain" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Setup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Geo", "Building Geometry.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Terrain", "Terrain", "Terrain Geometry. Make sure the terrain geometry is bigger than the ground plane of the wind tunnel.", GH_ParamAccess.list);

            pManager.AddGenericParameter("Boundary Condition", "BCond", "Boundary Condition", GH_ParamAccess.item);

            pManager[1].Optional = true;
            pManager[2].Optional = true;

            pManager.AddNumberParameter("Block size", "BS", "Block size", GH_ParamAccess.item, 20);

            pManager.AddNumberParameter("Length", "L", "Length of wind tunnel", GH_ParamAccess.item);
            pManager.AddNumberParameter("Width", "W", "Width of wind tunnel", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "H", "Height of wind tunnel", GH_ParamAccess.item);

            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Dom", "Simulation Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("Mesh", "Msh", "Mesh", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //DOMAIN GEOMETRY
            List<IGH_GeometricGoo> geoGooDomain = new List<IGH_GeometricGoo>();
            DA.GetDataList("Geometry", geoGooDomain);
            List<GeometryBase> buildings = new List<GeometryBase>();

            foreach (IGH_GeometricGoo g in geoGooDomain)
            {
                if (g != null)
                {
                    if (g.CastTo<GeometryBase>(out GeometryBase gb))
                    {
                        buildings.Add(gb);
                    }
                }
            }

            //TERRAIN GEOMETRY
            List<IGH_GeometricGoo> terrainGoo = new List<IGH_GeometricGoo>();
            List<GeometryBase> terrain = new List<GeometryBase>();
            DA.GetDataList("Terrain", terrainGoo);

            foreach (IGH_GeometricGoo g in terrainGoo)
            {
                if (g != null)
                {
                    if (g.CastTo<GeometryBase>(out GeometryBase gb))
                    {
                        terrain.Add(gb);
                    }
                }
            }

            if (Utilities.CheckForDuplicates(buildings))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Duplicate Geometries might lead to a crashing simulation. Please find duplicates with ""SelDup"" and remove them.");
            }

            if (Utilities.CheckForDuplicates(terrain))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Duplicate Geometries might lead to a crashing simulation. Please find duplicates with ""SelDup"" and remove them.");
            }

            BoundaryCondition bCond;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData("Boundary Condition", ref gobj)) { }

            if ((gobj.Value is ABL))
            {
                bCond = (ABL)gobj.Value;
            }
            else if ((gobj.Value is ConstU))
            {
                bCond = (ConstU)gobj.Value;
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid Boundary Condition object"); return;
            }

            double blockDimension = 20;
            DA.GetData("Block size", ref blockDimension);

            Mesh buildingGeometry = new Mesh();
            MeshingParameters mp = new MeshingParameters();

            //string windowsVersion = Utilities.GetOSInfo();
            //bool isWindows7 = Utilities.IsWindows7;
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            double width = 0;
            double length = 0;
            double height = 0;

            DA.GetData("Width", ref width);
            DA.GetData("Length", ref length);
            DA.GetData("Height", ref height);

            Mesh terrainMeshes = new Mesh();

            if (terrain.Count == 0)
            {
                // AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "If you don't provide a terrain,
                // Eddy will use a standard ground plane."); return;
            }
            else // (terrain.Count > 0)
            {
                foreach (GeometryBase b in terrain)
                {
                    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        Mesh obj = (Mesh)b;
                        terrainMeshes.Append(obj);
                    }
                    else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                    {
                        Brep obj = (Brep)b;
                        Mesh[] m = Mesh.CreateFromBrep(obj, mp);
                        foreach (Mesh mm in m)
                        {
                            terrainMeshes.Append(mm);
                        }
                    }
                }
            }

            if (buildings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please reference an input geometry."); return;
            }
            else
            {
                foreach (GeometryBase b in buildings)
                {
                    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        Mesh obj = (Mesh)b;
                        buildingGeometry.Append(obj);
                    }
                    else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                    {
                        Brep obj = (Brep)b;
                        Mesh[] m = Mesh.CreateFromBrep(obj, mp);
                        foreach (Mesh mm in m)
                        {
                            buildingGeometry.Append(mm);
                        }
                    }
                }
            }

            // For radiation simulation

            buildingGeometry.UserDictionary.Set("type", "Building");
            terrainMeshes.UserDictionary.Set("type", "Ground");

            // Check if lowest point in Domain is z_low < 0, then we cannot use a ABL

            if (buildingGeometry.GetBoundingBox(true).Min.Z < 0 && bCond is ABL)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "If your simulation domain extends below z = 0, you cannot use an ABL Boundary Condition. Please use the Constant U Boundary Condition."); return;
            }

            if (Utilities.CheckLicence() == true)
            {
                OFBoxDomain DOMBOX = new OFBoxDomain(buildingGeometry, terrainMeshes, bCond, blockDimension, length, width, height);

                FillWindDirRenderList(bCond, DOMBOX);

                if (DOMBOX.BCond.windDirs.Count > 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "For box-shaped domains you can only pass one wind direction per simulation setup."); return;
                }

                DA.SetData(0, DOMBOX);

                if (DOMBOX.hasTerrain)
                {
                    DA.SetDataList(1, DOMBOX.DomainMeshIntersection);
                }
                else
                {
                    DA.SetData(1, DOMBOX.DomainMesh);
                }
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Licence expired."); return;
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.Eddy_domainBox;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{0AD4BDF7-33AC-492D-ABF0-622A5488C8E2}");

        private List<Point3d> _pointWindDirRender;

        private List<Vector3d> _vecsWindDirRender;

        private void FillWindDirRenderList(BoundaryCondition bCond, OFBoxDomain DOM)
        {
            //clear
            _pointWindDirRender = new List<Point3d>();
            _vecsWindDirRender = new List<Vector3d>();

            var pt = new Point3d(0, 0, 0);
            if (DOM.DomainMesh != null)
            {
                pt = Utilities.CenterBottomBoundingBox(DOM.DomainMesh);
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not draw wind directions onto the canvas.");
            }

            var length = DOM.blockDimension * DOM.CellsAlongLength;

            //Fill render lists for arrow preview
            _vecsWindDirRender.Add(bCond.flowDir[0] * bCond.URef);
            _pointWindDirRender.Add(pt + (-bCond.flowDir[0] * length) + (-bCond.flowDir[0] * bCond.URef));

            // + (-bCond.flowDir[0] * DOM.length * 0.5) + (-bCond.flowDir[0] * bCond.URef )
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);

            if (this.Locked || _pointWindDirRender == null || _pointWindDirRender.Count == 0 || _vecsWindDirRender == null || _vecsWindDirRender.Count == 0)
            {
                return;
            }

            if (this.Attributes.Selected)
            {
                // Draw wind dir arrows
                for (int i = 0; i < _pointWindDirRender.Count; i++)
                {
                    var l = new Line(_pointWindDirRender[i], _vecsWindDirRender[i]);
                    args.Display.DrawArrow(l, args.WireColour_Selected, 25, 0);
                }

                return;
            }
            else
            {
                // Draw wind dir arrows
                for (int i = 0; i < _pointWindDirRender.Count; i++)
                {
                    var l = new Line(_pointWindDirRender[i], _vecsWindDirRender[i]);
                    args.Display.DrawArrow(l, args.WireColour, 25, 0);
                }

                return;
            }
        }
    }
}