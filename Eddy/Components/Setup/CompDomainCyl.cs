using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Microsoft.VisualBasic.Devices;
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
              "Eddy", "Setup")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            pManager.AddBrepParameter("Geometry", "Geo", "Building Geometry.", GH_ParamAccess.list);
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

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Dom", "Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.item);
            pManager.AddGenericParameter("Div", "Div", "Div", GH_ParamAccess.list);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {



            //public Box DomainBoundaryBox;
            List<GeometryBase> geometry = new List<GeometryBase>();
            DA.GetDataList("Geometry", geometry);


            List<GeometryBase> domain = new List<GeometryBase>();
            foreach (var g in geometry)
            {
                if (g != null)
                {
                    domain.Add(g);
                }
            }

            List<GeometryBase> terrain = new List<GeometryBase>();
            DA.GetDataList("Terrain", terrain);

            BoundaryConditions BCond = null;
            DA.GetData("BCond", ref BCond);
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData("BCond", ref gobj)) { }
            if ((gobj.Value is BoundaryConditions))
            {
                BCond = ((BoundaryConditions)gobj.Value);
            }
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid boundary condition object"); return; }



            // int CPUs = 1;
            double coreBlockSize = 20;
            //int gradingPerim = 1;
            //int divsConcentric = 1;
            double sizeInnerRect = 0;
            double sizeOuterCirc = 0;
            double sizeHeight = 0;

            DA.GetData("Block size", ref coreBlockSize);
            //DA.GetData(5, ref gradingPerim);
            //DA.GetData(6, ref divsConcentric);
            DA.GetData("Size of inner rectangle", ref sizeInnerRect);
            DA.GetData("Size of outer radius", ref sizeOuterCirc);
            DA.GetData("Height", ref sizeHeight);




            Mesh combinedMeshes = new Mesh();
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
                        var m = Mesh.CreateFromBrep(obj, mp);
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
                    combinedMeshes.Append(obj);
                }
                else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                {
                    Brep obj = (Brep)b;
                    var m = Mesh.CreateFromBrep(obj, mp);
                    foreach (Mesh mm in m)
                    {
                        combinedMeshes.Append(mm);
                    }
                }


            }



            // Those Breps are currently necessary to perform the point inclusion check for the probing components

            Brep inputBreps = new Brep();

            foreach (GeometryBase g in domain)
            {

                inputBreps.Append(Brep.TryConvertBrep(g));
            }






            if (Utilities.CheckLicence() == true)
            {


                OFCylDomain DOMCYL = new OFCylDomain(inputBreps, combinedMeshes, terrainMeshes, BCond, coreBlockSize, sizeInnerRect, sizeOuterCirc, sizeHeight);
                               
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
                Properties.Resources.Eddy_domCyl;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{DDB7971A-EBAD-4A6F-8BFB-E77FE24F73BD}");
    }

}
