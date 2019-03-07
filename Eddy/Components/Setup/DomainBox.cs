using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class BlockMeshBox : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public BlockMeshBox()
          : base("DomainBox", "DomainBox",
              "DomainBox",
              "Eddy", "Setup")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {


            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Eddy"));

            pManager.AddBrepParameter("Geometry", "Geo", "Building Geometry.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Terrain", "Terrain", "Terrain Geometry. Make sure the terrain geometry is bigger than the ground plane of the wind tunnel.", GH_ParamAccess.list);


            pManager.AddGenericParameter("BCond", "BCond", "BCond", GH_ParamAccess.item);

            pManager.AddNumberParameter("BaseMesh", "BaseMesh", "BaseMesh", GH_ParamAccess.item, 5);

            //pManager.AddGenericParameter("RAM", "RAM", "RAM", GH_ParamAccess.item);
            pManager.AddIntegerParameter("CPUs", "CPUs", "Number of CPUs. Set to -1 to set the number of CPUs for the simulation automatically.", GH_ParamAccess.item, 1);

            //pManager.AddBooleanParameter("Clean", "Clean", "Clean", GH_ParamAccess.item, false);


            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
          
            pManager.AddGenericParameter("Domain", "Dom", "Simulation Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("Box", "Box", "Domain", GH_ParamAccess.item);
        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string baseWorkingDirectory = "";
            DA.GetData("Directory", ref baseWorkingDirectory);
            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }


            //public Box DomainBoundaryBox;
            List<GeometryBase> geometries = new List<GeometryBase>();


            List<GeometryBase> terrain = new List<GeometryBase>();





            DA.GetDataList(1, geometries);
            DA.GetDataList(2, terrain);

            double blockDimension = 0;
            //    double RAM = 0;
            int CPUs = 1;

            BoundaryConditions BCond;
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(3, ref gobj)) { }

            if ((gobj.Value is BoundaryConditions))
            {
                BCond = (BoundaryConditions)gobj.Value;
            }
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid boundary condition object"); return; }





            DA.GetData(4, ref blockDimension);
            //DA.GetData(4, ref RAM);
            DA.GetData(5, ref CPUs);
            //DA.GetData(6, ref Run);



            Mesh combinedMeshes = new Mesh();
            MeshingParameters mp = new MeshingParameters();

            //string windowsVersion = Utilities.GetOSInfo();
            bool isWindows7 = Utilities.IsWindows7;
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            
         
            



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



            if (geometries == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please reference an input geometry."); return;
            }
            else
            {
                foreach (GeometryBase b in geometries)
                {

                    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        Mesh obj = (Mesh)b;
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
            }

            // Those Breps are currently necessary to perform the point inclusion check for the probing components

            Brep inputBreps = new Brep();

            foreach (GeometryBase g in geometries)
            {

                inputBreps.Append(Brep.TryConvertBrep(g));
            }



            if (Utilities.CheckLicence() == true)
            {





                //Fix paths

                baseWorkingDirectory = Utilities.FixDirectories(baseWorkingDirectory);
                //string OFbaseWorkingDirectory = Utilities.ReformatWorkingDir(baseWorkingDirectory);




                OFBoxDomain DOMBOX = new OFBoxDomain(inputBreps, combinedMeshes, terrainMeshes, BCond, blockDimension, CPUs, baseWorkingDirectory);

                
               





                DA.SetData(0, DOMBOX);
                DA.SetData(1, DOMBOX.newBoxDomain);



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
                Properties.Resources.Eddy_domBox;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{0AD4BDF7-33AC-492D-ABF0-622A5488C8E2}");
    }

}
