using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using EddyLib;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ABLComp : GH_Component
    {      List<double> dirs = new List<double>();
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ABLComp()
          : base("ABL", "ABL",  "Atmospheric Boundary Layer", "Eddy", "BC")
        {
            //dirs.Add(0);
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        /// 
  
       
         
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("wDir", "wDir", "wDir", GH_ParamAccess.list, dirs);
            pManager.AddNumberParameter("Uref", "Uref", "Uref", GH_ParamAccess.item, 5);
            pManager.AddNumberParameter("zref", "zref", "zref", GH_ParamAccess.item,10);
            pManager.AddNumberParameter("z0", "z0", "z0", GH_ParamAccess.item,1);
            pManager.AddNumberParameter("zGround", "zGround", "zGround", GH_ParamAccess.item,0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Bcond", "Bcond", "Bcond", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            dirs.Add(0);

            List<double> windDir = new List<double>();
            List<Vector3d> flowDir = new List<Vector3d>();
            double Uref = 0;
            double zref = 0;
            double z0 = 0;
            double zGround = 0;
      
            
            DA.GetDataList(0, windDir);
            DA.GetData(1, ref Uref);            
            DA.GetData(2, ref zref);
            DA.GetData(3, ref z0);
            DA.GetData(4, ref zGround);


            BoundaryConditions BCInflow = new BoundaryConditions( windDir, Uref, zref, z0, zGround);

               DA.SetData(0, BCInflow);    

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{820494F3-2858-4CB3-8E26-E11256EDAE35}"); }
        }
    }
}
