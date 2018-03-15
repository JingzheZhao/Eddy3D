using System;
using Grasshopper.Kernel;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ABL : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ABL()
          : base("ABL", "ABL",  "Atmospheric Boundary Layer", "Eddy", "BC")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("wDir", "wDir", "wDir", GH_ParamAccess.item);
            pManager.AddNumberParameter("Uref", "Uref", "Uref", GH_ParamAccess.item);
            pManager.AddNumberParameter("zref", "zref", "zref", GH_ParamAccess.item);
            pManager.AddNumberParameter("z0", "z0", "z0", GH_ParamAccess.item);
            pManager.AddNumberParameter("zGround", "zGround", "zGround", GH_ParamAccess.item);
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
            double windDir = 0;
            double Uref = 0;
            double zref = 0;
            double z0 = 0;
            double zGround = 0;


           

            DA.GetData(0, ref windDir);
            DA.GetData(1, ref Uref);            
            DA.GetData(2, ref zref);
            DA.GetData(3, ref z0);
            DA.GetData(4, ref zGround);


            BoundaryConditions BCInflow = new BoundaryConditions();

            BCInflow.btype = BoundaryType.abl;            
            BCInflow.U = Uref;
            BCInflow.zref = zref;
            BCInflow.z0 = z0;
            BCInflow.zGround = zGround;
            BCInflow.flowDir = new Vector3d(Math.Cos(windDir),Math.Sin(windDir),0);
            //BCInflow.flowDir = DOM

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
