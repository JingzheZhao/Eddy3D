using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using System.Linq;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;
using System.Text.RegularExpressions;
using Grasshopper;
using EddyLib;
using Eddy.Properties;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompLoadUTCI : GH_Component
    {


        DataTree<double> cpTree = new DataTree<double>();
        DataTree<Vector3d> uTree = new DataTree<Vector3d>();




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompLoadUTCI()
          : base("LoadUTCIProbewise", "LoadUTCIProbewise", "Misc", "Eddy", "UTCI")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item, @"C:\temp");


            pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 0);
            Param_Integer param = pManager[1] as Param_Integer;
            param.AddNamedValue("UTCI", 0);
            //  param.AddNamedValue("U_Probes", 1);

            //pManager.AddBooleanParameter("Run", "Run", "Clean the directory", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item, false);



        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Out", "Result", GH_ParamAccess.tree);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string workDir = "";
            DA.GetData(0, ref workDir);


            bool Run = false;

            if (Run)
            {
                if (!Directory.Exists(workDir)) return;


                string file = workDir + @"\UTCI.csv";

                if (!File.Exists(file)) return;


                var data = RadianceFiles.readCSVFile(file);

                var dataTree = new DataTree<double>();

                for (int i = 0; i < data.GetUpperBound(0); i++)
                {

                    for (int j = 0; j < data.GetUpperBound(1); j++)
                    {
                        dataTree.Add(data[i, j], new Grasshopper.Kernel.Data.GH_Path(i));
                    }
                }

                DA.SetDataTree(0, dataTree);
            }

           
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
                return Resources.Eddy_parseU;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{9C41A4DC-A82E-4D71-A997-7EA70D040FDC}"); }
        }
    }
}



