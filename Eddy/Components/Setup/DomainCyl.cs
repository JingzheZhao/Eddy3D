using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Microsoft.VisualBasic.Devices;
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

            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Eddy"));
            pManager.AddBrepParameter("Geometry", "Geo", "Building Geometry.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Terrain", "Terrain", "Terrain Geometry. Make sure the terrain geometry is bigger than the ground plane of the wind tunnel.", GH_ParamAccess.list);



            pManager.AddGenericParameter("BCond", "BCond", "BCond", GH_ParamAccess.item);


            pManager.AddIntegerParameter("Radial divisions", "RadDiv", "Radial divisions", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Concentric grading", "ConcGrad", "Concentric grading", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Concentric divisions", "ConcDiv", "Concentric Divisions", GH_ParamAccess.item, 1);

            pManager.AddNumberParameter("Size of inner rectangle", "InnerR", "Size of inner rectangle", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Size of outer radius", "OuterR", "Size of outer radius", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Height", "Height", "Height", GH_ParamAccess.item, 0);


         //   pManager.AddIntegerParameter("CPUs", "CPUs", "Number of CPUs. Set to -1 to set the number of CPUs for the simulation automatically.", GH_ParamAccess.item, 1);
           

            pManager[2].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.item);
            pManager.AddGenericParameter("Domain", "Dom", "Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("Cylinder", "Cyl", "Cylinder", GH_ParamAccess.item);
            pManager.AddGenericParameter("Div", "Div", "Div", GH_ParamAccess.list);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string baseWorkingDirectory = "";

            DA.GetData(0, ref baseWorkingDirectory);


            if (!Directory.Exists(baseWorkingDirectory)) { Directory.CreateDirectory(baseWorkingDirectory); }


            //public Box DomainBoundaryBox;
            List<GeometryBase> _domain = new List<GeometryBase>();
            DA.GetDataList(1, _domain);


            List<GeometryBase> domain = new List<GeometryBase>();
            foreach (var g in _domain)
            {
                if (g != null)
                {
                    domain.Add(g);
                }
            }

            List<GeometryBase> terrain = new List<GeometryBase>();
            DA.GetDataList(2, terrain);
            
            BoundaryConditions BCond = null;
            DA.GetData(3, ref BCond);
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(3, ref gobj)) { }
            if ((gobj.Value is BoundaryConditions))
            {
                BCond = ((BoundaryConditions)gobj.Value);
            }
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid boundary condition object"); return; }
                                  


           // int CPUs = 1;
            int divsRadial = 1;
            int gradingPerim = 1;
            int divsConcentric = 1;
            double sizeInnerRect = 0;
            double sizeOuterCirc = 0;
            double sizeHeight = 0;



            DA.GetData(4, ref divsRadial);
            DA.GetData(5, ref gradingPerim);
            DA.GetData(6, ref divsConcentric);
            DA.GetData(7, ref sizeInnerRect);
            DA.GetData(8, ref sizeOuterCirc);
            DA.GetData(9, ref sizeHeight);




            Mesh combinedMeshes = new Mesh();
            MeshingParameters mp = new MeshingParameters();

            //Error handling

            // //c//c//temp/abc/mesh/

            ////string windowsVersion = Utilities.GetOSInfo();
            //bool isWindows7 = Utilities.IsWindows7;
            //string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);



            
            //if (isWindows7)
            //{
            //    if (!baseWorkingDirectory.StartsWith(userFolder, StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "For Windows 7 and 8, the working directory must be in the user folder because of constraint with a deprecated Docker version.."); return;
            //    }
            //}






            var totalGBRam = Convert.ToInt32((new ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 2))) + 0.5);

            
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



            //Fix paths

            baseWorkingDirectory = Utilities.FixDirectories(baseWorkingDirectory);
            //string OFbaseWorkingDirectory = Utilities.ReformatWorkingDir(baseWorkingDirectory);





            if (Utilities.CheckLicence() == true)
            {


                OFCylDomain DOMCYL = new OFCylDomain(inputBreps, combinedMeshes, terrainMeshes, BCond, divsRadial, gradingPerim, divsConcentric, 1, sizeInnerRect, sizeOuterCirc, sizeHeight, baseWorkingDirectory);
             

               




                DA.SetData(1, DOMCYL);
                DA.SetData(2, DOMCYL.DomainMesh);
                DA.SetDataList(3, DOMCYL.concentricDivisions);


                string logFile = "";

                using (FileStream stream = File.Open(baseWorkingDirectory + @"\mesh\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        logFile = reader.ReadToEnd();

                    }
                }

                DA.SetData(0, logFile);


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
