//using System;
//using System.Collections.Generic;
//using System.IO;
//using Grasshopper.Kernel;
//using Rhino.Geometry;
//using System.Text;
//using System.Linq;
//using Grasshopper.Kernel.Parameters;
//using System.Diagnostics;
//using Grasshopper.Kernel.Types;
//using System.Text.RegularExpressions;
//using Grasshopper;
//using EddyLib;
//using Eddy.Properties;
//using Grasshopper.Kernel.Data;

//// In order to load the result of this wizard, you will also need to
//// add the output bin/ folder of this project to the list of loaded
//// folder in Grasshopper.
//// You can use the _GrasshopperDeveloperSettings Rhino command for that.

//namespace Eddy
//{
//    public class CompLoadAnnualExceedance : GH_Component
//    {


//        DataTree<double> cpTree = new DataTree<double>();
//        DataTree<Vector3d> uTree = new DataTree<Vector3d>();




//        /// <summary>
//        /// Each implementation of GH_Component must provide a public 
//        /// constructor without any arguments.
//        /// Category represents the Tab in which the component will appear, 
//        /// Subcategory the panel. If you use non-existing tab or panel names, 
//        /// new tabs/panels will automatically be created.
//        /// </summary>
//        public CompLoadAnnualExceedance()
//          : base("LoadUTCIProbewise", "LoadUTCIProbewise", "Misc", "Eddy", "UTCI")
//        {
//        }



//        /// <summary>
//        /// Registers all the input parameters for this component.
//        /// </summary>
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {

//            pManager.AddNumberParameter("UTCI", "UTCI", "UTCI", GH_ParamAccess.tree);


//        }

//        /// <summary>
//        /// Registers all the output parameters for this component.
//        /// </summary>
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddGenericParameter("TAS", "TAS", "TAS", GH_ParamAccess.tree);
//            pManager.AddGenericParameter("TAH", "TAH", "TAH", GH_ParamAccess.tree);
//            pManager.AddGenericParameter("TAC", "TAC", "TAC", GH_ParamAccess.tree);
//        }



//        /// <summary>
//        /// This is the method that actually does the work.
//        /// </summary>
//        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
//        /// to store data in output parameters.</param>
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {



//            //    //var utci = new DataTree<double>();



//            //    DA.GetDataTree(0, out GH_Structure<GH_Goo<double>> utci);

//            //    foreach (GH_Path path in utci.Paths)
//            //    {
//            //        foreach ()



//            //    }





//            //    for (int i = 0; i < data.GetUpperBound(0); i++)
//            //    {

//            //        for (int j = 0; j < data.GetUpperBound(1); j++)
//            //        {
//            //            dataTree.Add(data[i, j], new Grasshopper.Kernel.Data.GH_Path(i));
//            //        }
//            //    }

//            //    DA.SetDataTree(0, dataTree);
//            //}





//            /// <summary>
//            /// 
//            /// </summary>


//            var geo = new List<Brep>();

//            GH_Structure<GH_Brep> GH_GeoTree;



//            if (!DA.GetDataTree(0, out GH_GeoTree)) { }

//            foreach (GH_Path path in GH_GeoTree.Paths)

//            {

//                geo.AddRange(from GH_Brep o in GH_GeoTree.get_Branch(path) where o != null where o.Value != null select o.Value);

//            }



//            //var BC = new List<BoundaryConditionObject>();

//            ////List<Brep> GEO = new List<Brep>();

//            ////if (!DA.GetDataList(0, GEO)) { return; }





//            //var bcond = BoundaryCondition._UNSET_;



//            //if (Type == "ADIABAT") bcond = BoundaryCondition.ADIABAT;

//            //else bcond = BoundaryCondition.GROUND;



//            //  RhinoApp.WriteLine(bcond.ToString());



//            foreach (Brep b in geo) BC.Add(new BoundaryConditionObject(b, bcond));



//            DA.SetDataList(0, BC);




//        }

//        /// <summary>
//        /// Provides an Icon for every component that will be visible in the User Interface.
//        /// Icons need to be 24x24 pixels.
//        /// </summary>
//        protected override System.Drawing.Bitmap Icon
//        {
//            get
//            {
//                // You can add image files to your project resources and access them like this:
//                return Resources.Eddy_parseU;
//            }
//        }

//        /// <summary>
//        /// Each component must have a unique Guid to identify it. 
//        /// It is vital this Guid doesn't change otherwise old ghx files 
//        /// that use the old ID will partially fail during loading.
//        /// </summary>
//        public override Guid ComponentGuid
//        {
//            get { return new Guid("{73A6D65B-E61F-4475-82CD-F8B7D4B8C892}"); }
//        }
//    }
//}



