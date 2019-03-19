using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class BlockMesh : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>


        public BlockMesh()
          : base("DomainCyl", "DomainCyl",
              "DomainCyl",
              "Eddy", "1 | Setup")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddGeometryParameter("Geometry", "Geo", "Building Geometry.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Terrain", "Terrain", "Terrain Geometry. Make sure the terrain geometry is bigger than the ground plane of the wind tunnel.", GH_ParamAccess.list);

            pManager.AddGenericParameter("BCond", "BCond", "BCond", GH_ParamAccess.item);

            pManager.AddNumberParameter("Block size", "BS", "Block size", GH_ParamAccess.item, 20);
            //pManager.AddIntegerParameter("Concentric grading", "ConcGrad", "Concentric grading", GH_ParamAccess.item, 1);
            //pManager.AddIntegerParameter("Concentric divisions", "ConcDiv", "Concentric Divisions", GH_ParamAccess.item, 1);

            pManager.AddNumberParameter("Size of inner rectangle", "InnerR", "Size of inner rectangle", GH_ParamAccess.item);
            pManager.AddNumberParameter("Size of outer radius", "OuterR", "Size of outer radius", GH_ParamAccess.item);
            pManager.AddNumberParameter("Height", "Height", "Height", GH_ParamAccess.item);
            //   pManager.AddIntegerParameter("CPUs", "CPUs", "Number of CPUs. Set to -1 to set the number of CPUs for the simulation automatically.", GH_ParamAccess.item, 1);


            pManager[1].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
            pManager[6].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Dom", "Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("Mesh", "Msh", "Mesh", GH_ParamAccess.item);
            pManager.AddGenericParameter("Div", "Div", "Div", GH_ParamAccess.list);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            //DOMAIN GEOMETRY
            List<IGH_GeometricGoo> geoGooDomain = new List<IGH_GeometricGoo>();
            DA.GetDataList("Geometry", geoGooDomain);
            List<GeometryBase> domain = new List<GeometryBase>();


            foreach (IGH_GeometricGoo g in geoGooDomain)
            {
                if (g != null)
                {
                    if (g.CastTo<GeometryBase>(out GeometryBase gb))
                    {
                        domain.Add(gb);
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





            BoundaryConditions bCond = null;
            GH_ObjectWrapper gobj = null;
            if (DA.GetData("BCond", ref gobj))
            {
                if ((gobj.Value is BoundaryConditions))
                {
                    bCond = ((BoundaryConditions)gobj.Value);
                }
                else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid boundary condition object"); return; }
            }


            // int CPUs = 1;
            double coreBlockSize = 20;
            //int gradingPerim = 1;
            //int divsConcentric = 1;
            double sizeInnerRect = 1;
            double sizeOuterCirc = 1;
            double sizeHeight = 1;

            DA.GetData("Block size", ref coreBlockSize);
            DA.GetData("Size of inner rectangle", ref sizeInnerRect);
            DA.GetData("Size of outer radius", ref sizeOuterCirc);
            DA.GetData("Height", ref sizeHeight);


            // Check Domain dimensions

            if (sizeInnerRect < coreBlockSize)
            { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Size of inner rectangle must be larger than the Block Size."); return; }



            Mesh buildingGeometry = new Mesh();
            MeshingParameters mp = new MeshingParameters();

            Mesh terrainMeshes = new Mesh();

            if (terrain.Count == 0)
            {
                // AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "If you don't provide a terrain, Eddy will use a standard ground plane."); return;

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



            foreach (GeometryBase b in domain)
            {

                if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                {
                    Mesh obj = new Mesh();
                    obj = (Mesh)b;
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



            if (Utilities.CheckLicence() == true)
            {


                OFCylDomain DOMCYL = new OFCylDomain(buildingGeometry, terrainMeshes, bCond, coreBlockSize, sizeInnerRect, sizeOuterCirc, sizeHeight);

                DA.SetData(0, DOMCYL);
                DA.SetData(1, DOMCYL.DomainMesh);
                DA.SetDataList(2, DOMCYL.concentricDivisions);



            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Licence expired.");
            }


        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.Eddy_analysisCul;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{DDB7971A-EBAD-4A6F-8BFB-E77FE24F73BD}");
    }

}
